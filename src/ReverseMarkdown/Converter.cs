using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ReverseMarkdown.Converters;
using ReverseMarkdown.Helpers;


namespace ReverseMarkdown {
    /// <summary>
    /// Converts HTML to Markdown. Thread-safe for concurrent use.
    /// </summary>
    public class Converter {
        protected readonly IDictionary<string, IConverter> Converters = new Dictionary<string, IConverter>();
        protected readonly IConverter PassThroughTagsConverter;
        protected readonly IConverter DropTagsConverter;
        protected readonly IConverter ByPassTagsConverter;
        private readonly Dictionary<string, UnknownTagReplacer> _unknownTagReplacerConverters = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AliasTagConverter> _tagAliasConverters = new(StringComparer.OrdinalIgnoreCase);

        private readonly System.Threading.AsyncLocal<ConverterContext?> _context = new();

        public ConverterContext Context => _context.Value ??= new ConverterContext();

        public Converter() : this(new Config())
        {
        }

        public Converter(Config config) : this(config, null)
        {
        }

        public Converter(Config config, params Assembly[]? additionalAssemblies)
        {
            Config = config;

            var assemblies = new List<Assembly>() {
                typeof(IConverter).GetTypeInfo().Assembly
            };

            if (!(additionalAssemblies is null))
                assemblies.AddRange(additionalAssemblies);

            var types = new List<Type>();
            // instantiate all converters excluding the unknown tags converters
            foreach (var assembly in assemblies) {
                foreach (var converterType in assembly.GetTypes()
                             .Where(t => t.GetTypeInfo().GetInterfaces().Contains(typeof(IConverter)) &&
                                         !t.GetTypeInfo().IsAbstract
                                         && t != typeof(PassThrough)
                                         && t != typeof(Drop)
                                         && t != typeof(ByPass))) {
                    // Check to see if any existing types are children/equal to
                    // the type to add.
                    if (types.Any(e => converterType.IsAssignableFrom(e)))
                        // If they are, ignore the type.
                        continue;

                    // See if there is a type that is a parent of the
                    // current type.
                    var toRemove = types.FirstOrDefault(e => e.IsAssignableFrom(converterType));
                    // if there is ...
                    if (!(toRemove is null))
                        // ... remove the parent.
                        types.Remove(toRemove);

                    // finally, add the type.
                    types.Add(converterType);
                }
            }

            // For each type to register ...
            foreach (var converterType in types) {
                var ctor = converterType.GetConstructor(new[] { typeof(Converter) });
                if (ctor is null) {
                    continue;
                }

                // ... activate them
                Activator.CreateInstance(converterType, this);
            }

            // register the unknown tags converters
            PassThroughTagsConverter = new PassThrough(this);
            DropTagsConverter = new Drop(this);
            ByPassTagsConverter = new ByPass(this);
        }

        public Config Config { get; protected set; }

        public virtual string Convert(string html)
        {
            using var _ = EnsureContext();

            html = html.ReplaceLineEndings("\n");

            if (Config.CommonMark && LooksLikeCommonMarkHtmlBlock(html)) {
                return ApplyOutputLineEndings(html);
            }

            if (Config.CommonMark) {
                var trimmed = html.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
                if (trimmed.StartsWith("</", StringComparison.Ordinal) ||
                    html.Contains("<!--", StringComparison.Ordinal) ||
                    html.Contains("<![CDATA[", StringComparison.Ordinal)) {
                    return ApplyOutputLineEndings(html);
                }

                var paragraphTrimmed = html.Trim();
                if (paragraphTrimmed.StartsWith("<p>", StringComparison.OrdinalIgnoreCase) &&
                    paragraphTrimmed.EndsWith("</p>", StringComparison.OrdinalIgnoreCase)) {
                    var inner = paragraphTrimmed.Substring(3, paragraphTrimmed.Length - 7);
                    if (inner.TrimStart().StartsWith("</", StringComparison.Ordinal)) {
                        return ApplyOutputLineEndings(inner);
                    }
                }
            }

            if (Config.CommonMark) {
                html = html.Replace("\u00A0", "&nbsp;");
            }

            html = Cleaner.PreTidy(html, Config.RemoveComments);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var root = doc.DocumentNode;

            // ensure to start from body and ignore head etc
            if (root.Descendants("body").Any()) {
                root = root.SelectSingleNode("//body");
            }

            var result = ConvertNode(root);

            if (!Config.CommonMark) {
                // cleanup multiple new lines
                result = Regex.Replace(result, @"(^\p{Zs}*(\r\n|\n)){2,}", Environment.NewLine, RegexOptions.Multiline);
            }

            if (Config.SlackFlavored) {
                result = Cleaner.SlackTidy(result);
            }

            if (!Config.CleanupUnnecessarySpaces) {
                return ApplyOutputLineEndings(result);
            }

            if (Config.CommonMark) {
                result = result.TrimEnd();
                result = result.TrimStart('\r', '\n');
                return ApplyOutputLineEndings(result);
            }

            return ApplyOutputLineEndings(result.Trim().FixMultipleNewlines());
        }

        private string ApplyOutputLineEndings(string content)
        {
            var lineEnding = string.IsNullOrEmpty(Config.OutputLineEnding)
                ? Environment.NewLine
                : Config.OutputLineEnding;
            return content.ReplaceLineEndings(lineEnding);
        }

        private static bool LooksLikeCommonMarkHtmlBlock(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) {
                return false;
            }

            var trimmed = html.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
            if (trimmed.StartsWith("<!--", StringComparison.Ordinal) ||
                trimmed.StartsWith("<?", StringComparison.Ordinal) ||
                trimmed.StartsWith("<!", StringComparison.Ordinal)) {
                return true;
            }

            return HtmlBlockStart.IsMatch(trimmed);
        }

        private static readonly Regex HtmlBlockStart = new(
            @"^\s*<\/?(div|table|pre|script|style|iframe|article|section|header|footer|nav|aside|blockquote|h[1-6]|hr|details|summary|figure|figcaption|main|form|center|address|body|html|head|link|meta|title|tbody|thead|tfoot|tr|td|th)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        public virtual void Register(string tagName, IConverter converter)
        {
            Converters[tagName] = converter;
        }

        internal int MesureCapacity(HtmlNode node)
        {
            var startIndex = Math.Max(0, node.InnerStartIndex);
            var endNode = (node.EndNode ?? node.LastChild);
            var endIndex = Math.Max(startIndex, endNode.OuterStartIndex + endNode.OuterLength);
            var length = endIndex - startIndex;
            if (length < 10) length = 100;

            var capacity = (int) (length * 0.8);
            return capacity;
        }

        internal StringWriter CreateWriter(HtmlNode node)
        {
            var capacity = MesureCapacity(node);
            // TODO : use a pooled StringBuilder to further cut down memory allocations
            // important: find a way to select the best instance form pool based on the capacity needed
            var sb = new StringBuilder(capacity);
            var writer = new StringWriter(sb);
            return writer;
        }

        public virtual string ConvertNode(HtmlNode node)
        {
            using var _ = EnsureContext();
            using var writer = CreateWriter(node);
            ConvertNode(writer, node);
            return writer.GetStringBuilder().ToString();
        }

        public virtual void ConvertNode(TextWriter writer, HtmlNode node)
        {
            using var _ = EnsureContext();
            var converter = Lookup(node.Name);
            Context.Enter(node);
            converter.Convert(writer, node);
            Context.Leave(node);
        }

        public virtual IConverter Lookup(string tagName)
        {
            // if a tag is in the pass through list then use the pass through tags converter
            if (Config.PassThroughTags.Contains(tagName)) {
                return PassThroughTagsConverter;
            }

            if (Converters.TryGetValue(tagName, out var converter)) {
                return converter;
            }

            var aliasTargetTag = ResolveAliasTarget(tagName);
            if (aliasTargetTag is not null) {
                return GetAliasConverter(tagName, aliasTargetTag);
            }

            if (Config.UnknownTagsReplacer.TryGetValue(tagName, out var replacement)) {
                return GetUnknownTagReplacer(tagName, replacement);
            }

            return GetDefaultConverter(tagName);
        }

        private IConverter GetUnknownTagReplacer(string tagName, string replacement)
        {
            if (_unknownTagReplacerConverters.TryGetValue(tagName, out var converter) &&
                string.Equals(converter.Replacement, replacement, StringComparison.Ordinal)) {
                return converter;
            }

            var replacer = new UnknownTagReplacer(this, tagName, replacement);
            _unknownTagReplacerConverters[tagName] = replacer;
            return replacer;
        }

        private IConverter GetAliasConverter(string tagName, string targetTag)
        {
            if (_tagAliasConverters.TryGetValue(tagName, out var converter) &&
                string.Equals(converter.TargetTag, targetTag, StringComparison.OrdinalIgnoreCase)) {
                return converter;
            }

            var alias = new AliasTagConverter(this, tagName, targetTag);
            _tagAliasConverters[tagName] = alias;
            return alias;
        }

        private string? ResolveAliasTarget(string tagName)
        {
            if (!Config.TagAliases.TryGetValue(tagName, out var targetTag) || string.IsNullOrWhiteSpace(targetTag)) {
                return null;
            }

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { tagName };

            while (true) {
                if (string.Equals(targetTag, tagName, StringComparison.OrdinalIgnoreCase)) {
                    return null;
                }

                if (!visited.Add(targetTag)) {
                    return null;
                }

                if (!Config.TagAliases.TryGetValue(targetTag, out var nextTag) || string.IsNullOrWhiteSpace(nextTag)) {
                    return targetTag;
                }

                targetTag = nextTag;
            }
        }

        private IConverter GetDefaultConverter(string tagName)
        {
            return Config.UnknownTags switch {
                Config.UnknownTagsOption.PassThrough => PassThroughTagsConverter,
                Config.UnknownTagsOption.Drop => DropTagsConverter,
                Config.UnknownTagsOption.Bypass => ByPassTagsConverter,
                _ => throw new UnknownTagException(tagName)
            };
        }

        private IDisposable EnsureContext()
        {
            if (_context.Value is not null) {
                return NoopDisposable.Instance;
            }

            _context.Value = new ConverterContext();
            return new ContextScope(_context);
        }

        private sealed class ContextScope : IDisposable {
            private readonly System.Threading.AsyncLocal<ConverterContext?> _scope;

            public ContextScope(System.Threading.AsyncLocal<ConverterContext?> scope)
            {
                _scope = scope;
            }

            public void Dispose()
            {
                _scope.Value = null;
            }
        }

        private sealed class NoopDisposable : IDisposable {
            public static readonly NoopDisposable Instance = new();

            private NoopDisposable()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}

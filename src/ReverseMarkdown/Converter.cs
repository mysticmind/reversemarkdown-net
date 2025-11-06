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
    public class Converter {
        protected readonly IDictionary<string, IConverter> Converters = new Dictionary<string, IConverter>();
        protected readonly IConverter PassThroughTagsConverter;
        protected readonly IConverter DropTagsConverter;
        protected readonly IConverter ByPassTagsConverter;

        public ConverterContext Context { get; } = new();

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
            foreach (var converterType in types)
                // ... activate them
                Activator.CreateInstance(converterType, this);

            // register the unknown tags converters
            PassThroughTagsConverter = new PassThrough(this);
            DropTagsConverter = new Drop(this);
            ByPassTagsConverter = new ByPass(this);
        }

        public Config Config { get; protected set; }

        public virtual string Convert(string html)
        {
            html = Cleaner.PreTidy(html, Config.RemoveComments);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var root = doc.DocumentNode;

            // ensure to start from body and ignore head etc
            if (root.Descendants("body").Any()) {
                root = root.SelectSingleNode("//body");
            }

            var result = ConvertNode(root);

            // cleanup multiple new lines
            result = Regex.Replace(result, @"(^\p{Zs}*(\r\n|\n)){2,}", Environment.NewLine, RegexOptions.Multiline);

            if (Config.SlackFlavored) {
                result = Cleaner.SlackTidy(result);
            }

            return Config.CleanupUnnecessarySpaces ? result.Trim().FixMultipleNewlines() : result;
        }

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
            using var writer = CreateWriter(node);
            ConvertNode(writer, node);
            return writer.GetStringBuilder().ToString();
        }

        public virtual void ConvertNode(TextWriter writer, HtmlNode node)
        {
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

            return Converters.TryGetValue(tagName, out var converter) ? converter : GetDefaultConverter(tagName);
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
    }
}

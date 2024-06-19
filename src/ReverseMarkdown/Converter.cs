using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ReverseMarkdown.Converters;

namespace ReverseMarkdown
{
    public class Converter
    {
        protected readonly IDictionary<string, IConverter> Converters = new Dictionary<string, IConverter>();
        protected readonly IConverter PassThroughTagsConverter;
        protected readonly IConverter DropTagsConverter;
        protected readonly IConverter ByPassTagsConverter;

        public Converter() : this(new Config()) {}

        public Converter(Config config) : this(config, null) {}

        public Converter(Config config, params Assembly[] additionalAssemblies)
        {
            Config = config;

            var assemblies = new List<Assembly>()
            {
                typeof(IConverter).GetTypeInfo().Assembly
            };

            if (!(additionalAssemblies is null))
                assemblies.AddRange(additionalAssemblies);

            var types = new List<Type>();
            // instantiate all converters excluding the unknown tags converters
            foreach (var assembly in assemblies)
            {
                foreach (var converterType in assembly.GetTypes()
                    .Where(t => t.GetTypeInfo().GetInterfaces().Contains(typeof(IConverter)) &&
                    !t.GetTypeInfo().IsAbstract
                    && t != typeof(PassThrough)
                    && t != typeof(Drop)
                    && t != typeof(ByPass)))
                {
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
            if (root.Descendants("body").Any())
            {
                root = root.SelectSingleNode("//body");
            }

            var result = Lookup(root.Name).Convert(root);

            // cleanup multiple new lines
            result = Regex.Replace( result, @"(^\p{Zs}*(\r\n|\n)){2,}", Environment.NewLine, RegexOptions.Multiline);

            return Config.CleanupUnnecessarySpaces ? result.Trim().FixMultipleNewlines() : result;
        }

        public virtual void Register(string tagName, IConverter converter)
        {
            Converters[tagName] = converter;
        }

        public virtual IConverter Lookup(string tagName)
        {
            // if a tag is in the pass through list then use the pass through tags converter
            if (Config.PassThroughTags.Contains(tagName))
            {
                return PassThroughTagsConverter;
            }

            return Converters.TryGetValue(tagName, out var converter) ? converter : GetDefaultConverter(tagName);
        }

        private IConverter GetDefaultConverter(string tagName)
        {
            switch (Config.UnknownTags)
            {
                case Config.UnknownTagsOption.PassThrough:
                    return PassThroughTagsConverter;
                case Config.UnknownTagsOption.Drop:
                    return DropTagsConverter;
                case Config.UnknownTagsOption.Bypass:
                    return ByPassTagsConverter;
                default:
                    throw new UnknownTagException(tagName);
            }
        }
    }
}

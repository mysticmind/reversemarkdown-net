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
        protected readonly IDictionary<string, IConverter> _converters = new Dictionary<string, IConverter>();
        protected readonly IConverter _passThroughTagsConverter;
        protected readonly IConverter _dropTagsConverter;
        protected readonly IConverter _byPassTagsConverter;

        public Converter() : this(new Config()) {}

        public Converter(Config config, params Assembly[] additionalAssemblies)
        {
            Config = config;

            List<Assembly> assemblies = new List<Assembly>()
            {
                typeof(IConverter).GetTypeInfo().Assembly
            };

            assemblies.AddRange(additionalAssemblies);

            List<Type> types = new List<Type>();
            // instantiate all converters excluding the unknown tags converters
            foreach (var assembly in assemblies)
            {
                foreach (var ctype in assembly.GetTypes()
                    .Where(t => t.GetTypeInfo().GetInterfaces().Contains(typeof(IConverter)) &&
                    !t.GetTypeInfo().IsAbstract
                    && t != typeof(PassThrough)
                    && t != typeof(Drop)
                    && t != typeof(ByPass)))
                {
                    // Check to see if any existing types are children/equal to
                    // the type to add.
                    if (types.Any(e => ctype.IsAssignableFrom(e)))
                        // If they are, ignore the type.
                        continue;

                    // See if there is a type that is a parent of the
                    // current type.
                    Type toRemove = types.FirstOrDefault(e => e.IsAssignableFrom(ctype));
                    // if there is ...
                    if (!(toRemove is null))
                        // ... remove the parent.
                        types.Remove(toRemove);

                    // finally, add the type.
                    types.Add(ctype);
                }
            }

            // For each type to register ...
            foreach (var ctype in types)
                // ... activate them
                Activator.CreateInstance(ctype, this);

            // register the unknown tags converters
            _passThroughTagsConverter = new PassThrough(this);
            _dropTagsConverter = new Drop(this);
            _byPassTagsConverter = new ByPass(this);
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

            return result.Trim();
        }

        public virtual void Register(string tagName, IConverter converter)
        {
            _converters[tagName] = converter;
        }

        public virtual IConverter Lookup(string tagName)
        {
            // if a tag is in the pass through list then use the pass through tags converter
            if (Config.PassThroughTags.Contains(tagName))
            {
                return _passThroughTagsConverter;
            }

            return _converters.ContainsKey(tagName) ? _converters[tagName] : GetDefaultConverter(tagName);
        }

        private IConverter GetDefaultConverter(string tagName)
        {
            switch (Config.UnknownTags)
            {
                case Config.UnknownTagsOption.PassThrough:
                    return _passThroughTagsConverter;
                case Config.UnknownTagsOption.Drop:
                    return _dropTagsConverter;
                case Config.UnknownTagsOption.Bypass:
                    return _byPassTagsConverter;
                default:
                    throw new UnknownTagException(tagName);
            }
        }
    }
}

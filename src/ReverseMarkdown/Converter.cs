using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HtmlAgilityPack;
using ReverseMarkdown.Converters;

namespace ReverseMarkdown
{
    public class Converter
    {
        private readonly IDictionary<string, IConverter> _converters = new Dictionary<string, IConverter>();
        private readonly IConverter _passThroughTagsConverter;
        private readonly IConverter _dropTagsConverter;
        private readonly IConverter _byPassTagsConverter;

        public Converter() : this(new Config()) {}

        public Converter(Config config)
        {
            Config = config;

            // instantiate all converters excluding the unknown tags converters
            foreach (var ctype in typeof(IConverter).GetTypeInfo().Assembly.GetTypes()
                .Where(t => t.GetTypeInfo().GetInterfaces().Contains(typeof(IConverter)) &&
                !t.GetTypeInfo().IsAbstract
                && t != typeof(PassThrough)
                && t != typeof(Drop)
                && t != typeof(ByPass)))
            {
                Activator.CreateInstance(ctype, this);
            }

            // register the unknown tags converters
            _passThroughTagsConverter = new PassThrough(this);
            _dropTagsConverter = new Drop(this);
            _byPassTagsConverter = new ByPass(this);
        }

        public Config Config { get; }

        public string Convert(string html)
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

            return result;
        }

        public void Register(string tagName, IConverter converter)
        {
            _converters[tagName] = converter;
        }

        public IConverter Lookup(string tagName)
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

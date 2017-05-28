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

        public Converter()
            : this(new Config())
        {
        }

        public Converter(Config config)
        {
            Config = config;

            foreach (var ctype in typeof(ConverterBase).GetTypeInfo().Assembly.GetTypes().Where(ctype => ctype.GetTypeInfo().GetInterfaces().Contains(typeof(IConverter)) && !ctype.GetTypeInfo().IsAbstract))
            {
                Activator.CreateInstance(ctype, this);
            }
        }

        public Config Config { get; }

        public string Convert(string html)
        {
            html = new Cleaner().PreTidy(html);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var root = doc.DocumentNode;
            return Lookup(root.Name).Convert(root);
        }

        public void Register(string tagName, IConverter converter)
        {
            _converters.Add(tagName, converter);
        }

        public void Unregister(string tagName)
        {
            _converters.Remove(tagName);
        }

        public IConverter Lookup(string tagName)
        {
            return _converters.ContainsKey(tagName)
                ? _converters[tagName]
                : GetDefaultConverter(tagName);
        }

        protected IConverter GetDefaultConverter(string tagName)
        {
            switch (Config.UnknownTagsConverter)
            {
                case MagicStrings.PassThrough:
                    return new PassThrough(this);

                case MagicStrings.Drop:
                    return new Drop(this);

                case MagicStrings.Bypass:
                    return new ByPass(this);

                default:
                    throw new Exception($"Unknown tag: {tagName}");
            }
        }
    }
}
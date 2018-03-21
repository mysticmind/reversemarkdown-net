
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
		private IDictionary<string, IConverter> _converters = new Dictionary<string, IConverter>();
        private IConverter _passThroughTagsConverter;
        private IConverter _dropTagsConverter;
        private IConverter _byPassTagsConverter;
        private Config _config;

		public Converter()
			: this(new Config())
		{
		}

        public Converter(Config config)
        {
            this._config = config;

            // instanciate all converters excluding the unknown tags converters
            foreach (Type ctype in typeof(IConverter).GetTypeInfo().Assembly.GetTypes()
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

        public Config Config 
		{
			get { return this._config; }
		}

		public string Convert(string html)
		{
			var cleaner = new Cleaner();

			html = cleaner.PreTidy(html);

			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(html);

			var root = doc.DocumentNode;

			string result = this.Lookup(root.Name).Convert(root);

			return result;
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
			return this._converters.ContainsKey(tagName) ? this._converters[tagName] : GetDefaultConverter(tagName);
		}

		protected IConverter GetDefaultConverter(string tagName)
		{
			switch (this._config.UnknownTags)
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ReverseMarkdown.Converters;

namespace ReverseMarkdown
{
	public class ConverterUtils
	{
		private IDictionary<string, IConverter> _converters = new Dictionary<string, IConverter>();
		private Config _config;

		public ConverterUtils(Config config)
		{
			this._config = config;

			foreach(Type ctype in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IConverter)) && !t.IsAbstract))
			{
				Activator.CreateInstance(ctype, this);
			}
		}

		public Config Config 
		{
			get { return this._config; }
		}
		
		public void Register(string tagName, IConverter converter){
			_converters.Add(tagName, converter);
		}

		public void Unregister(string tagName)
		{
			_converters.Remove(tagName);
		}

		private IConverter GetDefaultConverter(string tagName)
		{
			switch(this._config.UnknownTagsConverter)
			{
				case "pass_through":
					return new PassThrough(this);
				case "drop":
					return new Drop(this);
				case "bypass":
					return new ByPass(this);
				default:
					throw new Exception(string.Format("Unknown tag: {0}", tagName));
			}
		}

		public IConverter Lookup(string tagName)
		{
			return this._converters.ContainsKey(tagName) ? this._converters[tagName] : GetDefaultConverter(tagName);
		}
	}
}

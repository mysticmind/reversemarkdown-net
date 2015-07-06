using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public abstract class ConverterBase : IConverter
    {
		private Converter _converter;

		private Dictionary<string,string> _escapedKeyChars = new Dictionary<string,string>();

		public ConverterBase(Converter converter) 
		{
			this._converter = converter;
 
			/*
			this._escapedKeyChars.Add("\\",@"\\");
			this._escapedKeyChars.Add("`",@"\`");
			this._escapedKeyChars.Add("*",@"\*");
			this._escapedKeyChars.Add("_",@"\_");
			this._escapedKeyChars.Add("{",@"\{");
			this._escapedKeyChars.Add("}",@"\}");
			this._escapedKeyChars.Add("[",@"\[");
			this._escapedKeyChars.Add("]",@"\]");
			this._escapedKeyChars.Add("(",@"\)");
			this._escapedKeyChars.Add("#",@"\#");
			this._escapedKeyChars.Add("+",@"\+");
			this._escapedKeyChars.Add("-",@"\-");
			this._escapedKeyChars.Add(".",@"\.");
			this._escapedKeyChars.Add("!",@"\!");
			 */

			this._escapedKeyChars.Add("*", @"\*");
			this._escapedKeyChars.Add("_", @"\_");

		}

		protected Converter Converter 
		{
			get 
			{
				return this._converter;
			}
		}

		public string TreatChildren(HtmlNode node)
		{
			string result = string.Empty;

			if (node.HasChildNodes)
			{
				foreach(HtmlNode nd in node.ChildNodes)
				{
					result+=this.Treat(nd);
				}
			}

			return result;
		}

		public string Treat(HtmlNode node){
			return this.Converter.Lookup(node.Name).Convert(node); 
		}

		public string EscapeKeyChars(string content)
		{
			foreach(var item in this._escapedKeyChars)
			{
				content = content.Replace(item.Key, item.Value);
			}
			
			return content;
		}

		public string ExtractTitle(HtmlNode node)
		{
			string title = node.GetAttributeValue("title", "");

			return title;
		}

		public abstract string Convert(HtmlNode node); 
    }
}

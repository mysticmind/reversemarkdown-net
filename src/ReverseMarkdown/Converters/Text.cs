using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class Text : ConverterBase
	{
		private Dictionary<string, string> _escapedKeyChars = new Dictionary<string, string>();

		public Text(Converter converter)
			: base(converter)
		{
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

			this.Converter.Register("#text", this);
		}

		public override string Convert(HtmlNode node)
		{
			string content = "";

			if (node.InnerText.Trim() == string.Empty)
				content = TreatEmpty(node);
			else
				content = TreatText(node);

			return content;
		}

		private string TreatEmpty(HtmlNode node)
		{
			string content = "";

			HtmlNode parent = node.ParentNode;
			if (parent.Name == "ol" || parent.Name == "ul")
			{
				content = "";
			}
			else if(node.InnerText == " ")
			{
				content = " ";
			}
			
			return content;
		}

		private string TreatText(HtmlNode node)
		{
			string content = node.InnerText;

			//strip leading spaces and tabs for text within list item 
			HtmlNode parent = node.ParentNode;
			if (parent.Name == "ol" || parent.Name == "ul")
			{
				content = content.Trim();
			}

			content =  this.EscapeKeyChars(content);
			
			content = this.PreserveKeyCharswithinBackTicks(content);

			return content;
		}

		private string EscapeKeyChars(string content)
		{
			foreach(var item in this._escapedKeyChars)
			{
				content = content.Replace(item.Key, item.Value);
			}
			
			return content;
		}
		
		private string PreserveKeyCharswithinBackTicks(string content)
		{
			Regex rx = new Regex("`.*?`");

			content = rx.Replace(content, (Match p) =>
			{
				return p.Value.Replace(@"\*", "*").Replace(@"\_", "_");
			});

			return content;
		}
	}
}

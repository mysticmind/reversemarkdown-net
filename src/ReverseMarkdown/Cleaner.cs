using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseMarkdown
{
	public class Cleaner
	{
		private string CleanTagBorders(string content)
		{
			// content from some htl editors such as CKEditor emits newline and tab between tags, clean that up
			return content.Replace("\n\t", "");
		}

		public string PreTidy(string content)
		{
			content = this.CleanTagBorders(content);

			return content;
		}
	}
}

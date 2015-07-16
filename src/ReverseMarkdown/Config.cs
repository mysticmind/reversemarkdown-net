using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReverseMarkdown
{
	public class Config
	{
		private string _unknownTagsConverter = "pass_through";
		private bool _githubFlavored = false;
		
		public Config() 
		{
		}

		public Config(string unknownTagsConverter="pass_through", bool githubFlavored=false)
		{
			this._unknownTagsConverter = unknownTagsConverter;
			this._githubFlavored = githubFlavored;
		}

		public string UnknownTagsConverter
		{
			get { return this._unknownTagsConverter; }
		}

		public bool GithubFlavored
		{
			get { return this._githubFlavored; }
		}
	}
}

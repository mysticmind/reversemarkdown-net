
namespace ReverseMarkdown
{
	public class Config
	{
		private string _unknownTags = "pass_through";
		private bool _githubFlavored = false;
		
		public Config()
		{
		}

		public Config(string unknownTags="pass_through", bool githubFlavored=false)
		{
			this._unknownTags = unknownTags;
			this._githubFlavored = githubFlavored;
		}

		public string UnknownTags
		{
			get { return this._unknownTags; }
		}

		public bool GithubFlavored
		{
			get { return this._githubFlavored; }
		}
	}
}


namespace ReverseMarkdown
{
	public class Config
	{
		private UnknownTagsOption _unknownTags = UnknownTagsOption.PassThrough;
		private bool _githubFlavored = false;
		
		public Config()
		{
		}

		public Config(UnknownTagsOption unknownTags=UnknownTagsOption.PassThrough, bool githubFlavored=false)
		{
			this._unknownTags = unknownTags;
			this._githubFlavored = githubFlavored;
		}

		public UnknownTagsOption UnknownTags
		{
			get { return this._unknownTags; }
		}

		public bool GithubFlavored
		{
			get { return this._githubFlavored; }
		}

        public enum UnknownTagsOption
        {
            PassThrough,
            Drop,
            Bypass,
            Raise
        }
	}
}

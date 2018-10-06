
namespace ReverseMarkdown
{
	public class Config
	{
		private UnknownTagsOption _unknownTags = UnknownTagsOption.PassThrough;
		private bool _githubFlavored = false;
        private bool _removeComments = false;
		
		public Config()
		{
		}

        public Config(UnknownTagsOption unknownTags = UnknownTagsOption.PassThrough, bool githubFlavored = false, bool removeComments = false)
		{
			this._unknownTags = unknownTags;
			this._githubFlavored = githubFlavored;
            this._removeComments = removeComments;
		}

		public UnknownTagsOption UnknownTags
		{
			get { return this._unknownTags; }
		}

		public bool GithubFlavored
		{
			get { return this._githubFlavored; }
		}

        public bool RemoveComments
        {
            get { return this._removeComments; }
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

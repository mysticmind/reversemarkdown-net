namespace ReverseMarkdown
{
    public class Config
    {
        public Config()
        {
        }

        public Config(UnknownTagsOption unknownTags = UnknownTagsOption.PassThrough, bool githubFlavored = false, bool removeComments = false)
        {
            UnknownTags = unknownTags;
            GithubFlavored = githubFlavored;
            RemoveComments = removeComments;
        }

        public UnknownTagsOption UnknownTags { get; } = UnknownTagsOption.PassThrough;

        public bool GithubFlavored { get; }

        public bool RemoveComments { get; }

        public enum UnknownTagsOption
        {
            PassThrough,
            Drop,
            Bypass,
            Raise
        }
    }
}

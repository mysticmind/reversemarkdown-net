namespace ReverseMarkdown
{
    public class Config
    {
        public Config()
        {
        }

        public Config(string unknownTagsConverter = MagicStrings.PassThrough, bool githubFlavored = false)
        {
            UnknownTagsConverter = unknownTagsConverter;
            GithubFlavored = githubFlavored;
        }

        public string UnknownTagsConverter { get; } = MagicStrings.PassThrough;

        public bool GithubFlavored { get; }
    }
}
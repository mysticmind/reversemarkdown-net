using System.Threading.Tasks;
using ReverseMarkdown;
using VerifyXunit;
using Xunit;

[UsesVerify]
public class Snippets
{
    [Fact]
    public async Task Usage()
    {
        #region Usage

        var converter = new ReverseMarkdown.Converter();

        string html = "This a sample <strong>paragraph</strong> from <a href=\"http://test.com\">my site</a>";

        string result = converter.Convert(html);

        #endregion

        await Verifier.Verify(result);
    }

    [Fact]
    public void UsageWithConfig()
    {
        #region UsageWithConfig

        var config = new ReverseMarkdown.Config
        {
            // Include the unknown tag completely in the result (default as well)
            UnknownTags = Config.UnknownTagsOption.PassThrough,
            // generate GitHub flavoured markdown, supported for BR, PRE and table tags
            GithubFlavored = true,
            // will ignore all comments
            RemoveComments = true,
            // remove markdown output for links where appropriate
            SmartHrefHandling = true
        };

        var converter = new ReverseMarkdown.Converter(config);

        #endregion
    }
}
using System;
using System.Threading.Tasks;
using ReverseMarkdown;
using VerifyXunit;
using Xunit;

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

    [Fact]
    public void Base64ImageInclude()
    {
        #region Base64ImageInclude

        var converter = new ReverseMarkdown.Converter();
        string html = "<img src=\"data:image/png;base64,iVBORw0KGg...\" alt=\"Sample Image\"/>";
        string result = converter.Convert(html);
        // Output: ![Sample Image](data:image/png;base64,iVBORw0KGg...)

        #endregion
    }

    [Fact]
    public void Base64ImageSkip()
    {
        #region Base64ImageSkip

        var config = new ReverseMarkdown.Config
        {
            Base64Images = Config.Base64ImageHandling.Skip
        };
        var converter = new ReverseMarkdown.Converter(config);
        string html = "<img src=\"data:image/png;base64,iVBORw0KGg...\" alt=\"Sample Image\"/>";
        string result = converter.Convert(html);
        // Output: (empty - image is skipped)

        #endregion
    }

    [Fact]
    public void Base64ImageSaveToFile()
    {
        #region Base64ImageSaveToFile

        var config = new ReverseMarkdown.Config
        {
            Base64Images = Config.Base64ImageHandling.SaveToFile,
            Base64ImageSaveDirectory = "/path/to/images"
        };
        var converter = new ReverseMarkdown.Converter(config);
        string html = "<img src=\"data:image/png;base64,iVBORw0KGg...\" alt=\"Sample Image\"/>";
        string result = converter.Convert(html);
        // Output: ![Sample Image](/path/to/images/image_0.png)
        // Image file saved to: /path/to/images/image_0.png

        #endregion
    }

    [Fact]
    public void Base64ImageCustomFilename()
    {
        #region Base64ImageCustomFilename

        var config = new ReverseMarkdown.Config
        {
            Base64Images = Config.Base64ImageHandling.SaveToFile,
            Base64ImageSaveDirectory = "/path/to/images",
            Base64ImageFileNameGenerator = (index, mimeType) => 
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                return $"converted_{timestamp}_{index}";
            }
        };
        var converter = new ReverseMarkdown.Converter(config);
        // Images will be saved as: converted_20260108_143022_0.png, converted_20260108_143022_1.jpg, etc.

        #endregion
    }
}
using ReverseMarkdown;

namespace Samples;

public static class Configuration
{
    public static void Base64()
    {
        #region sample_base64
        // Skip base64 images
        var skip = new Config { Images = { Base64Handling = Config.Base64ImageHandling.Skip } };

        // Save base64 images to disk
        var save = new Config
        {
            Images =
            {
                Base64Handling = Config.Base64ImageHandling.SaveToFile,
                Base64Directory = "/path/to/images",
                Base64FileName = (index, mime) => $"image_{index}",
            },
        };
        #endregion
        _ = (skip, save);
    }

    public static void HtmlFilters()
    {
        #region sample_html_filters
        var config = new Config();
        config.Html.ExcludeSelectors.Add("div.advertisement, aside.related");
        config.Html.ElementFilters.Add(el => el.ClassList.Contains("tracking"));
        #endregion
        _ = config;
    }
}

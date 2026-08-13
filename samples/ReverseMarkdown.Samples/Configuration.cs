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

    public static string CellListsAsText()
    {
        #region sample_cell_list_handling
        // A Markdown table cell cannot hold a real list, so by default the source HTML is kept.
        // InlineText flattens it instead: one item per line, separated by <br>.
        var config = new Config
        {
            Tables = { CellListHandling = Config.TableCellListHandlingOption.InlineText },
        };

        var markdown = new Converter(config).Convert(
            "<table><tr><th>Steps</th></tr><tr><td>" +
            "<ol><li><strong>Submit</strong> the request</li><li>Wait for approval</li></ol>" +
            "</td></tr></table>");
        // | Steps |
        // | --- |
        // | 1. **Submit** the request<br>2. Wait for approval |
        #endregion
        return markdown;
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

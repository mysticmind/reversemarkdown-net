using System.IO;
using HtmlAgilityPack;
using ReverseMarkdown.Helpers;


namespace ReverseMarkdown.Converters {
    public class Img : ConverterBase {
        private int _base64ImageCounter = 0;

        public Img(Converter converter) : base(converter)
        {
            Converter.Register("img", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (Converter.Config.SlackFlavored) {
                throw new SlackUnsupportedTagException(node.Name);
            }

            var alt = node.GetAttributeValue("alt", string.Empty);
            var src = node.GetAttributeValue("src", string.Empty);

            // Check if this is a base64-encoded image
            bool isBase64Image = ImageUtils.IsValidBase64ImageData(src);

            if (isBase64Image)
            {
                // Handle base64 images according to configuration
                switch (Converter.Config.Base64Images)
                {
                    case Config.Base64ImageHandling.Skip:
                        // Skip this image entirely
                        return;

                    case Config.Base64ImageHandling.SaveToFile:
                        // Save to file and update src
                        src = SaveBase64ImageToFile(src);
                        if (string.IsNullOrEmpty(src))
                        {
                            // If saving failed, skip the image
                            return;
                        }
                        break;

                    case Config.Base64ImageHandling.Include:
                    default:
                        // Include as-is (default behavior)
                        break;
                }
            }
            else
            {
                // For non-base64 images, check scheme whitelist
                var scheme = StringUtils.GetScheme(src);
                if (!Converter.Config.IsSchemeWhitelisted(scheme)) {
                    return;
                }
            }

            writer.Write("![");
            writer.Write(StringUtils.EscapeLinkText(alt));
            writer.Write("](");
            writer.Write(src);

            if (ExtractTitle(node) is { Length: > 0 } title) {
                writer.Write(" \"");
                writer.Write(title);
                writer.Write("\"");
            }

            writer.Write(')');
        }

        private string SaveBase64ImageToFile(string base64Src)
        {
            try
            {
                var saveDir = Converter.Config.Base64ImageSaveDirectory;
                if (string.IsNullOrWhiteSpace(saveDir))
                {
                    // If no save directory is configured, skip the image
                    return string.Empty;
                }

                // Ensure directory exists
                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }

                // Generate filename
                string fileName;
                if (Converter.Config.Base64ImageFileNameGenerator != null)
                {
                    // Extract MIME type for the generator
                    var mimeMatch = System.Text.RegularExpressions.Regex.Match(base64Src, @"^data:(?<mime>[\w/\-\.]+);base64,");
                    var mimeType = mimeMatch.Success ? mimeMatch.Groups["mime"].Value : "image/png";
                    fileName = Converter.Config.Base64ImageFileNameGenerator(_base64ImageCounter++, mimeType);
                }
                else
                {
                    fileName = $"image_{_base64ImageCounter++}";
                }

                // Save the image and get the file path
                var filePath = ImageUtils.SaveBase64Image(base64Src, saveDir, fileName);
                return filePath;
            }
            catch
            {
                // If saving fails for any reason, skip the image
                return string.Empty;
            }
        }
    }
}

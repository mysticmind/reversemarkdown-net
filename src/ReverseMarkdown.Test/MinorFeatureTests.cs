using System.IO;
using ReverseMarkdown;
using Xunit;

namespace ReverseMarkdown.Test
{
    public class MinorFeatureTests
    {
        private static string Norm(string s) => s.Replace("\r\n", "\n").Trim();

        [Fact]
        public void Pandoc_line_block()
        {
            var converter = new Converter(new Config());
            var html = "<div class=\"line-block\">First line<br>Second line</div>";
            Assert.Equal("| First line\n| Second line",
                Norm(converter.Render(converter.Parse(html), Config.MarkdownFlavor.Pandoc)));
        }

        [Fact]
        public void Default_line_block_keeps_content()
        {
            var converter = new Converter(new Config());
            var html = "<div class=\"line-block\">First line<br>Second line</div>";
            Assert.Contains("First line", converter.Render(converter.Parse(html)));
            Assert.Contains("Second line", converter.Render(converter.Parse(html)));
        }

        [Fact]
        public void Base64_save_to_file_writes_image_and_references_filename()
        {
            // 1x1 transparent PNG.
            const string b64 =
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
            var dir = Path.Combine(Path.GetTempPath(), "rmd-v6-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var config = new Config
                {
                    Base64Images = Config.Base64ImageHandling.SaveToFile,
                    Base64ImageSaveDirectory = dir,
                    Base64ImageFileNameGenerator = (i, _) => $"pic{i}",
                };
                var converter = new Converter(config);
                var md = Norm(converter.Render(converter.Parse($"<p><img src=\"data:image/png;base64,{b64}\" alt=\"x\"></p>")));

                Assert.Equal("![x](pic0.png)", md);
                Assert.True(File.Exists(Path.Combine(dir, "pic0.png")));
            }
            finally
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
        }

        [Fact]
        public void Base64_save_to_file_without_directory_drops_image()
        {
            var config = new Config { Base64Images = Config.Base64ImageHandling.SaveToFile };
            var converter = new Converter(config);
            var md = Norm(converter.Render(converter.Parse("<p>a<img src=\"data:image/png;base64,iVBOR\" alt=\"x\">b</p>")));
            Assert.Equal("ab", md);
        }
    }
}

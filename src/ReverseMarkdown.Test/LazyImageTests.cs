using ReverseMarkdown;
using Xunit;

namespace ReverseMarkdown.Test
{
    public class LazyImageTests
    {
        // https://github.com/mysticmind/reversemarkdown-net/issues/427

        [Fact]
        public void WhenLazyImageSrcFallbackDisabled_ThenSrcIsUsedAsIs()
        {
            const string html =
                "<img src=\"data:image/png;base64,iVBORw0KGgo=\" data-src=\"https://example.com/image.webp\">";

            Assert.Equal("![](data:image/png;base64,iVBORw0KGgo=)", new Converter().Convert(html));
        }

        [Fact]
        public void WhenLazyImageSrcFallbackEnabled_AndSrcIsDataPlaceholder_ThenUseDataSrc()
        {
            const string html =
                "<img src=\"data:image/png;base64,iVBORw0KGgo=\" data-src=\"https://example.com/image.webp\">";

            var converter = new Converter(new Config { LazyImageSrcFallback = true });

            Assert.Equal("![](https://example.com/image.webp)", converter.Convert(html));
        }

        [Fact]
        public void WhenLazyImageSrcFallbackEnabled_AndSrcIsEmpty_ThenUseDataSrc()
        {
            const string html = "<img src=\"\" data-src=\"https://example.com/image.webp\" alt=\"pic\">";

            var converter = new Converter(new Config { LazyImageSrcFallback = true });

            Assert.Equal("![pic](https://example.com/image.webp)", converter.Convert(html));
        }

        [Fact]
        public void WhenLazyImageSrcFallbackEnabled_AndSrcIsRealUrl_ThenKeepSrc()
        {
            const string html =
                "<img src=\"https://example.com/real.png\" data-src=\"https://example.com/other.webp\">";

            var converter = new Converter(new Config { LazyImageSrcFallback = true });

            Assert.Equal("![](https://example.com/real.png)", converter.Convert(html));
        }

        [Fact]
        public void WhenLazyImageSrcFallbackEnabled_AndOnlyDataSrcsetPresent_ThenUseFirstUrl()
        {
            const string html =
                "<img src=\"data:image/gif;base64,R0lGOD\" data-srcset=\"https://example.com/a.webp 1x, https://example.com/b.webp 2x\">";

            var converter = new Converter(new Config { LazyImageSrcFallback = true });

            Assert.Equal("![](https://example.com/a.webp)", converter.Convert(html));
        }
    }
}

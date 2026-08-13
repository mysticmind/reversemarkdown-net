using System;
using System.Text;
using Xunit;

namespace ReverseMarkdown.Test
{
    // Cleaner.FixUnclosedScriptStyle strips <script>/<style> open tags that are never closed. Left
    // in place the HTML5 parser swallows the rest of the document as their raw-text content.
    public class CleanerTests
    {
        [Fact]
        public void Unclosed_script_open_tag_is_stripped()
        {
            var html = Cleaner.FixUnclosedScriptStyle("<p>before</p><script>alert(1)<p>after</p>");

            Assert.DoesNotContain("<script>", html);
            Assert.Contains("<p>before</p>", html);
            Assert.Contains("<p>after</p>", html);
        }

        [Fact]
        public void Unclosed_style_open_tag_is_stripped()
        {
            var html = Cleaner.FixUnclosedScriptStyle("<style>p{color:red}<p>after</p>");

            Assert.DoesNotContain("<style>", html);
            Assert.Contains("<p>after</p>", html);
        }

        [Fact]
        public void Closed_script_and_style_are_left_alone()
        {
            const string source = "<style>p{color:red}</style><p>x</p><script>alert(1)</script>";

            Assert.Equal(source, Cleaner.FixUnclosedScriptStyle(source));
        }

        [Fact]
        public void A_closed_tag_before_an_unclosed_one_keeps_only_the_closed_pair()
        {
            var html = Cleaner.FixUnclosedScriptStyle("<script>a</script><p>x</p><script>b<p>y</p>");

            // The first has a matching close later in the document; the second does not.
            Assert.Equal("<script>a</script><p>x</p>b<p>y</p>", html);
        }

        [Fact]
        public void Attributes_and_spacing_on_the_open_tag_are_handled()
        {
            var html = Cleaner.FixUnclosedScriptStyle("<p>a</p>< script type=\"text/javascript\" >x<p>b</p>");

            Assert.DoesNotContain("javascript", html);
            Assert.Contains("<p>b</p>", html);
        }

        [Fact]
        public void Content_without_script_or_style_is_returned_unchanged()
        {
            const string source = "<p>nothing to do here</p>";

            Assert.Same(source, Cleaner.FixUnclosedScriptStyle(source));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Empty_input_is_returned_unchanged(string? source)
        {
            Assert.Equal(source, Cleaner.FixUnclosedScriptStyle(source!));
        }

        [Fact]
        public void Cost_scales_linearly_with_the_number_of_script_tags()
        {
            // Regression guard: this used to substring the remainder of the document once per open
            // tag, so allocations grew with the square of the tag count (a 420 KB page with 18
            // <script> tags allocated ~16 MB). Doubling the input should roughly double the work.
            static long Allocated(string html)
            {
                Cleaner.FixUnclosedScriptStyle(html);   // warm
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 10; i++)
                {
                    Cleaner.FixUnclosedScriptStyle(html);
                }

                return (GC.GetAllocatedBytesForCurrentThread() - before) / 10;
            }

            static string Build(int tags)
            {
                var sb = new StringBuilder();
                for (var i = 0; i < tags; i++)
                {
                    sb.Append("<script>var x=").Append(i).Append(";</script>")
                      .Append("<p>Paragraph ").Append(i).Append(" with filler text for body content.</p>");
                }

                return sb.ToString();
            }

            var small = Allocated(Build(200));
            var large = Allocated(Build(400));

            // Linear would be ~2x. Quadratic was ~4x and climbing; 3x leaves generous headroom for
            // measurement noise while still failing if the substring-per-match pattern comes back.
            Assert.True(large <= small * 3,
                $"allocation grew super-linearly: {small:N0} bytes at 200 tags, {large:N0} at 400");
        }
    }
}

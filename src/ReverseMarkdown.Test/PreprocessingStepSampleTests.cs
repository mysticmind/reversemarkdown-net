using System;
using System.Linq;
using System.Reflection;
using ReverseMarkdown.Preprocessing;
using Samples;
using Xunit;

namespace ReverseMarkdown.Test
{
    /// <summary>
    /// Asserts the output every preprocessing sample claims in its trailing comment. The samples are
    /// what the documentation renders (via region snippets), so compiling them proves the API is
    /// current and these assertions prove the documented *results* are too.
    /// </summary>
    public class PreprocessingStepSampleTests
    {
        private static string Norm(string s) => s.Replace("\r\n", "\n").Trim();

        // ---- Removal ----

        [Fact]
        public void Remove_sample() => Assert.Equal("Body text.", Norm(PreprocessingSteps.Remove()));

        [Fact]
        public void RemoveWhere_sample() => Assert.Equal("Real content.", Norm(PreprocessingSteps.RemoveWhere()));

        [Fact]
        public void KeepOnly_sample() => Assert.Equal("# Title\n\nBody.", Norm(PreprocessingSteps.KeepOnly()));

        [Fact]
        public void RemoveComments_sample() => Assert.Equal("Visible text.", Norm(PreprocessingSteps.RemoveComments()));

        [Fact]
        public void RemoveEmptyElements_sample() =>
            Assert.Equal("Kept.\n\nAlso kept.", Norm(PreprocessingSteps.RemoveEmptyElements()));

        // ---- Element and content transformation ----

        [Fact]
        public void Rename_sample() => Assert.Equal("## Title\n\nBody.", Norm(PreprocessingSteps.Rename()));

        [Fact]
        public void Unwrap_sample() => Assert.Equal("a b *c* d", Norm(PreprocessingSteps.Unwrap()));

        [Fact]
        public void Wrap_sample() => Assert.Equal("> Remember this.", Norm(PreprocessingSteps.Wrap()));

        [Fact]
        public void ReplaceWith_sample() => Assert.Equal("Before.\n\n> Note", Norm(PreprocessingSteps.ReplaceWith()));

        [Fact]
        public void ReplaceWithText_sample() =>
            Assert.Equal("Hi :smile: there.", Norm(PreprocessingSteps.ReplaceWithText()));

        [Fact]
        public void Transform_sample() =>
            Assert.Equal("[the docs](https://example.com/docs)", Norm(PreprocessingSteps.Transform()));

        [Fact]
        public void ReplaceText_sample() =>
            Assert.Equal("Hello Jane, welcome back.", Norm(PreprocessingSteps.ReplaceTextOrdinal()));

        [Fact]
        public void ReplaceText_regex_sample() =>
            Assert.Equal("Call [redacted] today.", Norm(PreprocessingSteps.ReplaceTextRegex()));

        // ---- Attributes, styles and scripts ----

        [Fact]
        public void RemoveAttributes_sample() =>
            Assert.Equal("<p class=\"intro\">Text.</p>", Norm(PreprocessingSteps.RemoveAttributes()));

        [Fact]
        public void RemoveInlineStyles_sample() =>
            Assert.Equal("<p id=\"a\">Text.</p>", Norm(PreprocessingSteps.RemoveInlineStyles()));

        [Fact]
        public void RemoveStyleSheets_sample() => Assert.Equal("Body.", Norm(PreprocessingSteps.RemoveStyleSheets()));

        [Fact]
        public void RemoveStyles_sample() => Assert.Equal("<p>Text.</p>", Norm(PreprocessingSteps.RemoveStyles()));

        [Fact]
        public void RemoveClasses_sample() => Assert.Equal("<p>Text.</p>", Norm(PreprocessingSteps.RemoveClasses()));

        [Fact]
        public void RemoveScripts_sample() => Assert.Equal("Body.", Norm(PreprocessingSteps.RemoveScripts()));

        // ---- Inline formatting ----

        [Fact]
        public void RemoveHidden_sample() => Assert.Equal("Visible.", Norm(PreprocessingSteps.RemoveHidden()));

        [Fact]
        public void ConvertInlineStylesToTags_sample() =>
            Assert.Equal("**bold** and *italic*", Norm(PreprocessingSteps.ConvertInlineStylesToTags()));

        [Fact]
        public void InlineStyle_helper_sample() => Assert.Equal("Kept.", Norm(PreprocessingSteps.InlineStyleHelper()));

        // ---- Table cells ----

        [Fact]
        public void SimplifyTableCellHtml_sample() =>
            Assert.Equal(
                "| Policy |\n| --- |\n| <ol><li>First point</li><li>Second point</li></ol> |",
                Norm(PreprocessingSteps.SimplifyTableCellHtml()));

        [Fact]
        public void TableCellScopedCleanup_sample() =>
            Assert.Equal(
                "| Policy |\n| --- |\n| <ol><li><p>First point</p></li></ol> |",
                Norm(PreprocessingSteps.TableCellScopedCleanup()));

        // ---- URLs ----

        [Fact]
        public void ResolveRelativeUrls_sample() =>
            Assert.Equal(
                "[docs](https://example.com/docs) and ![a](https://example.com/guide/img/a.png) and [top](#top)",
                Norm(PreprocessingSteps.ResolveRelativeUrls()));

        [Fact]
        public void CellListHandling_sample() =>
            Assert.Equal(
                "| Steps |\n| --- |\n| 1. **Submit** the request<br>2. Wait for approval |",
                Norm(Configuration.CellListsAsText()));

        [Fact]
        public void CellListRawHtml_sample() =>
            Assert.Equal(
                "| Steps |\n| --- |\n| <ol class=\"customList\"><li>Submit the request</li></ol> |",
                Norm(Configuration.CellListsAsRawHtml()));

        // ---- Coverage guards ----

        // Every sample method asserted above.
        private static readonly string[] AssertedSamples =
        [
            "ConvertInlineStylesToTags", "InlineStyleHelper", "KeepOnly", "Remove", "RemoveAttributes",
            "RemoveClasses", "RemoveComments", "RemoveEmptyElements", "RemoveHidden", "RemoveInlineStyles",
            "RemoveScripts", "RemoveStyleSheets", "RemoveStyles", "RemoveWhere", "Rename",
            "ReplaceTextOrdinal", "ReplaceTextRegex", "ReplaceWith", "ReplaceWithText",
            "ResolveRelativeUrls", "SimplifyTableCellHtml", "TableCellScopedCleanup", "Transform",
            "Unwrap", "Wrap"
        ];

        // Every HtmlPreprocessor helper that a worked example above demonstrates.
        private static readonly string[] HelpersWithExamples =
        [
            "ConvertInlineStylesToTags", "KeepOnly", "Remove", "RemoveAttributes", "RemoveClasses",
            "RemoveComments", "RemoveEmptyElements", "RemoveHidden", "RemoveInlineStyles", "RemoveScripts",
            "RemoveStyleSheets", "RemoveStyles", "RemoveWhere", "Rename", "ReplaceText", "ReplaceWith",
            "ReplaceWithText", "ResolveRelativeUrls", "SimplifyTableCellHtml", "Transform", "Unwrap", "Wrap"
        ];

        [Fact]
        public void Every_step_sample_is_asserted()
        {
            // A sample added without an assertion here would ship an unverified output to the docs.
            var samples = typeof(PreprocessingSteps)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.ReturnType == typeof(string) && m.GetParameters().Length == 0)
                .Select(m => m.Name);

            Assert.Equal(Sorted(AssertedSamples), Sorted(samples));
        }

        [Fact]
        public void Every_public_step_helper_has_a_worked_example()
        {
            // The rule: every preprocessing helper is documented with a real, compiled example. Add a
            // helper and this fails until PreprocessingSteps demonstrates it.
            var helpers = typeof(HtmlPreprocessor)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.ReturnType == typeof(HtmlPreprocessor))
                .Select(m => m.Name)
                // Pipeline plumbing rather than a transformation: Add/AddText are demonstrated by the
                // custom-step samples, and Clear just empties the pipeline.
                .Where(n => n is not ("Add" or "AddText" or "Clear"));

            Assert.Equal(Sorted(HelpersWithExamples), Sorted(helpers));
        }

        private static string[] Sorted(System.Collections.Generic.IEnumerable<string> names) =>
            names.Distinct().OrderBy(n => n, StringComparer.Ordinal).ToArray();
    }
}

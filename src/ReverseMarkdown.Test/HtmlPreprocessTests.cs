using System.Text.RegularExpressions;
using AngleSharp.Dom;
using ReverseMarkdown.Preprocessing;
using Xunit;

namespace ReverseMarkdown.Test
{
    // HTML preprocessing pipeline (Config.Preprocess): transformations applied to the source markup
    // before it is read into the Markdown DOM.
    public class HtmlPreprocessTests
    {
        private static string Norm(string s) => s.Replace("\r\n", "\n").Trim();

        private static string Convert(string html, System.Action<HtmlPreprocessor> configure,
            Config.MarkdownFlavor flavor = Config.MarkdownFlavor.Default)
        {
            var config = new Config { Flavor = flavor };
            configure(config.Preprocess);
            return Norm(new Converter(config).Convert(html));
        }

        // ---- Removal ----

        [Fact]
        public void Remove_drops_matching_elements_and_their_content()
        {
            var md = Convert(
                "<p>keep</p><div class=\"ad\"><p>spam</p></div><p>also keep</p>",
                p => p.Remove("div.ad"));

            Assert.Equal("keep\n\nalso keep", md);
        }

        [Fact]
        public void Remove_supports_grouped_selectors()
        {
            var md = Convert(
                "<nav><p>menu</p></nav><p>body</p><footer><p>fine print</p></footer>",
                p => p.Remove("nav, footer"));

            Assert.Equal("body", md);
        }

        [Fact]
        public void RemoveWhere_drops_elements_matching_the_predicate()
        {
            var md = Convert(
                "<p>real</p><p data-role=\"promo\">buy now</p>",
                p => p.RemoveWhere(e => e.GetAttribute("data-role") == "promo"));

            Assert.Equal("real", md);
        }

        [Fact]
        public void KeepOnly_reduces_the_document_to_the_match()
        {
            var md = Convert(
                "<header><p>site</p></header><article><h1>Title</h1><p>body</p></article><footer><p>end</p></footer>",
                p => p.KeepOnly("article"));

            Assert.Equal("# Title\n\nbody", md);
        }

        [Fact]
        public void KeepOnly_keeps_every_match_in_document_order()
        {
            var md = Convert(
                "<p>drop</p><section><p>one</p></section><p>drop</p><section><p>two</p></section>",
                p => p.KeepOnly("section"));

            Assert.Equal("one\n\ntwo", md);
        }

        [Fact]
        public void KeepOnly_does_not_duplicate_nested_matches()
        {
            var md = Convert(
                "<div class=\"x\"><p>outer</p><div class=\"x\"><p>inner</p></div></div><p>drop</p>",
                p => p.KeepOnly("div.x"));

            Assert.Equal("outer\n\ninner", md);
        }

        [Fact]
        public void KeepOnly_leaves_the_document_untouched_when_nothing_matches()
        {
            var md = Convert("<p>content</p>", p => p.KeepOnly("article.missing"));

            Assert.Equal("content", md);
        }

        [Fact]
        public void RemoveComments_drops_comments_from_the_source()
        {
            var md = Convert("<p>a<!-- hidden -->b</p>", p => p.RemoveComments());

            Assert.Equal("ab", md);
        }

        [Fact]
        public void RemoveEmptyElements_collapses_nested_empty_wrappers()
        {
            var md = Convert(
                "<p>text</p><div><p></p><span> </span></div><p>more</p>",
                p => p.RemoveEmptyElements());

            Assert.Equal("text\n\nmore", md);
        }

        [Fact]
        public void RemoveEmptyElements_keeps_elements_holding_media()
        {
            var md = Convert(
                "<div><img src=\"a.png\" alt=\"a\"></div>",
                p => p.RemoveEmptyElements("div"));

            Assert.Equal("![a](a.png)", md);
        }

        // ---- Element and content transformation ----

        [Fact]
        public void Rename_changes_the_tag_and_keeps_attributes_and_children()
        {
            var md = Convert(
                "<h1>Title</h1><p>x</p>",
                p => p.Rename("h1", "h2"));

            Assert.Equal("## Title\n\nx", md);
        }

        [Fact]
        public void Rename_preserves_attributes_and_children()
        {
            var config = new Config();
            config.Preprocess.Rename("div", "section");
            var converter = new Converter(config);

            var html = converter.Preprocess("<div id=\"a\" class=\"b\"><p>child</p></div>");

            Assert.Equal("<section id=\"a\" class=\"b\"><p>child</p></section>", html);
        }

        [Fact]
        public void Unwrap_keeps_the_children_and_drops_the_element()
        {
            var md = Convert(
                "<p>a <span>b <em>c</em></span> d</p>",
                p => p.Unwrap("span"));

            Assert.Equal("a b *c* d", md);
        }

        [Fact]
        public void Unwrap_handles_nested_matches()
        {
            var md = Convert(
                "<p><span><span>deep</span></span></p>",
                p => p.Unwrap("span"));

            Assert.Equal("deep", md);
        }

        [Fact]
        public void Unwrap_strips_links_but_keeps_their_text()
        {
            var md = Convert(
                "<p>see <a href=\"http://x.com\">my site</a></p>",
                p => p.Unwrap("a"));

            Assert.Equal("see my site", md);
        }

        [Fact]
        public void Wrap_nests_the_match_in_a_new_element()
        {
            var md = Convert(
                "<p>quoted</p>",
                p => p.Wrap("p", "blockquote"));

            Assert.Equal("> quoted", md);
        }

        [Fact]
        public void ReplaceWith_swaps_element_and_content_for_new_markup()
        {
            var md = Convert(
                "<p>before</p><div class=\"callout\"><span>junk</span></div><p>after</p>",
                p => p.ReplaceWith("div.callout", "<blockquote>Note</blockquote>"));

            Assert.Equal("before\n\n> Note\n\nafter", md);
        }

        [Fact]
        public void ReplaceWithText_swaps_the_element_for_plain_text()
        {
            var md = Convert(
                "<p>hi <img src=\"s.png\" class=\"emoji\" alt=\"smile\"> there</p>",
                p => p.ReplaceWithText("img.emoji", ":smile:"));

            Assert.Equal("hi :smile: there", md);
        }

        [Fact]
        public void Transform_can_rewrite_attributes()
        {
            var md = Convert(
                "<p><a href=\"/docs\">docs</a></p>",
                p => p.Transform("a[href^='/']",
                    a => a.SetAttribute("href", "https://site.com" + a.GetAttribute("href"))));

            Assert.Equal("[docs](https://site.com/docs)", md);
        }

        [Fact]
        public void ReplaceText_substitutes_text_content()
        {
            var md = Convert(
                "<p>Hello NAME, welcome NAME.</p>",
                p => p.ReplaceText("NAME", "Jane"));

            Assert.Equal("Hello Jane, welcome Jane.", md);
        }

        [Fact]
        public void ReplaceText_with_a_regex_substitutes_matches()
        {
            var md = Convert(
                "<p>call 555-1234 today</p>",
                p => p.ReplaceText(new Regex(@"\d{3}-\d{4}"), "REDACTED"));

            Assert.Equal("call REDACTED today", md);
        }

        [Fact]
        public void ReplaceText_leaves_script_and_style_content_alone()
        {
            var config = new Config();
            config.Preprocess.ReplaceText("secret", "public");
            var converter = new Converter(config);

            var html = converter.Preprocess("<style>.secret{color:red}</style><p>secret</p>");

            Assert.Contains(".secret{color:red}", html);
            Assert.Contains("<p>public</p>", html);
        }

        // ---- Attributes, styles and scripts ----

        [Fact]
        public void RemoveAttributes_supports_a_trailing_wildcard()
        {
            var config = new Config();
            config.Preprocess.RemoveAttributes("*", "data-*");
            var converter = new Converter(config);

            var html = converter.Preprocess("<p data-a=\"1\" data-b=\"2\" id=\"keep\">x</p>");

            Assert.DoesNotContain("data-", html);
            Assert.Contains("id=\"keep\"", html);
        }

        [Fact]
        public void RemoveInlineStyles_drops_style_attributes()
        {
            var config = new Config();
            config.Preprocess.RemoveInlineStyles();
            var converter = new Converter(config);

            var html = converter.Preprocess("<p style=\"color:red\" id=\"a\">x</p>");

            Assert.DoesNotContain("style=", html);
            Assert.Contains("id=\"a\"", html);
        }

        [Fact]
        public void RemoveStyleSheets_drops_head_hoisted_style_elements()
        {
            // The HTML5 parser moves a leading <style> into <head>, out of reach of body-only
            // filtering, so the pipeline has to run against the document element.
            var md = Convert(
                "<style>p{color:red}</style><p>body</p>",
                p => p.RemoveStyleSheets());

            Assert.Equal("body", md);
        }

        [Fact]
        public void RemoveStyleSheets_drops_stylesheet_links()
        {
            var config = new Config();
            config.Preprocess.RemoveStyleSheets();
            var converter = new Converter(config);

            var html = converter.Preprocess(
                "<link rel=\"stylesheet\" href=\"a.css\"><link rel=\"canonical\" href=\"b\"><p>x</p>");

            Assert.DoesNotContain("stylesheet", html);
            Assert.Contains("canonical", html);
        }

        [Fact]
        public void RemoveStyles_drops_inline_styles_and_stylesheets()
        {
            var config = new Config();
            config.Preprocess.RemoveStyles();
            var converter = new Converter(config);

            var html = converter.Preprocess("<style>p{color:red}</style><p style=\"color:blue\">x</p>");

            Assert.DoesNotContain("color:red", html);
            Assert.DoesNotContain("color:blue", html);
            Assert.Contains("<p>x</p>", html);
        }

        [Fact]
        public void RemoveClasses_drops_class_attributes()
        {
            var config = new Config();
            config.Preprocess.RemoveClasses();
            var converter = new Converter(config);

            var html = converter.Preprocess("<p class=\"lead\">x</p>");

            Assert.DoesNotContain("class=", html);
        }

        [Fact]
        public void RemoveClasses_can_spare_code_language_markers()
        {
            var config = new Config { GithubFlavored = true };
            config.Preprocess.RemoveClasses(":not(pre):not(code)");
            var converter = new Converter(config);

            var md = Norm(converter.Convert(
                "<div class=\"wrap\"><pre><code class=\"language-cs\">var x = 1;</code></pre></div>"));

            Assert.StartsWith("```cs", md);
        }

        [Fact]
        public void RemoveScripts_drops_script_elements_and_event_handlers()
        {
            var config = new Config();
            config.Preprocess.RemoveScripts();
            var converter = new Converter(config);

            var html = converter.Preprocess(
                "<script>alert(1)</script><p onclick=\"go()\" id=\"a\">x</p><noscript>no js</noscript>");

            Assert.DoesNotContain("alert(1)", html);
            Assert.DoesNotContain("onclick", html);
            Assert.DoesNotContain("no js", html);
            Assert.Contains("id=\"a\"", html);
        }

        [Fact]
        public void RemoveHidden_drops_inline_hidden_elements()
        {
            var md = Convert(
                "<p style=\"display:none\">preheader</p><p>visible</p>" +
                "<p style=\"visibility: hidden\">gone</p><p hidden>also gone</p>",
                p => p.RemoveHidden());

            Assert.Equal("visible", md);
        }

        [Fact]
        public void RemoveHidden_keeps_visible_styled_elements()
        {
            var md = Convert("<p style=\"display:block;color:red\">shown</p>", p => p.RemoveHidden());

            Assert.Equal("shown", md);
        }

        [Fact]
        public void ConvertInlineStylesToTags_recovers_bold_italic_and_strikethrough()
        {
            var md = Convert(
                "<p><span style=\"font-weight:700\">bold</span> " +
                "<span style=\"font-style:italic\">italic</span> " +
                "<span style=\"text-decoration:line-through\">gone</span></p>",
                p => p.ConvertInlineStylesToTags().Unwrap("span"));

            Assert.Equal("**bold** *italic* ~~gone~~", md);
        }

        [Fact]
        public void ConvertInlineStylesToTags_ignores_non_bold_weights()
        {
            var md = Convert(
                "<p><span style=\"font-weight:400\">normal</span><span style=\"font-weight:normal\">too</span></p>",
                p => p.ConvertInlineStylesToTags().Unwrap("span"));

            Assert.Equal("normaltoo", md);
        }

        [Fact]
        public void ConvertInlineStylesToTags_nests_combined_styles()
        {
            var md = Convert(
                "<p><span style=\"font-weight:bold;font-style:italic\">both</span></p>",
                p => p.ConvertInlineStylesToTags().Unwrap("span"));

            Assert.Equal("***both***", md);
        }

        [Fact]
        public void ConvertInlineStylesToTags_is_idempotent()
        {
            // The consumed declarations are stripped, so running the step twice must not double-wrap.
            var md = Convert(
                "<p><span style=\"font-weight:bold\">once</span></p>",
                p => p.ConvertInlineStylesToTags().ConvertInlineStylesToTags().Unwrap("span"));

            Assert.Equal("**once**", md);
        }

        [Fact]
        public void ConvertInlineStylesToTags_keeps_unrelated_declarations()
        {
            var config = new Config();
            config.Preprocess.ConvertInlineStylesToTags();
            var converter = new Converter(config);

            var html = converter.Preprocess("<p style=\"font-weight:bold;color:red\">x</p>");

            Assert.Contains("color: red", html);
            Assert.DoesNotContain("font-weight", html);
        }

        // ---- Inline style helper ----

        [Fact]
        public void InlineStyle_reads_declarations()
        {
            var element = new AngleSharp.Html.Parser.HtmlParser()
                .ParseDocument("<p style=\"font-weight: 700; color:red\">x</p>")
                .QuerySelector("p")!;

            Assert.Equal("red", InlineStyle.Get(element, "color"));
            Assert.Equal("700", InlineStyle.Get(element, "FONT-WEIGHT"));
            Assert.Null(InlineStyle.Get(element, "display"));
            Assert.True(InlineStyle.IsBold(element));
        }

        [Fact]
        public void InlineStyle_survives_semicolons_inside_url_values()
        {
            var element = new AngleSharp.Html.Parser.HtmlParser()
                .ParseDocument("<p style=\"background: url(data:image/png;base64,AAA); font-weight: bold\">x</p>")
                .QuerySelector("p")!;

            Assert.Equal("url(data:image/png;base64,AAA)", InlineStyle.Get(element, "background"));
            Assert.True(InlineStyle.IsBold(element));

            InlineStyle.Remove(element, "font-weight");
            Assert.Equal("background: url(data:image/png;base64,AAA)", element.GetAttribute("style"));
        }

        [Fact]
        public void InlineStyle_Remove_drops_the_attribute_when_empty()
        {
            var element = new AngleSharp.Html.Parser.HtmlParser()
                .ParseDocument("<p style=\"color:red\">x</p>")
                .QuerySelector("p")!;

            InlineStyle.Remove(element, "color");

            Assert.False(element.HasAttribute("style"));
        }

        // ---- Table cells (issue #435) ----

        // A nested list inside a table cell has no markdown form, so the reader keeps the source
        // OuterHtml verbatim - classes, inline styles and editor wrappers included.
        private const string CellListHtml =
            "<table><tr><th>Head</th></tr><tr><td>" +
            "<ol class=\"customList\"><li><p class=\"noSpacing\" data-text-type=\"noSpacing\">" +
            "<span style=\"font-size:17px\" data-fontsize=\"17px\">Item one</span></p></li>" +
            "<li><p><span style=\"font-size:17px\">Item two</span></p></li></ol>" +
            "</td></tr></table>";

        [Fact]
        public void Retained_cell_html_keeps_editor_noise_without_preprocessing()
        {
            var md = Norm(new Converter(new Config()).Convert(CellListHtml));

            Assert.Contains("class=\"customList\"", md);
            Assert.Contains("style=\"font-size:17px\"", md);
            Assert.Contains("data-fontsize", md);
        }

        [Fact]
        public void SimplifyTableCellHtml_trims_retained_cell_html_to_its_structure()
        {
            var md = Convert(CellListHtml, p => p.SimplifyTableCellHtml());

            Assert.Equal("| Head |\n| --- |\n| <ol><li>Item one</li><li>Item two</li></ol> |", md);
        }

        [Fact]
        public void SimplifyTableCellHtml_keeps_multi_block_list_items_intact()
        {
            // Two paragraphs in one item: the <p> wrappers carry meaning here, so they stay.
            var md = Convert(
                "<table><tr><th>Head</th></tr><tr><td><ul><li><p>first</p><p>second</p></li></ul></td></tr></table>",
                p => p.SimplifyTableCellHtml());

            Assert.Contains("<li><p>first</p><p>second</p></li>", md);
        }

        [Fact]
        public void SimplifyTableCellHtml_leaves_content_outside_tables_alone()
        {
            var config = new Config();
            config.Preprocess.SimplifyTableCellHtml();
            var converter = new Converter(config);

            var html = converter.Preprocess("<p class=\"keep\"><span style=\"color:red\">outside</span></p>");

            Assert.Contains("class=\"keep\"", html);
            Assert.Contains("<span style=\"color:red\">", html);
        }

        [Fact]
        public void Cell_html_can_also_be_cleaned_with_the_general_helpers()
        {
            // CSS descendant selectors scope any helper to table cells, for a different trade-off.
            var md = Convert(CellListHtml,
                p => p.Unwrap("td span, th span").RemoveAttributes("td *, th *", "class", "style", "data-*"));

            Assert.Equal("| Head |\n| --- |\n| <ol><li><p>Item one</p></li><li><p>Item two</p></li></ol> |", md);
        }

        // ---- Tables.CellListHandling (issue #435) ----

        private static string ConvertCellLists(string html, char bullet = '-')
        {
            var config = new Config
            {
                Tables = { CellListHandling = Config.TableCellListHandlingOption.InlineText },
                Formatting = { ListBulletChar = bullet },
            };
            return Norm(new Converter(config).Convert(html));
        }

        [Fact]
        public void CellListHandling_InlineText_flattens_an_ordered_list()
        {
            var md = ConvertCellLists(
                "<table><tr><th>H</th></tr><tr><td><ol><li>first</li><li>second</li></ol></td></tr></table>");

            Assert.Equal("| H |\n| --- |\n| 1. first<br>2. second |", md);
        }

        [Fact]
        public void CellListHandling_InlineText_converts_item_content_to_markdown()
        {
            var md = ConvertCellLists(
                "<table><tr><th>H</th></tr><tr><td><ul><li><strong>bold</strong> item</li>" +
                "<li><a href=\"http://x.com\">link</a></li></ul></td></tr></table>");

            Assert.Equal("| H |\n| --- |\n| - **bold** item<br>- [link](http://x.com) |", md);
        }

        [Fact]
        public void CellListHandling_InlineText_honours_the_ol_start_attribute()
        {
            var md = ConvertCellLists(
                "<table><tr><th>H</th></tr><tr><td><ol start=\"3\"><li>three</li><li>four</li></ol></td></tr></table>");

            Assert.Equal("| H |\n| --- |\n| 3. three<br>4. four |", md);
        }

        [Fact]
        public void CellListHandling_InlineText_uses_the_configured_bullet_unescaped()
        {
            var md = ConvertCellLists(
                "<table><tr><th>H</th></tr><tr><td><ul><li>a</li><li>b</li></ul></td></tr></table>", bullet: '*');

            Assert.Equal("| H |\n| --- |\n| * a<br>* b |", md);
        }

        [Fact]
        public void CellListHandling_InlineText_flattens_nested_lists()
        {
            var md = ConvertCellLists(
                "<table><tr><th>H</th></tr><tr><td><ul><li>top<ul><li>inner</li></ul></li>" +
                "<li>second</li></ul></td></tr></table>");

            Assert.Equal("| H |\n| --- |\n| - top<br>- inner<br>- second |", md);
        }

        [Fact]
        public void CellListHandling_InlineText_keeps_preceding_blocks_separate()
        {
            var md = ConvertCellLists(
                "<table><tr><th>H</th></tr><tr><td><p>Intro:</p><ol><li>one</li></ol></td></tr></table>");

            Assert.Equal("| H |\n| --- |\n| Intro:<br><br>1. one |", md);
        }

        [Fact]
        public void CellListHandling_InlineText_strips_editor_noise_without_preprocessing()
        {
            var md = ConvertCellLists(
                "<table><tr><th>H</th></tr><tr><td><ol class=\"c\"><li><p class=\"n\">" +
                "<span style=\"font-size:17px\">First</span></p></li></ol></td></tr></table>");

            Assert.Equal("| H |\n| --- |\n| 1. First |", md);
        }

        [Fact]
        public void CellListHandling_InlineText_leaves_nested_tables_as_raw_html()
        {
            var md = ConvertCellLists(
                "<table><tr><th>H</th></tr><tr><td><table><tr><td>inner</td></tr></table></td></tr></table>");

            Assert.Contains("<table><tr><td>inner</td></tr></table>", md);
        }

        [Fact]
        public void CellListHandling_defaults_to_RawHtml()
        {
            var config = new Config();
            Assert.Equal(Config.TableCellListHandlingOption.RawHtml, config.Tables.CellListHandling);

            var md = Norm(new Converter(config).Convert(
                "<table><tr><th>H</th></tr><tr><td><ol><li>first</li></ol></td></tr></table>"));

            Assert.Contains("<ol><li>first</li></ol>", md);
        }

        [Fact]
        public void CellListHandling_InlineText_leaves_lists_outside_tables_alone()
        {
            var md = ConvertCellLists("<ul><li>a</li><li>b</li></ul>");

            Assert.Equal("- a\n- b", md);
        }

        // ---- URLs ----

        [Fact]
        public void ResolveRelativeUrls_makes_links_and_images_absolute()
        {
            var md = Convert(
                "<p><a href=\"/docs\">docs</a> <img src=\"img/a.png\" alt=\"a\"></p>",
                p => p.ResolveRelativeUrls("https://site.com/guide/index.html"));

            Assert.Equal("[docs](https://site.com/docs) ![a](https://site.com/guide/img/a.png)", md);
        }

        [Fact]
        public void ResolveRelativeUrls_leaves_absolute_and_anchor_urls_alone()
        {
            var md = Convert(
                "<p><a href=\"https://other.com/x\">x</a> <a href=\"#top\">top</a> <a href=\"mailto:a@b.com\">mail</a></p>",
                p => p.ResolveRelativeUrls("https://site.com/"));

            Assert.Equal("[x](https://other.com/x) [top](#top) [mail](mailto:a@b.com)", md);
        }

        // ---- Pipeline semantics ----

        [Fact]
        public void Steps_run_in_the_order_they_were_added()
        {
            var renameThenRemove = Convert("<h1>Title</h1><p>x</p>",
                p => p.Rename("h1", "h2").Remove("h2"));
            var removeThenRename = Convert("<h1>Title</h1><p>x</p>",
                p => p.Remove("h2").Rename("h1", "h2"));

            Assert.Equal("x", renameThenRemove);
            Assert.Equal("## Title\n\nx", removeThenRename);
        }

        [Fact]
        public void Custom_delegate_steps_are_supported()
        {
            var md = Convert("<p>x</p>", p => p.Add("append", root =>
            {
                var document = root.Owner!;
                var paragraph = document.CreateElement("p");
                paragraph.TextContent = "added";
                document.Body!.AppendChild(paragraph);
            }));

            Assert.Equal("x\n\nadded", md);
        }

        [Fact]
        public void Custom_step_implementations_are_supported()
        {
            var config = new Config();
            config.Preprocess.Add(new UppercaseHeadings());

            Assert.Equal("# TITLE", Norm(new Converter(config).Convert("<h1>Title</h1>")));
        }

        private sealed class UppercaseHeadings : IHtmlPreprocessStep
        {
            public void Apply(IElement root)
            {
                foreach (var heading in root.QuerySelectorAll("h1, h2, h3"))
                {
                    heading.TextContent = heading.TextContent.ToUpperInvariant();
                }
            }
        }

        [Fact]
        public void Steps_are_exposed_and_clearable()
        {
            var preprocessor = new HtmlPreprocessor().Remove("nav").RemoveStyles();

            Assert.Equal(2, preprocessor.Count);
            Assert.Equal("Remove(nav)", preprocessor.Steps[0].ToString());

            preprocessor.Clear();
            Assert.Empty(preprocessor.Steps);
        }

        [Fact]
        public void An_empty_pipeline_leaves_conversion_unchanged()
        {
            const string html = "<div><p>a</p><!-- c --><script>x</script></div>";

            Assert.Equal(
                Norm(new Converter(new Config()).Convert(html)),
                Convert(html, _ => { }));
        }

        // ---- Integration with the conversion entry points ----

        [Fact]
        public void Preprocess_returns_the_input_unchanged_when_no_steps_are_configured()
        {
            const string html = "<p>x</p>";

            Assert.Same(html, new Converter(new Config()).Preprocess(html));
        }

        [Fact]
        public void Preprocess_keeps_head_metadata_when_the_source_has_a_head()
        {
            var config = new Config();
            config.Preprocess.Remove("nav");
            var converter = new Converter(config);

            var html = converter.Preprocess(
                "<html><head><title>T</title></head><body><nav>menu</nav><p>x</p></body></html>");

            Assert.Contains("<title>T</title>", html);
            Assert.DoesNotContain("menu", html);
        }

        [Fact]
        public void Preprocess_applies_to_Parse_and_the_Markdown_dom()
        {
            var config = new Config();
            config.Preprocess.Remove("aside");
            var converter = new Converter(config);

            var document = converter.Parse("<p>body</p><aside><p>related</p></aside>");

            Assert.Equal("body", Norm(converter.Render(document)));
        }

        [Fact]
        public void Preprocess_runs_before_the_CommonMark_raw_html_passthrough()
        {
            // Without preprocessing this input is passed through verbatim as a CommonMark HTML
            // block; the pipeline has to run first so the check sees the transformed markup.
            var md = Convert(
                "<div class=\"ad\"><p>spam</p></div><p>hi</p>",
                p => p.Remove("div.ad"),
                Config.MarkdownFlavor.CommonMark);

            Assert.Equal("hi", md);
        }

        [Fact]
        public void Preprocess_runs_before_the_Slack_unsupported_tag_guard()
        {
            var md = Convert(
                "<p>hi</p><table><tr><td>cell</td></tr></table>",
                p => p.Remove("table"),
                Config.MarkdownFlavor.Slack);

            Assert.Equal("hi", md);
        }

        [Fact]
        public void Preprocess_keeps_metadata_for_metadata_emitting_flavors()
        {
            var config = new Config { Flavor = Config.MarkdownFlavor.MultiMarkdown };
            config.Preprocess.Remove("nav");
            var converter = new Converter(config);

            var md = Norm(converter.Convert(
                "<html><head><title>My Title</title></head><body><nav>menu</nav><p>Body.</p></body></html>"));

            Assert.Equal("title: My Title\n\nBody.", md);
        }

        // ---- Text (pre-parse) steps ----

        [Fact]
        public void Text_steps_rewrite_the_markup_before_it_is_parsed()
        {
            var md = Convert(
                "<p>Hello {{name}}</p>",
                p => p.AddText("template", html => html.Replace("{{name}}", "Jane")));

            Assert.Equal("Hello Jane", md);
        }

        [Fact]
        public void Text_steps_can_repair_markup_the_parser_would_discard()
        {
            // The HTML5 parser treats everything after an unclosed <template> as its content, so the
            // fix has to happen before parsing - a DOM step would arrive too late.
            var md = Convert(
                "<template><p>swallowed</p>",
                p => p.AddText("drop-template", html => html.Replace("<template>", string.Empty)));

            Assert.Equal("swallowed", md);
        }

        [Fact]
        public void Text_steps_run_before_dom_steps_regardless_of_order()
        {
            var md = Convert(
                "<p>keep</p>",
                p => p.Remove("aside")
                      .AddText("inject", html => html + "<aside>injected</aside>"));

            // The text step appends the <aside>, and the DOM step still sees and removes it.
            Assert.Equal("keep", md);
        }

        [Fact]
        public void Text_steps_run_in_order()
        {
            var md = Convert(
                "<p>a</p>",
                p => p.AddText("one", html => html.Replace("a", "b"))
                      .AddText("two", html => html.Replace("b", "c")));

            Assert.Equal("c", md);
        }

        [Fact]
        public void Text_steps_count_towards_the_pipeline()
        {
            var preprocessor = new HtmlPreprocessor()
                .Remove("nav")
                .AddText("noop", html => html);

            Assert.Equal(2, preprocessor.Count);
            Assert.Single(preprocessor.Steps);
            Assert.Single(preprocessor.TextSteps);

            preprocessor.Clear();
            Assert.Equal(0, preprocessor.Count);
        }

        // ---- Structural safety ----

        [Fact]
        public void Broad_predicates_do_not_detach_the_document_structure()
        {
            // QuerySelectorAll runs from the document element, so this predicate also matches
            // html/head/body. Sweeping those away would leave nothing for the reader to read.
            var md = Convert("<p class=\"keep\">text</p><p>drop</p>",
                p => p.RemoveWhere(e => !e.HasAttribute("class")));

            Assert.Equal("text", md);
        }

        [Fact]
        public void Broad_selectors_do_not_detach_the_document_structure()
        {
            Assert.Equal("text", Convert("<p>text</p>", p => p.Remove("html, head, body")));
            Assert.Equal("text", Convert("<p>text</p>", p => p.RemoveEmptyElements("*")));
            Assert.Equal("text", Convert("<p>text</p>", p => p.KeepOnly("body")));
        }

        [Fact]
        public void A_custom_step_that_removes_the_body_fails_with_an_explanation()
        {
            var config = new Config();
            config.Preprocess.Add("nuke-body", root => root.Owner!.Body!.Remove());

            var error = Assert.Throws<System.InvalidOperationException>(
                () => new Converter(config).Convert("<p>text</p>"));

            Assert.Contains("preprocessing step removed the document structure", error.Message);
        }

        // ---- Custom AngleSharp browsing context ----

        [Fact]
        public void A_custom_browsing_context_is_used_for_parsing()
        {
            var context = AngleSharp.BrowsingContext.New(AngleSharp.Configuration.Default);
            var config = new Config();
            config.Preprocess.Remove("nav");

            var converter = new Converter(config, context);

            Assert.Equal("body", Norm(converter.Convert("<nav>menu</nav><p>body</p>")));
        }

        [Fact]
        public void A_null_browsing_context_is_rejected()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new Converter(new Config(), (AngleSharp.IBrowsingContext)null!));
        }

        [Fact]
        public void Preprocess_runs_before_the_html_exclude_filters()
        {
            var config = new Config();
            config.Preprocess.Rename("aside", "section");
            config.Html.ExcludeSelectors.Add("section");
            var converter = new Converter(config);

            // The rename happens first, so the exclude filter sees (and drops) a <section>.
            Assert.Equal("body", Norm(converter.Convert("<p>body</p><aside><p>related</p></aside>")));
        }
    }
}

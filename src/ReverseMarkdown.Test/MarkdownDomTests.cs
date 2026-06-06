using System.Linq;
using ReverseMarkdown;
using ReverseMarkdown.Dom;
using Xunit;

namespace ReverseMarkdown.Test
{
    // Phase A scaffolding tests for the v6 Markdown DOM path. See docs/v6/.
    public class MarkdownDomTests
    {
        private static string Norm(string s) => s.Replace("\r\n", "\n").Trim();

        [Fact]
        public void Parse_then_render_roundtrips_through_commonmark()
        {
            var converter = new Converter(new Config());

            var doc = converter.Parse("<h1>Hi</h1><p>Hello <strong>world</strong> and <em>you</em></p>");
            var md = converter.Render(doc);

            Assert.Equal("# Hi\n\nHello **world** and *you*", Norm(md));
        }

        [Fact]
        public void Parse_builds_a_navigable_tree()
        {
            var converter = new Converter(new Config());

            var doc = converter.Parse("<p>Hello <strong>world</strong></p>");

            Assert.Single(doc.Children);
            var paragraph = Assert.IsType<MdParagraph>(doc.Children[0]);
            Assert.Collection(paragraph.Children,
                inline => Assert.Equal("Hello ", Assert.IsType<MdText>(inline).Value),
                inline => Assert.IsType<MdStrong>(inline));
        }

        [Fact]
        public void Dom_is_mutable_remove_drops_node_from_output()
        {
            var converter = new Converter(new Config());
            var doc = converter.Parse("<p>Hello <strong>world</strong></p>");

            var strong = doc.Descendants().OfType<MdStrong>().Single();
            strong.Remove();

            Assert.Equal("Hello", Norm(converter.Render(doc)));
            Assert.Null(strong.Parent);
        }

        [Fact]
        public void Dom_is_mutable_replacewith_swaps_node()
        {
            var converter = new Converter(new Config());
            var doc = converter.Parse("<p>Hello <strong>world</strong></p>");

            var strong = doc.Descendants().OfType<MdStrong>().Single();
            strong.ReplaceWith(new MdEmphasis { Children = { new MdText("there") } });

            Assert.Equal("Hello *there*", Norm(converter.Render(doc)));
        }

        [Theory]
        [InlineData("<p>see <a href=\"https://x.io\">site</a></p>", "see [site](https://x.io)")]
        [InlineData("<p>see <a href=\"https://x.io\" title=\"t\">site</a></p>", "see [site](https://x.io \"t\")")]
        [InlineData("<p>a <img src=\"i.png\" alt=\"pic\"> b</p>", "a ![pic](i.png) b")]
        [InlineData("<p>run <code>dotnet build</code></p>", "run `dotnet build`")]
        [InlineData("<p>x<br>y</p>", "x  \ny")]
        [InlineData("<p>a <s>b</s> c</p>", "a ~~b~~ c")]
        [InlineData("<p>a <del>b</del> c</p>", "a ~~b~~ c")]
        public void Inline_nodes_render_through_commonmark(string html, string expected)
        {
            var converter = new Converter(new Config());
            Assert.Equal(expected, Norm(converter.Render(converter.Parse(html))));
        }

        [Fact]
        public void Thematic_break_renders()
        {
            var converter = new Converter(new Config());
            Assert.Equal("a\n\n***\n\nb", Norm(converter.Render(converter.Parse("<p>a</p><hr><p>b</p>"))));
        }

        [Fact]
        public void Blockquote_prefixes_each_line()
        {
            var converter = new Converter(new Config());
            var md = converter.Render(converter.Parse("<blockquote><p>one</p><p>two</p></blockquote>"));
            Assert.Equal("> one\n>\n> two", Norm(md));
        }

        [Fact]
        public void Inline_code_widens_fence_when_content_has_backtick()
        {
            var converter = new Converter(new Config());
            // AngleSharp keeps the backtick literal inside <code>
            var md = converter.Render(converter.Parse("<p><code>a`b</code></p>"));
            Assert.Equal("``a`b``", Norm(md));
        }

        [Fact]
        public void Unordered_list_renders()
        {
            var converter = new Converter(new Config());
            var md = converter.Render(converter.Parse("<ul><li>one</li><li>two</li></ul>"));
            Assert.Equal("- one\n- two", Norm(md));
        }

        [Fact]
        public void Ordered_list_respects_start()
        {
            var converter = new Converter(new Config());
            var md = converter.Render(converter.Parse("<ol start=\"3\"><li>a</li><li>b</li></ol>"));
            Assert.Equal("3. a\n4. b", Norm(md));
        }

        [Fact]
        public void Nested_list_is_indented()
        {
            var converter = new Converter(new Config());
            var md = converter.Render(converter.Parse("<ul><li>top<ul><li>inner</li></ul></li></ul>"));
            Assert.Equal("- top\n  - inner", Norm(md));
        }

        [Fact]
        public void Code_block_is_fenced_with_language()
        {
            var converter = new Converter(new Config());
            var md = converter.Render(converter.Parse("<pre><code class=\"language-csharp\">var x = 1;</code></pre>"));
            Assert.Equal("```csharp\nvar x = 1;\n```", Norm(md));
        }

        [Fact]
        public void Code_block_without_language()
        {
            var converter = new Converter(new Config());
            var md = converter.Render(converter.Parse("<pre>plain\ntext</pre>"));
            Assert.Equal("```\nplain\ntext\n```", Norm(md));
        }

        [Fact]
        public void Table_with_header_renders_as_pipe_table()
        {
            var converter = new Converter(new Config());
            var html = "<table><thead><tr><th>Name</th><th>Age</th></tr></thead>" +
                       "<tbody><tr><td>Alice</td><td>30</td></tr></tbody></table>";
            var md = converter.Render(converter.Parse(html));
            Assert.Equal("| Name | Age |\n| --- | --- |\n| Alice | 30 |", Norm(md));
        }

        [Fact]
        public void Table_without_header_uses_first_row()
        {
            var converter = new Converter(new Config());
            var html = "<table><tr><td>a</td><td>b</td></tr><tr><td>c</td><td>d</td></tr></table>";
            var md = converter.Render(converter.Parse(html));
            Assert.Equal("| a | b |\n| --- | --- |\n| c | d |", Norm(md));
        }

        [Fact]
        public void Table_honors_column_alignment()
        {
            var converter = new Converter(new Config());
            var html = "<table><tr><th style=\"text-align:center\">A</th>" +
                       "<th align=\"right\">B</th></tr><tr><td>1</td><td>2</td></tr></table>";
            var md = converter.Render(converter.Parse(html));
            Assert.Equal("| A | B |\n| :---: | ---: |\n| 1 | 2 |", Norm(md));
        }

        [Fact]
        public void Table_escapes_pipes_in_cells()
        {
            var converter = new Converter(new Config());
            var html = "<table><tr><th>h</th></tr><tr><td>a|b</td></tr></table>";
            var md = converter.Render(converter.Parse(html));
            Assert.Equal("| h |\n| --- |\n| a\\|b |", Norm(md));
        }

        [Fact]
        public void Unknown_tag_bypass_keeps_content_drops_wrapper()
        {
            var converter = new Converter(new Config { UnknownTags = Config.UnknownTagsOption.Bypass });
            Assert.Equal("hi", Norm(converter.Render(converter.Parse("<p><foo>hi</foo></p>"))));
        }

        [Fact]
        public void Unknown_tag_drop_removes_content()
        {
            var converter = new Converter(new Config { UnknownTags = Config.UnknownTagsOption.Drop });
            // The dropped <foo>b</foo> leaves "a " + " c"; cross-node whitespace collapsing
            // merges the boundary spaces into one.
            Assert.Equal("a c", Norm(converter.Render(converter.Parse("<p>a <foo>b</foo> c</p>"))));
        }

        [Fact]
        public void Unknown_tag_passthrough_emits_raw_html()
        {
            var converter = new Converter(new Config { UnknownTags = Config.UnknownTagsOption.PassThrough });
            Assert.Equal("a <foo>b</foo> c", Norm(converter.Render(converter.Parse("<p>a <foo>b</foo> c</p>"))));
        }

        [Fact]
        public void Unknown_tag_raise_throws()
        {
            var converter = new Converter(new Config { UnknownTags = Config.UnknownTagsOption.Raise });
            Assert.Throws<UnknownTagException>(() => converter.Parse("<p><foo>b</foo></p>"));
        }

        [Fact]
        public void PassThroughTags_emits_raw_regardless_of_mode()
        {
            var config = new Config { UnknownTags = Config.UnknownTagsOption.Bypass };
            config.PassThroughTags.Add("foo");
            var converter = new Converter(config);
            Assert.Equal("<foo>b</foo>", Norm(converter.Render(converter.Parse("<p><foo>b</foo></p>"))));
        }

        [Fact]
        public void TagAlias_routes_to_target_reader()
        {
            var config = new Config();
            config.TagAliases.Add("u", "em");
            var converter = new Converter(config);
            Assert.Equal("a *x* b", Norm(converter.Render(converter.Parse("<p>a <u>x</u> b</p>"))));
        }

        [Theory]
        [InlineData("<p>a    b</p>", "a b")]                                   // collapse runs
        [InlineData("<p>Hello\n    <strong>world</strong></p>", "Hello **world**")] // source newline+indent
        [InlineData("<p>   spaced   </p>", "spaced")]                          // trim block edges
        [InlineData("<p>a<strong> b </strong>c</p>", "a **b** c")]            // emphasis edge spaces move out
        [InlineData("<p><em> x </em></p>", "*x*")]                            // emphasis edges + edge trim
        public void Inline_whitespace_is_normalized(string html, string expected)
        {
            var converter = new Converter(new Config());
            Assert.Equal(expected, Norm(converter.Render(converter.Parse(html))));
        }

        [Theory]
        [InlineData("<p>x<sup>2</sup></p>", "x^2^")]
        [InlineData("<p>H<sub>2</sub>O</p>", "H<sub>2</sub>O")]
        [InlineData("<div><p>inside div</p></div>", "inside div")]
        [InlineData("<div>a<span>b</span>c</div>", "abc")]
        [InlineData("<section><h2>Heading</h2><p>body</p></section>", "## Heading\n\nbody")]
        public void Sup_sub_and_structural_wrappers(string html, string expected)
        {
            var converter = new Converter(new Config());
            Assert.Equal(expected, Norm(converter.Render(converter.Parse(html))));
        }

        [Fact]
        public void Definition_list_renders_term_and_description()
        {
            var converter = new Converter(new Config());
            var html = "<dl><dt>Term</dt><dd>Definition</dd></dl>";
            Assert.Equal("Term\n:   Definition", Norm(converter.Render(converter.Parse(html))));
        }

        [Fact]
        public void Definition_list_multiple_entries()
        {
            var converter = new Converter(new Config());
            var html = "<dl><dt>A</dt><dd>1</dd><dt>B</dt><dd>2</dd></dl>";
            Assert.Equal("A\n:   1\nB\n:   2", Norm(converter.Render(converter.Parse(html))));
        }

        [Fact]
        public void Link_with_nonwhitelisted_scheme_bypasses_to_text()
        {
            var config = new Config();
            config.WhitelistUriSchemes.Add("https");
            var converter = new Converter(config);
            Assert.Equal("click", Norm(converter.Render(converter.Parse("<p><a href=\"javascript:alert(1)\">click</a></p>"))));
        }

        [Fact]
        public void SmartHref_drops_link_when_text_equals_href()
        {
            var config = new Config { SmartHrefHandling = true };
            var converter = new Converter(config);
            Assert.Equal("https://x.io", Norm(converter.Render(converter.Parse("<p><a href=\"https://x.io\">https://x.io</a></p>"))));
        }

        [Fact]
        public void Base64_image_skip_drops_it()
        {
            var config = new Config { Base64Images = Config.Base64ImageHandling.Skip };
            var converter = new Converter(config);
            var html = "<p>a<img src=\"data:image/png;base64,iVBOR\" alt=\"x\">b</p>";
            Assert.Equal("ab", Norm(converter.Render(converter.Parse(html))));
        }

        [Fact]
        public void UnknownTagsReplacer_wraps_content()
        {
            var config = new Config();
            config.UnknownTagsReplacer.Add("u", "_");
            var converter = new Converter(config);
            Assert.Equal("a _x_ b", Norm(converter.Render(converter.Parse("<p>a <u>x</u> b</p>"))));
        }

        [Theory]
        [InlineData("<p>a * b _ c</p>", "a \\* b \\_ c")]                 // literal emphasis chars escaped
        [InlineData("<p>snake_case and a*b</p>", "snake\\_case and a\\*b")]
        public void Literal_emphasis_characters_are_escaped(string html, string expected)
        {
            var converter = new Converter(new Config());
            Assert.Equal(expected, Norm(converter.Render(converter.Parse(html))));
        }

        [Fact]
        public void Child_collections_maintain_parent_backpointer()
        {
            var paragraph = new MdParagraph();
            var text = new MdText("x");

            paragraph.Children.Add(text);
            Assert.Same(paragraph, text.Parent);

            paragraph.Children.Remove(text);
            Assert.Null(text.Parent);
        }
    }
}

using System;
using System.Threading.Tasks;
using VerifyTests;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace ReverseMarkdown.Test
{
    public class ConverterTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly VerifySettings _verifySettings;

        public ConverterTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _verifySettings = new VerifySettings();
            _verifySettings.DisableRequireUniquePrefix();
        }

        [Fact]
        public Task WhenThereIsAsideTag()
        {
            var html = "<aside>This text is in an aside tag.</aside> This text appears after aside.";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsHtmlLink_ThenConvertToMarkdownLink()
        {
            var html = @"This is <a href=""http://test.com"">a link</a>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsHtmlLinkWithTitle_ThenConvertToMarkdownLink()
        {
            var html = @"This is <a href=""http://test.com"" title=""with title"">a link</a>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereAreMultipleLinks_ThenConvertThemToMarkdownLinks()
        {
            var html =
                @"This is <a href=""http://test.com"">first link</a> and <a href=""http://test1.com"">second link</a>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsHtmlLinkNotWhitelisted_ThenBypass()
        {
            var html =
                @"Leave <a href=""http://example.com"">http</a>, <a href=""https://example.com"">https</a>, <a href=""ftp://example.com"">ftp</a>, <a href=""ftps://example.com"">ftps</a>, <a href=""file://example.com"">file</a>. Remove <a href=""data:text/plain;charset=UTF-8;page=21,the%20data:1234,5678"">data</a>, <a href=""tel://example.com"">tel</a> and <a href=""whatever://example.com"">whatever</a>";
            return CheckConversion(html, new Config
            {
                WhitelistUriSchemes = new[] {"http", "https", "ftp", "ftps", "file"}
            });
        }

        [Fact]
        public Task WhenThereHtmlWithHrefAndNoSchema_WhitelistedEmptyString_ThenConvertToMarkdown()
        {
            return CheckConversion(
                html: @"<a href=""example.com"">yeah</a>",
                config: new Config
                {
                    WhitelistUriSchemes = new[] {""}
                }
            );
        }

        [Fact]
        public Task WhenThereHtmlWithHrefAndNoSchema_NotWhitelisted_ThenConvertToPlain()
        {
            return CheckConversion(
                html: @"<a href=""example.com"">yeah</a>",
                config: new Config
                {
                    WhitelistUriSchemes = new[] {"whatever"}
                }
            );
        }

        [Fact]
        public Task WhenThereIsHtmlLinkWithDisallowedCharsInChildren_ThenEscapeTextInMarkdown()
        {
            return CheckConversion(
                html: @"<a href=""http://example.com"">this ]( might break things</a>",
                config: new Config
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public Task WhenThereIsHtmlLinkWithParensInHref_ThenEscapeHrefInMarkdown()
        {
            return CheckConversion(
                html: @"<a href=""http://example.com?id=foo)bar"">link</a>",
                config: new Config
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public Task WhenThereIsHtmlWithProtocolRelativeUrlHrefAndNameNotMatching_SmartHandling_ThenConvertToMarkdown()
        {
            return CheckConversion(
                html: @"<a href=""//example.com"">example.com</a>",
                config: new Config
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public Task WhenThereIsHtmlWithHrefAndNameNotMatching_SmartHandling_ThenConvertToMarkdown()
        {
            return CheckConversion(
                html: @"<a href=""https://example.com"">Something intact</a>",
                config: new Config
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public Task WhenThereIsHtmlWithHrefAndNameMatching_SmartHandling_ThenConvertToPlain()
        {
            return CheckConversion(
                html: @"<a href=""http://example.com/abc?x"">http://example.com/abc?x</a>",
                config: new Config
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public void WhenThereIsHtmlWithHttpSchemeAndNameWithoutScheme_SmartHandling_ThenConvertToPlain()
        {
            var config = new Config()
            {
                SmartHrefHandling = true
            };
            var converter = new Converter(config);
            var result = converter.Convert(@"<a href=""http://example.com"">example.com</a>");
            Assert.Equal("http://example.com", result, StringComparer.OrdinalIgnoreCase);

            var result1 = converter.Convert(@"<a href=""https://example.com"">example.com</a>");
            Assert.Equal("https://example.com", result1, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public Task WhenThereIsHtmlWithMailtoSchemeAndNameWithoutScheme_SmartHandling_ThenConvertToPlain()
        {
            return CheckConversion(
                html: @"<a href=""mailto:george@example.com"">george@example.com</a>",
                config: new Config
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public Task WhenThereIsHtmlWithTelSchemeAndNameWithoutScheme_SmartHandling_ThenConvertToPlain()
        {
            return CheckConversion(
                html: @"<a href=""tel:+1123-45678"">+1123-45678</a>",
                config: new Config
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public void WhenThereIsHtmlLinkWithHttpSchemaAndNameWithout_SmartHandling_ThenOutputOnlyHref()
        {
            var config = new Config
            {
                SmartHrefHandling = true
            };
            var converter = new Converter(config);
            var result = converter.Convert(@"<a href=""http://example.com"">example.com</a>");
            Assert.Equal("http://example.com", result, StringComparer.OrdinalIgnoreCase);
            var result1 = converter.Convert(@"<a href=""https://example.com"">example.com</a>");
            Assert.Equal("https://example.com", result1, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void WhenThereIsHtmlNonWellFormedLinkLink_SmartHandling_ThenConvertToMarkdown()
        {
            var config = new Config
            {
                SmartHrefHandling = true
            };

            //The string is not correctly escaped.
            var converter = new Converter(config);
            var result =
                converter.Convert(
                    @"<a href=""http://example.com/path/file name.docx"">http://example.com/path/file name.docx</a>");
            Assert.Equal("[http://example.com/path/file name.docx](http://example.com/path/file%20name.docx)", result,
                StringComparer.OrdinalIgnoreCase);

            //The string is an absolute Uri that represents an implicit file Uri.
            var result1 = converter.Convert(@"<a href=""c:\\directory\filename"">	c:\\directory\filename</a>");
            Assert.Equal(@"[c:\\directory\filename](c:\\directory\filename)", result1,
                StringComparer.OrdinalIgnoreCase);

            //The string is an absolute URI that is missing a slash before the path.
            var result2 =
                converter.Convert(@"<a href=""file://c:/directory/filename"">file://c:/directory/filename</a>");
            Assert.Equal("[file://c:/directory/filename](file://c:/directory/filename)", result2,
                StringComparer.OrdinalIgnoreCase);

            //The string contains unescaped backslashes even if they are treated as forward slashes.
            var result3 = converter.Convert(@"<a href=""http:\\host/path/file"">http:\\host/path/file</a>");
            Assert.Equal(@"[http:\\host/path/file](http:\\host/path/file)", result3, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public Task WhenThereIsHtmlLinkWithoutHttpSchemaAndNameWithoutScheme_SmartHandling_ThenConvertToMarkdown()
        {
            return CheckConversion(
                html: @"<a href=""ftp://example.com"">example.com</a>",
                config: new Config
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public Task WhenThereAreStrongTag_ThenConvertToMarkdownDoubleAsterisks()
        {
            var html = "This paragraph contains <strong>bold</strong> text";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereAreBTag_ThenConvertToMarkdownDoubleAsterisks()
        {
            var html = "This paragraph contains <b>bold</b> text";
            return CheckConversion(html);
        }

        [Fact]
        public Task
            WhenThereIsEncompassingStrongOrBTag_ThenConvertToMarkdownDoubleAsterisks_AnyStrongOrBTagsInsideAreIgnored()
        {
            var html =
                "<strong>Paragraph is encompassed with strong tag and also has <b>bold</b> text words within it</strong>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsSingleAsteriskInText_ThenConvertToMarkdownEscapedAsterisk()
        {
            var html = "This is a sample(*) paragraph";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsEmTag_ThenConvertToMarkdownSingleAsterisks()
        {
            var html = "This is a <em>sample</em> paragraph";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsITag_ThenConvertToMarkdownSingleAsterisks()
        {
            var html = "This is a <i>sample</i> paragraph";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsEncompassingEmOrITag_ThenConvertToMarkdownSingleAsterisks_AnyEmOrITagsInsideAreIgnored()
        {
            var html = "<em>This is a <span><i>sample</i></span> paragraph<em>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsBreakTag_ThenConvertToMarkdownDoubleSpacesCarriageReturn()
        {
            var html = "This is a paragraph.<br />This line appears after break.";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsCodeTag_ThenConvertToMarkdownWithBackTick()
        {
            var html = "This text has code <code>alert();</code>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsH1Tag_ThenConvertToMarkdownHeader()
        {
            var html = "This text has <h1>header</h1>. This text appear after header.";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsH2Tag_ThenConvertToMarkdownHeader()
        {
            var html = "This text has <h2>header</h2>. This text appear after header.";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsH3Tag_ThenConvertToMarkdownHeader()
        {
            var html = "This text has <h3>header</h3>. This text appear after header.";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsH4Tag_ThenConvertToMarkdownHeader()
        {
            var html = "This text has <h4>header</h4>. This text appear after header.";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsH5Tag_ThenConvertToMarkdownHeader()
        {
            var html = "This text has <h5>header</h5>. This text appear after header.";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsH6Tag_ThenConvertToMarkdownHeader()
        {
            var html = "This text has <h6>header</h6>. This text appear after header.";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsHeadingInsideTable_ThenIgnoreHeadingLevel()
        {
            var html =
                $"<table>{Environment.NewLine}<tr><th><h2>Heading <strong>text</strong></h2></th></tr>{Environment.NewLine}<tr><td>Content</td></tr>{Environment.NewLine}</table>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsBlockquoteTag_ThenConvertToMarkdownBlockquote()
        {
            var html = "This text has <blockquote>blockquote</blockquote>. This text appear after header.";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsEmptyBlockquoteTag_ThenConvertToMarkdownBlockquote()
        {
            var html = "This text has <blockquote></blockquote>. This text appear after header.";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsParagraphTag_ThenConvertToMarkdownDoubleLineBreakBeforeAndAfter()
        {
            var html = "This text has markup <p>paragraph.</p> Next line of text";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsHorizontalRule_ThenConvertToMarkdownHorizontalRule()
        {
            var html = "This text has horizontal rule.<hr/>Next line of text";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsImgTag_ThenConvertToMarkdownImage()
        {
            var html =
                @"This text has image <img alt=""alt"" title=""title"" src=""http://test.com/images/test.png""/>. Next line of text";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsImgTagWithoutTitle_ThenConvertToMarkdownImageWithoutTitle()
        {
            var html =
                @"This text has image <img alt=""alt"" src=""http://test.com/images/test.png""/>. Next line of text";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsImgTagWithoutAltText_ThenConvertToMarkdownImageWithoutAltText()
        {
            var html =
                @"This text has image <img src=""http://test.com/images/test.png""/>. Next line of text";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsImgTagWithMultilineAltText_ThenEnsureNoBlankLinesInMarkdownAltText()
        {
            var html =
                $@"This text has image <img alt=""cat{Environment.NewLine}{Environment.NewLine}dog"" src=""http://test.com/images/test.png""/>. Next line of text";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsImgTagWithBracesInAltText_ThenEnsureAltTextIsEscapedInMarkdown()
        {
            var html =
                @"This text has image <img alt=""a]b"" src=""http://test.com/images/test.png""/>. Next line of text";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsImgTag_SchemeNotWhitelisted_ThenEmptyOutput()
        {
            return CheckConversion(
                html: @"<img src=""data:image/gif;base64,R0lGODlhEAAQ...""/>",
                config: new Config
                {
                    WhitelistUriSchemes = new[] {"http"}
                }
            );
        }

        [Fact]
        public Task WhenThereIsImgTag_SchemeIsWhitelisted_ThenConvertToMarkdown()
        {
            return CheckConversion(
                html: @"<img src=""data:image/gif;base64,R0lGODlhEAAQ...""/>",
                config: new Config()
                {
                    WhitelistUriSchemes = new[] {"data"}
                }
            );
        }

        [Fact]
        public Task WhenThereIsImgTagAndSrcWithNoSchema_WhitelistedEmptyString_ThenConvertToMarkdown()
        {
            return CheckConversion(
                html: @"<img src=""example.com""/>",
                config: new Config()
                {
                    WhitelistUriSchemes = new[] {""}
                }
            );
        }

        [Fact]
        public Task WhenThereIsImgTagAndSrcWithNoSchema_NotWhitelisted_ThenConvertToPlain()
        {
            return CheckConversion(
                html: @"<img src=""data:image/gif;base64,R0lGODlhEAAQ...""/>",
                config: new Config()
                {
                    WhitelistUriSchemes = new[] {"whatever"}
                }
            );
        }

        [Fact]
        public Task WhenThereIsImgTagWithRelativeUrl_NotWhitelisted_ThenConvertToMarkdown()
        {
            return CheckConversion(
                html: @"<img src=""/example.gif""/>",
                config: new Config()
                {
                    WhitelistUriSchemes = new[] {"data"}
                }
            );
        }

        [Fact]
        public Task WhenThereIsImgTagWithUnixUrl_ConfigHasWhitelist_ThenConvertToMarkdown()
        {
            return CheckConversion(
                html: @"<img src=""/example.gif""/>",
                config: new Config()
                {
                    WhitelistUriSchemes = new[] {"file"}
                }
            );
        }

        [Fact]
        public Task WhenThereIsImgTagWithHttpProtocolRelativeUrl_ConfigHasWhitelist_ThenConvertToMarkdown()
        {
            var html = @"<img src=""//example.gif""/>";
            var config = new Config
            {
                WhitelistUriSchemes = new[] {"http"}
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task WhenThereIsPreTag_ThenConvertToMarkdownPre()
        {
            var html = "This text has pre tag content <pre>Predefined text</pre>Next line of text";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsEmptyPreTag_ThenConvertToMarkdownPre()
        {
            var html = "This text has pre tag content <pre><br/ ></pre>Next line of text";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsEmptyPreTag_ThenConvertToMarkdownPre_GFM()
        {
            var html = "This text has pre tag content <pre><br/ ></pre>Next line of text";
            return CheckConversion(html, new Config {GithubFlavored = true});
        }

        [Fact]
        public Task WhenThereIsUnorderedList_ThenConvertToMarkdownList()
        {
            var html = "This text has unordered list.<ul><li>Item1</li><li>Item2</li></ul>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsUnorderedListAndBulletIsAsterisk_ThenConvertToMarkdownList()
        {
            var html = "This text has unordered list.<ul><li>Item1</li><li>Item2</li></ul>";
            return CheckConversion(html, new Config {ListBulletChar = '*'});
        }

        [Fact]
        public Task WhenThereIsInputListWithGithubFlavoredEnabled_ThenConvertToMarkdownCheckList()
        {
            var html = "<ul><li><input type=\"checkbox\" disabled> Unchecked</li><li><input type=\"checkbox\" checked> Checked</li></ul>";

            var config = new Config
            {
                GithubFlavored = true,
            };

            return CheckConversion(html, config);
        }

        [Fact]
        public Task WhenThereIsInputListWithGithubFlavoredDisabled_ThenConvertToTypicalMarkdownList()
        {
            var html = "<ul><li><input type=\"checkbox\" disabled> Unchecked</li><li><input type=\"checkbox\" checked> Checked</li></ul>";

            var config = new Config
            {
                GithubFlavored = false,
            };

            return CheckConversion(html, config);
        }

        [Fact]
        public Task WhenThereIsOrderedList_ThenConvertToMarkdownList()
        {
            var html = "This text has ordered list.<ol><li>Item1</li><li>Item2</li></ol>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsOrderedListWithNestedUnorderedList_ThenConvertToMarkdownListWithNestedList()
        {
            var html =
                "This text has ordered list.<ol><li>OuterItem1<ul><li>InnerItem1</li><li>InnerItem2</li></ul></li><li>Item2</li></ol>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsUnorderedListWithNestedOrderedList_ThenConvertToMarkdownListWithNestedList()
        {
            var html =
                "This text has ordered list.<ul><li>OuterItem1<ol><li>InnerItem1</li><li>InnerItem2</li></ol></li><li>Item2</li></ul>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenThereIsWhitespaceAroundNestedLists_PreventBlankLinesWhenConvertingToMarkdownList()
        {
            var html = $"<ul>{Environment.NewLine}    ";
            html +=
                $"    <li>OuterItem1{Environment.NewLine}        <ol>{Environment.NewLine}            <li>InnerItem1</li>{Environment.NewLine}        </ol>{Environment.NewLine}    </li>{Environment.NewLine}";
            html += $"    <li>Item2</li>{Environment.NewLine}";
            html +=
                $"    <ol>{Environment.NewLine}        <li>InnerItem2</li>{Environment.NewLine}    </ol>{Environment.NewLine}";
            html += $"    <li>Item3</li>{Environment.NewLine}";
            html += "</ul>";

            return CheckConversion(html);
        }

        [Fact]
        public Task
            WhenListItemTextContainsLeadingAndTrailingSpacesAndTabs_ThenConvertToMarkdownListItemWithSpacesAndTabsStripped()
        {
            var html = @"<ol><li>	    This is a text with leading and trailing spaces and tabs		</li></ol>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenListContainsNewlineAndTabBetweenTagBorders_CleanupAndConvertToMarkdown()
        {
            var html =
                $"<ol>{Environment.NewLine}\t<li>{Environment.NewLine}\t\t<strong>Item1</strong></li>{Environment.NewLine}\t<li>{Environment.NewLine}\t\tItem2</li></ol>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenListContainsMultipleParagraphs_ConvertToMarkdownAndIndentSiblings()
        {
            var html =
                @"<ol>
	<li>
		<p>Paragraph 1</p>
        <p>Paragraph 1.1</p>
        <p>Paragraph 1.2</p></li>
	<li>
		<p>Paragraph 3</p></li></ol>";

            return CheckConversion(html);
        }

        [Fact]
        public Task WhenListContainsParagraphsOutsideItems_ConvertToMarkdownAndIndentSiblings()
        {
            var html =
                @"<ol>
	<li>Item1</li>
	<p>Item 1 additional info</p>
	<li>Item2</li>
</ol>";

            return CheckConversion(html);
        }

        [Fact]
        public Task When_OrderedListIsInTable_LeaveListAsHtml()
        {
            var html = "<table><tr><th>Heading</th></tr><tr><td><ol><li>Item1</li></ol></td></tr></table>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_UnorderedListIsInTable_LeaveListAsHtml()
        {
            var html = "<table><tr><th>Heading</th></tr><tr><td><ul><li>Item1</li></ul></td></tr></table>";
            return CheckConversion(html);
        }

        [Fact]
        public Task Check_Converter_With_Unknown_Tag_ByPass_Option()
        {
            var html = "<unknown-tag>text in unknown tag</unknown-tag>";
            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Bypass
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task WhenStyletagWithBypassOption_ReturnEmpty()
        {
            var html = @"<body><style type=""text/css"">.main {background-color: #ffffff;}</style></body>";
            var config = new Config()
            {
                UnknownTags = Config.UnknownTagsOption.Bypass
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task Check_Converter_With_Unknown_Tag_Drop_Option()
        {
            var html = "<unknown-tag>text in unknown tag</unknown-tag><p>paragraph text</p>";
            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Drop
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task Check_Converter_With_Unknown_Tag_PassThrough_Option()
        {
            var html = "<unknown-tag>text in unknown tag</unknown-tag><p>paragraph text</p>";
            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.PassThrough
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task Check_Converter_With_Unknown_Tag_Raise_Option()
        {
            var html = "<unknown-tag>text in unknown tag</unknown-tag><p>paragraph text</p>";
            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Raise
            };
            var converter = new Converter(config);
            return Verifier.Throws(() => converter.Convert(html), settings: _verifySettings)
                .IgnoreMember<Exception>(e => e.StackTrace);
        }

        [Fact]
        public Task WhenTable_ThenConvertToGFMTable()
        {
            var html =
                "<table><tr><th>col1</th><th>col2</th><th>col3</th></tr><tr><td>data1</td><td>data2</td><td>data3</td></tr></table>";

            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Bypass
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task
            WhenTable_WithoutHeaderRow_With_TableWithoutHeaderRowHandlingOptionEmptyRow_ThenConvertToGFMTable_WithEmptyHeaderRow()
        {
            var html =
                "<table><tr><td>data1</td><td>data2</td><td>data3</td></tr><tr><td>data4</td><td>data5</td><td>data6</td></tr></table>";
            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Bypass,
                TableWithoutHeaderRowHandling = Config.TableWithoutHeaderRowHandlingOption.EmptyRow
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task
            WhenTable_WithoutHeaderRow_With_TableWithoutHeaderRowHandlingOptionDefault_ThenConvertToGFMTable_WithFirstRowAsHeaderRow()
        {
            var html =
                "<table><colgroup><col><col><col></colgroup><tr><td>data1</td><td>data2</td><td>data3</td></tr><tr><td>data4</td><td>data5</td><td>data6</td></tr></table>";
            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Bypass,
                // TableWithoutHeaderRowHandling = Config.TableWithoutHeaderRowHandlingOption.Default - this is default
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task WhenTable_Cell_Content_WithNewline_Add_BR_ThenConvertToGFMTable()
        {
            var html =
                $"<table><tr><th>col1</th><th>col2</th><th>col3</th></tr><tr><td>data line1{Environment.NewLine}line2</td><td>data2</td><td>data3</td></tr></table>";
            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Bypass
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task WhenTable_CellContainsParagraph_AddBrThenConvertToGFMTable()
        {
            var html = "<table><tr><th>col1</th></tr><tr><td><p>line1</p><p>line2</p></td></tr></table>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenTable_ContainsTheadTh_ConvertToGFMTable()
        {
            var html =
                "<table><thead><tr><th>col1</th><th>col2</th></tr></thead><tbody><tr><td>data1</td><td>data2</td></tr><tbody></table>";
            var config = new Config
            {
                GithubFlavored = true,
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task WhenTable_ContainsTheadTd_ConvertToGFMTable()
        {
            var html =
                "<table><thead><tr><td>col1</td><td>col2</td></tr></thead><tbody><tr><td>data1</td><td>data2</td></tr><tbody></table>";
            var config = new Config
            {
                GithubFlavored = true,
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task WhenTable_CellContainsBr_PreserveBrAndConvertToGFMTable()
        {
            var html = "<table><tr><th>col1</th></tr><tr><td>line 1<br>line 2</td></tr></table>";
            var config = new Config
            {
                GithubFlavored = true,
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task WhenTable_HasEmptyRow_DropsEmptyRow()
        {
            var html = "<table><tr><td>abc</td></tr><tr></tr></table>";
            var config = new Config
            {
                GithubFlavored = true,
                TableWithoutHeaderRowHandling = Config.TableWithoutHeaderRowHandlingOption.EmptyRow,
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_BR_With_GitHubFlavored_Config_ThenConvertToGFM_BR()
        {
            var html = "First part<br />Second part";
            var config = new Config
            {
                GithubFlavored = true
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_PRE_With_GitHubFlavored_Config_ThenConvertToGFM_PRE()
        {
            var html = "<pre>var test = 'hello world';</pre>";
            var config = new Config
            {
                GithubFlavored = true
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_PRE_With_Confluence_Lang_Class_Att_And_GitHubFlavored_Config_ThenConvertToGFM_PRE()
        {
            var html = @"<pre class=""brush: python;"">var test = 'hello world';</pre>";
            var config = new Config
            {
                GithubFlavored = true
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_PRE_With_Github_Site_DIV_Parent_And_GitHubFlavored_Config_ThenConvertToGFM_PRE()
        {
            var html = @"<div class=""highlight highlight-source-csharp""><pre>var test = ""hello world"";</pre></div>";
            var config = new Config
            {
                GithubFlavored = true
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_PRE_With_HighlightJs_Lang_Class_Att_And_GitHubFlavored_Config_ThenConvertToGFM_PRE()
        {
            var html = @"<pre><code class=""hljs language-csharp"">var test = ""hello world"";</code></pre>";
            var config = new Config
            {
                GithubFlavored = true
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_PRE_With_Lang_Highlight_Class_Att_And_GitHubFlavored_Config_ThenConvertToGFM_PRE()
        {
            var html = @"<pre class=""highlight-python"">var test = 'hello world';</pre>";
            var config = new Config
            {
                GithubFlavored = true
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task WhenRemovedCommentsIsEnabled_CommentsAreRemoved()
        {
            var html =
                "Hello there <!-- This is a HTML comment block which will be removed! --><!-- This wont be removed because it is incomplete";

            var config = new Config
            {
                RemoveComments = true
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task WhenThereAreLineBreaksEncompassingParagraphText_It_Should_be_Removed()
        {
            var html = $"<p>{Environment.NewLine}Some text goes here.{Environment.NewLine}</p>";
            return CheckConversion(html);
        }

        [Fact]
        public Task TestConversionOfMultiParagraphWithHeaders()
        {
            var html = "<h1>Heading1</h1><p>First paragraph.</p><h1>Heading2</h1><p>Second paragraph.</p>";
            return CheckConversion(html);
        }

        [Fact]
        public void TestConversionWithPastedHtmlContainingUnicodeSpaces()
        {
            var html =
                @"<span style=""color: rgb(36, 41, 46); font-family: -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Helvetica, Arial, sans-serif, &quot;Apple Color Emoji&quot;, &quot;Segoe UI Emoji&quot;, &quot;Segoe UI Symbol&quot;; font-size: 16px; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; background-color: rgb(255, 255, 255); text-decoration-style: initial; text-decoration-color: initial; display: inline !important; float: none;"">Markdown Monster is an easy to use and extensible<span> </span></span><strong style=""box-sizing: border-box; font-weight: 600; color: rgb(36, 41, 46); font-family: -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Helvetica, Arial, sans-serif, &quot;Apple Color Emoji&quot;, &quot;Segoe UI Emoji&quot;, &quot;Segoe UI Symbol&quot;; font-size: 16px; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; background-color: rgb(255, 255, 255); text-decoration-style: initial; text-decoration-color: initial;"">Markdown Editor</strong><span style=""color: rgb(36, 41, 46); font-family: -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Helvetica, Arial, sans-serif, &quot;Apple Color Emoji&quot;, &quot;Segoe UI Emoji&quot;, &quot;Segoe UI Symbol&quot;; font-size: 16px; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; background-color: rgb(255, 255, 255); text-decoration-style: initial; text-decoration-color: initial; display: inline !important; float: none;"">,<span> </span></span><strong style=""box-sizing: border-box; font-weight: 600; color: rgb(36, 41, 46); font-family: -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Helvetica, Arial, sans-serif, &quot;Apple Color Emoji&quot;, &quot;Segoe UI Emoji&quot;, &quot;Segoe UI Symbol&quot;; font-size: 16px; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; background-color: rgb(255, 255, 255); text-decoration-style: initial; text-decoration-color: initial;"">Viewer</strong><span style=""color: rgb(36, 41, 46); font-family: -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Helvetica, Arial, sans-serif, &quot;Apple Color Emoji&quot;, &quot;Segoe UI Emoji&quot;, &quot;Segoe UI Symbol&quot;; font-size: 16px; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; background-color: rgb(255, 255, 255); text-decoration-style: initial; text-decoration-color: initial; display: inline !important; float: none;""><span> </span>and<span> </span></span><strong style=""box-sizing: border-box; font-weight: 600; color: rgb(36, 41, 46); font-family: -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Helvetica, Arial, sans-serif, &quot;Apple Color Emoji&quot;, &quot;Segoe UI Emoji&quot;, &quot;Segoe UI Symbol&quot;; font-size: 16px; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; background-color: rgb(255, 255, 255); text-decoration-style: initial; text-decoration-color: initial;"">Weblog Publisher</strong><span style=""color: rgb(36, 41, 46); font-family: -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Helvetica, Arial, sans-serif, &quot;Apple Color Emoji&quot;, &quot;Segoe UI Emoji&quot;, &quot;Segoe UI Symbol&quot;; font-size: 16px; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; background-color: rgb(255, 255, 255); text-decoration-style: initial; text-decoration-color: initial; display: inline !important; float: none;""><span> </span>for Windows. Our goal is to provide the best Markdown specific editor for Windows and make it as easy as possible to create Markdown documents. We provide a core editor and previewer, and a number of non-intrusive helpers to help embed content like images, links, tables, code and more into your documents with minimal effort.</span>";

            var config = new Config
            {
                GithubFlavored = true,
                UnknownTags =
                    Config.UnknownTagsOption
                        .PassThrough, // Include the unknown tag completely in the result (default as well)
                SmartHrefHandling = true // remove markdown output for links where appropriate
            };
            var converter = new Converter(config);
            var expected = converter.Convert(html);

            _testOutputHelper.WriteLine("Below is the generated markdown:");
            _testOutputHelper.WriteLine(expected);

            Assert.Contains("and **Weblog Publisher** for Windows", expected);
        }

        static Task CheckConversion(string html, Config config = null)
        {
            config = config ?? new Config();
            var converter = new Converter(config);
            var result = converter.Convert(html);
            var settings = new VerifySettings();
            settings.DisableRequireUniquePrefix();
            return Verifier.Verify(result, settings: settings, extension: "md");
        }

        [Fact]
        public Task When_InlineCode_Shouldnt_Contain_Encoded_Chars()
        {
            var html = "This is inline code: <code>&lt;AspNetCoreHostingModel&gt;</code>.";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_FencedCodeBlocks_Shouldnt_Have_Trailing_Line()
        {
            var html =
                $@"<pre><code class=""language-xml hljs""><span class=""hljs-tag"">&lt;<span class=""hljs-name"">AspNetCoreHostingModel</span>&gt;</span>InProcess<span class=""hljs-tag"">&lt;/<span class=""hljs-name"">AspNetCoreHostingModel</span>&gt;</span>{Environment.NewLine}</code></pre>";
            var config = new Config
            {
                GithubFlavored = true,
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_TextIsHtmlEncoded_DecodeText()
        {
            var html = "<p>cat&#39;s</p>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_TextContainsAngleBrackets_HexEscapeAngleBrackets()
        {
            var html = "<p>Value = &lt;Your text here&gt;</p>";
            return CheckConversion(html);
        }

        // [Fact]
        // public Task When_TextWithinParagraphContainsNewlineChars_ConvertNewlineCharsToSpace()
        // {
        //     // note that the string also has a tab space
        //     var html =
        //         $"<p>This service will be{Environment.NewLine}temporarily unavailable due to planned maintenance{Environment.NewLine}from 02:00-04:00 on 30/01/2020</p>";
        //     return CheckConversion(html);
        // }

        [Fact]
        public Task WhenTableCellsWithP_ThenDoNotAddNewlines()
        {
            var html =
                "<html><body><table><tbody><tr><td><p>col1</p></td><td><p>col2</p></td></tr><tr><td><p>data1</p></td><td><p>data2</p></td></tr></tbody></table></body></html>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenTableCellsWithDiv_ThenDoNotAddNewlines()
        {
            var html =
                "<html><body><table><tbody><tr><td><div>col1</div></td><td><div>col2</div></td></tr><tr><td><div>data1</div></td><td><div>data2</div></td></tr></tbody></table></body></html>";
            return CheckConversion(html);
        }

        // [Fact]
        // public Task WhenTableCellsWithPWithMarkupNewlines_ThenRenderBr()
        // {
        //     var html =
        //         $"<html><body><table><tbody>{Environment.NewLine}\t<tr>{Environment.NewLine}\t\t<td>{Environment.NewLine}\t\t\t<p>{Environment.NewLine}col1{Environment.NewLine}</p>{Environment.NewLine}\t\t</td>{Environment.NewLine}\t<tr>{Environment.NewLine}\t\t<td>{Environment.NewLine}\t\t\t<p>{Environment.NewLine}data1{Environment.NewLine}</p>{Environment.NewLine}\t\t</td>\t</tr></tbody></table></body></html>";
        //
        //     return CheckConversion(html);
        // }

        [Fact]
        public Task WhenTableCellsWithP_ThenNoNewlines()
        {
            var html = "<table><tr><td><p>data1</p></td></tr></table>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenTableCellsWithMultipleP_ThenNoNewlines()
        {
            var html = "<table><tr><td><p>p1</p><p>p2</p></td></tr></table>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenTableCellsWithDataAndP_ThenNewlineBeforeP()
        {
            var html = "<table><tr><td>data1<p>p</p></td></tr></table>";

            return CheckConversion(html);
        }

        [Fact(Skip =
            "Issue 61. Unclosed CDATA tags are invalid and HtmlAgilityPack won't parse it correctly. Browsers doesn't parse them correctly too.")]
        public Task WhenUnclosedStyleTag_WithBypassUnknownTags_ThenConvertToMarkdown()
        {
            var html = "<html><head><style></head><body><p>Test content</p></body></html>";

            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Bypass
            };
            return CheckConversion(html, config);
        }

        [Fact(Skip =
            "Issue 61. Unclosed CDATA tags are invalid and HtmlAgilityPack won't parse it correctly. Browsers doesn't parse them correctly too.")]
        public Task WhenUnclosedScriptTag_WithBypassUnknownTags_ThenConvertToMarkdown()
        {
            var html = "<html><body><script><p>Test content</p></body></html>";

            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Bypass
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task WhenCommentOverlapTag_WithRemoveComments_ThenDoNotStripContentBetweenComments()
        {
            var html = "<p>test <!-- comment -->content<!-- another comment --></p>";

            var config = new Config
            {
                RemoveComments = true
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task WhenBoldTagContainsBRTag_ThenConvertToMarkdown()
        {
            var html = "test<b><br/>test</b>";
            return CheckConversion(html);
        }

        [Fact]
        public Task WhenAnchorTagContainsImgTag_LinkTextShouldNotBeEscaped()
        {
            var html = "<a href=\"https://www.example.com\"><img src=\"https://example.com/image.jpg\"/></a>";
            return CheckConversion(html);
        }

        [Fact]
        public Task
            When_PRE_Without_Lang_Marker_Class_Att_And_GitHubFlavored_Config_With_DefaultCodeBlockLanguage_ThenConvertToGFM_PRE()
        {
            var html = @"<pre>var test = ""hello world"";</pre>";
            var config = new Config
            {
                GithubFlavored = true,
                DefaultCodeBlockLanguage = "csharp"
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task
            When_PRE_With_Parent_DIV_And_Non_GitHubFlavored_Config_FirstLine_CodeBlock_SpaceIndent_Should_Be_Retained()
        {
            var html = @"<div><pre>var test = ""hello world"";</pre></div>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_Converting_HTML_Ensure_To_Process_Only_Body()
        {
            var html =
                "<!DOCTYPE html><html lang=\"en\"><head><script>var x = 1;</script></head><body>sample text</body>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_Html_Containing_Nested_DIVs_Process_ONLY_Inner_Most_DIV()
        {
            var html = "<div><div>sample text</div></div>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_SingleChild_BlockTag_With_Parent_DIV_Ignore_Processing_DIV()
        {
            var html = "<div><p>sample text</p></div>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_Table_Within_List_Should_Be_Indented()
        {
            var html =
                "<ol><li>Item1</li><li>Item2<table><tr><th>col1</th><th>col2</th><th>col3</th></tr><tr><td>data1</td><td>data2</td><td>data3</td></tr></table></li><li>Item3</li></ol>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_Tag_In_PassThoughTags_List_Then_Use_PassThroughConverter()
        {
            var html =
                @"This text has image <img alt=""alt"" src=""http://test.com/images/test.png"">. Next line of text";
            var config = new Config
            {
                PassThroughTags = new[] {"img"}
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_CodeContainsSpaces_ShouldPreserveSpaces()
        {
            var html = "A JavaScript<code> function </code>...";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_CodeContainsSpanWithExtraSpaces_Should_NotNormalizeSpaces()
        {
            var html = "A JavaScript<code><span>    function  </span></code>...";
            return CheckConversion(html);
        }


        [Fact]
        public Task When_CodeContainsSpacesAndIsSurroundedByWhitespace_Should_NotRemoveSpaces()
        {
            var html = "A JavaScript <code> function </code> ...";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_PreTag_Contains_IndentedFirstLine_Should_PreserveIndentation()
        {
            var html = "<pre><code>    function foo {</code></pre>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_PreTag_Contains_IndentedFirstLine_Should_PreserveIndentation_GFM()
        {
            var html = "<pre><code>    function foo {</code></pre>";

            var config = new Config
            {
                GithubFlavored = true
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_PreTag_Within_List_Should_Be_Indented()
        {
            var html =
                $"<ol><li>Item1</li><li>Item2 <pre> test<br>{Environment.NewLine}  test</pre></li><li>Item3</li></ol>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_PreTag_Within_List_Should_Be_Indented_With_GitHub_FlavouredMarkdown()
        {
            var html =
                $"<ol><li>Item1</li><li>Item2 <pre> test<br>{Environment.NewLine}  test</pre></li><li>Item3</li></ol>";

            var config = new Config
            {
                GithubFlavored = true
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_Text_Contains_NewLineChars_Should_Not_Convert_To_BR()
        {
            var html = "<p><span>line 1<br><span/><span>line 2<br><span/></div>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_Text_Contains_NewLineChars_Should_Not_Convert_To_BR_GitHub_Flavoured()
        {
            var html = "<p><span>line 1<br><span/><span>line 2<br><span/></div>";
            return CheckConversion(html, new Config
            {
                GithubFlavored = true
            });
        }

        [Fact]
        public Task When_Consecutive_Strong_Tags_Should_Convert_Properly()
        {
            var html = "<Strong>block1</strong><Strong>block2</strong><b>block3</b><b>block4</b>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_Consecutive_Em_Tags_Should_Convert_Properly()
        {
            var html = "<em>block1</em><em>block2</em><i>block3</i><em>block4</em>";
            return CheckConversion(html);
        }

        [Fact]
        public Task Li_With_No_Parent()
        {
            var html = "<li>item</li>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_Span_with_newline_Should_Convert_Properly()
        {
            var html = $"<b>2 sets</b><span>{Environment.NewLine}</span><span>30 mountain climbers</span>";
            return CheckConversion(html);
        }

        [Fact]
        public Task Bug255_table_newline_char_issue()
        {
            var html =
                $"<table><thead>{Environment.NewLine}<tr>{Environment.NewLine}<th style=\"text-align: left;\">Progression</th>{Environment.NewLine}<th style=\"text-align: left;\">Focus</th>{Environment.NewLine}</tr>{Environment.NewLine}</thead></table>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_Content_Contains_script_tags_ignore_it()
        {
            var html =
                $"<div><script>var test = 10;</script><p>simple paragraph</p></div><script>var test2 = 20;</script>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_DescriptionListTag_ThenConvertToMarkdown_List()
        {
            var html =
                "<dl><dt>Coffee</dt><dd>Filter Coffee</dd><dd>Hot Black Coffee</dd><dt>Milk</dt><dd>White Cold Drink</dd></dl>";
            return CheckConversion(html);
        }

        [Fact]
        public Task Bug294_Table_bug_with_row_superfluous_newlines()
        {
            var html = @"<table>
<thead>
<tr>
<th>比较</th>
<th>wordpress</th>
<th>hexo &amp; hugo</th>
</tr>
</thead>
<tbody>
<tr>
<td>搭建要求</td>
<td>一台服务器以及运行环境</td>
<td>静态生成页面，无需服务器。</td>
</tr>
<tr>
<td>性能</td>
<td>由于是动态生成页面，可以通过自行配置提高性能，但是仍然无法媲美静态页面</td>
<td>几乎无需考虑性能问题</td>
</tr>
<tr>
<td>访问速度</td>
<td>依赖于服务器配置以及cdn加速。</td>
<td>只需考虑cdn加速</td>
</tr>
<tr>
<td>功能完善</td>
<td>作为强大的cms功能很完善，需要的功能基本可以插件下载直接实现。</td>
<td>额外功能也可以通过插件实现，不过稍微需要自行查找以及diy</td>
</tr>
<tr>
<td>后台管理</td>
<td>现成的后台管理功能，开箱即用</td>
<td>由于静态博客，本身没有后台管理，有需求需要自行搜索实现</td>
</tr>
</tbody>
</table>";

            return CheckConversion(html);
        }
        
        [Fact]
        public Task WhenTableHeadingWithAlignmentStyles_ThenTableHeaderShouldHaveProperAlignment()
        {
            var html =
                $"<table><tr><th style=\"text-align:left\">Col1</th><th style=\"text-align:center\">Col2</th><th style=\"text-align:right\">Col2</th></tr><tr><td>1</td><td>2</td><td>3</td></tr></table>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_Sup_And_Nested_Sup()
        {
            var html = $"This is the 1<sup>st</sup> sentence to t<sup>e<sup>s</sup></sup>t the sup tag conversion";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_Anchor_Text_with_Underscore_Do_Not_Escape()
        {
            var html = $"This a sample <strong>paragraph</strong> from <a href=\"https://www.w3schools.com/html/mov_bbb.mp4\">https://www.w3schools.com/html/mov_bbb.mp4</a>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_Strikethrough_And_Nested_Strikethrough()
        {
            var html = $"This is the 1<s>st</s> sentence to t<del>e<strike>s</strike></strike>t the strikethrough tag conversion";
            return CheckConversion(html);
        }
        
        [Fact]
        public Task When_Spaces_In_Inline_Tags_Should_Be_Retained()
        {
            var html = $"... example html <i>code </i>block";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_SuppressNewlineFlag_PrefixDiv_Should_Be_Empty()
        {
            var html = $"<div>the</div><div>fox</div><div>jumps</div><div>over</div>";
            return CheckConversion(html, new Config
            {
                SuppressDivNewlines = true
            });
        }

        [Fact]
        public Task WhenTable_WithColSpan_TableHeaderColumnSpansHandling_ThenConvertToGFMTable()
        {
            var html =
                "<table><tr><th>col1</th><th colspan=\"2\">col2</th><th>col3</th></tr><tr><td>data1</td><td>data2.1</td><td>data2.2</td><td>data3</td></tr></table>";

            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Bypass
            };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task Bug391_AnchorTagUnnecessarilyIndented()
        {
            var html =
                "<p>\n\n</p>\n\n\n<div style=\"white-space: pre-line\" class=\"alert alert-warning\">\nAn error occurred while importing data from feed 'FBA Producten'. More details can be found in the latest <a href=\"<REDACTED>\">feed validation report</a>.\n</div>\n\n\n\n\n<a href=\"<REDACTED>\" class=\"btn btn-primary btn-sm my-2\" target=\"_blank\">View feed 4</a>";
            var config = new Config
            {
                GithubFlavored = true,
            };
            
            return CheckConversion(html, config);
        }

        [Fact]
        public Task Bug393_RegressionWithVaryingNewLines()
        {
            const string html = "This is regular text\r\n<p class=\"c1\">This is HTML: <ul><li>Line 1</li><li>Line 2</li><li><mark>Line 3 has an unknown tag</mark></li></ul></p>";
            var config = new Config { UnknownTags = Config.UnknownTagsOption.Bypass, ListBulletChar = '*' };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task SlackFlavored_Bold()
        {
            const string html = "<b>test</b> | <strong>test</strong>";
            var config = new Config { SlackFlavored = true };
            return CheckConversion(html, config);
        }

        [Fact]
        public Task SlackFlavored_Italic()
        {
            const string html = "<i>test</i> | <em>test</em>";
            var config = new Config { SlackFlavored = true };
            return CheckConversion(html, config);
        }
        
        [Fact]
        public Task SlackFlavored_Strikethrough()
        {
            const string html = "<del>test</del>";
            var config = new Config { SlackFlavored = true };
            return CheckConversion(html, config);
        }
        
        [Fact]
        public Task SlackFlavored_Bullets()
        {
            const string html = "<ul>\n<li>Item 1</li>\n<li>Item 2</li>\n<li>Item 3</li>\n</ul>";
            var config = new Config { SlackFlavored = true };
            return CheckConversion(html, config);
        }
        
        [Fact]
        public void SlackFlavored_Unsupported_Hr()
        {
            const string html = "<hr/>";
            var config = new Config { SlackFlavored = true };
            var converter = new Converter(config);
            Assert.Throws<SlackUnsupportedTagException>(() => converter.Convert(html));
        }
        
        [Fact]
        public void SlackFlavored_Unsupported_Img()
        {
            const string html = "<img src=\"\"/>";
            var config = new Config { SlackFlavored = true };
            var converter = new Converter(config);
            Assert.Throws<SlackUnsupportedTagException>(() => converter.Convert(html));
        }
        
        [Fact]
        public void SlackFlavored_Unsupported_Sup()
        {
            const string html = "<sup>test</sup>";
            var config = new Config { SlackFlavored = true };
            var converter = new Converter(config);
            Assert.Throws<SlackUnsupportedTagException>(() => converter.Convert(html));
        }
        
        [Fact]
        public void SlackFlavored_Unsupported_Table()
        {
            const string html = "<table></table>";
            var config = new Config { SlackFlavored = true };
            var converter = new Converter(config);
            Assert.Throws<SlackUnsupportedTagException>(() => converter.Convert(html));
        }
        
        [Fact]
        public void SlackFlavored_Unsupported_Table_Td()
        {
            const string html = "<td></td>";
            var config = new Config { SlackFlavored = true };
            var converter = new Converter(config);
            Assert.Throws<SlackUnsupportedTagException>(() => converter.Convert(html));
        }
        
        [Fact]
        public void SlackFlavored_Unsupported_Table_Tr()
        {
            const string html = "<tr></tr>";
            var config = new Config { SlackFlavored = true };
            var converter = new Converter(config);
            Assert.Throws<SlackUnsupportedTagException>(() => converter.Convert(html));
        }
        
        [Fact]
        public Task Bug403_unexpectedBehaviourWhenTableBodyRowsWithTHCells()
        {
            var html = $"<table>{Environment.NewLine}<tr><th>Heading1</th><th>Heading2</th></tr>{Environment.NewLine}<tr><th>data 1</th><td>data 2</td></tr>{Environment.NewLine}<tr><th>data 3</th><td>data 4</td></tr>{Environment.NewLine}</table>";
            var config = new Config { UnknownTags = Config.UnknownTagsOption.Bypass, ListBulletChar = '*', GithubFlavored = true};
            return CheckConversion(html, config);
        }

        [Fact]
        public Task EscapeMarkdownCharsInTextProperly()
        {
            var html = "<span>[a-z]([0-9]){0,4}</span>";
            return CheckConversion(html);
        }
        
        [Fact]
        public Task Bug400_MissingSpanSpaceWithItalics()
        {
            var html = "<h3 data-reset-style=\"true\" data-anchor-id=\"8b5e184d-26f7-4d9a-80e0-bab2cd825457\"><i style=\"font-size: 14pt;\">What we thought:<span>&nbsp;</span></i><span style=\"color: rgb(41, 63, 77); font-size: 14pt; font-weight: normal;\">When we built Pages, we assumed that customers would use them like newsletters to share relevant, continually-updated information with field teams.</span><div style=\"text-align: left;\"><span style=\"line-height: 16px;\"><span><span height=\"18\" width=\"18\"><span></span></span><span></span></span></span></div></h3>";
            return CheckConversion(html);
        }

        [Fact]
        public Task When_NestedParagraphs_FiveLevelsDeep_ThenConvertCorrectly()
        {
            // Tests moderately nested <p> tags where HtmlAgilityPack creates nested structure
            var html = "<p>Level1<p>Level2<p>Level3<p>Level4<p>Level5</p></p></p></p></p>";
            
            var config = new Config
            {
                GithubFlavored = true,
                UnknownTags = Config.UnknownTagsOption.Bypass
            };
            
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_NestedSpans_FiveLevelsDeep_ThenConvertCorrectly()
        {
            // Tests moderately nested <span> tags to ensure span bypass converter handles nesting
            var html = "<span>Level1<span>Level2<span>Level3<span>Level4<span>Level5</span></span></span></span></span>";
            
            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Bypass
            };
            
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_InterleavedParagraphsAndSpans_ThenConvertCorrectly()
        {
            // Tests the interleaved <p><span> pattern common in malformed HTML
            var html = "<p><span>Text1<p><span>Text2<p><span>Text3<p><span>Text4</span></p></span></p></span></p>";
            
            var config = new Config
            {
                GithubFlavored = true,
                UnknownTags = Config.UnknownTagsOption.Bypass
            };
            
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_ManySequentialUnclosedParagraphs_ThenConvertCorrectly()
        {
            // Tests sequential unclosed <p> tags as found in user-generated content
            var html = "<p>Part1<p>Part2<p>Part3<p>Part4<p>Part5<p>Part6<p>Part7<p>Part8";
            
            var config = new Config
            {
                GithubFlavored = true,
                UnknownTags = Config.UnknownTagsOption.Bypass
            };
            
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_UnclosedParagraphsWithSpansAndTextNodes_ThenConvertCorrectly()
        {
            // Tests mixed content: properly closed tags, text nodes, and unclosed nested tags
            var html = @"<p><span>Intro</span></p> Filler text here. <p><span>Section1<p><span>Section2<p>Section3";
            
            var config = new Config
            {
                GithubFlavored = true,
                UnknownTags = Config.UnknownTagsOption.Bypass
            };
            
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_EmptyNestedParagraphs_ThenConvertCorrectly()
        {
            // Tests deeply nested empty <p> tags
            var html = "<p><p><p><p><p></p></p></p></p></p>";
            
            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Bypass
            };
            
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_AlternatingEmptyAndFilledNestedParagraphs_ThenConvertCorrectly()
        {
            // Tests combination of empty and filled nested <p> tags
            var html = "<p>A<p><p>B<p><p>C<p><p>D<p><p>E</p></p></p></p></p></p></p></p></p>";
            
            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Bypass
            };
            
            return CheckConversion(html, config);
        }

        [Fact]
        public Task When_NestedParagraphs_TenLevelsDeep_ThenConvertCorrectly()
        {
            // Tests deeper nesting (10 levels) to ensure performance remains linear after fix
            var html = "<p>L1<p>L2<p>L3<p>L4<p>L5<p>L6<p>L7<p>L8<p>L9<p>L10</p></p></p></p></p></p></p></p></p></p>";
            
            var config = new Config
            {
                GithubFlavored = true,
                UnknownTags = Config.UnknownTagsOption.Bypass
            };
            
            return CheckConversion(html, config);
        }
    }
}

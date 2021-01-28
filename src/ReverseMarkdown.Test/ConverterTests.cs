using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ReverseMarkdown.Test
{
    public class ConverterTests
    {
        public ITestOutputHelper Console { get; }
        private readonly ITestOutputHelper _testOutputHelper;

        public ConverterTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void WhenThereIsAsideTag()
        {
            const string html = @"<aside>This text is in an aside tag.</aside> This text appears after aside.";
            var expected =
                $"{Environment.NewLine}This text is in an aside tag.{Environment.NewLine} This text appears after aside.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsHtmlLink_ThenConvertToMarkdownLink()
        {
            const string html = @"This is <a href=""http://test.com"">a link</a>";
            const string expected = @"This is [a link](http://test.com)";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsHtmlLinkWithTitle_ThenConvertToMarkdownLink()
        {
            const string html = @"This is <a href=""http://test.com"" title=""with title"">a link</a>";
            const string expected = @"This is [a link](http://test.com ""with title"")";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereAreMultipleLinks_ThenConvertThemToMarkdownLinks()
        {
            const string html =
                @"This is <a href=""http://test.com"">first link</a> and <a href=""http://test1.com"">second link</a>";
            const string expected = @"This is [first link](http://test.com) and [second link](http://test1.com)";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsHtmlLinkNotWhitelisted_ThenBypass()
        {
            const string html =
                @"Leave <a href=""http://example.com"">http</a>, <a href=""https://example.com"">https</a>, <a href=""ftp://example.com"">ftp</a>, <a href=""ftps://example.com"">ftps</a>, <a href=""file://example.com"">file</a>. Remove <a href=""data:text/plain;charset=UTF-8;page=21,the%20data:1234,5678"">data</a>, <a href=""tel://example.com"">tel</a> and <a href=""whatever://example.com"">whatever</a>";
            const string expected =
                @"Leave [http](http://example.com), [https](https://example.com), [ftp](ftp://example.com), [ftps](ftps://example.com), [file](file://example.com). Remove data, tel and whatever";
            CheckConversion(html, expected, new Config()
            {
                WhitelistUriSchemes = new[] {"http", "https", "ftp", "ftps", "file"}
            });
        }

        [Fact]
        public void WhenThereHtmlWithHrefAndNoSchema_WhitelistedEmptyString_ThenConvertToMarkdown()
        {
            CheckConversion(
                html: @"<a href=""example.com"">yeah</a>",
                expected: @"[yeah](example.com)",
                config: new Config()
                {
                    WhitelistUriSchemes = new[] {""}
                }
            );
        }

        [Fact]
        public void WhenThereHtmlWithHrefAndNoSchema_NotWhitelisted_ThenConvertToPlain()
        {
            CheckConversion(
                html: @"<a href=""example.com"">yeah</a>",
                expected: @"yeah",
                config: new Config()
                {
                    WhitelistUriSchemes = new[] {"whatever"}
                }
            );
        }

        [Fact]
        public void WhenThereIsHtmlLinkWithDisallowedCharsInChildren_ThenEscapeTextInMarkdown()
        {
            CheckConversion(
                html: @"<a href=""http://example.com"">this ]( might break things</a>",
                expected: @"[this \]( might break things](http://example.com)",
                config: new Config()
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public void WhenThereIsHtmlLinkWithParensInHref_ThenEscapeHrefInMarkdown()
        {
            CheckConversion(
                html: @"<a href=""http://example.com?id=foo)bar"">link</a>",
                expected: @"[link](http://example.com?id=foo%29bar)",
                config: new Config()
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public void WhenThereIsHtmlWithProtocolRelativeUrlHrefAndNameNotMatching_SmartHandling_ThenConvertToMarkdown()
        {
            CheckConversion(
                html: @"<a href=""//example.com"">example.com</a>",
                expected: @"[example.com](//example.com)",
                config: new Config()
                {
                    SmartHrefHandling = true
                }
            );
        }


        [Fact]
        public void WhenThereIsHtmlWithHrefAndNameNotMatching_SmartHandling_ThenConvertToMarkdown()
        {
            CheckConversion(
                html: @"<a href=""https://example.com"">Something intact</a>",
                expected: @"[Something intact](https://example.com)",
                config: new Config()
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public void WhenThereIsHtmlWithHrefAndNameMatching_SmartHandling_ThenConvertToPlain()
        {
            CheckConversion(
                html: @"<a href=""http://example.com/abc?x"">http://example.com/abc?x</a>",
                expected: @"http://example.com/abc?x",
                config: new Config()
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public void WhenThereIsHtmlWithHttpSchemeAndNameWithoutScheme_SmartHandling_ThenConvertToPlain()
        {
            CheckConversion(
                html: @"<a href=""http://example.com"">example.com</a>",
                expected: @"http://example.com",
                config: new Config()
                {
                    SmartHrefHandling = true
                }
            );

            CheckConversion(
                html: @"<a href=""https://example.com"">example.com</a>",
                expected: @"https://example.com",
                config: new Config()
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public void WhenThereIsHtmlWithMailtoSchemeAndNameWithoutScheme_SmartHandling_ThenConvertToPlain()
        {
            CheckConversion(
                html: @"<a href=""mailto:george@example.com"">george@example.com</a>",
                expected: @"george@example.com",
                config: new Config()
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public void WhenThereIsHtmlWithTelSchemeAndNameWithoutScheme_SmartHandling_ThenConvertToPlain()
        {
            CheckConversion(
                html: @"<a href=""tel:+1123-45678"">+1123-45678</a>",
                expected: @"+1123-45678",
                config: new Config()
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public void WhenThereIsHtmlLinkWithHttpSchemaAndNameWithout_SmartHandling_ThenOutputOnlyHref()
        {
            CheckConversion(
                html: @"<a href=""http://example.com"">example.com</a>",
                expected: @"http://example.com",
                config: new Config()
                {
                    SmartHrefHandling = true
                }
            );
            CheckConversion(
                html: @"<a href=""https://example.com"">example.com</a>",
                expected: @"https://example.com",
                config: new Config()
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public void WhenThereIsHtmlNonWellFormedLinkLink_SmartHandling_ThenConvertToMarkdown()
        {
            //The string is not correctly escaped.	
            CheckConversion(
                html: @"<a href=""http://example.com/path/file name.docx"">http://example.com/path/file name.docx</a>",
                expected: @"[http://example.com/path/file name.docx](http://example.com/path/file%20name.docx)",
                config: new Config()
                {
                    SmartHrefHandling = true
                });
            //The string is an absolute Uri that represents an implicit file Uri.	
            CheckConversion(
                html: @"<a href=""c:\\directory\filename"">	c:\\directory\filename</a>",
                expected: @"[c:\\directory\filename](c:\\directory\filename)",
                config: new Config()
                {
                    SmartHrefHandling = true
                });
            //The string is an absolute URI that is missing a slash before the path.	
            CheckConversion(
                html: @"<a href=""file://c:/directory/filename"">file://c:/directory/filename</a>",
                expected: @"[file://c:/directory/filename](file://c:/directory/filename)",
                config: new Config()
                {
                    SmartHrefHandling = true
                });
            //The string contains unescaped backslashes even if they are treated as forward slashes.	
            CheckConversion(
                html: @"<a href=""http:\\host/path/file"">http:\\host/path/file</a>",
                expected: @"[http:\\host/path/file](http:\\host/path/file)",
                config: new Config()
                {
                    SmartHrefHandling = true
                });
        }


        [Fact]
        public void WhenThereIsHtmlLinkWithoutHttpSchemaAndNameWithoutScheme_SmartHandling_ThenConvertToMarkdown()
        {
            CheckConversion(
                html: @"<a href=""ftp://example.com"">example.com</a>",
                expected: @"[example.com](ftp://example.com)",
                config: new Config()
                {
                    SmartHrefHandling = true
                }
            );
        }

        [Fact]
        public void WhenThereAreStrongTag_ThenConvertToMarkdownDoubleAsterisks()
        {
            const string html = @"This paragraph contains <strong>bold</strong> text";
            const string expected = @"This paragraph contains **bold** text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereAreBTag_ThenConvertToMarkdownDoubleAsterisks()
        {
            const string html = @"This paragraph contains <b>bold</b> text";
            const string expected = @"This paragraph contains **bold** text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void
            WhenThereIsEncompassingStrongOrBTag_ThenConvertToMarkdownDoubleAsterisks_AnyStrongOrBTagsInsideAreIgnored()
        {
            const string html =
                @"<strong>Paragraph is encompassed with strong tag and also has <b>bold</b> text words within it</strong>";
            const string expected =
                @"**Paragraph is encompassed with strong tag and also has bold text words within it**";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsSingleAsteriskInText_ThenConvertToMarkdownEscapedAsterisk()
        {
            const string html = @"This is a sample(*) paragraph";
            const string expected = @"This is a sample(\*) paragraph";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsEmTag_ThenConvertToMarkdownSingleAsterisks()
        {
            const string html = @"This is a <em>sample</em> paragraph";
            const string expected = @"This is a *sample* paragraph";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsITag_ThenConvertToMarkdownSingleAsterisks()
        {
            const string html = @"This is a <i>sample</i> paragraph";
            const string expected = @"This is a *sample* paragraph";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsEncompassingEmOrITag_ThenConvertToMarkdownSingleAsterisks_AnyEmOrITagsInsideAreIgnored()
        {
            const string html = @"<em>This is a <span><i>sample</i></span> paragraph<em>";
            const string expected = @"*This is a sample paragraph*";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsBreakTag_ThenConvertToMarkdownDoubleSpacesCarriageReturn()
        {
            const string html = @"This is a paragraph.<br />This line appears after break.";
            var expected = $"This is a paragraph.  {Environment.NewLine}This line appears after break.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsCodeTag_ThenConvertToMarkdownWithBackTick()
        {
            const string html = @"This text has code <code>alert();</code>";
            const string expected = @"This text has code `alert();`";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsH1Tag_ThenConvertToMarkdownHeader()
        {
            const string html = @"This text has <h1>header</h1>. This text appear after header.";
            var expected =
                $"This text has {Environment.NewLine}# header{Environment.NewLine}. This text appear after header.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsH2Tag_ThenConvertToMarkdownHeader()
        {
            const string html = @"This text has <h2>header</h2>. This text appear after header.";
            var expected =
                $"This text has {Environment.NewLine}## header{Environment.NewLine}. This text appear after header.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsH3Tag_ThenConvertToMarkdownHeader()
        {
            const string html = @"This text has <h3>header</h3>. This text appear after header.";
            var expected =
                $"This text has {Environment.NewLine}### header{Environment.NewLine}. This text appear after header.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsH4Tag_ThenConvertToMarkdownHeader()
        {
            const string html = @"This text has <h4>header</h4>. This text appear after header.";
            var expected =
                $"This text has {Environment.NewLine}#### header{Environment.NewLine}. This text appear after header.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsH5Tag_ThenConvertToMarkdownHeader()
        {
            const string html = @"This text has <h5>header</h5>. This text appear after header.";
            var expected =
                $"This text has {Environment.NewLine}##### header{Environment.NewLine}. This text appear after header.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsH6Tag_ThenConvertToMarkdownHeader()
        {
            const string html = @"This text has <h6>header</h6>. This text appear after header.";
            var expected =
                $"This text has {Environment.NewLine}###### header{Environment.NewLine}. This text appear after header.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsHeadingInsideTable_ThenIgnoreHeadingLevel()
        {
            string html = $"<table>{Environment.NewLine}<tr><th><h2>Heading <strong>text</strong></h2></th></tr>{Environment.NewLine}<tr><td>Content</td></tr>{Environment.NewLine}</table>";
            var expected =
                $"{Environment.NewLine}{Environment.NewLine}| Heading **text** |{Environment.NewLine}| --- |{Environment.NewLine}| Content |{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsBlockquoteTag_ThenConvertToMarkdownBlockquote()
        {
            const string html = @"This text has <blockquote>blockquote</blockquote>. This text appear after header.";
            var expected =
                $"This text has {Environment.NewLine}{Environment.NewLine}> blockquote{Environment.NewLine}{Environment.NewLine}. This text appear after header.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsEmptyBlockquoteTag_ThenConvertToMarkdownBlockquote()
        {
            const string html = @"This text has <blockquote></blockquote>. This text appear after header.";
            var expected =
                $"This text has {Environment.NewLine}{Environment.NewLine}{Environment.NewLine}. This text appear after header.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsParagraphTag_ThenConvertToMarkdownDoubleLineBreakBeforeAndAfter()
        {
            const string html = @"This text has markup <p>paragraph.</p> Next line of text";
            var expected =
                $"This text has markup {Environment.NewLine}paragraph.{Environment.NewLine} Next line of text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsHorizontalRule_ThenConvertToMarkdownHorizontalRule()
        {
            const string html = @"This text has horizontal rule.<hr/>Next line of text";
            var expected =
                $"This text has horizontal rule.{Environment.NewLine}* * *{Environment.NewLine}Next line of text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsImgTag_ThenConvertToMarkdownImage()
        {
            const string html =
                @"This text has image <img alt=""alt"" title=""title"" src=""http://test.com/images/test.png""/>. Next line of text";
            const string expected =
                @"This text has image ![alt](http://test.com/images/test.png ""title""). Next line of text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsImgTagWithoutTitle_ThenConvertToMarkdownImageWithoutTitle()
        {
            const string html =
                @"This text has image <img alt=""alt"" src=""http://test.com/images/test.png""/>. Next line of text";
            const string expected = @"This text has image ![alt](http://test.com/images/test.png). Next line of text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsImgTagWithoutAltText_ThenConvertToMarkdownImageWithoutAltText()
        {
            const string html =
                @"This text has image <img src=""http://test.com/images/test.png""/>. Next line of text";
            const string expected = @"This text has image ![](http://test.com/images/test.png). Next line of text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsImgTagWithMutlilineAltText_ThenEnsureNoBlankLinesInMarkdownAltText()
        {
            string html =
                $@"This text has image <img alt=""cat{Environment.NewLine}{Environment.NewLine}dog"" src=""http://test.com/images/test.png""/>. Next line of text";
            string expected = $@"This text has image ![cat{Environment.NewLine}dog](http://test.com/images/test.png). Next line of text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsImgTagWithBracesInAltText_ThenEnsureAltTextIsEscapedInMarkdown()
        {
            string html =
                $@"This text has image <img alt=""a]b"" src=""http://test.com/images/test.png""/>. Next line of text";
            string expected = $@"This text has image ![a\]b](http://test.com/images/test.png). Next line of text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsImgTag_SchemeNotWhitelisted_ThenEmptyOutput()
        {
            CheckConversion(
                html: @"<img src=""data:image/gif;base64,R0lGODlhEAAQ...""/>",
                expected: @"",
                config: new Config()
                {
                    WhitelistUriSchemes = new[] {"http"}
                }
            );
        }

        [Fact]
        public void WhenThereIsImgTag_SchemeIsWhitelisted_ThenConvertToMarkdown()
        {
            CheckConversion(
                html: @"<img src=""data:image/gif;base64,R0lGODlhEAAQ...""/>",
                expected: @"![](data:image/gif;base64,R0lGODlhEAAQ...)",
                config: new Config()
                {
                    WhitelistUriSchemes = new[] {"data"}
                }
            );
        }

        [Fact]
        public void WhenThereIsImgTagAndSrcWithNoSchema_WhitelistedEmptyString_ThenConvertToMarkdown()
        {
            CheckConversion(
                html: @"<img src=""example.com""/>",
                expected: @"![](example.com)",
                config: new Config()
                {
                    WhitelistUriSchemes = new[] {""}
                }
            );
        }

        [Fact]
        public void WhenThereIsImgTagAndSrcWithNoSchema_NotWhitelisted_ThenConvertToPlain()
        {
            CheckConversion(
                html: @"<img src=""data:image/gif;base64,R0lGODlhEAAQ...""/>",
                expected: @"",
                config: new Config()
                {
                    WhitelistUriSchemes = new[] {"whatever"}
                }
            );
        }

        [Fact]
        public void WhenThereIsImgTagWithRelativeUrl_NotWhitelisted_ThenConvertToMarkdown()
        {
            CheckConversion(
                html: @"<img src=""/example.gif""/>",
                expected: @"",
                config: new Config()
                {
                    WhitelistUriSchemes = new[] {"data"}
                }
            );
        }

        [Fact]
        public void WhenThereIsImgTagWithUnixUrl_ConfigHasWhitelist_ThenConvertToMarkdown()
        {
            CheckConversion(
                html: @"<img src=""/example.gif""/>",
                expected: @"![](/example.gif)",
                config: new Config()
                {
                    WhitelistUriSchemes = new[] {"file"}
                }
            );
        }

        [Fact]
        public void WhenThereIsImgTagWithHttpProtocolRelativeUrl_ConfigHasWhitelist_ThenConvertToMarkdown()
        {
            CheckConversion(
                html: @"<img src=""//example.gif""/>",
                expected: @"![](//example.gif)",
                config: new Config()
                {
                    WhitelistUriSchemes = new[] {"http"}
                }
            );
        }

        [Fact]
        public void WhenThereIsPreTag_ThenConvertToMarkdownPre()
        {
            const string html = @"This text has pre tag content <pre>Predefined text</pre>Next line of text";
            var expected =
                $"This text has pre tag content {Environment.NewLine}{Environment.NewLine}    Predefined text{Environment.NewLine}{Environment.NewLine}Next line of text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsEmptyPreTag_ThenIgnorePre()
        {
            const string html = @"This text has pre tag content <pre><br/ ></pre>Next line of text";
            var expected =
                $"This text has pre tag content Next line of text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsUnorderedList_ThenConvertToMarkdownList()
        {
            const string html = @"This text has unordered list.<ul><li>Item1</li><li>Item2</li></ul>";
            var expected =
                $"This text has unordered list.{Environment.NewLine}- Item1{Environment.NewLine}- Item2{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsUnorderedListAndBulletIsAsterisk_ThenConvertToMarkdownList()
        {
            const string html = @"This text has unordered list.<ul><li>Item1</li><li>Item2</li></ul>";
            var expected =
                $"This text has unordered list.{Environment.NewLine}* Item1{Environment.NewLine}* Item2{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected, new Config { ListBulletChar = '*' });
        }

        [Fact]
        public void WhenThereIsOrderedList_ThenConvertToMarkdownList()
        {
            const string html = @"This text has ordered list.<ol><li>Item1</li><li>Item2</li></ol>";
            var expected =
                $"This text has ordered list.{Environment.NewLine}1. Item1{Environment.NewLine}2. Item2{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsOrderedListWithNestedUnorderedList_ThenConvertToMarkdownListWithNestedList()
        {
            const string html =
                @"This text has ordered list.<ol><li>OuterItem1<ul><li>InnerItem1</li><li>InnerItem2</li></ul></li><li>Item2</li></ol>";
            var expected =
                $"This text has ordered list.{Environment.NewLine}1. OuterItem1{Environment.NewLine}    - InnerItem1{Environment.NewLine}    - InnerItem2{Environment.NewLine}2. Item2{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsUnorderedListWithNestedOrderedList_ThenConvertToMarkdownListWithNestedList()
        {
            const string html =
                @"This text has ordered list.<ul><li>OuterItem1<ol><li>InnerItem1</li><li>InnerItem2</li></ol></li><li>Item2</li></ul>";
            var expected =
                $"This text has ordered list.{Environment.NewLine}- OuterItem1{Environment.NewLine}    1. InnerItem1{Environment.NewLine}    2. InnerItem2{Environment.NewLine}- Item2{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsWhitespaceAroundNestedLists_PreventBlankLinesWhenConvertingToMarkdownList()
        {
            string html = $@"<ul>{Environment.NewLine}    ";
            html += $@"    <li>OuterItem1{Environment.NewLine}        <ol>{Environment.NewLine}            <li>InnerItem1</li>{Environment.NewLine}        </ol>{Environment.NewLine}    </li>{Environment.NewLine}";
            html += $@"    <li>Item2</li>{Environment.NewLine}";
            html += $@"    <ol>{Environment.NewLine}        <li>InnerItem2</li>{Environment.NewLine}    </ol>{Environment.NewLine}";
            html += $@"    <li>Item3</li>{ Environment.NewLine}";
            html += $@"</ul>";

            var expected = $@"{Environment.NewLine}- OuterItem1{Environment.NewLine}    1. InnerItem1{Environment.NewLine}- Item2{Environment.NewLine}    1. InnerItem2{Environment.NewLine}- Item3{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void
            WhenListItemTextContainsLeadingAndTrailingSpacesAndTabs_ThenConvertToMarkdownListItemWithSpacesAndTabsStripped()
        {
            const string html = @"<ol><li>	    This is a text with leading and trailing spaces and tabs		</li></ol>";
            var expected =
                $"{Environment.NewLine}1. This is a text with leading and trailing spaces and tabs{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenListContainsNewlineAndTabBetweenTagBorders_CleanupAndConvertToMarkdown()
        {
            var html =
                $"<ol>{Environment.NewLine}\t<li>{Environment.NewLine}\t\t<strong>Item1</strong></li>{Environment.NewLine}\t<li>{Environment.NewLine}\t\tItem2</li></ol>";
            var expected =
                $"{Environment.NewLine}1. **Item1**{Environment.NewLine}2. Item2{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenListContainsMultipleParagraphs_ConvertToMarkdownAndIndentSiblings()
        {
            var html =
                $"<ol>{Environment.NewLine}\t<li>{Environment.NewLine}\t\t<p>Item1</p>{Environment.NewLine}        <p>Item2</p></li>{Environment.NewLine}\t<li>{Environment.NewLine}\t\t<p>Item3</p></li></ol>";
            var expected =
                $"{Environment.NewLine}1. Item1{Environment.NewLine}    Item2{Environment.NewLine}2. Item3{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenListContainsParagraphsOutsideItems_ConvertToMarkdownAndIndentSiblings()
        {
            var html =
                $"<ol>{Environment.NewLine}\t<li>Item1</li>{Environment.NewLine}\t<p>Item 1 additional info</p>{Environment.NewLine}\t<li>Item2</li>{Environment.NewLine}</ol>";
            var expected =
                $"{Environment.NewLine}1. Item1{Environment.NewLine}    Item 1 additional info{Environment.NewLine}2. Item2{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void When_OrderedListIsInTable_LeaveListAsHtml()
        {
            var html =
                $"<table><tr><th>Heading</th></tr><tr><td><ol><li>Item1</li></ol></td></tr></table>";

            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| Heading |{Environment.NewLine}";
            expected += $"| --- |{Environment.NewLine}";
            expected += $"| <ol><li>Item1</li></ol> |{Environment.NewLine}";
            expected += Environment.NewLine;

            CheckConversion(html, expected);
        }

        [Fact]
        public void When_UnorderedListIsInTable_LeaveListAsHtml()
        {
            var html =
                $"<table><tr><th>Heading</th></tr><tr><td><ul><li>Item1</li></ul></td></tr></table>";

            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| Heading |{Environment.NewLine}";
            expected += $"| --- |{Environment.NewLine}";
            expected += $"| <ul><li>Item1</li></ul> |{Environment.NewLine}";
            expected += Environment.NewLine;

            CheckConversion(html, expected);
        }

        [Fact]
        public void Check_Converter_With_Unknown_Tag_ByPass_Option()
        {
            const string html = @"<unknown-tag>text in unknown tag</unknown-tag>";
            const string expected = "text in unknown tag";
            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Bypass
            };
            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void WhenStyletagWithBypassOption_ReturnEmpty() {
            const string html = @"<body><style type=""text/css"">.main {background-color: #ffffff;}</style></body>";
            const string expected = "";
            CheckConversion(html, expected, new Config() {
                UnknownTags = Config.UnknownTagsOption.Bypass
            });
        }

        [Fact]
        public void Check_Converter_With_Unknown_Tag_Drop_Option()
        {
            const string html = @"<unknown-tag>text in unknown tag</unknown-tag><p>paragraph text</p>";
            var expected = $"{Environment.NewLine}paragraph text{Environment.NewLine}";
            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Drop
            };

            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Check_Converter_With_Unknown_Tag_PassThrough_Option()
        {
            const string html = @"<unknown-tag>text in unknown tag</unknown-tag><p>paragraph text</p>";
            var expected =
                $"<unknown-tag>text in unknown tag</unknown-tag>{Environment.NewLine}paragraph text{Environment.NewLine}";
            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.PassThrough
            };

            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Check_Converter_With_Unknown_Tag_Raise_Option()
        {
            const string html = @"<unknown-tag>text in unknown tag</unknown-tag><p>paragraph text</p>";
            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Raise
            };

            var converter = new Converter(config);
            Exception ex = Assert.Throws<UnknownTagException>(() => converter.Convert(html));
            Assert.Equal("Unknown tag: unknown-tag", ex.Message);
        }

        [Fact]
        public void WhenTable_ThenConvertToGFMTable()
        {
            const string html =
                @"<table><tr><th>col1</th><th>col2</th><th>col3</th></tr><tr><td>data1</td><td>data2</td><td>data3</td></tr></table>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"    | col1 | col2 | col3 |{Environment.NewLine}";
            expected += $"    | --- | --- | --- |{Environment.NewLine}";
            expected += $"    | data1 | data2 | data3 |{Environment.NewLine}";
            expected += Environment.NewLine;

            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Bypass
            };

            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void
            WhenTable_WithoutHeaderRow_With_TableWithoutHeaderRowHandlingOptionEmptyRow_ThenConvertToGFMTable_WithEmptyHeaderRow()
        {
            const string html =
                @"<table><tr><td>data1</td><td>data2</td><td>data3</td></tr><tr><td>data4</td><td>data5</td><td>data6</td></tr></table>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| <!----> | <!----> | <!----> |{Environment.NewLine}";
            expected += $"| --- | --- | --- |{Environment.NewLine}";
            expected += $"| data1 | data2 | data3 |{Environment.NewLine}";
            expected += $"| data4 | data5 | data6 |{Environment.NewLine}";
            expected += Environment.NewLine;

            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Bypass,
                TableWithoutHeaderRowHandling = Config.TableWithoutHeaderRowHandlingOption.EmptyRow
            };

            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void
            WhenTable_WithoutHeaderRow_With_TableWithoutHeaderRowHandlingOptionDefault_ThenConvertToGFMTable_WithFirstRowAsHeaderRow()
        {
            const string html =
                @"<table><colgroup><col><col><col></colgroup><tr><td>data1</td><td>data2</td><td>data3</td></tr><tr><td>data4</td><td>data5</td><td>data6</td></tr></table>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| data1 | data2 | data3 |{Environment.NewLine}";
            expected += $"| --- | --- | --- |{Environment.NewLine}";
            expected += $"| data4 | data5 | data6 |{Environment.NewLine}";
            expected += Environment.NewLine;

            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Bypass,
                // TableWithoutHeaderRowHandling = Config.TableWithoutHeaderRowHandlingOption.Default - this is default
            };

            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void WhenTable_Cell_Content_WithNewline_Add_BR_ThenConvertToGFMTable()
        {
            var html =
                $"<table><tr><th>col1</th><th>col2</th><th>col3</th></tr><tr><td>data line1{Environment.NewLine}line2</td><td>data2</td><td>data3</td></tr></table>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| col1 | col2 | col3 |{Environment.NewLine}";
            expected += $"| --- | --- | --- |{Environment.NewLine}";
            expected += $"| data line1<br>line2 | data2 | data3 |{Environment.NewLine}";
            expected += Environment.NewLine;

            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Bypass
            };

            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void WhenTable_CellContainsParagraph_AddBrThenConvertToGFMTable()
        {
            var html =
                $"<table><tr><th>col1</th></tr><tr><td><p>line1</p><p>line2</p></td></tr></table>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| col1 |{Environment.NewLine}";
            expected += $"| --- |{Environment.NewLine}";
            expected += $"| line1<br><br>line2 |{Environment.NewLine}";
            expected += Environment.NewLine;

            CheckConversion(html, expected);
        }


        [Fact]
        public void WhenTable_ContainsTheadTh_ConvertToGFMTable() {
            var html = "<table><thead><tr><th>col1</th><th>col2</th></tr></thead><tbody><tr><td>data1</td><td>data2</td></tr><tbody></table>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| col1 | col2 |{Environment.NewLine}";
            expected += $"| --- | --- |{Environment.NewLine}";
            expected += $"| data1 | data2 |{Environment.NewLine}";
            expected += Environment.NewLine;

            CheckConversion(html, expected, new Config {
                GithubFlavored = true,
            });
        }

        [Fact]
        public void WhenTable_ContainsTheadTd_ConvertToGFMTable() {
            var html = "<table><thead><tr><td>col1</td><td>col2</td></tr></thead><tbody><tr><td>data1</td><td>data2</td></tr><tbody></table>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| col1 | col2 |{Environment.NewLine}";
            expected += $"| --- | --- |{Environment.NewLine}";
            expected += $"| data1 | data2 |{Environment.NewLine}";
            expected += Environment.NewLine;

            CheckConversion(html, expected, new Config {
                GithubFlavored = true,
            });
        }

        [Fact]
        public void WhenTable_CellContainsBr_PreserveBrAndConvertToGFMTable()
        {
            var html =
                $"<table><tr><th>col1</th></tr><tr><td>line 1<br>line 2</td></tr></table>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| col1 |{Environment.NewLine}";
            expected += $"| --- |{Environment.NewLine}";
            expected += $"| line 1<br>line 2 |{Environment.NewLine}";
            expected += Environment.NewLine;

            CheckConversion(html, expected, new Config
            {
                GithubFlavored = true,
            });
        }

        [Fact]
        public void WhenTable_HasEmptyRow_DropsEmptyRow()
        {
            const string html =
                @"<table><tr><td>abc</td></tr><tr></tr></table>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| <!----> |{Environment.NewLine}";
            expected += $"| --- |{Environment.NewLine}";
            expected += $"| abc |{Environment.NewLine}";
            expected += Environment.NewLine;

            CheckConversion(html, expected, new Config
            {
                GithubFlavored = true,
                TableWithoutHeaderRowHandling = Config.TableWithoutHeaderRowHandlingOption.EmptyRow,
            });
        }

        [Fact]
        public void When_BR_With_GitHubFlavored_Config_ThenConvertToGFM_BR()
        {
            const string html = @"First part<br />Second part";
            var expected = $"First part{Environment.NewLine}Second part";

            var config = new Config
            {
                GithubFlavored = true
            };

            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void When_PRE_With_GitHubFlavored_Config_ThenConvertToGFM_PRE()
        {
            const string html = @"<pre>var test = 'hello world';</pre>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}```{Environment.NewLine}";
            expected += $"var test = 'hello world';{Environment.NewLine}";
            expected += $"```{Environment.NewLine}{Environment.NewLine}";

            var config = new Config
            {
                GithubFlavored = true
            };

            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void When_PRE_With_Confluence_Lang_Class_Att_And_GitHubFlavored_Config_ThenConvertToGFM_PRE()
        {
            const string html = @"<pre class=""brush: python;"">var test = 'hello world';</pre>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"```python{Environment.NewLine}";
            expected += $"var test = 'hello world';{Environment.NewLine}";
            expected += $"```{Environment.NewLine}{Environment.NewLine}";

            var config = new Config
            {
                GithubFlavored = true
            };

            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }


        [Fact]
        public void When_PRE_With_Github_Site_DIV_Parent_And_GitHubFlavored_Config_ThenConvertToGFM_PRE()
        {
            const string html = @"<div class=""highlight highlight-source-csharp""><pre>var test = ""hello world"";</pre></div>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"```csharp{Environment.NewLine}";
            expected += $@"var test = ""hello world"";{Environment.NewLine}";
            expected += $"```{Environment.NewLine}{Environment.NewLine}";

            var config = new Config
            {
                GithubFlavored = true
            };

            var converter = new Converter(config);
            var result = converter.Convert(html);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void When_PRE_With_HighlightJs_Lang_Class_Att_And_GitHubFlavored_Config_ThenConvertToGFM_PRE()
        {
            const string html = @"<pre><code class=""hljs language-csharp"">var test = ""hello world"";</code></pre>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"```csharp{Environment.NewLine}";
            expected += $@"var test = ""hello world"";{Environment.NewLine}";
            expected += $"```{Environment.NewLine}{Environment.NewLine}";

            var config = new Config
            {
                GithubFlavored = true
            };

            var converter = new Converter(config);
            var result = converter.Convert(html);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void When_PRE_With_Lang_Highlight_Class_Att_And_GitHubFlavored_Config_ThenConvertToGFM_PRE()
        {
            const string html = @"<pre class=""highlight-python"">var test = 'hello world';</pre>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"```python{Environment.NewLine}";
            expected += $"var test = 'hello world';{Environment.NewLine}";
            expected += $"```{Environment.NewLine}{Environment.NewLine}";

            var config = new Config
            {
                GithubFlavored = true
            };

            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void WhenRemovedCommentsIsEnabled_CommentsAreRemoved()
        {
            const string html =
                @"Hello there <!-- This is a HTML comment block which will be removed! --><!-- This wont be removed because it is incomplete";
            const string expected = @"Hello there ";

            var config = new Config
            {
                RemoveComments = true
            };

            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void WhenThereAreLineBreaksEncompassingParagraphText_It_Should_be_Removed()
        {

            var html = $"<p>{Environment.NewLine}Some text goes here.{Environment.NewLine}</p>";
            var expected = $"{Environment.NewLine}Some text goes here.{Environment.NewLine}";

            var converter = new Converter();
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestConversionOfMultiParagraphWithHeaders()
        {
            var html = $"<h1>Heading1</h1><p>First paragraph.</p><h1>Heading2</h1><p>Second paragraph.</p>";
            var expected =
                $"{Environment.NewLine}# Heading1{Environment.NewLine}{Environment.NewLine}First paragraph.{Environment.NewLine}{Environment.NewLine}# Heading2{Environment.NewLine}{Environment.NewLine}Second paragraph.{Environment.NewLine}";
            var converter = new Converter();
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestConversionWithPastedHtmlContainingUnicodeSpaces()
        {
            var html =
                @"<span style=""color: rgb(36, 41, 46); font-family: -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Helvetica, Arial, sans-serif, &quot;Apple Color Emoji&quot;, &quot;Segoe UI Emoji&quot;, &quot;Segoe UI Symbol&quot;; font-size: 16px; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; background-color: rgb(255, 255, 255); text-decoration-style: initial; text-decoration-color: initial; display: inline !important; float: none;"">Markdown Monster is an easy to use and extensible<span> </span></span><strong style=""box-sizing: border-box; font-weight: 600; color: rgb(36, 41, 46); font-family: -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Helvetica, Arial, sans-serif, &quot;Apple Color Emoji&quot;, &quot;Segoe UI Emoji&quot;, &quot;Segoe UI Symbol&quot;; font-size: 16px; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; background-color: rgb(255, 255, 255); text-decoration-style: initial; text-decoration-color: initial;"">Markdown Editor</strong><span style=""color: rgb(36, 41, 46); font-family: -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Helvetica, Arial, sans-serif, &quot;Apple Color Emoji&quot;, &quot;Segoe UI Emoji&quot;, &quot;Segoe UI Symbol&quot;; font-size: 16px; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; background-color: rgb(255, 255, 255); text-decoration-style: initial; text-decoration-color: initial; display: inline !important; float: none;"">,<span> </span></span><strong style=""box-sizing: border-box; font-weight: 600; color: rgb(36, 41, 46); font-family: -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Helvetica, Arial, sans-serif, &quot;Apple Color Emoji&quot;, &quot;Segoe UI Emoji&quot;, &quot;Segoe UI Symbol&quot;; font-size: 16px; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; background-color: rgb(255, 255, 255); text-decoration-style: initial; text-decoration-color: initial;"">Viewer</strong><span style=""color: rgb(36, 41, 46); font-family: -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Helvetica, Arial, sans-serif, &quot;Apple Color Emoji&quot;, &quot;Segoe UI Emoji&quot;, &quot;Segoe UI Symbol&quot;; font-size: 16px; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; background-color: rgb(255, 255, 255); text-decoration-style: initial; text-decoration-color: initial; display: inline !important; float: none;""><span> </span>and<span> </span></span><strong style=""box-sizing: border-box; font-weight: 600; color: rgb(36, 41, 46); font-family: -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Helvetica, Arial, sans-serif, &quot;Apple Color Emoji&quot;, &quot;Segoe UI Emoji&quot;, &quot;Segoe UI Symbol&quot;; font-size: 16px; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; background-color: rgb(255, 255, 255); text-decoration-style: initial; text-decoration-color: initial;"">Weblog Publisher</strong><span style=""color: rgb(36, 41, 46); font-family: -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Helvetica, Arial, sans-serif, &quot;Apple Color Emoji&quot;, &quot;Segoe UI Emoji&quot;, &quot;Segoe UI Symbol&quot;; font-size: 16px; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; background-color: rgb(255, 255, 255); text-decoration-style: initial; text-decoration-color: initial; display: inline !important; float: none;""><span> </span>for Windows. Our goal is to provide the best Markdown specific editor for Windows and make it as easy as possible to create Markdown documents. We provide a core editor and previewer, and a number of non-intrusive helpers to help embed content like images, links, tables, code and more into your documents with minimal effort.</span>";

            var config = new ReverseMarkdown.Config
            {
                GithubFlavored = true,
                UnknownTags =
                    ReverseMarkdown.Config.UnknownTagsOption
                        .PassThrough, // Include the unknown tag completely in the result (default as well)
                SmartHrefHandling = true // remove markdown output for links where appropriate
            };
            var converter = new ReverseMarkdown.Converter(config);
            string expected = converter.Convert(html);

            _testOutputHelper.WriteLine("Below is the generated markdown:");
            _testOutputHelper.WriteLine(expected);

            Assert.Contains("and **Weblog Publisher** for Windows", expected);
        }

        private static void CheckConversion(string html, string expected, Config config = null)
        {
            config = config ?? new Config();
            if (expected == null) throw new ArgumentNullException(nameof(expected));

            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void When_InlineCode_Shouldnt_Contain_Encoded_Chars()
        {

            var html = @"This is inline code: <code>&lt;AspNetCoreHostingModel&gt;</code>.";
            var expected = @"This is inline code: `<AspNetCoreHostingModel>`.";

            var converter = new Converter();
            var result = converter.Convert(html);
            Assert.Equal(expected, result, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void When_FencedCodeBlocks_Shouldnt_Have_Trailing_Line()
        {

            var html =
                $@"<pre><code class=""language-xml hljs""><span class=""hljs-tag"">&lt;<span class=""hljs-name"">AspNetCoreHostingModel</span>&gt;</span>InProcess<span class=""hljs-tag"">&lt;/<span class=""hljs-name"">AspNetCoreHostingModel</span>&gt;</span>{Environment.NewLine}</code></pre>";
            var expected = $@"{Environment.NewLine}{Environment.NewLine}```xml{Environment.NewLine}<AspNetCoreHostingModel>InProcess</AspNetCoreHostingModel>{Environment.NewLine}```{Environment.NewLine}{Environment.NewLine}";

            var config = new ReverseMarkdown.Config
            {
                GithubFlavored = true,
            };
            var converter = new Converter(config);
            var result = converter.Convert(html);

            Assert.Equal(expected, result, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void When_TextIsHtmlEncoded_DecodeText()
        {
            string html = @"<p>cat&#39;s</p>";

            string expected = $@"{Environment.NewLine}cat's{Environment.NewLine}";

            CheckConversion(html, expected);
        }

        [Fact]
        public void When_TextContainsAngleBrackets_HexEscapeAngleBrackets()
        {
            string html = @"<p>Value = &lt;Your text here&gt;</p>";

            string expected = $@"{Environment.NewLine}Value = &lt;Your text here&gt;{Environment.NewLine}";

            CheckConversion(html, expected);
        }

        [Fact]
        public void When_TextWithinParagraphContainsNewlineChars_ConvertNewlineCharsToSpace()
        {
            // note that the string also has a tab space
            string html = $"<p>This service will be{Environment.NewLine}temporarily unavailable due to planned maintenance{Environment.NewLine}from 02:00-04:00 on 30/01/2020</p>";
            string expected = $"{Environment.NewLine}This service will be temporarily unavailable due to planned maintenance from 02:00-04:00 on 30/01/2020{Environment.NewLine}";

            CheckConversion(html, expected);
        }
        
        [Fact]
        public void WhenTableCellsWithP_ThenDoNotAddNewlines() {
            string html = $@"<html><body><table><tbody><tr><td><p>col1</p></td><td><p>col2</p></td></tr><tr><td><p>data1</p></td><td><p>data2</p></td></tr></tbody></table></body></html>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| col1 | col2 |{Environment.NewLine}";
            expected += $"| --- | --- |{Environment.NewLine}";
            expected += $"| data1 | data2 |{Environment.NewLine}";
            expected += Environment.NewLine;

            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenTableCellsWithDiv_ThenDoNotAddNewlines() {
            string html = $@"<html><body><table><tbody><tr><td><div>col1</div></td><td><div>col2</div></td></tr><tr><td><div>data1</div></td><td><div>data2</div></td></tr></tbody></table></body></html>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| col1 | col2 |{Environment.NewLine}";
            expected += $"| --- | --- |{Environment.NewLine}";
            expected += $"| data1 | data2 |{Environment.NewLine}";
            expected += Environment.NewLine;

            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenTableCellsWithPWithMarkupNewlines_ThenTrimExcessNewlines() {
            string html = $"<html><body><table><tbody>{Environment.NewLine}\t<tr>{Environment.NewLine}\t\t<td>{Environment.NewLine}\t\t\t<p>{Environment.NewLine}col1{Environment.NewLine}</p>{Environment.NewLine}\t\t</td>{Environment.NewLine}\t<tr>{Environment.NewLine}\t\t<td>{Environment.NewLine}\t\t\t<p>{Environment.NewLine}data1{Environment.NewLine}</p>{Environment.NewLine}\t\t</td>\t</tr></tbody></table></body></html>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| col1 |{Environment.NewLine}";
            expected += $"| --- |{Environment.NewLine}";
            expected += $"| data1 |{Environment.NewLine}";
            expected += Environment.NewLine;

            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenTableCellsWithP_ThenNoNewlines() {
            string html = $@"<table><tr><td><p>data1</p></td></tr></table>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| data1 |{Environment.NewLine}";
            expected += $"| --- |{Environment.NewLine}";
            expected += Environment.NewLine;

            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenTableCellsWithMultipleP_ThenNoNewlines() {
            string html = $@"<table><tr><td><p>p1</p><p>p2</p></td></tr></table>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| p1<br><br>p2 |{Environment.NewLine}";
            expected += $"| --- |{Environment.NewLine}";
            expected += Environment.NewLine;

            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenTableCellsWithDataAndP_ThenNewlineBeforeP() {
            string html = $@"<table><tr><td>data1<p>p</p></td></tr></table>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| data1<br>p |{Environment.NewLine}";
            expected += $"| --- |{Environment.NewLine}";
            expected += Environment.NewLine;

            CheckConversion(html, expected);
        }

        [Fact(Skip = "Issue 61. Unclosed CDATA tags are invalid and HtmlAgilityPack won't parse it correctly. Browsers doesn't parse them correctly too.")]
        public void WhenUnclosedStyleTag_WithBypassUnknownTags_ThenConvertToMarkdown() {
            string html = @"<html><head><style></head><body><p>Test content</p></body></html>";
            string expected = $"{Environment.NewLine}Test content{Environment.NewLine}";

            CheckConversion(html, expected, new Config() {
                UnknownTags = Config.UnknownTagsOption.Bypass
            });
        }

        [Fact(Skip = "Issue 61. Unclosed CDATA tags are invalid and HtmlAgilityPack won't parse it correctly. Browsers doesn't parse them correctly too.")]
        public void WhenUnclosedScriptTag_WithBypassUnknownTags_ThenConvertToMarkdown() {
            string html = @"<html><body><script><p>Test content</p></body></html>";
            string expected = $"{Environment.NewLine}Test content{Environment.NewLine}";

            CheckConversion(html, expected, new Config() {
                UnknownTags = Config.UnknownTagsOption.Bypass
            });
        }

        [Fact]
        public void WhenCommentOverlapTag_WithRemoveComments_ThenDoNotStripContentBetweenComments() {
            string html = @"<p>test <!-- comment -->content<!-- another comment --></p>";
            string expected = $"{Environment.NewLine}Test content{Environment.NewLine}";

            CheckConversion(html, expected, new Config() {
                RemoveComments = true
            });
        }

        [Fact]
        public void WhenBoldTagContainsBRTag_ThenConvertToMarkdown()
        {
            const string html = "test<b><br/>test</b>";
            var expected = $"test**  {Environment.NewLine}test**";
            CheckConversion(html, expected);
        }
        
        [Fact]
        public void WhenAnchorTagContainsImgTag_LinkTextShouldNotBeEscaped()
        {
            const string html = "<a href=\"https://www.example.com\"><img src=\"https://example.com/image.jpg\"/></a>";
            var expected = $"[![](https://example.com/image.jpg)](https://www.example.com)";
            CheckConversion(html, expected);
        }

        [Fact]
        public void When_PRE_Without_Lang_Marker_Class_Att_And_GitHubFlavored_Config_With_DefaultCodeBlockLanguage_ThenConvertToGFM_PRE()
        {
            const string html = @"<pre>var test = ""hello world"";</pre>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"```csharp{Environment.NewLine}";
            expected += $@"var test = ""hello world"";{Environment.NewLine}";
            expected += $"```{Environment.NewLine}{Environment.NewLine}";

            var config = new Config
            {
                GithubFlavored = true,
                DefaultCodeBlockLanguage = "csharp"
            };

            var converter = new Converter(config);
            var result = converter.Convert(html);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void When_PRE_With_Parent_DIV_And_Non_GitHubFlavored_Config_FirstLine_CodeBlock_SpaceIndent_Should_Be_Retained()
        {
            const string html = @"<div><pre>var test = ""hello world"";</pre></div>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $@"    var test = ""hello world"";";
            expected += $"{Environment.NewLine}{Environment.NewLine}";

            var converter = new Converter();
            var result = converter.Convert(html);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void When_Converting_HTML_Ensure_To_Process_Only_Body()
        {
            const string html = "<!DOCTYPE html><html lang=\"en\"><head><script>var x = 1;</script></head><body>sample text</body>";
            var expected = $"sample text";
            var converter = new Converter();
            var result = converter.Convert(html);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void When_Html_Containing_Nested_DIVs_Process_ONLY_Inner_Most_DIV()
        {
            const string html = "<div><div>sample text</div></div>";
            var expected = $"{Environment.NewLine}sample text{Environment.NewLine}";
            var converter = new Converter();
            var result = converter.Convert(html);

            Assert.Equal(expected, result);
        }
        
        [Fact]
        public void When_SingleChild_BlockTag_With_Parent_DIV_Ignore_Processing_DIV()
        {
            const string html = "<div><p>sample text</p></div>";
            var expected = $"{Environment.NewLine}sample text{Environment.NewLine}";
            var converter = new Converter();
            var result = converter.Convert(html);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void When_Table_Within_List_Should_Be_Indented()
        {
            var html =
                $"<ol><li>Item1</li><li>Item2<table><tr><th>col1</th><th>col2</th><th>col3</th></tr><tr><td>data1</td><td>data2</td><td>data3</td></tr></table></li><li>Item3</li></ol>";

            var expected = $"{Environment.NewLine}1. Item1{Environment.NewLine}";
            expected += $"2. Item2{Environment.NewLine}";
            expected += Environment.NewLine;
            expected += $"    | col1 | col2 | col3 |{Environment.NewLine}";
            expected += $"    | --- | --- | --- |{Environment.NewLine}";
            expected += $"    | data1 | data2 | data3 |{Environment.NewLine}";
            expected += $"3. Item3{Environment.NewLine}{Environment.NewLine}";

            CheckConversion(html, expected);
        }

        [Fact]
        public void When_Tag_In_PassThoughTags_List_Then_Use_PassThroughConverter()
        {
            const string html = @"This text has image <img alt=""alt"" src=""http://test.com/images/test.png"">. Next line of text";
            CheckConversion(html, html, new Config
            {
                PassThroughTags = new string[] { "img" }
            });
        }
    }
}

﻿using System;
using Xunit;

namespace ReverseMarkdown.Test
{
    public class ConverterTests
    {
        [Fact]
        public void WhenThereIsAsideTag()
        {
            const string html = @"<aside>This text is in an aside tag.</aside> This text appears after aside.";
            var expected = $"{Environment.NewLine}This text is in an aside tag.{Environment.NewLine} This text appears after aside.";
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
        public void WhenThereIsHtmlLinkWithTitle_ThenConvertToMarkdownLink() {
            const string html = @"This is <a href=""http://test.com"" title=""with title"">a link</a>";
            const string expected = @"This is [a link](http://test.com ""with title"")";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereAreMultipleLinks_ThenConvertThemToMarkdownLinks()
        {
            const string html = @"This is <a href=""http://test.com"">first link</a> and <a href=""http://test1.com"">second link</a>";
            const string expected = @"This is [first link](http://test.com) and [second link](http://test1.com)";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsHtmlLinkNotWhitelisted_ThenBypass() {
            const string html = @"Leave <a href=""http://example.com"">http</a>, <a href=""https://example.com"">https</a>, <a href=""ftp://example.com"">ftp</a>, <a href=""ftps://example.com"">ftps</a>, <a href=""file://example.com"">file</a>. Remove <a href=""data:text/plain;charset=UTF-8;page=21,the%20data:1234,5678"">data</a>, <a href=""tel://example.com"">tel</a> and <a href=""whatever://example.com"">whatever</a>";
            const string expected = @"Leave [http](http://example.com), [https](https://example.com), [ftp](ftp://example.com), [ftps](ftps://example.com), [file](file://example.com). Remove data, tel and whatever";
            CheckConversion(html, expected, new Config() {
                WhitelistUriSchemes = new string[] {"http", "https", "ftp", "ftps", "file"}
            });
        }

        [Fact]
        public void WhenThereHtmlWithHrefAndNoSchema_WhitelistedEmptyString_ThenConvertToMarkdown() {
            CheckConversion(
                html: @"<a href=""example.com"">yeah</a>",
                expected: @"[yeah](example.com)",
                config: new Config() {
                    WhitelistUriSchemes = new[] {""}
                }
            );
        }

        [Fact]
        public void WhenThereHtmlWithHrefAndNoSchema_NotWhitelisted_ThenConvertToPlain() {
            CheckConversion(
                html: @"<a href=""example.com"">yeah</a>",
                expected: @"yeah",
                config: new Config() {
                    WhitelistUriSchemes = new[] { "whatever" }
                }
            );
        }


        [Fact]
        public void WhenThereIsHtmlWithProtocolRelativeUrlHrefAndNameNotMatching_SmartHandling_ThenConvertToMarkdown() {
            CheckConversion(
                html: @"<a href=""//example.com"">example.com</a>",
                expected: @"[example.com](//example.com)",
                config: new Config() {
                    SmartHrefHandling =  true
                }
            );
        }


        [Fact]
        public void WhenThereIsHtmlWithHrefAndNameNotMatching_SmartHandling_ThenConvertToMarkdown() {
            CheckConversion(
                html: @"<a href=""https://example.com"">Something intact</a>",
                expected: @"[Something intact](https://example.com)",
                config: new Config() {
                    SmartHrefHandling =  true
                }
            );
        }

        [Fact]
        public void WhenThereIsHtmlWithHrefAndNameMatching_SmartHandling_ThenConvertToPlain() {
            CheckConversion(
                html: @"<a href=""http://example.com/abc?x"">http://example.com/abc?x</a>",
                expected: @"http://example.com/abc?x",
                config: new Config() {
                    SmartHrefHandling =  true
                }
            );
        }

        [Fact]
        public void WhenThereIsHtmlWithHttpSchemeAndNameWithoutScheme_SmartHandling_ThenConvertToPlain() {
            CheckConversion(
                html: @"<a href=""http://example.com"">example.com</a>",
                expected: @"http://example.com",
                config: new Config() {
                    SmartHrefHandling =  true
                }
            );

            CheckConversion(
                html: @"<a href=""https://example.com"">example.com</a>",
                expected: @"https://example.com",
                config: new Config() {
                    SmartHrefHandling =  true
                }
            );
        }

        [Fact]
        public void WhenThereIsHtmlWithMailtoSchemeAndNameWithoutScheme_SmartHandling_ThenConvertToPlain() {
            CheckConversion(
                html: @"<a href=""mailto:george@example.com"">george@example.com</a>",
                expected: @"george@example.com",
                config: new Config() {
                    SmartHrefHandling =  true
                }
            );
        }

        [Fact]
        public void WhenThereIsHtmlWithTelSchemeAndNameWithoutScheme_SmartHandling_ThenConvertToPlain() {
            CheckConversion(
                html: @"<a href=""tel:+1123-45678"">+1123-45678</a>",
                expected: @"+1123-45678",
                config: new Config() {
                    SmartHrefHandling =  true
                }
            );
        }

        [Fact]
        public void WhenThereIsHtmlLinkWithHttpSchemaAndNameWithout_SmartHandling_ThenOutputOnlyHref() {
            CheckConversion(
                html: @"<a href=""http://example.com"">example.com</a>",
                expected: @"http://example.com",
                config: new Config() {
                    SmartHrefHandling =  true
                }
            );
            CheckConversion(
                    html: @"<a href=""https://example.com"">example.com</a>",
                    expected: @"https://example.com",
                    config: new Config() {
                        SmartHrefHandling =  true
                    }
            );
        }

        [Fact]
        public void WhenThereIsHtmlNonWellFormedLinkLink_SmartHandling_ThenConvertToMarkdown() {
            //The string is not correctly escaped.	
            CheckConversion(
                html: @"<a href=""http://example.com/path/file name.docx"">http://example.com/path/file name.docx</a>",
                expected: @"[http://example.com/path/file name.docx](http://example.com/path/file name.docx)",
                config: new Config() {
                    SmartHrefHandling =  true
            });
            //The string is an absolute Uri that represents an implicit file Uri.	
            CheckConversion(
                html: @"<a href=""c:\\directory\filename"">	c:\\directory\filename</a>",
                expected: @"[c:\\directory\filename](c:\\directory\filename)",
                config: new Config() {
                    SmartHrefHandling =  true
            });
            //The string is an absolute URI that is missing a slash before the path.	
            CheckConversion(
                html: @"<a href=""file://c:/directory/filename"">file://c:/directory/filename</a>",
                expected: @"[file://c:/directory/filename](file://c:/directory/filename)",
                config: new Config() {
                    SmartHrefHandling =  true
            });
            //The string contains unescaped backslashes even if they are treated as forward slashes.	
            CheckConversion(
                html: @"<a href=""http:\\host/path/file"">http:\\host/path/file</a>",
                expected: @"[http:\\host/path/file](http:\\host/path/file)",
                config: new Config() {
                    SmartHrefHandling =  true
            });
        }


        [Fact]
        public void WhenThereIsHtmlLinkWithoutHttpSchemaAndNameWithoutScheme_SmartHandling_ThenConvertToMarkdown() {
            CheckConversion(
                html: @"<a href=""ftp://example.com"">example.com</a>",
                expected: @"[example.com](ftp://example.com)",
                config: new Config() {
                    SmartHrefHandling =  true
                }
            );
        }

        [Fact]
        public void WhenThereAreStrongTag_ThenConvertToMarkdownDoubleAstericks()
        {
            const string html = @"This paragraph contains <strong>bold</strong> text";
            const string expected = @"This paragraph contains **bold** text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereAreBTag_ThenConvertToMarkdownDoubleAstericks()
        {
            const string html = @"This paragraph contains <b>bold</b> text";
            const string expected = @"This paragraph contains **bold** text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsEncompassingStrongOrBTag_ThenConvertToMarkdownDoubleAstericks_AnyStrongOrBTagsInsideAreIgnored()
        {
            const string html = @"<strong>Paragraph is encompassed with strong tag and also has <b>bold</b> text words within it</strong>";
            const string expected = @"**Paragraph is encompassed with strong tag and also has bold text words within it**";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsSingleAsterickInText_ThenConvertToMarkdownEscapedAsterick()
        {
            const string html = @"This is a sample(*) paragraph";
            const string expected = @"This is a sample(\*) paragraph";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsEmTag_ThenConvertToMarkdownSingleAstericks()
        {
            const string html = @"This is a <em>sample</em> paragraph";
            const string expected = @"This is a *sample* paragraph";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsITag_ThenConvertToMarkdownSingleAstericks()
        {
            const string html = @"This is a <i>sample</i> paragraph";
            const string expected = @"This is a *sample* paragraph";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsEncompassingEmOrITag_ThenConvertToMarkdownSingleAstericks_AnyEmOrITagsInsideAreIgnored()
        {
            const string html = @"<em>This is a <span><i>sample</i></span> paragraph<em>";
            const string expected = @"*This is a sample paragraph*";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsBreakTag_ThenConvertToMarkdownDoubleSpacesCarriagleReturn()
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
            var expected = $"This text has {Environment.NewLine}# header{Environment.NewLine}. This text appear after header.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsH2Tag_ThenConvertToMarkdownHeader()
        {
            const string html = @"This text has <h2>header</h2>. This text appear after header.";
            var expected = $"This text has {Environment.NewLine}## header{Environment.NewLine}. This text appear after header.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsH3Tag_ThenConvertToMarkdownHeader()
        {
            const string html = @"This text has <h3>header</h3>. This text appear after header.";
            var expected = $"This text has {Environment.NewLine}### header{Environment.NewLine}. This text appear after header.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsH4Tag_ThenConvertToMarkdownHeader()
        {
            const string html = @"This text has <h4>header</h4>. This text appear after header.";
            var expected = $"This text has {Environment.NewLine}#### header{Environment.NewLine}. This text appear after header.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsH5Tag_ThenConvertToMarkdownHeader()
        {
            const string html = @"This text has <h5>header</h5>. This text appear after header.";
            var expected = $"This text has {Environment.NewLine}##### header{Environment.NewLine}. This text appear after header.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsH6Tag_ThenConvertToMarkdownHeader()
        {
            const string html = @"This text has <h6>header</h6>. This text appear after header.";
            var expected = $"This text has {Environment.NewLine}###### header{Environment.NewLine}. This text appear after header.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsBlockquoteTag_ThenConvertToMarkdownBlockquote()
        {
            const string html = @"This text has <blockquote>blockquote</blockquote>. This text appear after header.";
            var expected = $"This text has {Environment.NewLine}{Environment.NewLine}> blockquote{Environment.NewLine}{Environment.NewLine}. This text appear after header.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsEmptyBlockquoteTag_ThenConvertToMarkdownBlockquote()
        {
            const string html = @"This text has <blockquote></blockquote>. This text appear after header.";
            var expected = $"This text has {Environment.NewLine}{Environment.NewLine}{Environment.NewLine}. This text appear after header.";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsParagraphTag_ThenConvertToMarkdownDoubleLineBreakBeforeAndAfter()
        {
            const string html = @"This text has markup <p>paragraph.</p> Next line of text";
            var expected = $"This text has markup {Environment.NewLine}{Environment.NewLine}paragraph.{Environment.NewLine}{Environment.NewLine} Next line of text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsHorizontalRule_ThenConvertToMarkdownHorizontalRule()
        {
            const string html = @"This text has horizontal rule.<hr/>Next line of text";
            var expected = $"This text has horizontal rule.{Environment.NewLine}* * *{Environment.NewLine}Next line of text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsImgTag_ThenConvertToMarkdownImage()
        {
            const string html = @"This text has image <img alt=""alt"" title=""title"" src=""http://test.com/images/test.png""/>. Next line of text";
            const string expected = @"This text has image ![alt](http://test.com/images/test.png ""title""). Next line of text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsImgTagWithoutTitle_ThenConvertToMarkdownImagewithoutTitle()
        {
            const string html = @"This text has image <img alt=""alt"" src=""http://test.com/images/test.png""/>. Next line of text";
            const string expected = @"This text has image ![alt](http://test.com/images/test.png). Next line of text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsImgTagWithoutAltText_ThenConvertToMarkdownImagewithoutAltText()
        {
            const string html = @"This text has image <img src=""http://test.com/images/test.png""/>. Next line of text";
            const string expected = @"This text has image ![](http://test.com/images/test.png). Next line of text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsImgTag_SchemeNotWhitelisted_ThenEmptyOutput() {
            CheckConversion(
                html: @"<img src=""data:image/gif;base64,R0lGODlhEAAQ...""/>",
                expected: @"",
                config: new Config() {
                    WhitelistUriSchemes = new[] {"http"}
                }
            );
        }

        [Fact]
        public void WhenThereIsImgTag_SchemeIsWhitelisted_ThenConvertToMarkdown() {
            CheckConversion(
                html: @"<img src=""data:image/gif;base64,R0lGODlhEAAQ...""/>",
                expected: @"![](data:image/gif;base64,R0lGODlhEAAQ...)",
                config: new Config() {
                    WhitelistUriSchemes = new[] { "data" }
                }
            );
        }

        [Fact]
        public void WhenThereIsImgTagAndSrcWithNoSchema_WhitelistedEmptyString_ThenConvertToMarkdown() {
            CheckConversion(
                html: @"<img src=""example.com""/>",
                expected: @"![](example.com)",
                config: new Config() {
                    WhitelistUriSchemes = new[] { "" }
                }
            );
        }

        [Fact]
        public void WhenThereIsImgTagAndSrcWithNoSchema_NotWhitelisted_ThenConvertToPlain() {
            CheckConversion(
                html: @"<img src=""data:image/gif;base64,R0lGODlhEAAQ...""/>",
                expected: @"",
                config: new Config() {
                    WhitelistUriSchemes = new[] { "whatever" }
                }
            );
        }

        [Fact]
        public void WhenThereIsImgTagWithRelativeUrl_NotWhitelisted_ThenConvertToMarkdown() {
            CheckConversion(
                html: @"<img src=""/example.gif""/>",
                expected: @"",
                config: new Config() {
                    WhitelistUriSchemes = new[] { "data" }
                }
            );
        }

        [Fact]
        public void WhenThereIsImgTagWithUnixUrl_ConfigHasWhitelist_ThenConvertToMarkdown() {
            CheckConversion(
                html: @"<img src=""/example.gif""/>",
                expected: @"![](/example.gif)",
                config: new Config() {
                    WhitelistUriSchemes = new[] { "file" }
                }
            );
        }

        [Fact]
        public void WhenThereIsImgTagWithHttpProtocolRelativeUrl_ConfigHasWhitelist_ThenConvertToMarkdown() {
            CheckConversion(
                html: @"<img src=""//example.gif""/>",
                expected: @"![](//example.gif)",
                config: new Config() {
                    WhitelistUriSchemes = new[] { "http" }
                }
            );
        }

        [Fact]
        public void WhenThereIsPreTag_ThenConvertToMarkdownPre()
        {
            const string html = @"This text has pre tag content <pre>Predefined text</pre>Next line of text";
            var expected = $"This text has pre tag content {Environment.NewLine}{Environment.NewLine}    Predefined text{Environment.NewLine}{Environment.NewLine}Next line of text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsEmptyPreTag_ThenConvertToMarkdownPre()
        {
            const string html = @"This text has pre tag content <pre><br/ ></pre>Next line of text";
            var expected = $"This text has pre tag content {Environment.NewLine}{Environment.NewLine}{Environment.NewLine}Next line of text";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsUnorderedList_ThenConvertToMarkdownList()
        {
            const string html = @"This text has unordered list.<ul><li>Item1</li><li>Item2</li></ul>";
            var expected = $"This text has unordered list.{Environment.NewLine}- Item1{Environment.NewLine}- Item2{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsOrderedList_ThenConvertToMarkdownList()
        {
            const string html = @"This text has ordered list.<ol><li>Item1</li><li>Item2</li></ol>";
            var expected = $"This text has ordered list.{Environment.NewLine}1. Item1{Environment.NewLine}2. Item2{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsOrderedListWithNestedUnorderedList_ThenConvertToMarkdownListWithNestedList()
        {
            const string html = @"This text has ordered list.<ol><li>OuterItem1<ul><li>InnerItem1</li><li>InnerItem2</li></ul></li><li>Item2</li></ol>";
            var expected = $"This text has ordered list.{Environment.NewLine}1. OuterItem1{Environment.NewLine}  - InnerItem1{Environment.NewLine}  - InnerItem2{Environment.NewLine}2. Item2{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenThereIsUnorderedListWithNestedOrderedList_ThenConvertToMarkdownListWithNestedList()
        {
            const string html = @"This text has ordered list.<ul><li>OuterItem1<ol><li>InnerItem1</li><li>InnerItem2</li></ol></li><li>Item2</li></ul>";
            var expected = $"This text has ordered list.{Environment.NewLine}- OuterItem1{Environment.NewLine}  1. InnerItem1{Environment.NewLine}  2. InnerItem2{Environment.NewLine}- Item2{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenListItemTextContainsLeadingAndTrailingSpacesAndTabs_ThenConvertToMarkdownListItemWithSpacesAndTabsStripped()
        {
            const string html = @"<ol><li>	    This is a text with leading and trailing spaces and tabs		</li></ol>";
            var expected = $"{Environment.NewLine}1. This is a text with leading and trailing spaces and tabs{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenListContainsNewlineAndTabBetweenTagBorders_CleanupAndConvertToMarkdown()
        {
            var html = $"<ol>{Environment.NewLine}\t<li>{Environment.NewLine}\t\t<strong>Item1</strong></li>{Environment.NewLine}\t<li>{Environment.NewLine}\t\tItem2</li></ol>";
            var expected = $"{Environment.NewLine}1. **Item1**{Environment.NewLine}2. Item2{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void WhenListContainsMultipleParagraphs_ConvertToMarkdownAndIndentSiblings()
        {
            var html = $"<ol>{Environment.NewLine}\t<li>{Environment.NewLine}\t\t<p>Item1</p>{Environment.NewLine}        <p>Item2</p></li>{Environment.NewLine}\t<li>{Environment.NewLine}\t\t<p>Item3</p></li></ol>";
            var expected = $"{Environment.NewLine}1. Item1{Environment.NewLine}{Environment.NewLine}    Item2{Environment.NewLine}2. Item3{Environment.NewLine}{Environment.NewLine}";
            CheckConversion(html, expected);
        }

        [Fact]
        public void Check_Converter_With_Unknown_Tag_ByPass_Option()
        {
            const string html = @"<unknown-tag>text in unknown tag</unknown-tag>";
            const string expected = "text in unknown tag";
            var config = new Config(Config.UnknownTagsOption.Bypass);
            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Check_Converter_With_Unknown_Tag_Drop_Option()
        {
            const string html = @"<unknown-tag>text in unknown tag</unknown-tag><p>paragraph text</p>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}paragraph text{Environment.NewLine}{Environment.NewLine}";
            var config = new Config(Config.UnknownTagsOption.Drop);
            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Check_Converter_With_Unknown_Tag_PassThrough_Option()
        {
            const string html = @"<unknown-tag>text in unknown tag</unknown-tag><p>paragraph text</p>";
            var expected = $"<unknown-tag>text in unknown tag</unknown-tag>{Environment.NewLine}{Environment.NewLine}paragraph text{Environment.NewLine}{Environment.NewLine}";
            var config = new Config(Config.UnknownTagsOption.PassThrough);
            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Check_Converter_With_Unknown_Tag_Raise_Option()
        {
            const string html = @"<unknown-tag>text in unknown tag</unknown-tag><p>paragraph text</p>";
            var config = new Config(Config.UnknownTagsOption.Raise);
            var converter = new Converter(config);
            Exception ex = Assert.Throws<UnknownTagException>(() => converter.Convert(html));
            Assert.Equal("Unknown tag: unknown-tag", ex.Message);
        }

        [Fact]
        public void WhenTable_ThenConvertToGFMTable()
        {
            const string html = @"<table><tr><th>col1</th><th>col2</th><th>col3</th></tr><tr><td>data1</td><td>data2</td><td>data3</td></tr></table>";
            var expected = $"{Environment.NewLine}{Environment.NewLine}";
            expected += $"| col1 | col2 | col3 |{Environment.NewLine}";
            expected += $"| --- | --- | --- |{Environment.NewLine}";
            expected += $"| data1 | data2 | data3 |{Environment.NewLine}";
            expected += Environment.NewLine;

            var config = new Config(Config.UnknownTagsOption.Bypass);
            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void When_BR_With_GitHubFlavored_Config_ThenConvertToGFM_BR()
        {
            const string html = @"First part<br />Second part";
            var expected = $"First part{Environment.NewLine}Second part";

            var config = new Config(githubFlavored: true);
            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void When_PRE_With_GitHubFlavored_Config_ThenConvertToGFM_PRE()
        {
            const string html = @"<pre>var test = 'hello world';</pre>";
            var expected = $"{Environment.NewLine}```{Environment.NewLine}";
            expected += $"var test = 'hello world';{Environment.NewLine}";
            expected += $"```{Environment.NewLine}";

            var config = new Config(githubFlavored: true);
            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void When_PRE_With_Confluence_Lang_Class_Att_And_GitHubFlavored_Config_ThenConvertToGFM_PRE()
        {
            const string html = @"<pre class=""brush: python;"">var test = 'hello world';</pre>";
            var expected = Environment.NewLine;
            expected += $"```python{Environment.NewLine}";
            expected += $"var test = 'hello world';{Environment.NewLine}";
            expected += $"```{Environment.NewLine}";

            var config = new Config(githubFlavored: true);
            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void When_PRE_With_Lang_Highlight_Class_Att_And_GitHubFlavored_Config_ThenConvertToGFM_PRE()
        {
            const string html = @"<pre class=""highlight-python"">var test = 'hello world';</pre>";
            var expected = Environment.NewLine;
            expected += $"```python{ Environment.NewLine}";
            expected += $"var test = 'hello world';{Environment.NewLine}";
            expected += $"```{Environment.NewLine}";

            var config = new Config(githubFlavored: true);
            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void WhenRemovedCommentsIsEnabled_CommentsAreRemoved()
        {
            const string html = @"Hello there <!-- This is a HTML comment block which will be removed! --><!-- This wont be removed because it is incomplete";
            const string expected = @"Hello there <!-- This wont be removed because it is incomplete";

            var config = new Config(removeComments: true);
            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result);
        }

        private static void CheckConversion(string html, string expected, Config config = null) {
            config = config ?? new Config();
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            
            var converter = new Converter(config);
            var result = converter.Convert(html);
            Assert.Equal(expected, result, StringComparer.OrdinalIgnoreCase);
            //Assert.True(string.CompareOrdinal(expected, result) == 0);
        }
    }
}


using Xunit;

namespace ReverseMarkdown.Test
{
	public class ConverterTests
	{
		[Fact]
		public void WhenThereIsHtmlLink_ThenConvertToMarkdownLink()
		{
			const string html = @"This is <a href=""http://test.com"">a link</a>";
			const string expected = @"This is [a link](http://test.com)";
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
			string expected = @"This is a paragraph.  
This line appears after break.";
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
			const string expected = @"This text has 
# header
. This text appear after header.";
			CheckConversion(html, expected);
		}

		[Fact]
		public void WhenThereIsH2Tag_ThenConvertToMarkdownHeader()
		{
			const string html = @"This text has <h2>header</h2>. This text appear after header.";
			const string expected = @"This text has 
## header
. This text appear after header.";
			CheckConversion(html, expected);
		}

		[Fact]
		public void WhenThereIsH3Tag_ThenConvertToMarkdownHeader()
		{
			const string html = @"This text has <h3>header</h3>. This text appear after header.";
			const string expected = @"This text has 
### header
. This text appear after header.";
			CheckConversion(html, expected);
		}

		[Fact]
		public void WhenThereIsH4Tag_ThenConvertToMarkdownHeader()
		{
			const string html = @"This text has <h4>header</h4>. This text appear after header.";
			const string expected = @"This text has 
#### header
. This text appear after header.";
			CheckConversion(html, expected);
		}

		[Fact]
		public void WhenThereIsH5Tag_ThenConvertToMarkdownHeader()
		{
			const string html = @"This text has <h5>header</h5>. This text appear after header.";
			const string expected = @"This text has 
##### header
. This text appear after header.";
			CheckConversion(html, expected);
		}

		[Fact]
		public void WhenThereIsH6Tag_ThenConvertToMarkdownHeader()
		{
			const string html = @"This text has <h6>header</h6>. This text appear after header.";
			const string expected = @"This text has 
###### header
. This text appear after header.";
			CheckConversion(html, expected);
		}

		[Fact]
		public void WhenThereIsBlockquoteTag_ThenConvertToMarkdownBlockquote()
		{
			const string html = @"This text has <blockquote>blockquote</blockquote>. This text appear after header.";
			const string expected = @"This text has 

> blockquote

. This text appear after header.";
			CheckConversion(html, expected);
		}

		[Fact]
		public void WhenThereIsParagraphTag_ThenConvertToMarkdownDoubleLineBreakBeforeAndAfter()
		{
			const string html = @"This text has markup <p>paragraph.</p> Next line of text";
			const string expected = @"This text has markup 

paragraph.

 Next line of text";
			CheckConversion(html, expected);
		}

		[Fact]
		public void WhenThereIsHorizontalRule_ThenConvertToMarkdownHorizontalRule()
		{
			const string html = @"This text has horizontal rule.<hr/>Next line of text";
			const string expected = @"This text has horizontal rule.
* * *
Next line of text";
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
		public void WhenThereIsPreTag_ThenConvertToMarkdownPre()
		{
			const string html = @"This text has pre tag content <pre>Predefined text</pre>Next line of text";
			const string expected = @"This text has pre tag content 

    Predefined text

Next line of text";
			CheckConversion(html, expected);
		}

		[Fact]
		public void WhenThereIsUnorderedList_ThenConvertToMarkdownList()
		{
			const string html = @"This text has unordered list.<ul><li>Item1</li><li>Item2</li></ul>";
			const string expected = @"This text has unordered list.
- Item1
- Item2

";
			CheckConversion(html, expected);
		}

		[Fact]
		public void WhenThereIsOrderedList_ThenConvertToMarkdownList()
		{
			const string html = @"This text has ordered list.<ol><li>Item1</li><li>Item2</li></ol>";
			const string expected = @"This text has ordered list.
1. Item1
2. Item2

";
			CheckConversion(html, expected);
		}

		[Fact]
		public void WhenThereIsOrderedListWithNestedUnorderedList_ThenConvertToMarkdownListWithNestedList()
		{
			const string html = @"This text has ordered list.<ol><li><ul><li>InnerItem1</li><li>InnerItem2</li></ul></li><li>Item2</li></ol>";
			const string expected = @"This text has ordered list.
1. - InnerItem1
 - InnerItem2
2. Item2

";
			CheckConversion(html, expected);
		}

		[Fact]
		public void WhenThereIsUnorderedListWithNestedOrderedList_ThenConvertToMarkdownListWithNestedList()
		{
			const string html = @"This text has ordered list.<ul><li><ol><li>InnerItem1</li><li>InnerItem2</li></ol></li><li>Item2</li></ul>";
			const string expected = @"This text has ordered list.
- 1. InnerItem1
 2. InnerItem2
- Item2

";
			CheckConversion(html, expected);
		}

		[Fact]
		public void WhenListItemTextContainsLeadingAndTrailingSpacesAndTabs_TheConvertToMarkdownListItemWithSpacesAndTabsStripped()
		{
			const string html = @"<ol><li>	    This is a text with leading and trailing spaces and tabs		</li></ol>";
			const string expected = @"
1. This is a text with leading and trailing spaces and tabs

";
			CheckConversion(html, expected);
		}

		[Fact]
		public void WhenListContainsNewlineAndTabBetweenTagBorders_CleanupAndConvertToMarkdown()
		{
			const string html = @"<ol>
	<li>
		<strong>Item1</strong></li>
	<li>
		Item2</li></ol>";
			const string expected = @"
1. **Item1**
2. Item2

";
			CheckConversion(html, expected);
		}

		[Fact]
		public void WhenListContainsMultipleParagraphs_ConvertToMarkdownAndIndentSiblings()
		{
			const string html = @"<ol>
	<li>
		<p>Item1</p>
        <p>Item2</p></li>
	<li>
		<p>Item3</p></li></ol>";
			const string expected = @"
1. Item1

    Item2
2. Item3

";
			CheckConversion(html, expected);
		}

		private static void CheckConversion(string html, string expected)
		{
			var converter = new Converter();
			var result = converter.Convert(html);
			Assert.Equal<string>(expected, result);
		}
	}
}

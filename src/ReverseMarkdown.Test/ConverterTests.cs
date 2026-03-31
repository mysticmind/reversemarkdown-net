using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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

        private static readonly Lazy<Dictionary<string, string>> CaseHtmlById = new(() =>
        {
            var path = Path.Combine(GetProjectDirectory().FullName, "TestData", "cases.json");
            var json = File.ReadAllText(path);
            var cases = JsonSerializer.Deserialize<List<CaseHtml>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CaseHtml>();
            return cases.Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .ToDictionary(item => item.Id, item => item.Html);
        });

        private static string LoadHtml(string id)
        {
            if (!CaseHtmlById.Value.TryGetValue(id, out var html)) {
                throw new InvalidOperationException($"Missing html for case '{id}'.");
            }

            return html;
        }

        private static DirectoryInfo GetProjectDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ReverseMarkdown.Test.csproj"))) {
                directory = directory.Parent;
            }

            if (directory == null) {
                throw new DirectoryNotFoundException("Could not locate test project directory.");
            }

            return directory;
        }

        private sealed class CaseHtml
        {
            public string Id { get; set; } = string.Empty;
            public string Html { get; set; } = string.Empty;
        }

        

        

        

        

        

        

        

        

        

        

        

        

        

        [Fact]
        public void WhenThereIsHtmlWithHttpSchemeAndNameWithoutScheme_SmartHandling_ThenConvertToPlain()
        {
            var config = new Config()
            {
                SmartHrefHandling = true
            };
            var converter = new Converter(config);
            var result = converter.Convert(LoadHtml("SmartHandling_HttpScheme_Http"));
            Assert.Equal("http://example.com", result, StringComparer.OrdinalIgnoreCase);

            var result1 = converter.Convert(LoadHtml("SmartHandling_HttpScheme_Https"));
            Assert.Equal("https://example.com", result1, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void WhenEscapeMarkdownLineStartsEnabled_ThenEscapeHeadingAndListMarkers()
        {
            var config = new Config
            {
                EscapeMarkdownLineStarts = true
            };
            var converter = new Converter(config);

            Assert.Equal(@"\# Heading 1", converter.Convert("<p># Heading 1</p>"));
            Assert.Equal(@"\- Point 1", converter.Convert("<p>- Point 1</p>"));
            Assert.Equal(@"1\. Point 1", converter.Convert("<p>1. Point 1</p>"));
        }

        [Fact]
        public void WhenTextContainsBracketsBracesAndParentheses_ThenDoNotEscapeThem()
        {
            const string html = "This is [a] test of the (reverse) {markdown} system.";

            var converter = new Converter();
            Assert.Equal(html, converter.Convert(html));

            var commonMarkConverter = new Converter(new Config { CommonMark = true });
            Assert.Equal(html, commonMarkConverter.Convert(html));
        }

        [Fact]
        public void WhenCommonMarkTextContainsMarkdownLinkPattern_ThenEscapeOnlyPatternDelimiters()
        {
            const string html = "This is [a] and [label](https://example.com/path) with {plain} braces.";

            var converter = new Converter(new Config { CommonMark = true });

            Assert.Equal(
                "This is [a] and \\[label\\]\\(https://example.com/path\\) with {plain} braces.",
                converter.Convert(html)
            );
        }

        [Fact]
        public void WhenOutputLineEndingConfigured_ThenNormalizeOutputLineEndings()
        {
            var html = "<p>one</p>\r\n<p>two</p>\r<p>three</p>\n<p>four</p>";
            var config = new Config
            {
                OutputLineEnding = "\n"
            };
            var converter = new Converter(config);

            var result = converter.Convert(html);

            Assert.Equal(result, result.ReplaceLineEndings("\n"));
            Assert.DoesNotContain("\r", result);
        }

        

        

        [Fact]
        public void WhenThereIsHtmlLinkWithHttpSchemaAndNameWithout_SmartHandling_ThenOutputOnlyHref()
        {
            var config = new Config
            {
                SmartHrefHandling = true
            };
            var converter = new Converter(config);
            var result = converter.Convert(LoadHtml("SmartHandling_OutputOnlyHref_Http"));
            Assert.Equal("http://example.com", result, StringComparer.OrdinalIgnoreCase);
            var result1 = converter.Convert(LoadHtml("SmartHandling_OutputOnlyHref_Https"));
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
            var result = converter.Convert(LoadHtml("SmartHandling_NonWellFormed"));
            Assert.Equal("[http://example.com/path/file name.docx](http://example.com/path/file%20name.docx)", result,
                StringComparer.OrdinalIgnoreCase);

            //The string is an absolute Uri that represents an implicit file Uri.
            var result1 = converter.Convert(LoadHtml("SmartHandling_ImplicitFile"));
            Assert.Equal(@"[c:\\directory\filename](c:\\directory\filename)", result1,
                StringComparer.OrdinalIgnoreCase);

            //The string is an absolute URI that is missing a slash before the path.
            var result2 = converter.Convert(LoadHtml("SmartHandling_FileMissingSlash"));
            Assert.Equal("[file://c:/directory/filename](file://c:/directory/filename)", result2,
                StringComparer.OrdinalIgnoreCase);

            //The string contains unescaped backslashes even if they are treated as forward slashes.
            var result3 = converter.Convert(LoadHtml("SmartHandling_UnescapedBackslashes"));
            Assert.Equal(@"[http:\\host/path/file](http:\\host/path/file)", result3, StringComparer.OrdinalIgnoreCase);
        }

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        [Fact]
        public void WhenThereIsBase64PngImgTag_WithSaveToFileConfigAndValidDirectory_ThenSaveImageAndReferenceFilePath()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "reversemarkdown_test_" + Guid.NewGuid());
            try
            {
                var html = LoadHtml("Base64_SaveToFile_Png");
                var config = new Config
                {
                    Base64Images = Config.Base64ImageHandling.SaveToFile,
                    Base64ImageSaveDirectory = tempDir
                };

                var converter = new Converter(config);
                var result = converter.Convert(html);

                // Verify the image file was created
                var imageFiles = Directory.GetFiles(tempDir, "image_*.png");
                Assert.Single(imageFiles);
                Assert.True(File.Exists(imageFiles[0]));

                // Verify the markdown references the saved file path
                Assert.Contains(imageFiles[0], result);
                Assert.Contains("Before", result);
                Assert.Contains("After", result);
                Assert.Contains("![test]", result);
            }
            finally
            {
                // Clean up temp directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void WhenThereIsBase64JpegImgTag_WithSaveToFileConfigAndValidDirectory_ThenSaveImageWithJpgExtension()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "reversemarkdown_test_" + Guid.NewGuid());
            try
            {
                var html = LoadHtml("Base64_SaveToFile_Jpeg");
                var config = new Config
                {
                    Base64Images = Config.Base64ImageHandling.SaveToFile,
                    Base64ImageSaveDirectory = tempDir
                };

                var converter = new Converter(config);
                var result = converter.Convert(html);

                // Verify the image file was created with .jpg extension
                var imageFiles = Directory.GetFiles(tempDir, "image_*.jpg");
                Assert.Single(imageFiles);
                Assert.True(File.Exists(imageFiles[0]));
                Assert.EndsWith(".jpg", imageFiles[0]);

                // Verify the markdown references the saved file path
                Assert.Contains(imageFiles[0], result);
                Assert.Contains("![jpeg]", result);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void WhenMultipleBase64ImgTags_WithSaveToFileConfig_ThenSaveAllImagesWithUniqueNames()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "reversemarkdown_test_" + Guid.NewGuid());
            try
            {
                var html = LoadHtml("Base64_SaveToFile_Multiple");
                var config = new Config
                {
                    Base64Images = Config.Base64ImageHandling.SaveToFile,
                    Base64ImageSaveDirectory = tempDir
                };

                var converter = new Converter(config);
                var result = converter.Convert(html);

                // Verify both image files were created with unique names
                var imageFiles = Directory.GetFiles(tempDir, "image_*.png");
                Assert.Equal(2, imageFiles.Length);

                // Verify both images are referenced in the markdown
                Assert.Contains("![first]", result);
                Assert.Contains("![second]", result);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void WhenBase64ImgTag_WithCustomFileNameGenerator_ThenUseCustomFileName()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "reversemarkdown_test_" + Guid.NewGuid());
            try
            {
                var html = LoadHtml("Base64_SaveToFile_CustomName");
                var config = new Config
                {
                    Base64Images = Config.Base64ImageHandling.SaveToFile,
                    Base64ImageSaveDirectory = tempDir,
                    Base64ImageFileNameGenerator = (index, mimeType) => $"custom_image_{index}"
                };

                var converter = new Converter(config);
                var result = converter.Convert(html);

                // Verify the custom filename was used
                var imageFiles = Directory.GetFiles(tempDir, "custom_image_*.png");
                Assert.Single(imageFiles);
                Assert.Contains("custom_image_0", imageFiles[0]);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void WhenBase64ImgTag_WithSaveToFileAndNonExistentDirectory_ThenCreateDirectoryAndSaveImage()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "reversemarkdown_test_" + Guid.NewGuid(), "nested", "path");
            try
            {
                // Ensure the directory does not exist
                Assert.False(Directory.Exists(tempDir));

                var html = LoadHtml("Base64_SaveToFile_NonExistentDir");
                var config = new Config
                {
                    Base64Images = Config.Base64ImageHandling.SaveToFile,
                    Base64ImageSaveDirectory = tempDir
                };

                var converter = new Converter(config);
                var result = converter.Convert(html);

                // Verify the directory was created
                Assert.True(Directory.Exists(tempDir));

                // Verify the image file was created
                var imageFiles = Directory.GetFiles(tempDir, "image_*.png");
                Assert.Single(imageFiles);
            }
            finally
            {
                // Clean up the entire temp directory tree
                var rootTempDir = Path.Combine(Path.GetTempPath(), Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(tempDir))));
                if (Directory.Exists(rootTempDir))
                {
                    Directory.Delete(rootTempDir, true);
                }
            }
        }

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        [Fact]
        public Task When_Underline_Tag_With_AliasConverter_Register_ThenConvertToItalics()
        {
            var html = LoadHtml("Underline_Tag_Alias");
            var converter = new Converter();
            converter.Register("u", new ReverseMarkdown.Converters.AliasConverter(converter, "em"));
            var result = converter.Convert(html);
            var settings = new VerifySettings();
            settings.DisableRequireUniquePrefix();
            return Verifier.Verify(result, settings: settings, extension: "md");
        }

        

        

        

        

        [Fact]
        public Task Check_Converter_With_Unknown_Tag_Raise_Option()
        {
            var html = LoadHtml("Unknown_Tag_Raise");
            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Raise
            };
            var converter = new Converter(config);
            return Verifier.Throws(() => converter.Convert(html), settings: _verifySettings)
                .IgnoreMember<Exception>(e => e.StackTrace);
        }

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        [Fact]
        public void TestConversionWithPastedHtmlContainingUnicodeSpaces()
        {
            var html = LoadHtml("PastedHtmlUnicodeSpaces");

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

        [Fact]
        public async Task Converter_Is_Thread_Safe_For_Concurrent_Use()
        {
            var html = LoadHtml("Converter_Is_Thread_Safe_For_Concurrent_Use");
            var converter = new Converter(new Config { GithubFlavored = true });
            var expected = converter.Convert(html);

            const int iterations = 50;
            var tasks = new Task<string>[iterations];

            for (var i = 0; i < iterations; i++) {
                tasks[i] = Task.Run(() => converter.Convert(html));
            }

            var results = await Task.WhenAll(tasks);

            foreach (var result in results) {
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public void WhenPreContainsTable_ThenTreatAsCodeBlock()
        {
            var htmlTable = "<table><tr><td>a</td></tr></table>";
            var htmlPre = $"<pre>{htmlTable}</pre>";
            var htmlPreCode = $"<pre><code>{htmlTable}</code></pre>";
            var converter = new Converter(new Config { GithubFlavored = true });
            var expected = string.Join(Environment.NewLine, new[] {
                "```",
                "a",
                "```"
            });

            Assert.Equal(expected, converter.Convert(htmlPre));
            Assert.Equal(expected, converter.Convert(htmlPreCode));
        }

        [Fact]
        public void WhenPreContainsHtml_WithConvertPreContentAsHtml_ThenConvertHtml()
        {
            var htmlContent = "<p>Title</p><p><strong>Bold</strong></p>";
            var htmlPre = $"<pre>{htmlContent}</pre>";
            var htmlPreCode = $"<pre><code>{htmlContent}</code></pre>";
            var converter = new Converter(new Config { ConvertPreContentAsHtml = true });
            var expected = string.Join(Environment.NewLine, new[] {
                "Title",
                string.Empty,
                "**Bold**"
            });

            Assert.Equal(expected, converter.Convert(htmlPre));
            Assert.Equal(expected, converter.Convert(htmlPreCode));
        }

        [Fact]
        public void SlackFlavored_Unsupported_Hr()
        {
            var html = LoadHtml("SlackFlavored_Unsupported_Hr");
            var config = new Config { SlackFlavored = true };
            var converter = new Converter(config);
            Assert.Throws<SlackUnsupportedTagException>(() => converter.Convert(html));
        }

        [Fact]
        public void TelegramMarkdownV2_BasicFormatting()
        {
            var html = "This is <strong>bold</strong>, <em>italic</em>, <del>strikethrough</del> and <a href=\"https://example.com\">a link</a>";
            var converter = new Converter(new Config { TelegramMarkdownV2 = true });

            var result = converter.Convert(html);

            Assert.Equal("This is *bold*, _italic_, ~strikethrough~ and [a link](https://example.com)", result);
        }

        [Fact]
        public void TelegramMarkdownV2_EscapeSpecialCharactersInText()
        {
            var html = "<p>Special _ * [ ] ( ) ~ ` > # + - = | { } . ! \\</p>";
            var converter = new Converter(new Config { TelegramMarkdownV2 = true });

            var result = converter.Convert(html);

            Assert.Equal("Special \\_ \\* \\[ \\] \\( \\) \\~ \\` \\> \\# \\+ \\- \\= \\| \\{ \\} \\. \\! \\\\", result);
        }

        [Fact]
        public void TelegramMarkdownV2_EscapeLinkTextAndHref()
        {
            var html = "<a href=\"https://example.com/path_(one)?q=1)2\">a_b[c]</a>";
            var converter = new Converter(new Config { TelegramMarkdownV2 = true });

            var result = converter.Convert(html);

            Assert.Equal("[a\\_b\\[c\\]](https://example.com/path_(one\\)?q=1\\)2)", result);
        }

        [Fact]
        public void TelegramMarkdownV2_EscapesListMarkers()
        {
            var html = "<ul><li>Item 1</li></ul><ol><li>Item 2</li></ol>";
            var converter = new Converter(new Config { TelegramMarkdownV2 = true });

            var result = converter.Convert(html);

            Assert.Contains("\\- Item 1", result);
            Assert.Contains("1\\. Item 2", result);
        }

        [Fact]
        public void TelegramMarkdownV2_Img_FallsBackToLink()
        {
            var converter = new Converter(new Config { TelegramMarkdownV2 = true });

            var result = converter.Convert("<img src=\"https://example.com/test.png\" />");

            Assert.Equal("[Image](https://example.com/test.png)", result);
        }

        [Fact]
        public void TelegramMarkdownV2_Sup_FallsBackToCaretNotation()
        {
            var converter = new Converter(new Config { TelegramMarkdownV2 = true });

            var result = converter.Convert("x<sup>2</sup>");

            Assert.Equal("x^2", result);
        }

        [Fact]
        public void TelegramMarkdownV2_Table_FallsBackToCodeBlock()
        {
            var converter = new Converter(new Config { TelegramMarkdownV2 = true });

            var result = converter.Convert("<table><tr><td>value</td></tr></table>");

            Assert.Contains("```", result);
            Assert.Contains("value", result);
        }

        [Fact]
        public void SlackFlavored_Unsupported_Img()
        {
            var html = LoadHtml("SlackFlavored_Unsupported_Img");
            var config = new Config { SlackFlavored = true };
            var converter = new Converter(config);
            Assert.Throws<SlackUnsupportedTagException>(() => converter.Convert(html));
        }

        [Fact]
        public void SlackFlavored_Unsupported_Sup()
        {
            var html = LoadHtml("SlackFlavored_Unsupported_Sup");
            var config = new Config { SlackFlavored = true };
            var converter = new Converter(config);
            Assert.Throws<SlackUnsupportedTagException>(() => converter.Convert(html));
        }

        [Fact]
        public void SlackFlavored_Unsupported_Table()
        {
            var html = LoadHtml("SlackFlavored_Unsupported_Table");
            var config = new Config { SlackFlavored = true };
            var converter = new Converter(config);
            Assert.Throws<SlackUnsupportedTagException>(() => converter.Convert(html));
        }

        [Fact]
        public void SlackFlavored_Unsupported_Table_Td()
        {
            var html = LoadHtml("SlackFlavored_Unsupported_Table_Td");
            var config = new Config { SlackFlavored = true };
            var converter = new Converter(config);
            Assert.Throws<SlackUnsupportedTagException>(() => converter.Convert(html));
        }

        [Fact]
        public void SlackFlavored_Unsupported_Table_Tr()
        {
            var html = LoadHtml("SlackFlavored_Unsupported_Table_Tr");
            var config = new Config { SlackFlavored = true };
            var converter = new Converter(config);
            Assert.Throws<SlackUnsupportedTagException>(() => converter.Convert(html));
        }

        

        

        

        

        

        // [Fact]
        // public Task When_TextWithinParagraphContainsNewlineChars_ConvertNewlineCharsToSpace()
        // {
        //     // note that the string also has a tab space
        //     var html =
        //         $"<p>This service will be{Environment.NewLine}temporarily unavailable due to planned maintenance{Environment.NewLine}from 02:00-04:00 on 30/01/2020</p>";
        //     return CheckConversion(html);
        // }

        // [Fact]
        // public Task WhenTableCellsWithPWithMarkupNewlines_ThenRenderBr()
        // {
        //     var html =
        //         $"<html><body><table><tbody>{Environment.NewLine}\t<tr>{Environment.NewLine}\t\t<td>{Environment.NewLine}\t\t\t<p>{Environment.NewLine}col1{Environment.NewLine}</p>{Environment.NewLine}\t\t</td>{Environment.NewLine}\t<tr>{Environment.NewLine}\t\t<td>{Environment.NewLine}\t\t\t<p>{Environment.NewLine}data1{Environment.NewLine}</p>{Environment.NewLine}\t\t</td>\t</tr></tbody></table></body></html>";
        //
        //     return CheckConversion(html);
        // }

    }

    public class DataDrivenCasesTests
    {
        public static IEnumerable<object[]> Cases => LoadCases()
            .Select(testCase => new object[] { testCase });

        [Theory]
        [MemberData(nameof(Cases))]
        public void CaseRuns(CaseData testCase)
        {
            var config = BuildConfig(testCase);
            var converter = new Converter(config);
            var result = converter.Convert(testCase.Html);
            var expected = LoadExpected(testCase);
            Assert.Equal(expected, result);
        }

        private static IEnumerable<CaseData> LoadCases()
        {
            var path = GetCasesPath();
            if (!File.Exists(path)) {
                throw new FileNotFoundException("cases.json not found", path);
            }

            var json = File.ReadAllText(path);
            var cases = JsonSerializer.Deserialize<List<CaseData>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            var filtered = (cases ?? new List<CaseData>()).Where(testCase => !testCase.DataOnly);
            return ApplyTagFilter(filtered);
        }

        private static IEnumerable<CaseData> ApplyTagFilter(IEnumerable<CaseData> cases)
        {
            var filter = Environment.GetEnvironmentVariable("RM_TEST_TAGS");
            if (string.IsNullOrWhiteSpace(filter)) {
                return cases;
            }

            var tags = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return cases.Where(testCase => testCase.Tags.Any(tag => tags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
        }

        private static string LoadExpected(CaseData testCase)
        {
            if (!string.IsNullOrWhiteSpace(testCase.Expected)) {
                return testCase.Expected;
            }

            if (string.IsNullOrWhiteSpace(testCase.ExpectedFile)) {
                throw new InvalidOperationException($"Case '{testCase.Id}' missing expected output.");
            }

            var projectDir = GetProjectDirectory();
            var path = Path.Combine(projectDir.FullName, testCase.ExpectedFile);
            if (!File.Exists(path)) {
                throw new FileNotFoundException($"Expected file not found for '{testCase.Id}'", path);
            }

            var expected = File.ReadAllText(path);
            if (expected.EndsWith("\r\n", StringComparison.Ordinal)) {
                return expected[..^2];
            }

            if (expected.EndsWith("\n", StringComparison.Ordinal)) {
                return expected[..^1];
            }

            return expected;
        }

        private static Config BuildConfig(CaseData testCase)
        {
            if (!string.IsNullOrWhiteSpace(testCase.ConfigPreset)) {
                return ConfigPresets.Get(testCase.ConfigPreset);
            }

            var config = new Config();
            var overrides = testCase.Config;
            if (overrides == null) {
                return config;
            }

            ApplyBool(overrides.GithubFlavored, value => config.GithubFlavored = value);
            ApplyBool(overrides.SlackFlavored, value => config.SlackFlavored = value);
            ApplyBool(overrides.TelegramMarkdownV2, value => config.TelegramMarkdownV2 = value);
            ApplyBool(overrides.CommonMark, value => config.CommonMark = value);
            ApplyBool(overrides.CommonMarkIntrawordEmphasisSpacing,
                value => config.CommonMarkIntrawordEmphasisSpacing = value);
            ApplyBool(overrides.CommonMarkUseHtmlInlineTags,
                value => config.CommonMarkUseHtmlInlineTags = value);
            ApplyBool(overrides.SuppressDivNewlines, value => config.SuppressDivNewlines = value);
            ApplyBool(overrides.RemoveComments, value => config.RemoveComments = value);
            ApplyBool(overrides.SmartHrefHandling, value => config.SmartHrefHandling = value);
            ApplyBool(overrides.CleanupUnnecessarySpaces, value => config.CleanupUnnecessarySpaces = value);
            ApplyBool(overrides.TableHeaderColumnSpanHandling,
                value => config.TableHeaderColumnSpanHandling = value);

            if (!string.IsNullOrWhiteSpace(overrides.DefaultCodeBlockLanguage)) {
                config.DefaultCodeBlockLanguage = overrides.DefaultCodeBlockLanguage;
            }

            if (!string.IsNullOrWhiteSpace(overrides.ListBulletChar)) {
                config.ListBulletChar = overrides.ListBulletChar[0];
            }

            ApplyEnum<Config.UnknownTagsOption>(overrides.UnknownTags,
                value => config.UnknownTags = value);
            ApplyEnum<Config.TableWithoutHeaderRowHandlingOption>(overrides.TableWithoutHeaderRowHandling,
                value => config.TableWithoutHeaderRowHandling = value);
            ApplyEnum<Config.Base64ImageHandling>(overrides.Base64Images,
                value => config.Base64Images = value);

            if (!string.IsNullOrWhiteSpace(overrides.Base64ImageSaveDirectory)) {
                config.Base64ImageSaveDirectory = overrides.Base64ImageSaveDirectory;
            }

            if (overrides.WhitelistUriSchemes.Length > 0) {
                foreach (var scheme in overrides.WhitelistUriSchemes) {
                    config.WhitelistUriSchemes.Add(scheme);
                }
            }

            if (overrides.PassThroughTags.Length > 0) {
                foreach (var tag in overrides.PassThroughTags) {
                    config.PassThroughTags.Add(tag);
                }
            }

            if (overrides.UnknownTagsReplacer.Count > 0) {
                foreach (var entry in overrides.UnknownTagsReplacer) {
                    config.UnknownTagsReplacer[entry.Key] = entry.Value;
                }
            }

            if (overrides.TagAliases.Count > 0) {
                foreach (var entry in overrides.TagAliases) {
                    config.TagAliases[entry.Key] = entry.Value;
                }
            }

            return config;
        }

        private static void ApplyBool(bool? value, Action<bool> apply)
        {
            if (value.HasValue) {
                apply(value.Value);
            }
        }

        private static void ApplyEnum<T>(string value, Action<T> apply) where T : struct
        {
            if (string.IsNullOrWhiteSpace(value)) {
                return;
            }

            if (Enum.TryParse<T>(value, out var parsed)) {
                apply(parsed);
            }
        }

        private static DirectoryInfo GetProjectDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ReverseMarkdown.Test.csproj"))) {
                directory = directory.Parent;
            }

            if (directory == null) {
                throw new DirectoryNotFoundException("Could not locate test project directory.");
            }

            return directory;
        }

        private static string GetCasesPath()
        {
            return Path.Combine(GetProjectDirectory().FullName, "TestData", "cases.json");
        }

        public class CaseData
        {
            public string Id { get; set; } = string.Empty;
            public string Html { get; set; } = string.Empty;
            public string Expected { get; set; } = string.Empty;
            public string ExpectedFile { get; set; } = string.Empty;
            public CaseConfig Config { get; set; } = new CaseConfig();
            public string ConfigPreset { get; set; } = string.Empty;
            public string[] Tags { get; set; } = Array.Empty<string>();
            public bool DataOnly { get; set; }

            public override string ToString()
            {
                return Id;
            }
        }

        public class CaseConfig
        {
            public bool? GithubFlavored { get; set; }
            public bool? SlackFlavored { get; set; }
            public bool? TelegramMarkdownV2 { get; set; }
            public bool? CommonMark { get; set; }
            public bool? CommonMarkIntrawordEmphasisSpacing { get; set; }
            public bool? CommonMarkUseHtmlInlineTags { get; set; }
            public bool? SuppressDivNewlines { get; set; }
            public bool? RemoveComments { get; set; }
            public bool? SmartHrefHandling { get; set; }
            public bool? CleanupUnnecessarySpaces { get; set; }
            public bool? TableHeaderColumnSpanHandling { get; set; }
            public string DefaultCodeBlockLanguage { get; set; } = string.Empty;
            public string ListBulletChar { get; set; } = string.Empty;
            public string UnknownTags { get; set; } = string.Empty;
            public string TableWithoutHeaderRowHandling { get; set; } = string.Empty;
            public string Base64Images { get; set; } = string.Empty;
            public string Base64ImageSaveDirectory { get; set; } = string.Empty;
            public string[] WhitelistUriSchemes { get; set; } = Array.Empty<string>();
            public string[] PassThroughTags { get; set; } = Array.Empty<string>();
            public Dictionary<string, string> UnknownTagsReplacer { get; set; } = new();
            public Dictionary<string, string> TagAliases { get; set; } = new();
        }

        private static class ConfigPresets
        {
            public static Config Get(string name)
            {
                throw new InvalidOperationException($"Unknown config preset '{name}'.");
            }
        }
    }
}

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        


        

        

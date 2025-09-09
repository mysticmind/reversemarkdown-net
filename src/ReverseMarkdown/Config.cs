using System;
using System.Linq;

namespace ReverseMarkdown
{
    public class Config
    {
        public bool GithubFlavored { get; set; } = false;

        public bool SlackFlavored { get; set; } = false;

        public bool SuppressDivNewlines { get; set; } = false;

        public bool RemoveComments { get; set; } = false;

        /// <summary>
        /// Specify which schemes (without a trailing colon) are to be allowed for &lt;a&gt; and &lt;img&gt; tags. Others will be bypassed. By default, allows everything.
        /// <para>If <see cref="string.Empty" /> provided and when href schema couldn't be determined - allowlists</para>
        /// </summary>
        public string[] WhitelistUriSchemes { get; set; }

        /// <summary>
        /// How to handle &lt;a&gt; tag href attribute
        /// <para>false - Outputs [{name}]({href}{title}) even if name and href are identical. This is the default option.</para>
        /// True - If name and href equal, outputs are just the `name`. Note that if Uri is not well-formed as per <see cref="Uri.IsWellFormedUriString"/> (i.e., string is not correctly escaped like `http://example.com/path/file name.docx`) then Markdown syntax will be used anyway.
        /// <para>If href contains http/https protocol, and the name doesn't, but otherwise is the same, output href only</para>
        /// If tel: or mailto: scheme, but afterward identical with name, output name only.
        /// </summary>
        public bool SmartHrefHandling { get; set; } = false;

        public TableWithoutHeaderRowHandlingOption TableWithoutHeaderRowHandling { get; set; } =
            TableWithoutHeaderRowHandlingOption.Default;

        private char _listBulletChar = '-';

        /// <summary>
        /// Option to set a different bullet character for unordered lists
        /// </summary>
        /// <remarks>
        /// This option is ignored when <see cref="SlackFlavored"/> is enabled.
        /// </remarks>
        public char ListBulletChar
        {
            get => SlackFlavored ? '•' : _listBulletChar;
            set => _listBulletChar = value;
        }

        /// <summary>
        /// Option to set a default GFM code block language if class-based language markers are not available
        /// </summary>
        public string DefaultCodeBlockLanguage { get; set; }
        
        /// <summary>
        /// Option to pass a list of tags to pass through as is without any processing
        /// </summary>
        public string[] PassThroughTags { get; set; } = { };
        
        /// <summary>
        /// Option to pass a list of tags to drop without any processing
        /// </summary>
        public string[] DropTags { get; set; } = { };
        
        public enum TableWithoutHeaderRowHandlingOption
        {
            /// <summary>
            /// By default, the first row will be used as the header row
            /// </summary>
            Default,

            /// <summary>
            /// An empty row will be added as the header row
            /// </summary>
            EmptyRow
        }

        /// <summary>
        /// Set this flag to handle the table header column with column spans
        /// </summary>
        public bool TableHeaderColumnSpanHandling { get; set; } = true;

        /// <summary>
        /// Determines whether unnecessary spaces should be removed in the output Markdown.
        /// When set to <c>true</c>, spaces at the beginning and end of content, as well as multiple consecutive newlines or spaces,
        /// will be trimmed or normalized in generated Markdown.
        /// If <c>false</c>, the spacing of output Markdown remains unaltered.
        /// </summary>
        public bool CleanupUnnecessarySpaces { get; set; } = true;

        /// <summary>
        /// Indicates whether header and footer information should be excluded during processing.
        /// When set to true, header and footer content will be skipped.
        /// </summary>
        public bool SkipHeaderFooter { get; set; } = true;

        /// <summary>
        /// Determines whether navigation elements (such as &lt;nav&gt; tags) should be skipped during processing.
        /// When enabled, these elements will not be included in the output.
        /// </summary>
        public bool SkipNav { get; set; } = true;

        /// <summary>
        /// Determines whether url is allowed: WhitelistUriSchemes contains no elements or contains passed url.
        /// </summary>
        /// <param name="scheme">Scheme name without trailing colon</param>
        internal bool IsSchemeWhitelisted(string scheme)
        {
            if (scheme == null) throw new ArgumentNullException(nameof(scheme));
            var isSchemeAllowed = WhitelistUriSchemes == null || WhitelistUriSchemes.Length == 0 ||
                                  WhitelistUriSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase);
            return isSchemeAllowed;
        }
    }
}
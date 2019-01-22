using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReverseMarkdown
{
    public class Config
    {
        public Config()
        {
        }

        public Config(UnknownTagsOption unknownTags = UnknownTagsOption.PassThrough, bool githubFlavored = false, bool removeComments = false)
        {
            UnknownTags = unknownTags;
            GithubFlavored = githubFlavored;
            RemoveComments = removeComments;
        }

        public UnknownTagsOption UnknownTags { get; } = UnknownTagsOption.PassThrough;

        public bool GithubFlavored { get; }

        public bool RemoveComments { get; }


        /// <summary>
        /// Specify which schemes (without trailing colon) are to be allowed for <a> and <img> tags. Others will be bypassed. By default allows everything.
        /// <para>If <see cref="string.Empty" /> provided and when href schema coudn't be determined - whitelists</para>
        /// </summary>
        public string[] WhitelistUriSchemes { get; set; }

        public HrefHandlingOption HrefHandling { get; set; } = HrefHandlingOption.None;

        public enum UnknownTagsOption
        {
            /// <summary>
            /// Include the unknown tag completely into the result. That is, the tag along with the text will be left in output.
            /// </summary>
            PassThrough,
            /// <summary>
            ///  Drop the unknown tag and its content
            /// </summary>
            Drop,
            /// <summary>
            /// Ignore the unknown tag but try to convert its content
            /// </summary>
            Bypass,
            /// <summary>
            /// Raise an error to let you know
            /// </summary>
            Raise
        }

        public enum HrefHandlingOption {
            /// <summary>
            /// Outputs [{name}]({href}{title}) even if name and href is identical. This is the default option.
            /// </summary>
            None,
            /// <summary>
            /// If name and href equals, outputs just the name instead of [{name}]({href}{title}). Note that if Uri is not well formed (string is not correctly escaped like http://example.com/path/file name.docx) then markdown syntax will be used anyway.
            /// <para>If href contains http/https protocol, and name doesn't but otherwise are the same, output href only</para>
            /// If tel: or mailto: url, but afterwards identical with name, output name only.
            /// </summary>
            Smart
        }


        /// <summary>
        /// Determines whether url is allowed: WhitelistUriSchemes contains no elements or contains passed url.
        /// </summary>
        /// <param name="url">Scheme name without trailing colon</param>
        internal bool IsSchemeWhitelisted(string scheme) {
            if (scheme == null) throw new ArgumentNullException(nameof(scheme));
            var isSchemeAllowed = WhitelistUriSchemes == null || WhitelistUriSchemes.Length == 0 || WhitelistUriSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase);
            return isSchemeAllowed;
        }
    }
}

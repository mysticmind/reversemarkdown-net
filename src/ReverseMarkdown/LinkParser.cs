using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReverseMarkdown {
    internal static class LinkParser {

        /// <summary>
        /// <para>Gets scheme for provided uri string to overcome different behavior between windows/linux. https://github.com/dotnet/corefx/issues/1745</para>
        /// Assume http for url starting with //
        /// <para>Assume file for url starting with /</para>
        /// Otherwise give what <see cref="Uri.Scheme" /> gives us.
        /// <para>If non parseable by Uri, return empty string. Will never return null</para>
        /// </summary>
        /// <returns></returns>
        internal static string GetScheme(string url) {
            var isValidUri = Uri.TryCreate(url, UriKind.Absolute, out Uri uri);
            //IETF RFC 3986 
            if (Regex.IsMatch(url, "^//[^/]")) {
                return "http";
            }
            //Unix style path
            else if (Regex.IsMatch(url, "^/[^/]")) {
                return "file";
            } 
            else if (isValidUri) {
                return uri.Scheme;
            }
            else {
                return String.Empty;
            }
        }
    }
}

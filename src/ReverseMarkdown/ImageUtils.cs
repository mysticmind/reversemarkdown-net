using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ReverseMarkdown
{
    public static class ImageUtils
    {
        public static string SaveBase64Image(string base64Image, string targetDir, string? fileNameWithoutExtension = null)
        {
            if (string.IsNullOrWhiteSpace(base64Image))
                throw new ArgumentException("Base64 image string is null or empty.", nameof(base64Image));

            // Regex to extract mime type and base64 data
            var match = Regex.Match(base64Image, @"^data:(?<mime>[\w/\-\.]+);base64,(?<data>.+)$");
            if (!match.Success)
                throw new FormatException("Invalid or non supported base64 image format.");

            var mimeType = match.Groups["mime"].Value;  // e.g. "image/png"
            var base64Data = match.Groups["data"].Value;

            // Get extension from a MIME type
            var extension = MimeTypeToExtension(mimeType);
            
            if (string.IsNullOrEmpty(extension))
                throw new FormatException("Invalid or non supported base64 image format.");

            // Decode
            var imageBytes = Convert.FromBase64String(base64Data);

            // Build the file path
            var fileName = (fileNameWithoutExtension ?? "image") + extension;
            var filePath = Path.Combine(targetDir, fileName);
            File.WriteAllBytes(filePath, imageBytes);
            return filePath;
        }

        public static bool IsValidBase64ImageData(string data)
        {
            var match = Regex.Match(data, @"^data:image/(?<ext>[a-zA-Z0-9+\-\.]+);base64,(?<data>[a-zA-Z0-9+/=\r\n]+)$");

            if (!match.Success)
                return false;

            var mimeType = "image/" + match.Groups["ext"].Value;
            var extension = MimeTypeToExtension(mimeType);
            return !string.IsNullOrWhiteSpace(extension);
        }

        private static string MimeTypeToExtension(string mimeType)
        {
            return mimeType.ToLower() switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/gif" => ".gif",
                "image/bmp" => ".bmp",
                "image/tiff" => ".tiff",
                "image/webp" => ".webp",
                "image/svg+xml" => ".svg",
                _ => ""
            };
        }
    }

}
using System;

namespace Clippy.Models
{
    public enum ClipboardEntryType
    {
        Text,
        Html,
        Image
    }

    public class ClipboardEntry
    {
        public long Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? HtmlContent { get; set; }
        public string Preview { get; set; } = string.Empty;
        public ClipboardEntryType EntryType { get; set; } = ClipboardEntryType.Text;
        public string? ImagePath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsPinned { get; set; }
        public string ContentHash { get; set; } = string.Empty;
        public string SourceApp { get; set; } = string.Empty;

        public static string ComputeHash(string content)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static string ComputeHash(byte[] data)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = sha.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }

        public static string CreatePreview(string content, int maxLength = 120)
        {
            if (string.IsNullOrEmpty(content)) return string.Empty;
            var singleLine = content.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
            return singleLine.Length > maxLength
                ? singleLine[..maxLength] + "..."
                : singleLine;
        }

        public bool IsImage => EntryType == ClipboardEntryType.Image;
        public bool IsHtml => EntryType == ClipboardEntryType.Html;
    }
}

using System.Collections.Generic;

namespace LinCms.Web.Models.Video
{
    public class DownloadRequestDto
    {
        public string Url { get; set; } = string.Empty;
        public string? IdempotencyKey { get; set; }
        public string? FormatId { get; set; }
        public string? Quality { get; set; } // low|medium|high|auto
        public bool? AudioOnly { get; set; }
        public string? FilenameTemplate { get; set; }
        public string? Proxy { get; set; }
        public string? RateLimit { get; set; }
        public string? PlaylistItems { get; set; }
        public string? Referer { get; set; }
        public Dictionary<string,string>? Headers { get; set; }
        public string? ExtractorArgs { get; set; }
        public string? Cookies { get; set; }
        public string? CookiesFile { get; set; }
    }
}

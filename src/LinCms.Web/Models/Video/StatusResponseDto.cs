namespace LinCms.Web.Models.Video
{
    public class StatusResponseDto
    {
        public string TaskId { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public long CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public object? Progress { get; set; }
        public object? Output { get; set; }
        public object? Error { get; set; }
    }
}

namespace LinCms.Web.Models.Video
{
    public class DownloadResponseDto
    {
        public string TaskId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // queued/running/success/failed/canceled
    }
}

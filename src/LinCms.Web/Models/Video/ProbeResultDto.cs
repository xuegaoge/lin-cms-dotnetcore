namespace LinCms.Web.Models.Video
{
    public class ProbeResultDto
    {
        public bool Ok { get; set; }
        public string? DirectUrl { get; set; }
        public bool? SeparateStreams { get; set; }
        public string? DirectVideoUrl { get; set; }
        public string? DirectAudioUrl { get; set; }
        public string? Resolver { get; set; }
        public string? FallbackUrl { get; set; }
        public string? Reason { get; set; }
    }
}

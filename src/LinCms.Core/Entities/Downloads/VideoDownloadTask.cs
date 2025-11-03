using IGeekFan.FreeKit.Extras.AuditEntity;
using FreeSql.DataAnnotations;
using System;

namespace LinCms.Entities.Downloads
{
    /// <summary>
    /// 视频下载任务（MVP）
    /// </summary>
    [Table(Name = "download_video_task")]
    public class VideoDownloadTask : FullAuditEntity<long, long>
    {
        /// <summary>
        /// 原始视频URL
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 任务状态：Queued|Running|Success|Failed
        /// </summary>
        public string Status { get; set; } = "Queued";

        /// <summary>
        /// 进度百分比（0-100）
        /// </summary>
        public double? ProgressPercent { get; set; }

        /// <summary>
        /// 下载速度（文本，如 1.2 MB/s）
        /// </summary>
        public string? Speed { get; set; }

        /// <summary>
        /// 预计剩余时间（文本，如 00:01:23）
        /// </summary>
        public string? Eta { get; set; }

        /// <summary>
        /// 生成的文件相对路径（相对下载根目录）
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// 扩展名（mp4/flv等）
        /// </summary>
        public string? Ext { get; set; }

        /// <summary>
        /// 错误码（可选）
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// 错误信息（可选）
        /// </summary>
        public string? ErrorMsg { get; set; }

        /// <summary>
        /// 归属用户Id（发起者）
        /// </summary>
        public long? UserId { get; set; }
    }
}
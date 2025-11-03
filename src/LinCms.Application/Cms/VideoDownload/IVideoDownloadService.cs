using LinCms.Entities.Downloads;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinCms.Cms.VideoDownload
{
    public interface IVideoDownloadService
    {
        Task<long> CreateTaskAsync(string url, long? userId);
        Task<VideoDownloadTask?> GetTaskAsync(long taskId);
        Task<(long total, List<VideoDownloadTask> items)> GetTasksAsync(int page, int size, long? userId);
        Task EnqueueAsync(long taskId);
    }
}
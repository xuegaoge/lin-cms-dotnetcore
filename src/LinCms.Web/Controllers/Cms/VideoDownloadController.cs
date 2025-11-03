using LinCms.Cms.VideoDownload;
using LinCms.Entities.Downloads;
using LinCms.Security;
using Microsoft.AspNetCore.Mvc;
using IGeekFan.FreeKit.Extras.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace LinCms.Web.Controllers.Cms
{
    [Route("cms/video-download")]
    [ApiController]
    [Authorize]
    [ApiExplorerSettings(GroupName = "cms")]
    public class VideoDownloadController : ControllerBase
    {
        private readonly IVideoDownloadService _service;
        private readonly ICurrentUser _currentUser;
        private readonly IConfiguration _config;
        private readonly ILogger<VideoDownloadController> _logger;

        public VideoDownloadController(IVideoDownloadService service, ICurrentUser currentUser, IConfiguration config, ILogger<VideoDownloadController> logger)
        {
            _service = service;
            _currentUser = currentUser;
            _config = config;
            _logger = logger;
        }

        // 现有本地任务创建（保留）
        [HttpPost("tasks")]
        public async Task<object> CreateTaskAsync([FromBody] CreateTaskInput input)
        {
            // 统一走 sidecar：POST /download
            var api = _config.GetSection("YtDlp").GetValue<string>("ApiBase");
            if (string.IsNullOrWhiteSpace(api)) return BadRequest(new { code = "ConfigError", message = "YtDlp.ApiBase not configured" });
            using var http = new HttpClient();
            var payload = JsonSerializer.Serialize(new { url = input.Url });
            var resp = await http.PostAsync($"{api.TrimEnd('/')}/download", new StringContent(payload, Encoding.UTF8, "application/json"));
            var text = await resp.Content.ReadAsStringAsync();
            // 直接透传 sidecar 返回，兼容前端
            return Content(text, "application/json", Encoding.UTF8);
        }

        // 现有本地任务查询（保留）
        [HttpGet("tasks/{taskId:long}")]
        public async Task<object> GetTaskAsync([FromRoute] long taskId)
        {
            // 统一走 sidecar：GET /status/{id}
            var api = _config.GetSection("YtDlp").GetValue<string>("ApiBase");
            if (string.IsNullOrWhiteSpace(api)) return BadRequest(new { code = "ConfigError", message = "YtDlp.ApiBase not configured" });
            using var http = new HttpClient();
            var resp = await http.GetAsync($"{api.TrimEnd('/')}/status/{taskId}");
            var text = await resp.Content.ReadAsStringAsync();
            return Content(text, "application/json", Encoding.UTF8);
        }

        [HttpGet("tasks")]
        public async Task<object> GetTasksAsync([FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var resp = await _service.GetTasksAsync(page, size, _currentUser.FindUserId());
            return new { total = resp.total, items = resp.items };
        }

        // ========== Sidecar 代理最小集 ==========

        [AllowAnonymous]
        [HttpGet("health")]
        public async Task<IActionResult> HealthAsync()
        {
            var api = _config.GetSection("YtDlp").GetValue<string>("ApiBase");
            if (string.IsNullOrWhiteSpace(api)) return Ok(new { ok = false, message = "ApiBase not configured" });
            using var http = new HttpClient();
            try
            {
                var resp = await http.GetAsync($"{api.TrimEnd('/')}/health");
                var text = await resp.Content.ReadAsStringAsync();
                return Content(text, "application/json", Encoding.UTF8);
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "Sidecar health 调用失败");
                return StatusCode(502, new { ok = false, message = "sidecar unreachable" });
            }
        }

        // POST /cms/video-download/sidecar/tasks  -> sidecar /download
        [HttpPost("sidecar/tasks")]
        public async Task<IActionResult> CreateSidecarTaskAsync([FromBody] CreateTaskInput input)
        {
            var api = _config.GetSection("YtDlp").GetValue<string>("ApiBase");
            if (string.IsNullOrWhiteSpace(api)) return BadRequest(new { code = "ConfigError", message = "YtDlp.ApiBase not configured" });

            using var http = new HttpClient();
            var payload = JsonSerializer.Serialize(new { url = input.Url });
            var resp = await http.PostAsync($"{api.TrimEnd('/')}/download", new StringContent(payload, Encoding.UTF8, "application/json"));
            var text = await resp.Content.ReadAsStringAsync();
            return Content(text, "application/json", Encoding.UTF8);
        }

        // GET /cms/video-download/sidecar/tasks/{taskId} -> sidecar /status/{taskId}
        [HttpGet("sidecar/tasks/{taskId}")]
        public async Task<IActionResult> GetSidecarTaskAsync([FromRoute] string taskId)
        {
            var api = _config.GetSection("YtDlp").GetValue<string>("ApiBase");
            if (string.IsNullOrWhiteSpace(api)) return BadRequest(new { code = "ConfigError", message = "YtDlp.ApiBase not configured" });

            using var http = new HttpClient();
            var resp = await http.GetAsync($"{api.TrimEnd('/')}/status/{taskId}");
            var text = await resp.Content.ReadAsStringAsync();
            return Content(text, "application/json", Encoding.UTF8);
        }

        // POST /cms/video-download/sidecar/cancel/{taskId} -> sidecar /cancel/{taskId}
        [HttpPost("sidecar/cancel/{taskId}")]
        public async Task<IActionResult> CancelSidecarTaskAsync([FromRoute] string taskId)
        {
            var api = _config.GetSection("YtDlp").GetValue<string>("ApiBase");
            if (string.IsNullOrWhiteSpace(api)) return BadRequest(new { code = "ConfigError", message = "YtDlp.ApiBase not configured" });

            using var http = new HttpClient();
            var resp = await http.PostAsync($"{api.TrimEnd('/')}/cancel/{taskId}", new StringContent("", Encoding.UTF8, "application/json"));
            var text = await resp.Content.ReadAsStringAsync();
            return Content(text, "application/json", Encoding.UTF8);
        }

        // GET /cms/video-download/formats?url=...
        [HttpGet("formats")]
        public async Task<IActionResult> GetFormatsAsync([FromQuery] string url)
        {
            var api = _config.GetSection("YtDlp").GetValue<string>("ApiBase");
            if (string.IsNullOrWhiteSpace(api)) return BadRequest(new { code = "ConfigError", message = "YtDlp.ApiBase not configured" });
            if (string.IsNullOrWhiteSpace(url)) return BadRequest(new { code = "BadRequest", message = "url required" });

            using var http = new HttpClient();
            var resp = await http.GetAsync($"{api.TrimEnd('/')}/formats?url={System.Web.HttpUtility.UrlEncode(url)}");
            var text = await resp.Content.ReadAsStringAsync();
            return Content(text, "application/json", Encoding.UTF8);
        }

        public class CreateTaskInput
        {
            public string Url { get; set; } = string.Empty;
        }
    }
}
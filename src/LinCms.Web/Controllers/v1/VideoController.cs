using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LinCms.Web.Models.Video;
using Microsoft.AspNetCore.Mvc;

namespace LinCms.Web.Controllers.V1
{
    [ApiController]
    [Route("v1/video")]
    [ApiExplorerSettings(GroupName = "v1")]
    public class VideoController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VideoController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            var client = _httpClientFactory.CreateClient("YtDlpSidecar");
            var res = await client.GetAsync("/health");
            return StatusCode((int)res.StatusCode, await res.Content.ReadAsStringAsync());
        }

        [HttpGet("probe")]
        public async Task<ActionResult<ProbeResultDto>> Probe([FromQuery] string url)
        {
            var client = _httpClientFactory.CreateClient("YtDlpSidecar");
            var res = await client.GetAsync($"/probe?url={Uri.EscapeDataString(url)}");
            if (!res.IsSuccessStatusCode)
            {
                return Ok(new ProbeResultDto { Ok = false, FallbackUrl = url, Reason = $"HTTP {(int)res.StatusCode}" });
            }
            var json = await res.Content.ReadFromJsonAsync<ProbeResultDto>();
            if (json == null || !json.Ok)
            {
                return Ok(new ProbeResultDto { Ok = false, FallbackUrl = url, Reason = "ProbeFailed" });
            }
            return Ok(json);
        }

        [HttpPost("download")]
        public async Task<ActionResult<DownloadResponseDto>> Download([FromBody] DownloadRequestDto request)
        {
            var client = _httpClientFactory.CreateClient("YtDlpSidecar");
            var res = await client.PostAsJsonAsync("/download", request);
            if (!res.IsSuccessStatusCode)
            {
                return StatusCode((int)res.StatusCode, await res.Content.ReadAsStringAsync());
            }
            var json = await res.Content.ReadFromJsonAsync<DownloadResponseDto>();
            return Ok(json);
        }

        [HttpGet("status/{taskId}")]
        public async Task<ActionResult<StatusResponseDto>> Status([FromRoute] string taskId)
        {
            var client = _httpClientFactory.CreateClient("YtDlpSidecar");
            var res = await client.GetAsync($"/status/{Uri.EscapeDataString(taskId)}");
            if (!res.IsSuccessStatusCode)
            {
                return StatusCode((int)res.StatusCode, await res.Content.ReadAsStringAsync());
            }
            var json = await res.Content.ReadFromJsonAsync<StatusResponseDto>();
            return Ok(json);
        }

        [HttpPost("cancel/{taskId}")]
        public async Task<IActionResult> Cancel([FromRoute] string taskId)
        {
            var client = _httpClientFactory.CreateClient("YtDlpSidecar");
            var res = await client.PostAsync($"/cancel/{Uri.EscapeDataString(taskId)}", content: null);
            return StatusCode((int)res.StatusCode, await res.Content.ReadAsStringAsync());
        }

        [HttpGet("formats")]
        public async Task<IActionResult> Formats([FromQuery] string url)
        {
            var client = _httpClientFactory.CreateClient("YtDlpSidecar");
            var res = await client.GetAsync($"/formats?url={Uri.EscapeDataString(url)}");
            return StatusCode((int)res.StatusCode, await res.Content.ReadAsStringAsync());
        }
    }
}

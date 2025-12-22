using LinCms.Application.Contracts.Selection;
using LinCms.Application.Selection.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace LinCms.Web.Controllers.Selection
{
    /// <summary>
    /// 产品指标历史API
    /// </summary>
    [Route("api/selection/products/{productId}/metrics")]
    [ApiController]
    public class MetricsController : ControllerBase
    {
        private readonly ProductMetricsHistoryService _metricsService;

        public MetricsController(ProductMetricsHistoryService metricsService)
        {
            _metricsService = metricsService;
        }

        /// <summary>
        /// 添加历史数据
        /// </summary>
        [HttpPost("history")]
        public async Task<IActionResult> AddMetrics(long productId, [FromBody] AddMetricsHistoryDto dto)
        {
            var result = await _metricsService.AddMetricsAsync(productId, dto);
            return Ok(new { code = 200, message = "数据添加成功", data = result });
        }

        /// <summary>
        /// 获取历史数据
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(
            long productId,
            [FromQuery] DateTime? start,
            [FromQuery] DateTime? end,
            [FromQuery] int page = 1,
            [FromQuery] int size = 100)
        {
            var result = await _metricsService.GetHistoryAsync(productId, start, end, page, size);
            return Ok(new { code = 200, data = result });
        }

        /// <summary>
        /// 获取趋势分析
        /// </summary>
        [HttpGet("trends")]
        public async Task<IActionResult> GetTrends(long productId, [FromQuery] int days = 30)
        {
            var result = await _metricsService.GetTrendAnalysisAsync(productId, days);
            return Ok(new { code = 200, data = result });
        }

        /// <summary>
        /// 批量上传历史数据
        /// </summary>
        [HttpPost("history/batch")]
        public async Task<IActionResult> BatchAddMetrics(long productId, [FromBody] System.Collections.Generic.List<AddMetricsHistoryDto> list)
        {
            var success = await _metricsService.BatchAddMetricsAsync(productId, list);
            return Ok(new { code = 200, message = $"成功导入{list.Count}条数据" });
        }
    }
}

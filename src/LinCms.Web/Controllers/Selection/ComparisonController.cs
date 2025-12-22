using LinCms.Application.Contracts.Selection;
using LinCms.Application.Selection.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LinCms.Web.Controllers.Selection
{
    /// <summary>
    /// 产品对比API
    /// </summary>
    [Route("api/selection/comparison")]
    [ApiController]
    public class ComparisonController : ControllerBase
    {
        private readonly ProductComparisonService _comparisonService;

        public ComparisonController(ProductComparisonService comparisonService)
        {
            _comparisonService = comparisonService;
        }

        /// <summary>
        /// 创建产品对比
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateComparison([FromBody] CreateComparisonDto dto)
        {
            var result = await _comparisonService.CreateComparisonAsync(dto);
            return Ok(new { code = 200, message = "对比创建成功", data = result });
        }

        /// <summary>
        /// 获取对比详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetComparison(long id)
        {
            var result = await _comparisonService.GetComparisonAsync(id);
            if (result == null)
            {
                return NotFound(new { code = 404, message = "对比记录不存在" });
            }
            return Ok(new { code = 200, data = result });
        }

        /// <summary>
        /// 获取对比列表
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetComparisons([FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            var result = await _comparisonService.GetComparisonsAsync(page, size);
            return Ok(new { code = 200, data = result });
        }

        /// <summary>
        /// 删除对比
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComparison(long id)
        {
            var success = await _comparisonService.DeleteComparisonAsync(id);
            if (!success)
            {
                return NotFound(new { code = 404, message = "对比记录不存在" });
            }
            return Ok(new { code = 200, message = "删除成功" });
        }
    }
}

using LinCms.Application.Contracts.Selection;
using LinCms.Application.Selection.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinCms.Web.Controllers.Selection
{
    /// <summary>
    /// 产品管理API
    /// </summary>
    [Route("api/selection/products")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ProductDataService _productService;

        public ProductController(ProductDataService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// 获取产品列表
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] ProductQueryDto query)
        {
            var (items, total) = await _productService.GetProductsAsync(query);
            
            return Ok(new
            {
                code = 200,
                data = new
                {
                    total,
                    page = query.Page,
                    size = query.Size,
                    pages = (int)System.Math.Ceiling((double)total / query.Size),
                    items
                }
            });
        }

        /// <summary>
        /// 获取单个产品详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(long id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound(new { code = 404, message = "产品不存在" });
            }

            return Ok(new { code = 200, data = product });
        }

        /// <summary>
        /// 创建产品
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateUpdateProductDto dto)
        {
            var product = await _productService.CreateProductAsync(dto);
            return Ok(new { code = 200, message = "创建成功", data = product });
        }

        /// <summary>
        /// 更新产品
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(long id, [FromBody] CreateUpdateProductDto dto)
        {
            var product = await _productService.UpdateProductAsync(id, dto);
            return Ok(new { code = 200, message = "更新成功", data = product });
        }

        /// <summary>
        /// 部分更新产品
        /// </summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchProduct(long id, [FromBody] CreateUpdateProductDto dto)
        {
            var product = await _productService.UpdateProductAsync(id, dto);
            return Ok(new { code = 200, message = "更新成功", data = product });
        }

        /// <summary>
        /// 删除产品
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(long id)
        {
            var success = await _productService.DeleteProductAsync(id);
            if (!success)
            {
                return NotFound(new { code = 404, message = "产品不存在" });
            }

            return Ok(new { code = 200, message = "删除成功" });
        }

        /// <summary>
        /// 批量删除产品
        /// </summary>
        [HttpDelete("batch")]
        public async Task<IActionResult> BatchDeleteProducts([FromBody] List<long> ids)
        {
            var count = await _productService.BatchDeleteProductsAsync(ids);
            return Ok(new { code = 200, message = $"成功删除{count}个产品" });
        }

        /// <summary>
        /// 批量导入产品
        /// </summary>
        [HttpPost("batch")]
        public async Task<IActionResult> BatchImportProducts()
        {
            // TODO: 实现批量导入逻辑
            return Ok(new { code = 200, message = "批量导入功能待实现" });
        }

        /// <summary>
        /// 导出产品
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> ExportProducts([FromQuery] string ids, [FromQuery] string format = "csv")
        {
            // TODO: 实现导出逻辑
            return Ok(new { code = 200, message = "导出功能待实现" });
        }
        /// <summary>
        /// 更新产品关键词 (S19)
        /// </summary>
        [HttpPut("{id}/keywords")]
        public async Task<IActionResult> UpdateKeywords(long id, [FromBody] List<ProductKeywordDto> dtos)
        {
            if (!await _productService.ExistsAsync(id))
            {
                return NotFound(new { code = 404, message = "产品不存在" });
            }

            await _productService.SaveKeywordsAsync(id, dtos);
            return Ok(new { code = 200, message = "关键词更新成功" });
        }

        /// <summary>
        /// 更新产品趋势数据 (S20)
        /// </summary>
        [HttpPut("{id}/trends")]
        public async Task<IActionResult> UpdateTrends(long id, [FromBody] List<ProductTrendDto> dtos)
        {
            if (!await _productService.ExistsAsync(id))
            {
                return NotFound(new { code = 404, message = "产品不存在" });
            }

            await _productService.SaveTrendsAsync(id, dtos);
            return Ok(new { code = 200, message = "趋势数据更新成功" });
        }
    }
}

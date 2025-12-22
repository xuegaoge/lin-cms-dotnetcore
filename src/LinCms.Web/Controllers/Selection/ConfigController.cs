using LinCms.Application.Contracts.Selection;
using LinCms.Application.Selection.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LinCms.Web.Controllers.Selection
{
    /// <summary>
    /// 全局配置API
    /// </summary>
    [Route("api/selection/config")]
    [ApiController]
    public class ConfigController : ControllerBase
    {
        private readonly GlobalConfigService _configService;

        public ConfigController(GlobalConfigService configService)
        {
            _configService = configService;
        }

        /// <summary>
        /// 获取配置列表
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetConfigs([FromQuery] string group = null, [FromQuery] int page = 1, [FromQuery] int size = 50)
        {
            var configs = await _configService.GetConfigsAsync(group, page, size);
            return Ok(new { code = 200, data = configs });
        }

        /// <summary>
        /// 获取单个配置
        /// </summary>
        [HttpGet("{group}/{key}")]
        public async Task<IActionResult> GetConfig(string group, string key)
        {
            var config = await _configService.GetConfigAsync(group, key);
            if (config == null)
            {
                return NotFound(new { code = 404, message = "配置不存在" });
            }

            return Ok(new { code = 200, data = config });
        }

        /// <summary>
        /// 创建配置
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateConfig([FromBody] CreateUpdateGlobalConfigDto dto)
        {
            var config = await _configService.CreateConfigAsync(dto);
            return Ok(new { code = 200, message = "创建成功", data = config });
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateConfig(long id, [FromBody] CreateUpdateGlobalConfigDto dto)
        {
            var config = await _configService.UpdateConfigAsync(id, dto);
            return Ok(new { code = 200, message = "更新成功", data = config });
        }

        /// <summary>
        /// 删除配置
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConfig(long id)
        {
            var success = await _configService.DeleteConfigAsync(id);
            if (!success)
            {
                return NotFound(new { code = 404, message = "配置不存在" });
            }

            return Ok(new { code = 200, message = "删除成功" });
        }
    }
}

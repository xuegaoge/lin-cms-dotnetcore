using LinCms.Application.Contracts.Selection;
using LinCms.Application.Selection.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LinCms.Web.Controllers.Selection
{
    /// <summary>
    /// 企业定位API
    /// </summary>
    [Route("api/selection/enterprise")]
    [ApiController]
    public class EnterpriseController : ControllerBase
    {
        private readonly EnterpriseProfileService _enterpriseService;

        public EnterpriseController(EnterpriseProfileService enterpriseService)
        {
            _enterpriseService = enterpriseService;
        }

        /// <summary>
        /// 获取当前企业定位
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetCurrentProfile([FromQuery] long organizationId = 1)
        {
            var profile = await _enterpriseService.GetCurrentProfileAsync(organizationId);
            if (profile == null)
            {
                return NotFound(new { code = 404, message = "企业定位不存在" });
            }

            return Ok(new { code = 200, data = profile });
        }

        /// <summary>
        /// 创建企业定位评估
        /// </summary>
        [HttpPost("profile")]
        public async Task<IActionResult> CreateProfile([FromBody] CreateEnterpriseProfileDto dto, [FromQuery] long organizationId = 1)
        {
            var profile = await _enterpriseService.CreateProfileAsync(organizationId, dto);
            return Ok(new { code = 200, message = "企业定位评估成功", data = profile });
        }

        /// <summary>
        /// 获取历史评估记录
        /// </summary>
        [HttpGet("profile/history")]
        public async Task<IActionResult> GetHistory([FromQuery] long organizationId = 1, [FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            var profiles = await _enterpriseService.GetHistoryAsync(organizationId, page, size);
            return Ok(new { code = 200, data = profiles });
        }

        /// <summary>
        /// 更新企业定位
        /// </summary>
        [HttpPut("profile/{id}")]
        public async Task<IActionResult> UpdateProfile(long id, [FromBody] CreateEnterpriseProfileDto dto)
        {
            // TODO: 实现更新逻辑
            return Ok(new { code = 200, message = "更新功能待实现" });
        }

        /// <summary>
        /// 激活某个历史评估
        /// </summary>
        [HttpPost("profile/{id}/activate")]
        public async Task<IActionResult> ActivateProfile(long id, [FromQuery] long organizationId = 1)
        {
            var success = await _enterpriseService.ActivateProfileAsync(id, organizationId);
            if (!success)
            {
                return NotFound(new { code = 404, message = "评估记录不存在" });
            }

            return Ok(new { code = 200, message = "激活成功" });
        }
    }
}

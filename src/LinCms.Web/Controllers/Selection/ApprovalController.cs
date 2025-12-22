using LinCms.Application.Contracts.Selection;
using LinCms.Application.Selection.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LinCms.Web.Controllers.Selection
{
    /// <summary>
    /// 审批流程API
    /// </summary>
    [Route("api/selection")]
    [ApiController]
    public class ApprovalController : ControllerBase
    {
        private readonly ProductApprovalService _approvalService;

        public ApprovalController(ProductApprovalService approvalService)
        {
            _approvalService = approvalService;
        }

        /// <summary>
        /// 提交产品审批
        /// </summary>
        [HttpPost("products/{productId}/approval")]
        public async Task<IActionResult> SubmitApproval(long productId, [FromBody] SubmitApprovalDto dto)
        {
            var result = await _approvalService.SubmitApprovalAsync(productId, dto);
            return Ok(new { code = 200, message = "审批已提交", data = result });
        }

        /// <summary>
        /// 审批操作
        /// </summary>
        [HttpPost("approval/{approvalId}/approve")]
        public async Task<IActionResult> Approve(long approvalId, [FromBody] ApproveActionDto dto)
        {
            // TODO: 从JWT token中获取当前用户ID
            long currentUserId = 1; // 临时硬编码
            
            var result = await _approvalService.ApproveAsync(approvalId, dto, currentUserId);
            return Ok(new { code = 200, message = "审批完成", data = result });
        }

        /// <summary>
        /// 获取审批状态
        /// </summary>
        [HttpGet("products/{productId}/approval/status")]
        public async Task<IActionResult> GetApprovalStatus(long productId)
        {
            var result = await _approvalService.GetApprovalStatusAsync(productId);
            if (result == null)
            {
                return NotFound(new { code = 404, message = "未找到审批记录" });
            }
            return Ok(new { code = 200, data = result });
        }

        /// <summary>
        /// 获取待我审批列表
        /// </summary>
        [HttpGet("approval/pending")]
        public async Task<IActionResult> GetPendingApprovals([FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            // TODO: 从JWT token中获取当前用户ID
            long currentUserId = 1; // 临时硬编码
            
            var result = await _approvalService.GetPendingApprovalsAsync(currentUserId, page, size);
            return Ok(new { code = 200, data = result });
        }

        /// <summary>
        /// 获取我的审批历史
        /// </summary>
        [HttpGet("approval/my-history")]
        public async Task<IActionResult> GetMyApprovalHistory([FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            // TODO: 从JWT token中获取当前用户ID
            long currentUserId = 1; // 临时硬编码
            
            var result = await _approvalService.GetMyApprovalHistoryAsync(currentUserId, page, size);
            return Ok(new { code = 200, data = result });
        }
    }
}

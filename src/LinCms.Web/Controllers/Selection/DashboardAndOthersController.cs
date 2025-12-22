using LinCms.Application.Selection.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LinCms.Web.Controllers.Selection
{
    /// <summary>
    /// BI仪表板API
    /// </summary>
    [Route("api/selection/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly BIDashboardService _dashboardService;

        public DashboardController(BIDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// 获取BI仪表板数据
        /// </summary>
        [HttpGet("bi")]
        public async Task<IActionResult> GetBIDashboard()
        {
            var data = await _dashboardService.GetDashboardDataAsync();
            return Ok(new { code = 200, data });
        }

        /// <summary>
        /// 获取预警列表
        /// </summary>
        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts([FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            var data = await _dashboardService.GetAlertsAsync(page, size);
            return Ok(new { code = 200, data });
        }
    }

    /// <summary>
    /// 产品KPI API
    /// </summary>
    [Route("api/selection/products")]
    [ApiController]
    public class ProductKPIController : ControllerBase
    {
        private readonly BIDashboardService _dashboardService;

        public ProductKPIController(BIDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// 获取单个产品KPI
        /// </summary>
        [HttpGet("{productId}/kpi")]
        public async Task<IActionResult> GetProductKPI(long productId)
        {
            var data = await _dashboardService.GetProductKPIAsync(productId);
            if (data == null)
            {
                return NotFound(new { code = 404, message = "产品不存在" });
            }
            return Ok(new { code = 200, data });
        }
    }

    /// <summary>
    /// SOP执行API
    /// </summary>
    [Route("api/selection")]
    [ApiController]
    public class SOPController : ControllerBase
    {
        /// <summary>
        /// 创建SOP计划
        /// </summary>
        [HttpPost("products/{productId}/sop")]
        public IActionResult CreateSOP(long productId)
        {
            var data = new
            {
                id = 1,
                productId,
                status = "created",
                totalTasks = 20,
                completedTasks = 0,
                startDate = System.DateTime.Now,
                estimatedEndDate = System.DateTime.Now.AddDays(30)
            };
            return Ok(new { code = 200, message = "SOP计划已创建", data });
        }

        /// <summary>
        /// 获取SOP状态
        /// </summary>
        [HttpGet("products/{productId}/sop")]
        public IActionResult GetSOP(long productId)
        {
            var data = new
            {
                productId,
                status = "in_progress",
                completion = 0.6m,
                totalTasks = 20,
                completedTasks = 12,
                tasks = new[]
                {
                    new { id = 1, name = "市场调研", status = "completed", assignee = "张三" },
                    new { id = 2, name = "样品采购", status = "in_progress", assignee = "李四" },
                    new { id = 3, name = "质检测试", status = "pending", assignee = "王五" }
                }
            };
            return Ok(new { code = 200, data });
        }

        /// <summary>
        /// 更新SOP任务
        /// </summary>
        [HttpPatch("sop/tasks/{taskId}")]
        public IActionResult UpdateTask(long taskId, [FromBody] object updateData)
        {
            return Ok(new { code = 200, message = "任务已更新", data = new { taskId, status = "completed" } });
        }

        /// <summary>
        /// 获取甘特图数据
        /// </summary>
        [HttpGet("sop/gantt")]
        public IActionResult GetGantt([FromQuery] long? productId)
        {
            var data = new
            {
                productId,
                tasks = new[]
                {
                    new { id = 1, name = "市场调研", start = "2025-01-01", end = "2025-01-07", progress = 100 },
                    new { id = 2, name = "样品采购", start = "2025-01-08", end = "2025-01-15", progress = 60 },
                    new { id = 3, name = "质检测试", start = "2025-01-16", end = "2025-01-22", progress = 0 },
                    new { id = 4, name = "上架准备", start = "2025-01-23", end = "2025-01-30", progress = 0 }
                }
            };
            return Ok(new { code = 200, data });
        }
    }

    /// <summary>
    /// 检查清单API
    /// </summary>
    [Route("api/selection")]
    [ApiController]
    public class ChecklistController : ControllerBase
    {
        /// <summary>
        /// 获取检查清单
        /// </summary>
        [HttpGet("products/{productId}/checklist")]
        public IActionResult GetChecklist(long productId)
        {
            var data = new
            {
                productId,
                totalItems = 20,
                completedItems = 12,
                completion = 0.6m,
                items = new[]
                {
                    new { id = 1, category = "市场分析", name = "竞品调研完成", @checked = true, priority = "high" },
                    new { id = 2, category = "市场分析", name = "需求验证完成", @checked = true, priority = "high" },
                    new { id = 3, category = "产品开发", name = "样品确认", @checked = false, priority = "medium" },
                    new { id = 4, category = "产品开发", name = "质量检测", @checked = false, priority = "high" },
                    new { id = 5, category = "运营准备", name = "Listing优化", @checked = false, priority = "medium" }
                }
            };
            return Ok(new { code = 200, data });
        }

        /// <summary>
        /// 标记检查项完成
        /// </summary>
        [HttpPost("checklist/{itemId}/check")]
        public IActionResult CheckItem(long itemId, [FromBody] object checkData)
        {
            return Ok(new { code = 200, message = "已标记完成", data = new { itemId, @checked = true } });
        }

        /// <summary>
        /// 获取检查清单模板
        /// </summary>
        [HttpGet("checklist/template")]
        public IActionResult GetTemplate()
        {
            var data = new
            {
                templateName = "标准选品检查清单",
                version = "1.0",
                totalItems = 20,
                categories = new[]
                {
                    new { name = "市场分析", itemCount = 5 },
                    new { name = "产品开发", itemCount = 6 },
                    new { name = "运营准备", itemCount = 5 },
                    new { name = "风险评估", itemCount = 4 }
                },
                items = new[]
                {
                    new { id = 1, category = "市场分析", name = "竞品调研完成", description = "至少分析3个主要竞品" },
                    new { id = 2, category = "市场分析", name = "需求验证完成", description = "通过问卷或访谈验证需求" },
                    new { id = 3, category = "产品开发", name = "样品确认", description = "样品质量符合标准" }
                }
            };
            return Ok(new { code = 200, data });
        }
    }
}

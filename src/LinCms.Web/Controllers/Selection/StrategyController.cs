using LinCms.Application.Contracts.Selection;
using LinCms.Application.Selection.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinCms.Web.Controllers.Selection
{
    /// <summary>
    /// 策略执行API
    /// </summary>
    [Route("api/selection/strategies")]
    [ApiController]
    public class StrategyController : ControllerBase
    {
        private readonly StrategyExecutionService _strategyService;

        public StrategyController(StrategyExecutionService strategyService)
        {
            _strategyService = strategyService;
        }

        /// <summary>
        /// 获取所有策略清单
        /// </summary>
        [HttpGet]
        public IActionResult GetAllStrategies()
        {
            var strategies = _strategyService.GetAllStrategies();
            return Ok(new { code = 200, data = strategies });
        }

        /// <summary>
        /// 执行单个策略
        /// </summary>
        [HttpPost("{strategyCode}/execute")]
        public async Task<IActionResult> ExecuteStrategy(string strategyCode, [FromBody] ExecuteStrategyDto dto)
        {
            var result = await _strategyService.ExecuteStrategyAsync(strategyCode, dto.ProductId);
            return Ok(new { code = 200, data = result });
        }

        /// <summary>
        /// 批量执行策略
        /// </summary>
        [HttpPost("execute-batch")]
        public async Task<IActionResult> ExecuteBatchStrategies([FromBody] ExecuteBatchStrategyDto dto)
        {
            var results = await _strategyService.ExecuteBatchStrategiesAsync(dto.StrategyCodes, dto.ProductId);
            return Ok(new { code = 200, data = results });
        }

        /// <summary>
        /// 执行所有策略
        /// </summary>
        [HttpPost("execute-all")]
        public async Task<IActionResult> ExecuteAllStrategies([FromBody] ExecuteStrategyDto dto)
        {
            var results = await _strategyService.ExecuteAllStrategiesAsync(dto.ProductId);
            return Ok(new { code = 200, data = results });
        }

        /// <summary>
        /// 获取策略执行历史
        /// </summary>
        [HttpGet("products/{productId}/strategies")]
        public async Task<IActionResult> GetExecutionHistory(long productId, [FromQuery] string strategyCode = null, [FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            var history = await _strategyService.GetExecutionHistoryAsync(productId, strategyCode, page, size);
            return Ok(new { code = 200, data = history });
        }

        /// <summary>
        /// 获取单次执行详情
        /// </summary>
        [HttpGet("executions/{executionId}")]
        public async Task<IActionResult> GetExecutionDetail(long executionId)
        {
            var detail = await _strategyService.GetExecutionDetailAsync(executionId);
            if (detail == null)
            {
                return NotFound(new { code = 404, message = "执行记录不存在" });
            }

            return Ok(new { code = 200, data = detail });
        }

        /// <summary>
        /// 重新执行历史策略
        /// </summary>
        [HttpPost("executions/{executionId}/re-execute")]
        public async Task<IActionResult> ReExecuteStrategy(long executionId)
        {
            var result = await _strategyService.ReExecuteStrategyAsync(executionId);
            return Ok(new { code = 200, message = "重新执行成功", data = result });
        }

        /// <summary>
        /// S02-40题自诊提交
        /// </summary>
        [HttpPost("S02/submit")]
        public async Task<IActionResult> SubmitSelfDiagnosis([FromBody] SelfDiagnosisSubmitDto dto)
        {
            var result = await _strategyService.SubmitSelfDiagnosisAsync(dto);
            return Ok(new { code = 200, message = "自诊提交成功", data = result });
        }

        /// <summary>
        /// S03-敏感性分析
        /// </summary>
        [HttpPost("S03/sensitivity")]
        public async Task<IActionResult> SensitivityAnalysis([FromBody] SensitivityAnalysisDto dto)
        {
            var result = await _strategyService.SensitivityAnalysisAsync(dto);
            return Ok(new { code = 200, data = result });
        }

        /// <summary>
        /// S18-压力测试
        /// </summary>
        [HttpPost("S18/stress-test")]
        public async Task<IActionResult> StressTest([FromBody] StressTestDto dto)
        {
            var result = await _strategyService.StressTestAsync(dto);
            return Ok(new { code = 200, data = result });
        }

        /// <summary>
        /// 获取策略配置
        /// </summary>
        [HttpGet("{strategyCode}/config")]
        public IActionResult GetStrategyConfig(string strategyCode)
        {
            var config = _strategyService.GetStrategyConfig(strategyCode);
            return Ok(new { code = 200, data = config });
        }

        /// <summary>
        /// 更新策略阈值（管理员）
        /// </summary>
        [HttpPut("{strategyCode}/config")]
        public async Task<IActionResult> UpdateStrategyConfig(string strategyCode, [FromBody] StrategyConfigDto config)
        {
            var result = await _strategyService.UpdateStrategyConfigAsync(strategyCode, config);
            return Ok(new { code = 200, message = "配置更新成功", data = result });
        }
    }
}

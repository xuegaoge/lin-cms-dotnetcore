using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LinCms.Application.Contracts.Selection
{
    /// <summary>
    /// 策略执行结果DTO
    /// </summary>
    public class StrategyExecutionDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string StrategyCode { get; set; }
        public string StrategyName { get; set; }
        public string StrategyType { get; set; }

        // 主要结果
        public decimal? Score { get; set; }
        public string Grade { get; set; }
        public string Decision { get; set; }
        public string Reason { get; set; }

        // 详细结果
        public object DetailJson { get; set; }
        public List<SubResultDto> SubResults { get; set; }
        public List<string> Warnings { get; set; }
        public List<string> Suggestions { get; set; }

        // 执行元数据
        public DateTime ExecutedAt { get; set; }
        public long? ExecutedBy { get; set; }
        public int? ExecutionTimeMs { get; set; }
        public bool IsLatest { get; set; }
    }

    /// <summary>
    /// 策略实时执行结果DTO
    /// </summary>
    public class StrategyResultDto
    {
        public long ExecutionId { get; set; }
        public string StrategyCode { get; set; }
        public string StrategyName { get; set; }
        public decimal? Score { get; set; }
        public string Grade { get; set; }
        public string Decision { get; set; }
        public string Reason { get; set; }
        public object DetailJson { get; set; }
        public List<SubResultDto> SubResults { get; set; }
        public List<string> Warnings { get; set; }
        public List<string> Suggestions { get; set; }
        public DateTime ExecutedAt { get; set; }
        public long ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// 子结果DTO
    /// </summary>
    public class SubResultDto
    {
        public string Name { get; set; }
        public decimal Score { get; set; }
        public decimal Weight { get; set; }
        public decimal WeightedScore { get; set; }
        public string Grade { get; set; }
        public List<IndicatorDto> Indicators { get; set; }
    }

    /// <summary>
    /// 指标DTO
    /// </summary>
    public class IndicatorDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public object RawValue { get; set; }
        public decimal Score { get; set; }
        public decimal Weight { get; set; }
        public string Grade { get; set; }
        public string Formula { get; set; }
        public string Calculation { get; set; }
    }


    /// <summary>
    /// 执行策略请求DTO
    /// </summary>
    public class ExecuteStrategyDto
    {
        [JsonProperty("product_id")] // Support snake_case from frontend/backend config
        public long ProductId { get; set; }
    }

    /// <summary>
    /// 批量执行策略请求DTO
    /// </summary>
    public class ExecuteBatchStrategyDto
    {
        public long ProductId { get; set; }
        public List<string> StrategyCodes { get; set; }
    }

    /// <summary>
    /// 策略清单信息DTO
    /// </summary>
    public class StrategyDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public List<string> RequiredFields { get; set; }
    }

    /// <summary>
    /// S02自诊提交DTO
    /// </summary>
    public class SelfDiagnosisSubmitDto
    {
        public long ProductId { get; set; }
        public Dictionary<string, bool> Answers { get; set; }
    }

    /// <summary>
    /// S03敏感性分析DTO
    /// </summary>
    public class SensitivityAnalysisDto
    {
        public long ProductId { get; set; }
        public List<string> Scenarios { get; set; }
    }

    /// <summary>
    /// S18压力测试DTO
    /// </summary>
    public class StressTestDto
    {
        public long ProductId { get; set; }
    }

    /// <summary>
    /// 策略配置DTO
    /// </summary>
    public class StrategyConfigDto
    {
        public string StrategyCode { get; set; }
        public Dictionary<string, object> Thresholds { get; set; }
        public bool IsActive { get; set; }
    }
}

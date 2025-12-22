using LinCms.Entities.Selection;
using System;
using System.Collections.Generic;

namespace LinCms.Application.Selection.Models
{
    /// <summary>
    /// 策略执行结果
    /// </summary>
    public class StrategyResult
    {
        /// <summary>
        /// 策略代码
        /// </summary>
        public string StrategyCode { get; set; }

        /// <summary>
        /// 策略名称
        /// </summary>
        public string StrategyName { get; set; }

        /// <summary>
        /// 策略类型
        /// </summary>
        public StrategyType Type { get; set; }

        /// <summary>
        /// 是否执行成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误信息（执行失败时）
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 执行时间
        /// </summary>
        public DateTime ExecutedAt { get; set; }

        /// <summary>
        /// 执行耗时(毫秒)
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        // === 评分型结果 ===
        /// <summary>
        /// 评分 (0-100)
        /// </summary>
        public decimal? Score { get; set; }

        /// <summary>
        /// 等级 (A/B/C/D 或 S/A/B/C/D)
        /// </summary>
        public string Grade { get; set; }

        // === 判定型结果 ===
        /// <summary>
        /// 决策建议 (GO/WAIT/STOP)
        /// </summary>
        public string Decision { get; set; }

        /// <summary>
        /// 判定理由
        /// </summary>
        public string Reason { get; set; }

        // === 详细计算过程 ===
        /// <summary>
        /// 子结果列表（多维度策略使用）
        /// </summary>
        public List<SubResult> SubResults { get; set; } = new List<SubResult>();

        /// <summary>
        /// 指标列表
        /// </summary>
        public List<Indicator> Indicators { get; set; } = new List<Indicator>();

        // === 警告与建议 ===
        /// <summary>
        /// 警告列表
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// 建议列表
        /// </summary>
        public List<string> Suggestions { get; set; } = new List<string>();

        // === 风险型结果 ===
        /// <summary>
        /// 风险预警列表
        /// </summary>
        public List<RiskAlertItem> RiskAlerts { get; set; } = new List<RiskAlertItem>();

        // === 推荐型结果 ===
        /// <summary>
        /// 打法推荐列表
        /// </summary>
        public List<TacticRecommendation> Recommendations { get; set; } = new List<TacticRecommendation>();

        // === 详细JSON（用于存储到数据库） ===
        /// <summary>
        /// 详细计算明细JSON
        /// </summary>
        public string DetailJson { get; set; }
        /// <summary>
        /// 额外数据对象
        /// </summary>
        public object Data { get; set; }
    }

    /// <summary>
    /// 子结果（多维度策略使用）
    /// </summary>
    public class SubResult
    {
        public string Name { get; set; }
        public decimal Score { get; set; }
        public decimal Weight { get; set; }
        public decimal WeightedScore { get; set; }
        public string Grade { get; set; }
        public string Description { get; set; }
        public List<Indicator> Indicators { get; set; } = new List<Indicator>();
    }

    /// <summary>
    /// 指标详情
    /// </summary>
    public class Indicator
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public object RawValue { get; set; }
        
        // Added for compatibility
        public decimal? Value { get; set; }
        public string Unit { get; set; }
        public string Status { get; set; }

        public decimal Score { get; set; }
        public decimal Weight { get; set; }
        public string Grade { get; set; }
        public string Formula { get; set; }
        public string Calculation { get; set; }
    }

    /// <summary>
    /// 风险预警项
    /// </summary>
    public class RiskAlertItem
    {
        public string RiskCode { get; set; }
        public string RiskName { get; set; }
        public string RiskLevel { get; set; }
        public string RiskType { get; set; }
        public string Description { get; set; }
        public string TriggerValue { get; set; }
        public string ThresholdValue { get; set; }
        public List<string> Suggestions { get; set; } = new List<string>();
    }

    /// <summary>
    /// 打法推荐项
    /// </summary>
    public class TacticRecommendation
    {
        public string RecommendationCode { get; set; }
        public string RecommendationName { get; set; }
        public string RecommendationType { get; set; }
        public string Priority { get; set; }
        public string Description { get; set; }
        public List<string> ActionSteps { get; set; } = new List<string>();
        public string ExpectedImpact { get; set; }
        public decimal MatchScore { get; set; }
    }

    /// <summary>
    /// 决策类型常量
    /// </summary>
    public static class DecisionType
    {
        public const string GO = "GO";
        public const string WAIT = "WAIT";
        public const string STOP = "STOP";
    }
}

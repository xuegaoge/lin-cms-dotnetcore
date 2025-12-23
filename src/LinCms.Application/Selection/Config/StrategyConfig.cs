using System.Collections.Generic;

namespace LinCms.Application.Selection.Config
{
    /// <summary>
    /// 策略阈值/权重配置 - 集中管理所有策略的配置参数
    /// </summary>
    public static class StrategyConfig
    {
        // ========================================
        // S01 - 四层评估体系
        // ========================================
        public static class FourLayer
        {
            // 四层权重
            public const decimal MarketWeight = 0.25m;
            public const decimal ProductWeight = 0.25m;
            public const decimal OperationWeight = 0.25m;
            public const decimal FinancialWeight = 0.25m;

            // GO阈值（按企业等级）
            public static readonly Dictionary<string, int> GoThreshold = new Dictionary<string, int>
            {
                {"A", 65}, {"B", 70}, {"C", 75}, {"D", 80}, {"E", 85}
            };

            // 红线阈值
            public const int RiskScoreRedLine = 40;
            public const int FinancialScoreRedLine = 30;
            public const decimal ROIRedLine = 0.15m;

            // M层指标评分阈值
            public static class MarketLayer
            {
                public static readonly int[] SearchVolumeGrades = { 10000, 20000, 50000, 100000 };
                // 注意：SearchGrowthRate 存储的是百分比值（8.3 代表 8.3%），阈值也使用百分比值
                public static readonly decimal[] GrowthRateGrades = { 5m, 10m, 15m, 30m };
                // 头部集中度阈值（越低越好）：≤60%得2分, ≤45%得4分, ≤30%得6分, ≤15%得8分, <15%得10分
                public static readonly decimal[] ConcentrationGrades = { 0.60m, 0.45m, 0.30m, 0.15m };
            }

            // P层指标评分阈值
            public static class ProductLayer
            {
                public static readonly decimal[] MarginGrades = { 0.25m, 0.30m, 0.35m, 0.40m };
                public static readonly int[] DifferentiationGrades = { 1, 2, 3, 5 };
            }

            // O层指标评分阈值
            public static class OperationLayer
            {
                public static readonly decimal[] CPCGrades = { 1.2m, 0.8m, 0.5m, 0.3m };
                public static readonly decimal[] ConversionGrades = { 0.01m, 0.015m, 0.02m, 0.03m };
                public static readonly decimal[] RatingGrades = { 4.0m, 4.2m, 4.5m, 4.8m };
            }

            // F层指标评分阈值
            public static class FinancialLayer
            {
                public static readonly decimal[] ROIGrades = { 0.15m, 0.25m, 0.35m, 0.50m };
                public static readonly int[] PaybackGrades = { 12, 9, 6, 3 };
            }
        }

        // ========================================
        // S02 - 40题自诊系统
        // ========================================
        public static class SelfDiagnosis
        {
            public const int GoScore = 800;      // ≥800分：GO
            public const int WaitScore = 600;    // 600-799：WAIT
            // <600：STOP

            // 各类别权重
            public const decimal LifecycleWeight = 0.25m;
            public const decimal ProductWeight = 0.25m;
            public const decimal CategoryWeight = 0.27m;
            public const decimal SupplyChainWeight = 0.13m;
            public const decimal RiskWeight = 0.10m;
        }

        // ========================================
        // S03 - 利润模型
        // ========================================
        public static class ProfitModel
        {
            public const decimal MinGrossMargin = 0.25m;    // 最低毛利率
            public const decimal MinROI = 0.15m;            // 最低ROI
            public const int MaxPaybackMonths = 12;         // 最长回本周期

            // 默认费率
            public const decimal DefaultCommissionRate = 0.15m;
            public const decimal DefaultReturnRate = 0.05m;
            public const decimal DefaultLossRate = 0.02m;
        }

        // ========================================
        // S04 - 风险预警
        // ========================================
        public static class RiskAlert
        {
            // 市场风险阈值
            public const int MinSearchVolume = 5000;
            public const decimal MaxSearchDecline = -0.10m;
            public const decimal MaxConcentration = 0.70m;
            public const int MaxCompetitorCount = 500;

            // 财务风险阈值
            public const decimal MinMargin = 0.25m;
            public const decimal MinROI = 0.15m;
            public const decimal MinPrice = 15m;
            public const decimal MaxPrice = 100m;

            // 供应链风险阈值
            public const int MinSupplierStability = 60;
            public const int MaxLeadTime = 45;
            public const int MaxMOQ = 1000;

            // 合规风险
            public const decimal MaxPolicyRisk = 0.70m;
        }

        // ========================================
        // S10 - 热度评级
        // ========================================
        public static class HeatRating
        {
            // 搜索量分级
            public static readonly int[] SearchVolumeGrades = { 10000, 50000, 200000, 1000000 };

            // 竞争度分级（越低越好）
            public static readonly decimal[] ConcentrationGrades = { 0.50m, 0.30m, 0.15m, 0.05m };

            // 毛利率分级
            public static readonly decimal[] MarginGrades = { 0.20m, 0.25m, 0.35m, 0.45m };

            // 增长率分级 - 注意：SearchGrowthRate 存储的是百分比值
            public static readonly decimal[] GrowthGrades = { 5m, 10m, 20m, 30m };

            // 热度等级对应分数
            public const int ExtremeHotScore = 85;
            public const int HotScore = 65;
            public const int WarmScore = 45;
            public const int ColdScore = 25;
        }

        // ========================================
        // S07 - 赛道评估
        // ========================================
        public static class TrackEvaluation
        {
            // 等级分数区间
            public const int SGradeMin = 85;
            public const int AGradeMin = 70;
            public const int BGradeMin = 55;
            public const int CGradeMin = 40;
            // <40: D级
        }

        // ========================================
        // S11 - 企业定位
        // ========================================
        public static class EnterpriseProfile
        {
            // 8维度权重
            public const decimal FundingWeight = 0.15m;
            public const decimal TeamWeight = 0.20m;
            public const decimal SupplyChainWeight = 0.15m;
            public const decimal OperationWeight = 0.15m;
            public const decimal RiskToleranceWeight = 0.10m;
            public const decimal MarketInsightWeight = 0.10m;
            public const decimal TechWeight = 0.10m;
            public const decimal BrandWeight = 0.05m;

            // 等级分数区间
            public const int AGradeMin = 90;
            public const int BGradeMin = 80;
            public const int CGradeMin = 70;
            public const int DGradeMin = 60;
            // <60: E级
        }
    }
}

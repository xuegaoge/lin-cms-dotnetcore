using LinCms.Application.Selection.Models;
using LinCms.Entities.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinCms.Application.Selection.Strategies.Implementations
{
    /// <summary>
    /// S02 - 40题自诊系统（已在StrategyExecutionService中实现基础版本）
    /// 完整版本需要从StrategyManualInput表读取40个问题的答案
    /// </summary>
    public class SelfDiagnosisStrategy : BaseStrategy
    {
        public override string Code => "S02";
        public override string Name => "40题自诊系统";
        public override string Description => "15分钟快速评分，40个是非题判定产品可行性";
        public override StrategyType Type => StrategyType.Decision;

        public override IReadOnlyList<string> RequiredFields => new[] { "ProductName" };

        public override string LogicDefinition => @"
### 策略定义
40题自诊系统（当前版本为基于现有数据的模拟版），通过40个（模拟13个）是非题快速判定产品可行性。

### 核心输入
*   数据字段: 搜索量, 竞品数, 评分, 风险等级, 加价率等

### 计算逻辑
1.  **模拟问卷**: 将现有产品数据映射到问卷问题（如: 搜索量>5000 -> 是）。
2.  **通过率**: 统计""是""的个数。
3.  **评分**: (通过数 / 总题数40) * 100。
4.  **决策**: 通过率 > 60% -> GO。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            // 简化实现：基于产品基础数据模拟40题评分
            var passCount = 0;
            var totalCount = 40;

            // 模拟40题判定逻辑（实际应从StrategyManualInput表读取）
            if (product.MonthlySearchVolume >= 5000) passCount += 3;
            if (product.CompetitorCount < 500) passCount += 3;
            if (product.AverageRating >= 4.0m) passCount += 3;
            if (product.InfringementRisk != "高") passCount += 4;
            if (product.PolicyRisk < 0.5m) passCount += 4;
            if ((product.TargetPrice - product.PurchaseCost) / product.TargetPrice >= 0.25m) passCount += 5;
            if (product.Seasonality < 0.5m) passCount += 3;
            if (product.SupplierStability >= 70) passCount += 3;
            if (product.LeadTimeDays <= 30) passCount += 2;
            if (product.MOQ <= 500) passCount += 2;
            if (product.CertificationLevel != "严") passCount += 2;
            if (product.DifferentiationPoints >= 3) passCount += 3;
            if (product.VariantCount >= 3) passCount += 3;

            var passRate = (decimal)passCount / totalCount;

            return new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = passRate * 100,
                Grade = passRate >= 0.8m ? "A" : passRate >= 0.6m ? "B" : passRate >= 0.4m ? "C" : "D",
                Decision = passRate >= 0.6m ? "GO" : passRate >= 0.4m ? "WAIT" : "STOP",
                Reason = $"通过率: {passRate:P0} ({passCount}/{totalCount}题通过)",
                Warnings = passRate < 0.6m ? new List<string> { "通过率偏低，建议重新评估" } : new List<string>(),
                Suggestions = new List<string> { "建议完成完整40题自诊问卷以获得更准确结果" }
            };
        }
    }

    /// <summary>
    /// S05 - 11维度评估
    /// </summary>
    public class ElevenDimensionStrategy : BaseStrategy
    {
        public override string Code => "S05";
        public override string Name => "11维度评估";
        public override string Description => "11个关键维度加权评分";
        public override StrategyType Type => StrategyType.Scoring;

        public override IReadOnlyList<string> RequiredFields => new[]
        {
            nameof(ProductData.TargetPrice),
            nameof(ProductData.PurchaseCost)
        };

        public override string LogicDefinition => @"
### 策略定义
11维度全方位评估，涵盖市场、竞争、利润、差异化、供应链等关键要素。

### 核心输入
*   需要产品全量数据 (搜索量, 竞品数, 价格, 成本, 评分, 差异化等)

### 计算逻辑
1.  **维度打分**: 系统设定11个维度（市场需求、竞争强度、利润空间...），每个维度根据规则评定 40/60/75/90 分。
2.  **加权汇总**: 根据各维度权重（如市场15%、利润15%、竞争12%）计算加权平均分。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            var dimensions = new Dictionary<string, decimal>
            {
                ["市场需求"] = CalculateMarketDemand(product),
                ["竞争强度"] = CalculateCompetition(product),
                ["利润空间"] = CalculateProfit(product),
                ["产品差异化"] = CalculateDifferentiation(product),
                ["供应链"] = CalculateSupplyChain(product),
                ["风险等级"] = CalculateRisk(product),
                ["趋势性"] = CalculateTrend(product),
                ["季节性"] = CalculateSeasonality(product),
                ["认证难度"] = CalculateCertification(product),
                ["物流成本"] = CalculateLogistics(product),
                ["企业匹配"] = CalculateEnterpriseMatch(product, context)
            };

            var weights = new Dictionary<string, decimal>
            {
                ["市场需求"] = 0.15m,
                ["竞争强度"] = 0.12m,
                ["利润空间"] = 0.15m,
                ["产品差异化"] = 0.10m,
                ["供应链"] = 0.10m,
                ["风险等级"] = 0.12m,
                ["趋势性"] = 0.08m,
                ["季节性"] = 0.06m,
                ["认证难度"] = 0.04m,

                ["物流成本"] = 0.04m,
                ["企业匹配"] = 0.04m
            };

            var totalScore = dimensions.Sum(d => d.Value * weights[d.Key]);

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = totalScore,
                Grade = GetGrade(totalScore),
                Decision = totalScore >= 70 ? "GO" : totalScore >= 50 ? "WAIT" : "STOP",
                Reason = $"11维度综合评分: {totalScore:F1}分"
            };

            result.SubResults = dimensions.Select(d => new SubResult
            {
                Name = d.Key,
                Score = d.Value,
                Weight = weights[d.Key]
            }).ToList();

            return result;
        }

        private decimal CalculateMarketDemand(ProductData product) =>
            product.MonthlySearchVolume >= 10000 ? 90 :
            product.MonthlySearchVolume >= 5000 ? 75 :
            product.MonthlySearchVolume >= 3000 ? 60 : 40;

        private decimal CalculateCompetition(ProductData product) =>
            product.CompetitorCount < 100 ? 90 :
            product.CompetitorCount < 300 ? 75 :
            product.CompetitorCount < 500 ? 60 : 40;

        private decimal CalculateProfit(ProductData product)
        {
            var margin = (product.TargetPrice - product.PurchaseCost) / product.TargetPrice;
            return margin >= 0.4m ? 90 : margin >= 0.3m ? 75 : margin >= 0.2m ? 60 : 40;
        }

        private decimal CalculateDifferentiation(ProductData product) =>
            product.DifferentiationPoints >= 5 ? 90 :
            product.DifferentiationPoints >= 3 ? 75 :
            product.DifferentiationPoints >= 2 ? 60 : 40;

        private decimal CalculateSupplyChain(ProductData product) =>
            product.SupplierStability >= 90 ? 90 :
            product.SupplierStability >= 70 ? 75 :
            product.SupplierStability >= 50 ? 60 : 40;

        private decimal CalculateRisk(ProductData product)
        {
            var riskScore = 100m;
            if (product.InfringementRisk == "高") riskScore -= 40;
            else if (product.InfringementRisk == "中") riskScore -= 20;
            if (product.PolicyRisk > 0.5m) riskScore -= 20;
            return Math.Max(riskScore, 0);
        }

        private decimal CalculateTrend(ProductData product) =>
            product.SearchGrowthRate >= 0.3m ? 90 :
            product.SearchGrowthRate >= 0.1m ? 75 :
            product.SearchGrowthRate >= 0 ? 60 : 40;

        private decimal CalculateSeasonality(ProductData product) =>
            product.Seasonality < 0.3m ? 90 :
            product.Seasonality < 0.5m ? 75 :
            product.Seasonality < 0.7m ? 60 : 40;

        private decimal CalculateCertification(ProductData product) =>
            product.CertificationLevel == "无" ? 90 :
            product.CertificationLevel == "轻" ? 75 :
            product.CertificationLevel == "中" ? 60 : 40;

        private decimal CalculateLogistics(ProductData product)
        {
            var logisticsCost = (product.ShippingCost + product.FBACost) / product.TargetPrice;
            return logisticsCost < 0.15m ? 90 : logisticsCost < 0.25m ? 75 : logisticsCost < 0.35m ? 60 : 40;
        }

        private decimal CalculateEnterpriseMatch(ProductData product, ExecutionContext context)
        {
            if (context?.EnterpriseProfile == null) return 70;
            // 简化：基于企业等级匹配
            return 75;
        }
    }

    /// <summary>
    /// S06 - 五维选品模型
    /// </summary>
    public class FiveDimensionStrategy : BaseStrategy
    {
        public override string Code => "S06";
        public override string Name => "五维选品模型";
        public override string Description => "需求/竞争/产品/趋势/自身五大维度评估";
        public override StrategyType Type => StrategyType.Scoring;

        public override IReadOnlyList<string> RequiredFields => new[]
        {
            nameof(ProductData.MonthlySearchVolume),
            nameof(ProductData.CompetitorCount)
        };

        public override string LogicDefinition => @"
### 策略定义
五维选品模型，从需求、竞争、产品、趋势、自身五个宏观维度进行均衡评估。

### 核心输入
*   SearchVolume, SearchGrowthRate, CompetitorCount, TopConcentration

### 计算逻辑
1.  **维度算分**: 
    - 需求: 搜索量 + 增长率
    - 竞争: 竞品数 + 垄断度 (反向)
    - 产品: 差异化 + 评分
    - 趋势: 增长率 + 季节性
    - 自身: 供应链 + MOQ
2.  **综合评分**: 五个维度得分的算术平均值。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            var dimensions = new Dictionary<string, decimal>
            {
                ["需求维度"] = CalculateDemand(product),
                ["竞争维度"] = CalculateCompetition(product),
                ["产品维度"] = CalculateProduct(product),
                ["趋势维度"] = CalculateTrend(product),
                ["自身维度"] = CalculateSelf(product, context)
            };

            var totalScore = dimensions.Values.Average();

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = totalScore,
                Grade = GetGrade(totalScore),
                Decision = totalScore >= 70 ? "GO" : totalScore >= 50 ? "WAIT" : "STOP",
                Reason = $"五维综合评分: {totalScore:F1}分"
            };

            result.SubResults = dimensions.Select(d => new SubResult
            {
                Name = d.Key,
                Score = d.Value,
                Weight = 0.2m
            }).ToList();

            return result;
        }

        private decimal CalculateDemand(ProductData product)
        {
            var score = 0m;
            if (product.MonthlySearchVolume >= 10000) score += 30;
            else if (product.MonthlySearchVolume >= 5000) score += 20;
            else score += 10;

            if (product.SearchGrowthRate >= 0.2m) score += 20;
            else if (product.SearchGrowthRate >= 0) score += 10;

            return Math.Min(score + 50, 100);
        }

        private decimal CalculateCompetition(ProductData product)
        {
            var score = 100m;
            if (product.CompetitorCount > 500) score -= 30;
            else if (product.CompetitorCount > 300) score -= 20;
            else if (product.CompetitorCount > 100) score -= 10;

            if (product.TopConcentration > 0.6m) score -= 20;
            else if (product.TopConcentration > 0.4m) score -= 10;

            return Math.Max(score, 0);
        }

        private decimal CalculateProduct(ProductData product)
        {
            var score = 50m;
            score += (product.DifferentiationPoints ?? 0) * 10;
            if (product.AverageRating >= 4.5m) score += 10;
            if (product.VariantCount >= 5) score += 10;
            return Math.Min(score, 100);
        }

        private decimal CalculateTrend(ProductData product)
        {
            var score = 60m;
            if (product.SearchGrowthRate >= 0.3m) score += 30;
            else if (product.SearchGrowthRate >= 0.1m) score += 20;
            else if (product.SearchGrowthRate >= 0) score += 10;

            if (product.Seasonality < 0.3m) score += 10;
            return Math.Min(score, 100);
        }

        private decimal CalculateSelf(ProductData product, ExecutionContext context)
        {
            var score = 70m;
            if (product.SupplierStability >= 80) score += 15;
            if (product.MOQ <= 500) score += 15;
            return Math.Min(score, 100);
        }
    }

    // 继续在下一个文件...
}

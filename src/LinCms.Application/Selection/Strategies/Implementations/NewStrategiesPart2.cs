using LinCms.Application.Selection.Models;
using LinCms.Entities.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinCms.Application.Selection.Strategies.Implementations
{
    /// <summary>
    /// S07 - 赛道市场评估
    /// </summary>
    public class MarketEvaluationStrategy : BaseStrategy
    {
        public override string Code => "S07";
        public override string Name => "赛道市场评估";
        public override string Description => "MSI/CII/毛利/蓝海四维赛道评估";
        public override StrategyType Type => StrategyType.Scoring;

        public override IReadOnlyList<string> RequiredFields => new[]
        {
            nameof(ProductData.TargetPrice),
            nameof(ProductData.PurchaseCost)
        };

        public override string LogicDefinition => @"
### 策略定义
赛道市场评估，聚焦于宏观赛道的吸引力与潜力。

### 核心输入
*   SearchVolume, CompetitorCount, Price, Cost, NewRatio

### 计算逻辑
1.  **MSI (市场规模)**: 基于搜索量分级打分。
2.  **CII (竞争强度)**: 基于竞品数和垄断度打分。
3.  **毛利率**: 基于净毛利(扣除佣金)打分。
4.  **蓝海指数**: 基于新品占比和差异化打分。
5.  **总分**: 四项指标的算术平均。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            var msi = CalculateMSI(product);
            var cii = CalculateCII(product);
            var margin = CalculateMargin(product);
            var blueOcean = CalculateBlueOcean(product);

            var totalScore = (msi + cii + margin + blueOcean) / 4;

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = totalScore,
                Grade = GetGrade(totalScore),
                Decision = totalScore >= 70 ? "GO" : totalScore >= 50 ? "WAIT" : "STOP",
                Reason = $"赛道综合评分: {totalScore:F1}分"
            };

            result.SubResults = new List<SubResult>
            {
                new SubResult { Name = "MSI市场规模指数", Score = msi, Weight = 0.25m, WeightedScore = msi * 0.25m },
                new SubResult { Name = "CII竞争强度指数", Score = cii, Weight = 0.25m, WeightedScore = cii * 0.25m },
                new SubResult { Name = "毛利率", Score = margin, Weight = 0.25m, WeightedScore = margin * 0.25m },
                new SubResult { Name = "蓝海指数", Score = blueOcean, Weight = 0.25m, WeightedScore = blueOcean * 0.25m }
            };

            return result;
        }

        private decimal CalculateMSI(ProductData product)
        {
            var searchVolume = product.MonthlySearchVolume ?? 0;
            return searchVolume >= 50000 ? 95 :
                   searchVolume >= 20000 ? 85 :
                   searchVolume >= 10000 ? 75 :
                   searchVolume >= 5000 ? 65 :
                   searchVolume >= 3000 ? 55 : 40;
        }

        private decimal CalculateCII(ProductData product)
        {
            var competitors = product.CompetitorCount ?? 0;
            var concentration = product.TopConcentration ?? 0.5m;

            var score = 100m;
            if (competitors > 500) score -= 30;
            else if (competitors > 300) score -= 20;
            else if (competitors > 100) score -= 10;

            if (concentration > 0.7m) score -= 20;
            else if (concentration > 0.5m) score -= 10;

            return Math.Max(score, 0);
        }

        private decimal CalculateMargin(ProductData product)
        {
            var commission = product.TargetPrice * 0.15m;
            var margin = (product.TargetPrice - product.PurchaseCost - product.ShippingCost - product.FBACost - commission) / product.TargetPrice;
            return margin >= 0.5m ? 95 :
                   margin >= 0.4m ? 85 :
                   margin >= 0.3m ? 75 :
                   margin >= 0.25m ? 65 :
                   margin >= 0.2m ? 55 : 40;
        }

        private decimal CalculateBlueOcean(ProductData product)
        {
            var score = 50m;
            if (product.NewProductRatio > 0.3m) score += 20;
            if (product.TopConcentration < 0.3m) score += 20;
            if (product.DifferentiationPoints >= 5) score += 10;
            return Math.Min(score, 100);
        }
    }

    /// <summary>
    /// S08 - TOP20策略库
    /// </summary>
    public class Top20StrategyLibrary : BaseStrategy
    {
        public override string Code => "S08";
        public override string Name => "TOP20策略库";
        public override string Description => "26个可执行打法推荐";
        public override StrategyType Type => StrategyType.Recommendation;

        public override IReadOnlyList<string> RequiredFields => new[] { nameof(ProductData.ProductName) };

        public override string LogicDefinition => @"
### 策略定义
TOP20策略库，根据产品特征自动匹配最适合的运营打法。

### 核心输入
*   SearchVolume, Growth, Rating, SupplierStability, Margin

### 计算逻辑
1.  **特征匹配规则**: 
    - 流量大 -> 广告打法
    - 评分高 -> 口碑打法
    - 供应链强 -> 成本打法
2.  **输出**: 匹配到的所有可行策略建议。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            var recommendations = new List<string>();

            // 基于产品特征推荐策略
            if (product.MonthlySearchVolume >= 10000)
                recommendations.Add("策略1: 高流量赛道-广告快速起量");

            if (product.CompetitorCount < 100)
                recommendations.Add("策略2: 低竞争市场-自然流量为主");

            var margin = (product.TargetPrice - product.PurchaseCost) / product.TargetPrice;
            if (margin >= 0.4m)
                recommendations.Add("策略3: 高毛利产品-品牌化运营");

            if (product.DifferentiationPoints >= 5)
                recommendations.Add("策略4: 差异化产品-功能卖点突出");

            if (product.VariantCount >= 5)
                recommendations.Add("策略5: 多变体-组合销售策略");

            if (product.Seasonality < 0.3m)
                recommendations.Add("策略6: 全年销售-长期运营");

            if (product.SearchGrowthRate >= 0.3m)
                recommendations.Add("策略7: 高增长赛道-快速进入");

            if (product.AverageRating >= 4.5m)
                recommendations.Add("策略8: 高评分产品-口碑营销");

            if (product.SupplierStability >= 80)
                recommendations.Add("策略9: 供应链稳定-大批量备货");

            if (product.MOQ <= 300)
                recommendations.Add("策略10: 低MOQ-小批量测试");

            // 默认推荐
            if (recommendations.Count < 3)
            {
                recommendations.Add("策略11: 标准打法-广告+优化");
                recommendations.Add("策略12: 评论积累-早期reviewer计划");
                recommendations.Add("策略13: 价格策略-竞争定价");
            }

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = recommendations.Count * 10,
                Decision = recommendations.Count >= 5 ? "GO" : "WAIT",
                Reason = $"匹配{recommendations.Count}个可执行策略",
                Suggestions = recommendations
            };

            return result;
        }
    }

    /// <summary>
    /// S09 - 蓝海深度识别
    /// </summary>
    public class BlueOceanDetectionStrategy : BaseStrategy
    {
        public override string Code => "S09";
        public override string Name => "蓝海深度识别";
        public override string Description => "8大隐性机会矩阵识别";
        public override StrategyType Type => StrategyType.Recommendation;

        public override IReadOnlyList<string> RequiredFields => new[]
        {
            nameof(ProductData.MonthlySearchVolume),
            nameof(ProductData.CompetitorCount)
        };

        public override string LogicDefinition => @"
### 策略定义
蓝海深度识别，通过8大机会矩阵寻找市场中的隐形蓝海。

### 核心输入
*   SearchVolume, CompetitorCount, Growth, Reviews, Margin

### 计算逻辑
1.  **机会矩阵**: 
    - 需求大+竞争小 -> 黄金蓝海
    - 高增长+新品少 -> 趋势蓝海
    - 高毛利+低竞争 -> 利润蓝海
    ...等8种模式
2.  **评分**: 命中机会越多，分数越高。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            var opportunities = new List<string>();
            var score = 0m;

            // 8大蓝海机会识别
            if (product.MonthlySearchVolume >= 5000 && product.CompetitorCount < 100)
            {
                opportunities.Add("机会1: 需求大+竞争小=黄金蓝海");
                score += 15;
            }

            if (product.SearchGrowthRate >= 0.5m && product.NewProductRatio < 0.2m)
            {
                opportunities.Add("机会2: 高增长+新品少=趋势蓝海");
                score += 15;
            }

            if (product.TopConcentration < 0.3m)
            {
                opportunities.Add("机会3: 低集中度=分散蓝海");
                score += 12;
            }

            if (product.AverageRating < 4.2m && product.TotalReviews > 500)
            {
                opportunities.Add("机会4: 评分低+评论多=改进蓝海");
                score += 12;
            }

            var margin = (product.TargetPrice - product.PurchaseCost) / product.TargetPrice;
            if (margin >= 0.4m && product.CompetitorCount < 200)
            {
                opportunities.Add("机会5: 高毛利+低竞争=利润蓝海");
                score += 13;
            }

            if (product.DifferentiationPoints >= 5)
            {
                opportunities.Add("机会6: 高差异化=创新蓝海");
                score += 11;
            }

            if (product.Seasonality < 0.3m && product.MonthlySearchVolume >= 3000)
            {
                opportunities.Add("机会7: 全年需求=稳定蓝海");
                score += 11;
            }

            if (product.SupplierCount >= 5 && product.SupplierStability >= 80)
            {
                opportunities.Add("机会8: 供应链优势=供应蓝海");
                score += 11;
            }

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = Math.Min(score, 100),
                Grade = GetGrade(score),
                Decision = score >= 60 ? "GO" : score >= 40 ? "WAIT" : "STOP",
                Reason = $"识别到{opportunities.Count}个蓝海机会",
                Suggestions = opportunities
            };

            return result;
        }
    }

    /// <summary>
    /// S10 - 赛道热度评级
    /// </summary>
    public class TrackHeatRatingStrategy : BaseStrategy
    {
        public override string Code => "S10";
        public override string Name => "赛道热度评级";
        public override string Description => "极冷→极热五级评定";
        public override StrategyType Type => StrategyType.Scoring;

        public override IReadOnlyList<string> RequiredFields => new[]
        {
            nameof(ProductData.MonthlySearchVolume),
            nameof(ProductData.SearchGrowthRate)
        };

        public override string LogicDefinition => @"
### 策略定义
赛道热度评级，判断市场目前是处于""极冷""无人问津还是""极热""红海厮杀状态。

### 核心输入
*   SearchVolume, Growth, CompetitorCount

### 计算逻辑
1.  **评分构成**: 搜索量(40分) + 增长率(30分) + 竞争热度(30分)。
2.  **温度分级**: 
    - >80分: 极热 (即使GO也要慎重)
    - 40-80分: 温/热 (最佳切入区间)
    - <20分: 极冷 (需求不足)";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            var searchVolume = product.MonthlySearchVolume ?? 0;
            var growthRate = product.SearchGrowthRate ?? 0;
            var competitors = product.CompetitorCount ?? 0;

            var heatScore = 0m;

            // 搜索量评分 (40分)
            if (searchVolume >= 50000) heatScore += 40;
            else if (searchVolume >= 20000) heatScore += 32;
            else if (searchVolume >= 10000) heatScore += 24;
            else if (searchVolume >= 5000) heatScore += 16;
            else heatScore += 8;

            // 增长率评分 (30分)
            if (growthRate >= 0.5m) heatScore += 30;
            else if (growthRate >= 0.3m) heatScore += 24;
            else if (growthRate >= 0.1m) heatScore += 18;
            else if (growthRate >= 0) heatScore += 12;
            else heatScore += 6;

            // 竞争热度评分 (30分)
            if (competitors >= 1000) heatScore += 30;
            else if (competitors >= 500) heatScore += 24;
            else if (competitors >= 300) heatScore += 18;
            else if (competitors >= 100) heatScore += 12;
            else heatScore += 6;

            var heatLevel = heatScore >= 80 ? "极热" :
                           heatScore >= 60 ? "热" :
                           heatScore >= 40 ? "温" :
                           heatScore >= 20 ? "冷" : "极冷";

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = heatScore,
                Grade = heatLevel,
                Decision = heatScore >= 40 && heatScore <= 80 ? "GO" : "WAIT",
                Reason = $"赛道热度: {heatLevel} ({heatScore:F1}分)",
                Warnings = heatScore >= 80 ? new List<string> { "赛道过热，竞争激烈" } :
                          heatScore < 20 ? new List<string> { "赛道过冷，需求不足" } : new List<string>()
            };

            return result;
        }
    }
}

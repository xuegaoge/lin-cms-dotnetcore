using LinCms.Application.Selection.Models;
using LinCms.Entities.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinCms.Application.Selection.Strategies.Implementations
{
    /// <summary>
    /// S16 - 供应链评估
    /// </summary>
    public class SupplyChainEvaluationStrategy : BaseStrategy
    {
        public override string Code => "S16";
        public override string Name => "供应链评估";
        public override string Description => "8维度供应链综合评分";
        public override StrategyType Type => StrategyType.Scoring;

        public override IReadOnlyList<string> RequiredFields => new[]
        {
            nameof(ProductData.SupplierStability),
            nameof(ProductData.LeadTimeDays)
        };

        public override string LogicDefinition => @"
### 策略定义
供应链评估系统，对后端供应能力进行8维度综合评分，评估供应风险。

### 核心输入
*   SupplierCount, Stability, LeadTime, MOQ, Quality, etc.

### 计算逻辑
1.  **维度打分**: 
    - 数量(10%), 稳定性(20%), 交期(15%), MOQ(10%)
    - 价格波动(15%), 质量(15%), 沟通(10%), 创新(5%)
2.  **总分**: 各维度得分加权求和。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            var dimensions = new Dictionary<string, decimal>
            {
                ["供应商数量"] = ScoreSupplierCount(product.SupplierCount),
                ["供应商稳定性"] = ScoreSupplierStability(product.SupplierStability),
                ["交货周期"] = ScoreLeadTime(product.LeadTimeDays),
                ["最小起订量"] = ScoreMOQ(product.MOQ),
                ["价格稳定性"] = ScorePriceVolatility(product.PriceVolatility),
                ["质量保证"] = ScoreQuality(product.CertificationLevel),
                ["沟通效率"] = ScoreCommunication(product.SupplierStability), // 简化：用稳定性代替
                ["创新能力"] = ScoreInnovation(product.DifferentiationPoints)
            };

            var weights = new Dictionary<string, decimal>
            {
                ["供应商数量"] = 0.10m,
                ["供应商稳定性"] = 0.20m,
                ["交货周期"] = 0.15m,
                ["最小起订量"] = 0.10m,
                ["价格稳定性"] = 0.15m,
                ["质量保证"] = 0.15m,
                ["沟通效率"] = 0.10m,
                ["创新能力"] = 0.05m
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
                Reason = $"供应链综合评分: {totalScore:F1}分",
                DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    Score = totalScore,
                    Grade = GetGrade(totalScore),
                    Reason = $"供应链综合评分: {totalScore:F1}分",
                    SubResults = dimensions.Select(d => new { Name = d.Key, Score = d.Value, Weight = weights[d.Key] })
                })
            };

            result.SubResults = dimensions.Select(d => new SubResult
            {
                Name = d.Key,
                Score = d.Value,
                Weight = weights[d.Key],
                WeightedScore = d.Value * weights[d.Key]
            }).ToList();

            if (totalScore < 60)
            {
                result.Warnings.Add("供应链风险较高，建议寻找备选供应商");
            }

            if (product.LeadTimeDays > 45)
            {
                result.Warnings.Add("交货周期过长，可能影响资金周转");
            }

            if (product.MOQ > 1000)
            {
                result.Warnings.Add("起订量较高，首次投入风险大");
            }

            return result;
        }

        private decimal ScoreSupplierCount(int? count) =>
            count >= 10 ? 90 :
            count >= 5 ? 80 :
            count >= 3 ? 70 :
            count >= 1 ? 60 : 40;

        private decimal ScoreSupplierStability(int? stability) =>
            stability >= 90 ? 95 :
            stability >= 80 ? 85 :
            stability >= 70 ? 75 :
            stability >= 60 ? 65 :
            stability >= 50 ? 55 : 40;

        private decimal ScoreLeadTime(int? days) =>
            days <= 15 ? 95 :
            days <= 30 ? 85 :
            days <= 45 ? 70 :
            days <= 60 ? 55 : 40;

        private decimal ScoreMOQ(int? moq) =>
            moq <= 100 ? 95 :
            moq <= 300 ? 85 :
            moq <= 500 ? 75 :
            moq <= 1000 ? 65 : 50;

        private decimal ScorePriceVolatility(decimal? volatility) =>
            volatility < 0.05m ? 90 :
            volatility < 0.10m ? 80 :
            volatility < 0.15m ? 70 :
            volatility < 0.20m ? 60 : 50;

        private decimal ScoreQuality(string certLevel) =>
            certLevel == "无" ? 90 :
            certLevel == "轻" ? 80 :
            certLevel == "中" ? 70 :
            certLevel == "严" ? 60 : 50;

        private decimal ScoreCommunication(int? stability) =>
            stability >= 80 ? 85 :
            stability >= 60 ? 75 :
            stability >= 40 ? 65 : 55;

        private decimal ScoreInnovation(int? diffPoints) =>
            diffPoints >= 5 ? 90 :
            diffPoints >= 3 ? 75 :
            diffPoints >= 1 ? 60 : 50;
    }

    /// <summary>
    /// S17 - 6大创新矩阵
    /// </summary>
    public class InnovationMatrixStrategy : BaseStrategy
    {
        public override string Code => "S17";
        public override string Name => "6大创新矩阵";
        public override string Description => "6个方向创新打法推荐";
        public override StrategyType Type => StrategyType.Recommendation;

        public override IReadOnlyList<string> RequiredFields => new[] { nameof(ProductData.ProductName) };

        public override string LogicDefinition => @"
### 策略定义
6大创新矩阵，基于产品现状智能推荐创新迭代方向。

### 核心输入
*   Differentiation, Margin, Variants, Reviews, LeadTime

### 计算逻辑
1.  **创新方向匹配**: 
    - 差异化低 -> 产品创新/包装创新
    - 利润高 -> 品牌溢价/会员模式
    - 变体少 -> 组合销售
    - 评分高 -> 视频营销/KOL
    - 交期长 -> 供应链优化
2.  **输出**: 匹配到的可行创新策略建议。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            // 6大创新方向评估
            var innovationDimensions = new List<(string direction, decimal score, List<string> tactics, string trigger)>();
            
            // 计算毛利率
            var margin = product.TargetPrice > 0 
                ? (product.TargetPrice - product.PurchaseCost) / product.TargetPrice 
                : 0;

            // 1. 产品创新 (权重25%)
            var productScore = 0m;
            var productTactics = new List<string>();
            if (product.DifferentiationPoints < 5)
            {
                productScore = 85;
                productTactics.Add("功能升级: 增加智能化/多功能设计");
                productTactics.Add("材质改进: 使用环保/高端材料");
                if (product.AverageRating < 4.2m)
                    productTactics.Add("质量提升: 解决竞品差评痛点");
            }
            else
            {
                productScore = 50;
                productTactics.Add("保持现有差异化优势");
            }
            innovationDimensions.Add(("产品创新", productScore, productTactics, 
                product.DifferentiationPoints < 5 ? "差异化不足" : "差异化良好"));

            // 2. 定价创新 (权重15%)
            var priceScore = 0m;
            var priceTactics = new List<string>();
            if (margin >= 0.40m)
            {
                priceScore = 90;
                priceTactics.Add("高端定位: 提升品牌溢价空间");
                priceTactics.Add("会员制: 订阅模式锁定复购");
            }
            else if (margin >= 0.25m)
            {
                priceScore = 70;
                priceTactics.Add("价值定位: 强调性价比");
                priceTactics.Add("捆绑销售: 提升客单价");
            }
            else
            {
                priceScore = 40;
                priceTactics.Add("成本优化: 需先改善毛利空间");
            }
            innovationDimensions.Add(("定价创新", priceScore, priceTactics, 
                $"毛利率{margin:P0}"));

            // 3. 组合创新 (权重15%)
            var comboScore = 0m;
            var comboTactics = new List<string>();
            if (product.VariantCount < 5)
            {
                comboScore = 85;
                comboTactics.Add("套装销售: 主品+配件捆绑");
                comboTactics.Add("多规格: 满足不同用户需求");
                comboTactics.Add("颜色扩展: 增加热门色系");
            }
            else
            {
                comboScore = 60;
                comboTactics.Add("变体优化: 保留热销款");
            }
            innovationDimensions.Add(("组合创新", comboScore, comboTactics, 
                $"当前{product.VariantCount}个变体"));

            // 4. 渠道创新 (权重15%)
            var channelScore = 70m;
            var channelTactics = new List<string>
            {
                "多站点布局: 欧洲/日本/中东站",
                "自建站: 品牌独立站DTC"
            };
            if (product.TopConcentration > 0.6m)
            {
                channelScore = 85;
                channelTactics.Add("蓝海站点: 避开主站激烈竞争");
            }
            innovationDimensions.Add(("渠道创新", channelScore, channelTactics, 
                "渠道多元化"));

            // 5. 营销创新 (权重20%)
            var marketingScore = 0m;
            var marketingTactics = new List<string>();
            if (product.AverageRating >= 4.3m && product.TotalReviews >= 50)
            {
                marketingScore = 90;
                marketingTactics.Add("KOL合作: 红人带货推广");
                marketingTactics.Add("视频营销: TikTok/YouTube种草");
                marketingTactics.Add("UGC内容: 鼓励用户晒单");
            }
            else if (product.AverageRating >= 4.0m)
            {
                marketingScore = 70;
                marketingTactics.Add("内容营销: A+页面优化");
                marketingTactics.Add("广告优化: 精准关键词投放");
            }
            else
            {
                marketingScore = 50;
                marketingTactics.Add("评价改善: 优先提升产品质量");
            }
            innovationDimensions.Add(("营销创新", marketingScore, marketingTactics, 
                $"评分{product.AverageRating}星"));

            // 6. 供应链创新 (权重10%)
            var supplyScore = 0m;
            var supplyTactics = new List<string>();
            if (product.LeadTimeDays > 30)
            {
                supplyScore = 85;
                supplyTactics.Add("快反供应链: 缩短交期至15天内");
                supplyTactics.Add("海外仓: 本地化发货提升时效");
            }
            else if (product.LeadTimeDays > 15)
            {
                supplyScore = 70;
                supplyTactics.Add("备货策略: 提前备货应对旺季");
            }
            else
            {
                supplyScore = 60;
                supplyTactics.Add("供应链稳定: 保持现有优势");
            }
            innovationDimensions.Add(("供应链创新", supplyScore, supplyTactics, 
                $"交期{product.LeadTimeDays}天"));

            // 计算综合得分 (加权平均)
            var weights = new[] { 0.25m, 0.15m, 0.15m, 0.15m, 0.20m, 0.10m };
            var totalScore = 0m;
            for (int i = 0; i < innovationDimensions.Count && i < weights.Length; i++)
            {
                totalScore += innovationDimensions[i].score * weights[i];
            }

            // 收集所有推荐的策略
            var allTactics = innovationDimensions
                .SelectMany(d => d.tactics.Select(t => $"[{d.direction}] {t}"))
                .ToList();

            // 高分方向
            var topDirections = innovationDimensions
                .Where(d => d.score >= 80)
                .OrderByDescending(d => d.score)
                .Select(d => d.direction)
                .ToList();

            // SubResults
            var subResults = innovationDimensions.Select((d, i) => new SubResult
            {
                Name = d.direction,
                Score = d.score,
                Weight = i < weights.Length ? weights[i] : 0.1m,
                WeightedScore = d.score * (i < weights.Length ? weights[i] : 0.1m),
                Description = $"{d.trigger} → {d.tactics.Count}个打法"
            }).ToList();

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = Math.Round(totalScore, 1),
                Grade = GetGrade(totalScore),
                Decision = topDirections.Count >= 3 ? "GO" : topDirections.Count >= 1 ? "WAIT" : "STOP",
                Reason = topDirections.Count > 0 
                    ? $"重点方向: {string.Join("、", topDirections.Take(3))} (共{allTactics.Count}个打法)"
                    : "创新空间有限",
                SubResults = subResults,
                Suggestions = allTactics.Take(10).Cast<object>().ToList(),
                DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    TotalScore = Math.Round(totalScore, 1),
                    TopDirections = topDirections,
                    Dimensions = innovationDimensions.Select(d => new 
                    { 
                        d.direction, 
                        d.score, 
                        d.trigger, 
                        TacticCount = d.tactics.Count,
                        Tactics = d.tactics 
                    }),
                    AllTactics = allTactics
                })
            };

            return result;
        }
    }
}

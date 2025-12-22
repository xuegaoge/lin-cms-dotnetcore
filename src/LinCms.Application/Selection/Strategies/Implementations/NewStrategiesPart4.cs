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
            var innovations = new List<string>();
            var score = 0m;

            // 1. 产品创新
            if (product.DifferentiationPoints < 5)
            {
                innovations.Add("产品创新: 功能升级-增加智能化/多功能设计");
                innovations.Add("产品创新: 材质改进-使用环保/高端材料");
                innovations.Add("产品创新: 包装创新-礼盒装/便携设计");
                score += 15;
            }

            // 2. 定价创新
            var margin = (product.TargetPrice - product.PurchaseCost) / product.TargetPrice;
            if (margin >= 0.3m)
            {
                innovations.Add("定价创新: 高端定位-提升品牌溢价");
                innovations.Add("定价创新: 会员制-订阅模式");
                score += 12;
            }
            else
            {
                innovations.Add("定价创新: 性价比-走量策略");
                score += 8;
            }

            // 3. 组合创新
            if (product.VariantCount < 5)
            {
                innovations.Add("组合创新: 套装销售-主品+配件");
                innovations.Add("组合创新: 多规格-满足不同需求");
                score += 13;
            }

            // 4. 渠道创新
            innovations.Add("渠道创新: 多站点布局-欧洲/日本站");
            innovations.Add("渠道创新: 自建站-品牌独立站");
            score += 10;

            // 5. 营销创新
            if (product.AverageRating >= 4.3m)
            {
                innovations.Add("营销创新: KOL合作-红人带货");
                innovations.Add("营销创新: 视频营销-短视频种草");
                score += 14;
            }
            else
            {
                innovations.Add("营销创新: 内容营销-图文优化");
                score += 8;
            }

            // 6. 供应链创新
            if (product.LeadTimeDays > 30)
            {
                innovations.Add("供应链创新: 快反供应链-缩短交期");
                innovations.Add("供应链创新: 海外仓-本地化发货");
                score += 11;
            }

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = Math.Min(score, 100),
                Grade = GetGrade(score),
                Decision = innovations.Count >= 8 ? "GO" : "WAIT",
                Reason = $"推荐{innovations.Count}个创新方向",
                Suggestions = innovations,
                DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    Score = Math.Min(score, 100),
                    Grade = GetGrade(score),
                    Reason = $"推荐{innovations.Count}个创新方向",
                    Suggestions = innovations
                })
            };

            return result;
        }
    }
}

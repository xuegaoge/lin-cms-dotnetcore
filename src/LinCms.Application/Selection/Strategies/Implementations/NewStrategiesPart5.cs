using LinCms.Application.Selection.Models;
using LinCms.Entities.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinCms.Application.Selection.Strategies.Implementations
{
    /// <summary>
    /// S11 - 企业定位评估
    /// </summary>
    public class EnterpriseProfileStrategy : BaseStrategy
    {
        public override string Code => "S11";
        public override string Name => "企业定位评估";
        public override string Description => "8维度企业能力评估";
        public override StrategyType Type => StrategyType.Scoring;

        public override IReadOnlyList<string> RequiredFields => new[] { nameof(ProductData.ProductName) };

        public override string LogicDefinition => @"
### 策略定义
企业定位评估，判断企业自身资源与产品的匹配度，实现人货匹配。

### 核心输入
*   EnterpriseProfile上下文数据 (资金, 团队, 资源等)

### 计算逻辑
1.  **能力评估**: 资金(20%), 团队(15%), 供应链(15%), 运营(15%), 品牌(10%), 技术(10%), 市场(10%), 风险(5%)。
2.  **匹配分析**: 根据企业所处阶段(初创/成长/成熟)动态调整选品建议。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            // 如果没有企业定位信息，返回默认评分
            if (context?.EnterpriseProfile == null)
            {
                return new StrategyResult
                {
                    StrategyCode = Code,
                    StrategyName = Name,
                    Type = Type,
                    Score = 70,
                    Grade = "B",
                    Decision = "WAIT",
                    Reason = "未配置企业定位信息",
                    Warnings = new List<string> { "建议先完成企业定位评估" }
                };
            }

            var profile = context.EnterpriseProfile;

            // 8维度评估
            var dimensions = new Dictionary<string, decimal>
            {
                ["资金实力"] = EvaluateCapital(profile),
                ["团队能力"] = EvaluateTeam(profile),
                ["供应链"] = EvaluateSupplyChain(profile),
                ["运营经验"] = EvaluateExperience(profile),
                ["品牌建设"] = EvaluateBrand(profile),
                ["技术能力"] = EvaluateTechnology(profile),
                ["市场资源"] = EvaluateMarket(profile),
                ["风险承受"] = EvaluateRisk(profile)
            };

            var weights = new Dictionary<string, decimal>
            {
                ["资金实力"] = 0.20m,
                ["团队能力"] = 0.15m,
                ["供应链"] = 0.15m,
                ["运营经验"] = 0.15m,
                ["品牌建设"] = 0.10m,
                ["技术能力"] = 0.10m,
                ["市场资源"] = 0.10m,
                ["风险承受"] = 0.05m
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
                Reason = $"企业综合能力评分: {totalScore:F1}分"
            };

            result.SubResults = dimensions.Select(d => new SubResult
            {
                Name = d.Key,
                Score = d.Value,
                Weight = weights[d.Key],
                WeightedScore = d.Value * weights[d.Key]
            }).ToList();

            // 根据企业等级给出建议
            if (profile.EnterpriseLevel == "初创")
            {
                result.Suggestions.Add("建议选择低风险、小批量产品");
                result.Suggestions.Add("优先考虑快速回款的品类");
            }
            else if (profile.EnterpriseLevel == "成长")
            {
                result.Suggestions.Add("可以尝试中等风险产品");
                result.Suggestions.Add("建立品牌化运营");
            }
            else if (profile.EnterpriseLevel == "成熟")
            {
                result.Suggestions.Add("可以进入高投入品类");
                result.Suggestions.Add("多品牌矩阵布局");
            }

            return result;
        }

        private decimal EvaluateCapital(EnterpriseProfile profile)
        {
            // 基于资金规模评分
            var capital = profile.CapitalScale;
            return capital >= 1000000 ? 90 :
                   capital >= 500000 ? 80 :
                   capital >= 200000 ? 70 :
                   capital >= 100000 ? 60 : 50;
        }

        private decimal EvaluateTeam(EnterpriseProfile profile)
        {
            // 基于团队规模评分
            var teamSize = profile.TeamSize;
            return teamSize >= 20 ? 90 :
                   teamSize >= 10 ? 80 :
                   teamSize >= 5 ? 70 :
                   teamSize >= 3 ? 60 : 50;
        }

        private decimal EvaluateSupplyChain(EnterpriseProfile profile)
        {
            // 基于供应链能力评分
            return profile.SupplyChainCapability switch
            {
                "强" => 90,
                "中" => 70,
                "弱" => 50,
                _ => 60
            };
        }

        private decimal EvaluateExperience(EnterpriseProfile profile)
        {
            // 基于运营年限评分
            var years = profile.OperationYears;
            return years >= 5 ? 90 :
                   years >= 3 ? 80 :
                   years >= 2 ? 70 :
                   years >= 1 ? 60 : 50;
        }

        private decimal EvaluateBrand(EnterpriseProfile profile)
        {
            // 基于品牌能力评分
            return profile.BrandCapability switch
            {
                "强" => 90,
                "中" => 70,
                "弱" => 50,
                _ => 60
            };
        }

        private decimal EvaluateTechnology(EnterpriseProfile profile)
        {
            // 基于技术能力评分
            return profile.TechnologyCapability switch
            {
                "强" => 90,
                "中" => 70,
                "弱" => 50,
                _ => 60
            };
        }

        private decimal EvaluateMarket(EnterpriseProfile profile)
        {
            // 基于市场资源评分
            return profile.MarketResources switch
            {
                "丰富" => 90,
                "一般" => 70,
                "缺乏" => 50,
                _ => 60
            };
        }

        private decimal EvaluateRisk(EnterpriseProfile profile)
        {
            // 基于风险承受能力评分
            return profile.RiskPreference switch
            {
                "高" => 90,
                "中" => 70,
                "低" => 50,
                _ => 60
            };
        }
    }

    /// <summary>
    /// S18 - 压力测试
    /// </summary>
    public class StressTestStrategy : BaseStrategy
    {
        public override string Code => "S18";
        public override string Name => "压力测试";
        public override string Description => "8种极端场景压力测试";
        public override StrategyType Type => StrategyType.RiskDetection;

        public override IReadOnlyList<string> RequiredFields => new[]
        {
            nameof(ProductData.TargetPrice),
            nameof(ProductData.PurchaseCost)
        };

        public override string LogicDefinition => @"
### 策略定义
压力测试，模拟8种极端市场变化场景，测试产品的抗风险能力与生存边界。

### 核心输入
*   Price, Cost, Shipping, FBA, EstimatedSales

### 计算逻辑
1.  **场景模拟**: 设定8个极端场景(如: 价格跳水10%, 成本暴涨15%, 销量腰斩30%及各种组合)。
2.  **利润演算**: 重新计算每个场景下的净利润 (Price-Cost-Ship-FBA-Comm-Ad)。
3.  **生存指标**: 
    - 生存率 = 盈利场景数 / 8。
    - 决策: 生存率>75% -> GO; <50% -> STOP。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            // 定义8种压力场景
            var scenarios = new[]
            {
                new { name = "场景1-正常", priceChange = 0m, costChange = 0m, volumeChange = 0m },
                new { name = "场景2-价格下降10%", priceChange = -0.1m, costChange = 0m, volumeChange = 0m },
                new { name = "场景3-成本上升15%", priceChange = 0m, costChange = 0.15m, volumeChange = 0m },
                new { name = "场景4-销量下降30%", priceChange = 0m, costChange = 0m, volumeChange = -0.3m },
                new { name = "场景5-价格降+成本升", priceChange = -0.1m, costChange = 0.1m, volumeChange = 0m },
                new { name = "场景6-价格降+销量降", priceChange = -0.1m, costChange = 0m, volumeChange = -0.2m },
                new { name = "场景7-成本升+销量降", priceChange = 0m, costChange = 0.15m, volumeChange = -0.2m },
                new { name = "场景8-极端情况", priceChange = -0.15m, costChange = 0.2m, volumeChange = -0.3m }
            };

            var results = scenarios.Select(s => new
            {
                scenario_name = s.name,
                net_profit = CalculateProfit(product, s.priceChange, s.costChange, s.volumeChange),
                result = CalculateProfit(product, s.priceChange, s.costChange, s.volumeChange) > 0 ? "PASS" : "FAIL"
            }).ToList();

            var passCount = results.Count(r => r.result == "PASS");
            var survivalRate = (decimal)passCount / results.Count;
            var worstCaseProfit = results.Min(r => r.net_profit);

            var decision = survivalRate >= 0.75m ? "GO" : survivalRate >= 0.5m ? "WAIT" : "STOP";

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = survivalRate * 100,
                Grade = GetGrade(survivalRate * 100),
                Decision = decision,
                Reason = $"压力测试通过率: {survivalRate:P0} ({passCount}/8场景通过)",
                DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    Score = survivalRate * 100,
                    Grade = GetGrade(survivalRate * 100),
                    Decision = decision,
                    Reason = $"压力测试通过率: {survivalRate:P0} ({passCount}/8场景通过)",
                    Indicators = results.Select(r => new { Name = r.scenario_name, Value = r.net_profit, Status = r.result })
                })
            };

            // 添加详细结果
            result.Indicators = results.Select(r => new Indicator
            {
                Name = r.scenario_name,
                Value = r.net_profit,
                Unit = "元",
                Status = r.result
            }).ToList();

            // 添加预警
            if (survivalRate < 0.75m)
            {
                result.Warnings.Add($"生存率偏低({survivalRate:P0})，抗风险能力不足");
            }

            if (worstCaseProfit < -1000)
            {
                result.Warnings.Add($"极端情况下亏损严重({worstCaseProfit:C})");
            }

            // 添加建议
            if (survivalRate >= 0.75m)
            {
                result.Suggestions.Add("产品抗风险能力强，可以进入");
            }
            else if (survivalRate >= 0.5m)
            {
                result.Suggestions.Add("建议优化成本结构，提高抗风险能力");
            }
            else
            {
                result.Suggestions.Add("风险过高，不建议进入");
            }

            return result;
        }

        private decimal CalculateProfit(ProductData product, decimal priceChange, decimal costChange, decimal volumeChange)
        {
            var price = (product.TargetPrice ?? 0) * (1 + priceChange);
            var cost = (product.PurchaseCost ?? 0) * (1 + costChange);
            var shipping = product.ShippingCost ?? 0;
            var fba = product.FBACost ?? 0;
            var referralFee = price * 0.15m; // 假设15%
            var acos = price * 0.2m; // 假设20% ACOS
            var volume = (product.EstimatedMonthlySales ?? 100) * (1 + volumeChange);

            var unitProfit = price - cost - shipping - fba - referralFee - acos;
            return unitProfit * volume;
        }
    }
}

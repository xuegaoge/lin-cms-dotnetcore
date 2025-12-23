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
            nameof(ProductData.PurchaseCost),
            nameof(ProductData.MonthlySearchVolume),
            nameof(ProductData.CompetitorCount)
        };

        public override string LogicDefinition => @"
### 策略定义
赛道市场评估，聚焦于宏观赛道的吸引力与潜力。

### 核心输入
*   SearchVolume, CompetitorCount, Price, Cost, TopConcentration, NewRatio

### 计算逻辑
1.  **MSI (市场规模)**: 月搜索量 × 平均售价 × 估算转化率(5%)，计算月销售额
2.  **CII (竞争强度)**: 竞品数量 × 头部集中度 × (1-新品占比)
3.  **毛利率**: (售价-成本-运费-FBA-佣金) / 售价
4.  **蓝海指数**: (月搜索量 × 供需比) / 竞品数
5.  **总分**: 四项指标加权求和 (30% + 25% + 25% + 20%)";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            // 按设计文档计算四个维度
            var (msiValue, msiScore) = CalculateMSI(product);
            var (ciiValue, ciiScore) = CalculateCII(product);
            var (marginValue, marginScore) = CalculateMargin(product);
            var (boiValue, boiScore) = CalculateBlueOcean(product);

            // 加权求和: MSI 30% + CII 25% + 毛利 25% + 蓝海 20%
            var totalScore = msiScore * 0.30m + ciiScore * 0.25m + marginScore * 0.25m + boiScore * 0.20m;

            // 赛道等级判定 (设计文档第474-482行)
            var grade = totalScore >= 85 ? "S" :
                       totalScore >= 70 ? "A" :
                       totalScore >= 55 ? "B" :
                       totalScore >= 40 ? "C" : "D";
            
            var decision = totalScore >= 70 ? "GO" : totalScore >= 55 ? "WAIT" : "STOP";

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = Math.Round(totalScore, 1),
                Grade = grade,
                Decision = decision,
                Reason = $"赛道等级{grade}级，综合评分{totalScore:F1}分"
            };

            result.SubResults = new List<SubResult>
            {
                new SubResult 
                { 
                    Name = "MSI市场规模指数", 
                    Score = msiScore, 
                    Weight = 0.30m, 
                    WeightedScore = msiScore * 0.30m,
                    Description = $"月销售额估算: ${msiValue:N0}"
                },
                new SubResult 
                { 
                    Name = "CII竞争强度指数", 
                    Score = ciiScore, 
                    Weight = 0.25m, 
                    WeightedScore = ciiScore * 0.25m,
                    Description = $"竞争指数: {ciiValue:F1} (越低越好)"
                },
                new SubResult 
                { 
                    Name = "毛利率空间", 
                    Score = marginScore, 
                    Weight = 0.25m, 
                    WeightedScore = marginScore * 0.25m,
                    Description = $"毛利率: {marginValue:P1}"
                },
                new SubResult 
                { 
                    Name = "蓝海指数", 
                    Score = boiScore, 
                    Weight = 0.20m, 
                    WeightedScore = boiScore * 0.20m,
                    Description = $"蓝海指数: {boiValue:F1}"
                }
            };

            result.DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                MSI = new { Value = msiValue, Score = msiScore },
                CII = new { Value = ciiValue, Score = ciiScore },
                Margin = new { Value = marginValue, Score = marginScore },
                BOI = new { Value = boiValue, Score = boiScore },
                TotalScore = totalScore,
                Grade = grade
            });

            return result;
        }

        /// <summary>
        /// MSI市场规模指数: 月搜索量 × 平均售价 × 估算转化率(5%)
        /// 设计文档: 绿>500K, 黄200-500K, 红<200K (月销售额美元)
        /// </summary>
        private (decimal value, decimal score) CalculateMSI(ProductData product)
        {
            var estimatedConversion = 0.05m; // 5% 估算转化率
            var msi = (product.MonthlySearchVolume ?? 0) * (product.TargetPrice ?? 0) * estimatedConversion;
            
            // 评分转换 (基于月销售额)
            decimal score = msi >= 500000 ? 100 :
                           msi >= 300000 ? 90 :
                           msi >= 200000 ? 80 :
                           msi >= 100000 ? 70 :
                           msi >= 50000 ? 60 :
                           msi >= 20000 ? 50 : 40;
            
            return (msi, score);
        }

        /// <summary>
        /// CII竞争强度指数: 竞品数量 × 头部集中度 × (1 - 新品占比)
        /// 设计文档: 绿<50, 黄50-200, 红>200 (越低越好)
        /// </summary>
        private (decimal value, decimal score) CalculateCII(ProductData product)
        {
            var competitors = product.CompetitorCount ?? 0;
            var concentration = product.TopConcentration ?? 0.5m;
            var newRatio = product.NewProductRatio ?? 0.15m;
            
            var cii = competitors * concentration * (1 - newRatio);
            
            // 评分转换 (越低越好)
            decimal score = cii < 50 ? 100 :
                           cii < 100 ? 85 :
                           cii < 150 ? 70 :
                           cii < 200 ? 55 :
                           cii < 300 ? 45 : 30;
            
            return (cii, score);
        }

        /// <summary>
        /// 毛利率: (售价 - 采购成本 - 运费 - FBA - 佣金15%) / 售价
        /// 设计文档: 绿≥35%, 黄25-35%, 红<25%
        /// </summary>
        private (decimal value, decimal score) CalculateMargin(ProductData product)
        {
            var price = product.TargetPrice ?? 0;
            var cost = product.PurchaseCost ?? 0;
            var shipping = product.ShippingCost ?? 0;
            var fba = product.FBACost ?? 0;
            var commission = price * 0.15m; // 15% 佣金
            
            var grossProfit = price - cost - shipping - fba - commission;
            var margin = price > 0 ? grossProfit / price : 0;
            
            // 评分转换
            decimal score = margin >= 0.45m ? 100 :
                           margin >= 0.40m ? 90 :
                           margin >= 0.35m ? 80 :
                           margin >= 0.30m ? 70 :
                           margin >= 0.25m ? 60 :
                           margin >= 0.20m ? 50 : 40;
            
            return (margin, score);
        }

        /// <summary>
        /// 蓝海指数: (月搜索量 × 供需比) / 竞品数
        /// 供需比 = 月搜索量 / 竞品数
        /// 设计文档: 绿>10, 黄5-10, 红<5
        /// </summary>
        private (decimal value, decimal score) CalculateBlueOcean(ProductData product)
        {
            var searchVolume = product.MonthlySearchVolume ?? 0;
            var competitors = product.CompetitorCount ?? 1;
            if (competitors == 0) competitors = 1;
            
            // 供需比
            var sdr = (decimal)searchVolume / competitors;
            // 蓝海指数 = (月搜 × 供需比) / 竞品数
            var boi = (searchVolume * sdr) / competitors;
            // 简化: boi = 月搜^2 / 竞品数^2
            
            // 评分转换
            decimal score = boi >= 100 ? 100 :
                           boi >= 50 ? 90 :
                           boi >= 20 ? 80 :
                           boi >= 10 ? 70 :
                           boi >= 5 ? 60 :
                           boi >= 2 ? 50 : 40;
            
            return (boi, score);
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

            // 注意：SearchGrowthRate 存储的是百分比值
            if (product.SearchGrowthRate >= 30m)
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
                Suggestions = recommendations.Cast<object>().ToList()
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
            // 8大机会识别 - 每个机会有对应的成功率权重
            var opportunities = new List<(string code, string name, decimal successRate, bool triggered)>();
            
            // O1: 关键词断层 (成功率85%)
            bool o1 = product.MonthlySearchVolume >= 5000 && product.CompetitorCount < 100;
            opportunities.Add(("O1", "需求大+竞争小=黄金蓝海", 0.85m, o1));

            // O2: 趋势蓝海 (成功率80%) - SearchGrowthRate 存储的是百分比值
            bool o2 = product.SearchGrowthRate >= 50m && product.NewProductRatio < 0.2m;
            opportunities.Add(("O2", "高增长+新品少=趋势蓝海", 0.80m, o2));

            // O3: 分散蓝海 (成功率78%)
            bool o3 = product.TopConcentration < 0.3m;
            opportunities.Add(("O3", "低集中度=分散蓝海", 0.78m, o3));

            // O4: 改进蓝海 (成功率75%)
            bool o4 = product.AverageRating < 4.2m && product.TotalReviews > 500;
            opportunities.Add(("O4", "评分低+评论多=改进蓝海", 0.75m, o4));

            // O5: 利润蓝海 (成功率78%)
            var margin = (product.TargetPrice - product.PurchaseCost) / product.TargetPrice;
            bool o5 = margin >= 0.4m && product.CompetitorCount < 200;
            opportunities.Add(("O5", "高毛利+低竞争=利润蓝海", 0.78m, o5));

            // O6: 创新蓝海 (成功率80%)
            bool o6 = product.DifferentiationPoints >= 5;
            opportunities.Add(("O6", "高差异化=创新蓝海", 0.80m, o6));

            // O7: 稳定蓝海 (成功率76%)
            bool o7 = product.Seasonality < 0.3m && product.MonthlySearchVolume >= 3000;
            opportunities.Add(("O7", "全年需求=稳定蓝海", 0.76m, o7));

            // O8: 供应蓝海 (成功率72%)
            bool o8 = product.SupplierCount >= 5 && product.SupplierStability >= 80;
            opportunities.Add(("O8", "供应链优势=供应蓝海", 0.72m, o8));

            // 计算加权得分
            var triggeredOpportunities = opportunities.Where(o => o.triggered).ToList();
            int totalOpportunities = triggeredOpportunities.Count;
            
            decimal score;
            if (totalOpportunities == 0)
            {
                // 没有识别到任何机会，评估基础市场状况
                // 基于市场容量和竞争度给一个基础分
                decimal marketScore = product.MonthlySearchVolume >= 10000 ? 20 : 
                                      product.MonthlySearchVolume >= 5000 ? 15 : 10;
                decimal competitionScore = product.CompetitorCount < 200 ? 15 :
                                           product.CompetitorCount < 400 ? 10 : 5;
                score = marketScore + competitionScore; // 基础分 15-35
            }
            else
            {
                // 基于识别到的机会成功率加权计算
                // 公式: 基础分(30) + 每个机会贡献分 = 30 + sum(成功率 * 10)
                // 例如: 识别到O1(85%) + O3(78%) = 30 + 8.5 + 7.8 = 46.3分
                decimal opportunityBonus = triggeredOpportunities.Sum(o => o.successRate * 10);
                
                // 加上市场规模加成 (0-10分)
                decimal marketBonus = product.MonthlySearchVolume >= 50000 ? 10 :
                                      product.MonthlySearchVolume >= 20000 ? 7 :
                                      product.MonthlySearchVolume >= 10000 ? 5 : 0;
                
                // 加上增长趋势加成 (0-10分) - SearchGrowthRate 存储的是百分比值
                decimal growthBonus = product.SearchGrowthRate >= 20m ? 10 :
                                      product.SearchGrowthRate >= 10m ? 6 :
                                      product.SearchGrowthRate >= 5m ? 3 : 0;
                
                // 基础分(30) + 机会加成 + 市场加成 + 增长加成
                score = 30 + opportunityBonus + marketBonus + growthBonus;
            }
            
            score = Math.Min(score, 100);

            // 构建结果
            var suggestionList = triggeredOpportunities
                .Select(o => $"机会{o.code}: {o.name} (成功率{o.successRate:P0})")
                .ToList();

            if (triggeredOpportunities.Count == 0)
            {
                suggestionList.Add("暂未识别到明显蓝海机会，建议关注市场变化或寻找更多差异化方向");
            }

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = Math.Round(score, 1),
                Grade = GetGrade(score),
                Decision = score >= 60 ? "GO" : score >= 40 ? "WAIT" : "STOP",
                Reason = $"识别到{totalOpportunities}个蓝海机会，综合蓝海指数{score:F1}分",
                Suggestions = suggestionList.Cast<object>().ToList()
            };

            // 构建SubResults展示各机会的状态
            result.SubResults = opportunities.Select(o => new SubResult
            {
                Name = o.name,
                Score = o.triggered ? o.successRate * 100 : 0,
                Weight = 0.125m, // 8个机会平均权重
                WeightedScore = o.triggered ? o.successRate * 12.5m : 0,
                Description = o.triggered ? $"✓ 已触发 (预期成功率{o.successRate:P0})" : "✗ 未触发"
            }).ToList();

            result.DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(new 
            { 
                Opportunities = opportunities.Select(o => new { o.code, o.name, o.successRate, o.triggered }),
                TotalOpportunities = totalOpportunities,
                OverallBlueOceanScore = score
            });

            return result;
        }
    }

    /// <summary>
    /// S10 - 赛道热度评级
    /// 按照设计文档: 4个维度(搜索量/竞争度/毛利率/增长率)，每个0-25分，总分0-100
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
            nameof(ProductData.TopConcentration),
            nameof(ProductData.TargetPrice),
            nameof(ProductData.PurchaseCost),
            nameof(ProductData.SearchGrowthRate)
        };

        public override string LogicDefinition => @"
### 策略定义
赛道热度评级，判断市场目前是处于""极冷""无人问津还是""极热""红海厮杀状态。

### 核心输入
*   SearchVolume, TopConcentration, GrossMargin, GrowthRate

### 计算逻辑 (设计文档第688-733行)
1.  **评分构成**: 搜索量(25分) + 竞争度(25分) + 毛利率(25分) + 增长率(25分) = 100分
2.  **温度分级**: 
    - ≥85分: 极热 (龙头聚焦)
    - 65-84分: 热赛道 (全力冲刺)
    - 45-64分: 温赛道 (标准进入)
    - 25-44分: 冷赛道 (小额测试)
    - <25分: 极冷 (避开)";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            // ============================================
            // 按设计文档计算4个维度，每个0-25分
            // ============================================
            
            // 1. 搜索量评分 (0-25分) - 设计文档第689-695行
            var searchVolume = product.MonthlySearchVolume ?? 0;
            decimal searchScore = searchVolume > 1000000 ? 25 :
                                  searchVolume > 200000 ? 20 :
                                  searchVolume > 50000 ? 15 :
                                  searchVolume > 10000 ? 10 : 5;

            // 2. 竞争度评分 (0-25分) - 越低越好 - 设计文档第697-703行
            var concentration = product.TopConcentration ?? 0.5m;
            decimal competitionScore = concentration < 0.05m ? 25 :
                                       concentration < 0.15m ? 20 :
                                       concentration < 0.30m ? 15 :
                                       concentration < 0.50m ? 10 : 5;

            // 3. 毛利率评分 (0-25分) - 设计文档第705-712行
            var price = product.TargetPrice ?? 0;
            var cost = product.PurchaseCost ?? 0;
            var shipping = product.ShippingCost ?? 0;
            var fba = product.FBACost ?? 0;
            var commission = price * 0.15m;
            var grossProfit = price - cost - shipping - fba - commission;
            var margin = price > 0 ? grossProfit / price : 0;
            
            decimal marginScore = margin > 0.45m ? 25 :
                                  margin > 0.35m ? 20 :
                                  margin > 0.25m ? 15 :
                                  margin > 0.20m ? 10 : 5;

            // 4. 增长率评分 (0-25分) - SearchGrowthRate是百分比值(如30代表30%)
            // 设计文档第714-720行使用的是小数(0.30代表30%)，需要转换
            var growthRate = product.SearchGrowthRate ?? 0;
            // 转换：代码中8.3代表8.3%，设计文档判断>30%即>0.30
            decimal growthScore = growthRate > 30 ? 25 :   // >30%
                                  growthRate > 20 ? 20 :   // >20%
                                  growthRate > 10 ? 15 :   // >10%
                                  growthRate > 5 ? 10 : 5; // >5%

            // 总分 = 四个维度之和 (0-100)
            var totalScore = searchScore + competitionScore + marginScore + growthScore;

            // 热度等级判定 - 设计文档第724-730行
            var heatLevel = totalScore >= 85 ? "极热" :
                           totalScore >= 65 ? "热" :
                           totalScore >= 45 ? "温" :
                           totalScore >= 25 ? "冷" : "极冷";

            // 策略建议 - 设计文档第654-660行
            var strategy = heatLevel switch
            {
                "极热" => "龙头聚焦/全部资源投入",
                "热" => "全力冲刺/抢占地位",
                "温" => "标准进入/完整流程",
                "冷" => "谨慎进入/小额测试",
                _ => "避开/等风口"
            };

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = totalScore,
                Grade = heatLevel,
                Decision = totalScore >= 45 && totalScore <= 85 ? "GO" : 
                           totalScore >= 25 ? "WAIT" : "STOP",
                Reason = $"赛道热度: {heatLevel} ({totalScore}分)，建议: {strategy}",
                Warnings = totalScore >= 85 ? new List<string> { "赛道过热，竞争激烈，需评估自身实力" } :
                          totalScore < 25 ? new List<string> { "赛道过冷，需求不足，建议避开" } : new List<string>()
            };

            // 构造子结果 - 每个维度归一化为100分制便于展示
            result.SubResults = new List<SubResult>
            {
                new SubResult 
                { 
                    Name = "搜索量热度", 
                    Score = searchScore / 25m * 100, 
                    Weight = 0.25m, 
                    WeightedScore = searchScore,
                    Description = $"月搜索量 {searchVolume:N0}"
                },
                new SubResult 
                { 
                    Name = "竞争度", 
                    Score = competitionScore / 25m * 100, 
                    Weight = 0.25m, 
                    WeightedScore = competitionScore,
                    Description = $"头部集中度 {concentration:P1} (越低越好)"
                },
                new SubResult 
                { 
                    Name = "毛利率空间", 
                    Score = marginScore / 25m * 100, 
                    Weight = 0.25m, 
                    WeightedScore = marginScore,
                    Description = $"毛利率 {margin:P1}"
                },
                new SubResult 
                { 
                    Name = "增长势能", 
                    Score = growthScore / 25m * 100, 
                    Weight = 0.25m, 
                    WeightedScore = growthScore,
                    Description = $"搜索增长率 {growthRate:F1}%"
                }
            };

            result.DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                Factors = new
                {
                    SearchVolumeScore = searchScore,
                    CompetitionScore = competitionScore,
                    MarginScore = marginScore,
                    GrowthScore = growthScore
                },
                RawValues = new
                {
                    SearchVolume = searchVolume,
                    TopConcentration = concentration,
                    GrossMargin = margin,
                    GrowthRate = growthRate
                },
                TotalScore = totalScore,
                Level = heatLevel,
                Strategy = strategy
            });

            return result;
        }
    }
}

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
            // ========================================
            // 按设计文档实现40题自诊系统
            // 26题可自动判断(基于product_data字段)
            // 14题需要手填(暂时标记为待填)
            // 评分: 每题25分，满分1000分
            // ========================================
            
            var questions = new List<(string code, string question, bool passed, string category, string source)>();
            
            // ============ 生命周期验证 (10题) ============
            // Q1: 搜索趋势增长>20%? - SearchGrowthRate是百分比值(20代表20%)
            questions.Add(("Q1", "搜索趋势增长>20%?", 
                product.SearchGrowthRate >= 20m, "生命周期", "auto"));
            
            // Q2: 新品榜30天销量>300? - 需要外部数据，暂时基于综合判断
            questions.Add(("Q2", "新品榜30天销量>300?", 
                false, "生命周期", "manual"));
            
            // Q3: 供需比>1.5? - SDR = 月搜索量/竞品数
            var sdr = product.CompetitorCount > 0 
                ? (decimal)(product.MonthlySearchVolume ?? 0) / product.CompetitorCount 
                : 0;
            questions.Add(("Q3", "供需比>1.5?", 
                sdr > 1.5m, "生命周期", "auto"));
            
            // Q4: 类目处于成长期? - 主观判断，暂标记待填
            questions.Add(("Q4", "类目处于成长期?", 
                false, "生命周期", "manual"));
            
            // Q5: 未来12个月无衰退? - 需预测，暂标记待填
            questions.Add(("Q5", "未来12个月无衰退?", 
                false, "生命周期", "manual"));
            
            // Q6: 季节性<0.6?
            questions.Add(("Q6", "季节性<0.6?", 
                product.Seasonality < 0.6m, "生命周期", "auto"));
            
            // Q7: 搜索量>1000?
            questions.Add(("Q7", "搜索量>1000?", 
                product.MonthlySearchVolume > 1000, "生命周期", "auto"));
            
            // Q8: 增速>15%?
            questions.Add(("Q8", "增速>15%?", 
                product.SearchGrowthRate >= 15m, "生命周期", "auto"));
            
            // Q9: 新品成功率>30%? - 需要外部数据
            questions.Add(("Q9", "新品成功率>30%?", 
                product.NewProductRatio > 0.3m, "生命周期", "auto")); // 用新品占比近似
            
            // Q10: Google Trends向上? - 需要外部数据
            questions.Add(("Q10", "Google Trends向上?", 
                false, "生命周期", "manual"));
            
            // ============ 产品属性验证 (10题) ============
            // Q11: FBM毛利≥35%?
            var fbmMargin = product.TargetPrice > 0 
                ? (product.TargetPrice - product.PurchaseCost) / product.TargetPrice 
                : 0;
            questions.Add(("Q11", "FBM毛利≥35%?", 
                fbmMargin >= 0.35m, "产品属性", "auto"));
            
            // Q12: FBA毛利≥25%? (含FBA费用)
            var fbaMargin = product.TargetPrice > 0 
                ? (product.TargetPrice - product.PurchaseCost - (product.FBACost ?? 0) - product.TargetPrice * 0.15m) / product.TargetPrice 
                : 0;
            questions.Add(("Q12", "FBA毛利≥25%?", 
                fbaMargin >= 0.25m, "产品属性", "auto"));
            
            // Q13: 退货率<行业均值? - 需要行业基准，用通用阈值
            questions.Add(("Q13", "退货率<行业均值?", 
                product.ReturnRate < 0.10m, "产品属性", "auto")); // <10%视为合格
            
            // Q14: 评分≥4.2且Review<200?
            questions.Add(("Q14", "评分≥4.2且Review<200?", 
                product.AverageRating >= 4.2m && product.TotalReviews < 200, "产品属性", "auto"));
            
            // Q15: 重量<5磅(2.27kg)?
            questions.Add(("Q15", "重量<5磅?", 
                product.WeightKg < 2.27m, "产品属性", "auto"));
            
            // Q16: 非易碎/液体/危险品? - 需用户确认
            questions.Add(("Q16", "非易碎/液体/危险品?", 
                false, "产品属性", "manual"));
            
            // Q17: 单价$15-50?
            questions.Add(("Q17", "单价$15-50?", 
                product.TargetPrice >= 15 && product.TargetPrice <= 50, "产品属性", "auto"));
            
            // Q18: 差异化≥3点?
            questions.Add(("Q18", "差异化≥3点?", 
                product.DifferentiationPoints >= 3, "产品属性", "auto"));
            
            // Q19: 生命周期>12月? - 主观判断
            questions.Add(("Q19", "生命周期>12月?", 
                product.ProductLifecycle >= 12, "产品属性", "auto"));
            
            // Q20: 复购率>10%? - 需要经验判断
            questions.Add(("Q20", "复购率>10%?", 
                product.RepurchaseRate > 0.10m, "产品属性", "auto"));
            
            // ============ 类目竞争验证 (10题) ============
            // Q21: 长尾词>30%?
            questions.Add(("Q21", "长尾词>30%?", 
                product.LongTailKeywordRatio > 0.30m, "类目竞争", "auto"));
            
            // Q22: 无专利风险?
            var infRisk = (product.InfringementRisk ?? "").ToLower();
            questions.Add(("Q22", "无专利风险?", 
                infRisk == "低" || infRisk == "low", "类目竞争", "auto"));
            
            // Q23: 适合自建站? - 主观判断
            questions.Add(("Q23", "适合自建站?", 
                false, "类目竞争", "manual"));
            
            // Q24: 无亚马逊自营?
            questions.Add(("Q24", "无亚马逊自营?", 
                product.HasAmazonChoice != true, "类目竞争", "auto"));
            
            // Q25: ASIN<500?
            questions.Add(("Q25", "ASIN<500?", 
                product.CompetitorCount < 500, "类目竞争", "auto"));
            
            // Q26: BSR梯度合理? - 需外部数据计算
            questions.Add(("Q26", "BSR梯度合理?", 
                false, "类目竞争", "manual"));
            
            // Q27: 无垄断?
            questions.Add(("Q27", "无垄断?", 
                product.TopConcentration < 0.70m, "类目竞争", "auto"));
            
            // Q28: CPC<$0.8?
            questions.Add(("Q28", "CPC<$0.8?", 
                product.AdvertisingCPC < 0.80m, "类目竞争", "auto"));
            
            // Q29: CTR>0.3%?
            questions.Add(("Q29", "CTR>0.3%?", 
                product.ClickThroughRate > 0.003m, "类目竞争", "auto"));
            
            // Q30: 转化率>1.5%?
            questions.Add(("Q30", "转化率>1.5%?", 
                product.ConversionRate > 0.015m, "类目竞争", "auto"));
            
            // ============ 供应链验证 (6题) ============
            // Q31: 供应商>5家?
            questions.Add(("Q31", "供应商>5家?", 
                product.SupplierCount > 5, "供应链", "auto"));
            
            // Q32: 支持小单定制? - 需供应商确认
            questions.Add(("Q32", "支持小单定制?", 
                false, "供应链", "manual"));
            
            // Q33: 交期<30天?
            questions.Add(("Q33", "交期<30天?", 
                product.LeadTimeDays < 30, "供应链", "auto"));
            
            // Q34: MOQ<500件?
            questions.Add(("Q34", "MOQ<500件?", 
                product.MOQ < 500, "供应链", "auto"));
            
            // Q35: 价格波动<10%?
            questions.Add(("Q35", "价格波动<10%?", 
                product.PriceVolatility < 0.10m, "供应链", "auto"));
            
            // Q36: 供应商稳定>80?
            questions.Add(("Q36", "供应商稳定>80?", 
                product.SupplierStability > 80, "供应链", "auto"));
            
            // ============ 风险合规验证 (4题) ============
            // Q37: IP风险低?
            questions.Add(("Q37", "IP风险低?", 
                infRisk == "低" || infRisk == "low", "风险合规", "auto"));
            
            // Q38: 政策风险低?
            questions.Add(("Q38", "政策风险低?", 
                product.PolicyRisk < 0.30m, "风险合规", "auto"));
            
            // Q39: 6月ROI>2:1? - 需财务模型计算
            var estimatedROI = fbaMargin > 0 ? fbaMargin * 6 : 0; // 简化估算
            questions.Add(("Q39", "6月ROI>2:1?", 
                estimatedROI > 1.0m, "风险合规", "auto")); // ROI>100%约等于2:1
            
            // Q40: 黄金三问满足? - 主观判断
            questions.Add(("Q40", "黄金三问满足?", 
                false, "风险合规", "manual"));
            
            // ========================================
            // 统计结果
            // ========================================
            var autoQuestions = questions.Where(q => q.source == "auto").ToList();
            var manualQuestions = questions.Where(q => q.source == "manual").ToList();
            
            var autoPassCount = autoQuestions.Count(q => q.passed);
            var totalPassCount = questions.Count(q => q.passed);
            
            // 按设计文档: 每题25分，满分1000分
            var score = totalPassCount * 25;
            var passRate = (decimal)totalPassCount / 40;
            
            // 按分类统计
            var categoryStats = questions
                .GroupBy(q => q.category)
                .Select(g => new SubResult
                {
                    Name = g.Key,
                    Score = g.Count(q => q.passed) * 100m / g.Count(),
                    Weight = g.Count() / 40m,
                    WeightedScore = g.Count(q => q.passed) * 25m,
                    Description = $"通过 {g.Count(q => q.passed)}/{g.Count()} 题"
                })
                .ToList();

            // 决策判定 (设计文档: ≥800分GO, 600-799分WAIT, <600分STOP)
            var decision = score >= 800 ? "GO" : score >= 600 ? "WAIT" : "STOP";
            var grade = score >= 800 ? "A" : score >= 600 ? "B" : score >= 400 ? "C" : "D";

            // 红线熔断机制 (K.O. Rules)
            var killerQuestions = new[] { "Q11", "Q12", "Q22", "Q37", "Q38", "Q40" };
            var failedKillers = questions
                .Where(q => killerQuestions.Contains(q.code) && !q.passed)
                .Select(q => q.question)
                .ToList();

            if (failedKillers.Any())
            {
                decision = "STOP";
                grade = "D"; // 强制D级
                // 将红线原因附加到决策理由中
                foreach (var killer in failedKillers)
                {
                    // 简化文本，去掉问号
                    var reasonText = killer.TrimEnd('?').TrimEnd('？');
                    categoryStats.Add(new SubResult 
                    { 
                        Name = "红线触发", 
                        Description = $"触犯红线: {reasonText}",
                        Score = 0,
                        WeightedScore = 0 
                    });
                }
            }

            return new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = score,
                Grade = grade,
                Decision = decision,
                Reason = $"自诊得分: {score}/1000分 (通过{totalPassCount}/40题，其中自动判断{autoPassCount}题)",
                SubResults = categoryStats,
                Warnings = score < 600 ? new List<string> { "得分偏低，建议重新评估产品可行性" } : new List<string>(),
                Suggestions = manualQuestions.Count > 0 
                    ? new List<object> { $"还有{manualQuestions.Count}道题需要手动填写以获得更准确结果" } 
                    : new List<object>(),
                DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(new 
                { 
                    Questions = questions.Select(q => new { q.code, q.question, q.passed, q.category, q.source }),
                    Score = score,
                    PassRate = passRate,
                    AutoFilledCount = autoQuestions.Count,
                    AutoPassCount = autoPassCount,
                    ManualPendingCount = manualQuestions.Count
                })
            };
        }
    }

    /// <summary>
    /// S05 - 11维度评估
    /// </summary>
    public class ElevenDimensionStrategy : BaseStrategy
    {
        public override string Code => "S05";
        public override string Name => "11维度评估模型";
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

            // === 建议生成逻辑 ===
            var suggestions = new List<object>();
            
            // 1. 低分警示
            if (dimensions["市场需求"] < 60) suggestions.Add(new { Dimension = "市场需求", Issue = "评分过低", Recommendation = "寻找更大流量池或验证长尾价值" });
            if (dimensions["竞争强度"] < 60) suggestions.Add(new { Dimension = "竞争强度", Issue = "竞争激烈", Recommendation = "避开正面交锋，寻找差异化切入点" });
            if (dimensions["利润空间"] < 60) suggestions.Add(new { Dimension = "利润空间", Issue = "利润微薄", Recommendation = "优化供应链成本或提高溢价" });
            if (dimensions["产品差异化"] < 60) suggestions.Add(new { Dimension = "产品差异化", Issue = "同质化严重", Recommendation = "挖掘独特卖点，避免价格战" });
            if (dimensions["供应链"] < 60) suggestions.Add(new { Dimension = "供应链", Issue = "供应不稳", Recommendation = "储备备选工厂，防止断货" });
            
            // 2. 优势放大 (>=90)
            foreach (var dim in dimensions.Where(d => d.Value >= 90))
            {
                suggestions.Add(new { Dimension = dim.Key, Issue = "核心优势", Recommendation = $"{dim.Key}表现卓越，建议作为核心卖点重点宣传" });
            }

            // 3. 潜力挖掘 (60-89)
            if (suggestions.Count < 3)
            {
                foreach (var dim in dimensions.Where(d => d.Value >= 60 && d.Value < 90).OrderByDescending(d => weights[d.Key]).Take(2))
                {
                    suggestions.Add(new { Dimension = dim.Key, Issue = "提升空间", Recommendation = $"{dim.Key}表现尚可，仍有优化潜力" });
                }
            }
            
            // 4. 兜底
            if (!suggestions.Any())
            {
                suggestions.Add(new { Dimension = "综合评价", Issue = "全面优秀", Recommendation = "各项指标均衡且优秀，建议加速推进" });
            }

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = totalScore,
                Grade = GetGrade(totalScore),
                Decision = totalScore >= 70 ? "GO" : totalScore >= 50 ? "WAIT" : "STOP",
                Reason = $"11维度综合评分: {totalScore:F1}分",
                Suggestions = suggestions
            };

            result.SubResults = dimensions.Select(d => new SubResult
            {
                Name = d.Key,
                Score = d.Value,
                Weight = weights[d.Key],
                WeightedScore = d.Value * weights[d.Key]
            }).ToList();

            return result;
        }

        /// <summary>
        /// 市场需求评估 - 综合考虑搜索量和SPR供需比
        /// SPR = 月搜索量 / 竞品数 * 1000
        /// 行业标准: SPR>300 蓝海, SPR<100 红海
        /// </summary>
        private decimal CalculateMarketDemand(ProductData product)
        {
            var score = 0m;
            
            // 1. 搜索量评分 (50%)
            var searchScore = product.MonthlySearchVolume >= 10000 ? 45m :
                              product.MonthlySearchVolume >= 5000 ? 35m :
                              product.MonthlySearchVolume >= 3000 ? 25m : 15m;
            score += searchScore;
            
            // 2. SPR供需比评分 (50%) - 行业核心指标
            // SPR = 月搜索量 / 竞品数 * 1000
            var spr = product.CompetitorCount > 0 
                ? (decimal)(product.MonthlySearchVolume ?? 0) / product.CompetitorCount * 1000 
                : 0;
            
            var sprScore = spr >= 300 ? 45m :   // 极易推广 - 蓝海市场
                           spr >= 200 ? 35m :   // 容易推广
                           spr >= 100 ? 25m :   // 一般难度
                           spr >= 50 ? 15m :    // 较难推广
                           10m;                  // 非常难 - 红海市场
            score += sprScore;
            
            return score;
        }

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
            // 侵权风险 - 兼容中英文格式
            var infRisk = (product.InfringementRisk ?? "").ToLower();
            if (infRisk == "高" || infRisk == "high") riskScore -= 40;
            else if (infRisk == "中" || infRisk == "medium") riskScore -= 20;
            if (product.PolicyRisk > 0.5m) riskScore -= 20;
            return Math.Max(riskScore, 0);
        }

        // 注意：SearchGrowthRate 存储的是百分比值（8.3 代表 8.3%）
        private decimal CalculateTrend(ProductData product) =>
            product.SearchGrowthRate >= 30m ? 90 :
            product.SearchGrowthRate >= 10m ? 75 :
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
                Weight = 0.2m,
                WeightedScore = d.Value * 0.2m
            }).ToList();

            return result;
        }

        /// <summary>
        /// 需求维度评估 - 综合考虑搜索量、增长率、SDR供需比
        /// SDR (Supply-Demand Ratio) = 月搜索量 / 竞品数
        /// 行业标准: SDR>100 供大于求(优), SDR<20 供不应求(差)
        /// </summary>
        private decimal CalculateDemand(ProductData product)
        {
            var score = 0m;
            
            // 1. 搜索量评分 (30分)
            if (product.MonthlySearchVolume >= 10000) score += 25;
            else if (product.MonthlySearchVolume >= 5000) score += 20;
            else if (product.MonthlySearchVolume >= 3000) score += 15;
            else score += 10;
            
            // 2. SDR供需比评分 (35分) - 核心指标
            // SDR = 月搜索量 / 竞品数 (不乘1000，直接用原始比例)
            var sdr = product.CompetitorCount > 0 
                ? (decimal)(product.MonthlySearchVolume ?? 0) / product.CompetitorCount 
                : 0;
            
            if (sdr > 100) score += 35;       // 搜索量远大于竞品，蓝海
            else if (sdr > 50) score += 28;   // 供需比良好
            else if (sdr > 20) score += 20;   // 供需平衡
            else if (sdr > 10) score += 12;   // 竞争较激烈
            else score += 5;                   // 供过于求，红海

            // 3. 增长率评分 (20分) - SearchGrowthRate是百分比值
            if (product.SearchGrowthRate >= 20m) score += 20;
            else if (product.SearchGrowthRate >= 10m) score += 15;
            else if (product.SearchGrowthRate >= 0) score += 10;
            else score += 5;  // 负增长

            // 4. 季节性评分 (15分)
            if (product.Seasonality < 0.3m) score += 15;      // 全年稳定
            else if (product.Seasonality < 0.5m) score += 10; // 轻度季节性
            else score += 5;                                   // 强季节性风险

            return Math.Min(score, 100);
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
            // 注意：SearchGrowthRate 存储的是百分比值
            if (product.SearchGrowthRate >= 30m) score += 30;
            else if (product.SearchGrowthRate >= 10m) score += 20;
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

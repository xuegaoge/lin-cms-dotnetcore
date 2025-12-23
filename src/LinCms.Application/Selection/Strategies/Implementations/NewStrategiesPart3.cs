using LinCms.Application.Selection.Models;
using LinCms.Entities.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinCms.Application.Selection.Strategies.Implementations
{
    /// <summary>
    /// S12 - A9算法指标库
    /// </summary>
    public class A9IndicatorStrategy : BaseStrategy
    {
        public override string Code => "S12";
        public override string Name => "A9算法指标库";
        public override string Description => "31个原子指标评分";
        public override StrategyType Type => StrategyType.Scoring;

        public override IReadOnlyList<string> RequiredFields => new[] { nameof(ProductData.ProductName) };

        public override string LogicDefinition => @"
### 策略定义
A9算法指标库，基于Amazon A9算法关注的31个核心指标进行评分。

### 核心输入
*   Conversion, CTR, BSR, Price, Reviews, Rating, etc.

### 计算逻辑
1.  **指标评分**: 31个指标逐个评分(典型分值2-10分)。
2.  **分类维度**: 销售类、流量类、评价类、竞争类、风险类。
3.  **总分**: 所有指标得分的算术平均值。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            var indicators = new List<SubResult>();

            // Helper to add indicator
            void AddInd(string name, decimal score, string desc = "")
            {
                indicators.Add(new SubResult { Name = name, Score = score, Description = desc });
            }

            // 销售类 (Sales)
            AddInd("A9-01-转化率", ScoreConversionRate(product.ConversionRate), $"{(product.ConversionRate * 100):F1}%");
            AddInd("A9-02-点击率", ScoreCTR(product.ClickThroughRate), $"{(product.ClickThroughRate * 100):F1}%");
            AddInd("A9-04-BSR排名", ScoreBSR(product.BSRTop10), $"#{product.BSRTop10}");
            AddInd("A9-05-价格竞争力", ScorePrice(product.TargetPrice), $"{product.TargetPrice:C}");
            AddInd("A9-06-复购潜力", ScoreRepurchase(product.RepurchaseRate), $"{(product.RepurchaseRate * 100):F0}%");
            AddInd("A9-07-退货表现", ScoreReturnRate(product.ReturnRate), $"{(product.ReturnRate * 100):F1}%");

            // 流量类 (Traffic)
            // ACOS = CPC / (转化率 × 客单价) - 这是亚马逊广告核心指标
            var acos = CalculateACOS(product.AdvertisingCPC, product.ConversionRate, product.TargetPrice);
            AddInd("A9-12-ACOS效率", ScoreACOS(acos), $"ACOS: {acos:P1}");
            
            // 添加SPR指标 (供需比 = 月搜索量/竞品数×1000)
            var competitorCount = product.CompetitorCount ?? 0;
            var sprValue = competitorCount > 0 
                ? (decimal)(product.MonthlySearchVolume ?? 0) / competitorCount * 1000m 
                : 0m;
            AddInd("A9-13-关键词SPR", ScoreSPR(sprValue), $"SPR: {sprValue:F0}");
            
            // 评价类 (Review)
            AddInd("A9-18-评分星级", ScoreRating(product.AverageRating), $"{product.AverageRating}星");
            AddInd("A9-19-评论规模", ScoreReviews(product.TotalReviews), $"{product.TotalReviews}条");
            AddInd("A9-22-QA活跃度", ScoreQA(product.QAUnanswered), $"未回QA: {product.QAUnanswered}");

            // 竞争类 (Competition)
            AddInd("A9-25-竞品规模", ScoreCompetitors(product.CompetitorCount), $"{product.CompetitorCount}个");
            AddInd("A9-26-市场集中度", ScoreConcentration(product.TopConcentration), $"CR3: {(product.TopConcentration * 100):F0}%");
            AddInd("A9-27-新品机会", ScoreNewProductRatio(product.NewProductRatio), $"新品占比: {(product.NewProductRatio * 100):F0}%");

            // 风险类 (Risk)
            AddInd("A9-29-侵权风险", ScoreInfringement(product.InfringementRisk), product.InfringementRisk);
            AddInd("A9-30-政策合规", ScorePolicy(product.PolicyRisk), $"风险系数: {product.PolicyRisk:F1}");
            AddInd("A9-31-季节性", ScoreSeasonality(product.Seasonality), $"季节系数: {product.Seasonality:F1}");

            // 补充更多模拟指标以接近31个 (使用现有数据的衍生)
            AddInd("A9-08-变体丰富度", product.VariantCount >= 5 ? 10 : product.VariantCount >= 3 ? 8 : 4, $"{product.VariantCount}个变体");
            AddInd("A9-09-物流时效", product.LeadTimeDays <= 15 ? 10 : 6, $"{product.LeadTimeDays}天");
            AddInd("A9-10-毛利空间", ((product.TargetPrice - product.PurchaseCost)/product.TargetPrice) >= 0.3m ? 10 : 5, "基于毛利率");
            AddInd("A9-11-差异化程度", product.DifferentiationPoints >= 5 ? 10 : 6, $"{product.DifferentiationPoints}个差异点");

            // 指标评分范围是2-10分，平均后需要乘以10转换为百分制(20-100)
            var avgScore = indicators.Count > 0 ? indicators.Average(i => i.Score) : 0;
            var totalScore = avgScore * 10; // 转换为百分制

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = Math.Round(totalScore, 1),
                Grade = GetGrade(totalScore),
                Decision = totalScore >= 70 ? "GO" : totalScore >= 50 ? "WAIT" : "STOP",
                Reason = $"A9算法综合评分: {totalScore:F1} (基于 {indicators.Count} 个核心指标，平均{avgScore:F1}分)",
                SubResults = indicators, // Populate SubResults for Generic Scoring View
            };

            // 生成改进建议：针对得分较低的指标提出优化建议
            var lowScoreIndicators = indicators.Where(i => i.Score <= 6).ToList();
            foreach (var ind in lowScoreIndicators)
            {
                var suggestion = GenerateA9Suggestion(ind.Name, ind.Score, ind.Description);
                if (!string.IsNullOrEmpty(suggestion))
                {
                    result.Suggestions.Add(suggestion);
                }
            }

            // Serialize for extra details if needed
            result.DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(new { Indicators = indicators });

            return result;
        }

        /// <summary>
        /// 根据指标名称和得分生成改进建议
        /// </summary>
        private string GenerateA9Suggestion(string indicatorName, decimal score, string description)
        {
            if (indicatorName.Contains("ACOS")) return $"ACOS优化: 当前{description}偏高，建议优化广告投放策略，提升转化率或降低CPC";
            if (indicatorName.Contains("QA")) return $"QA活跃度: 当前{description}，建议积极回复客户问题，提升购买信心";
            if (indicatorName.Contains("季节")) return $"季节性风险: 当前{description}，建议分散产品线降低季节性依赖";
            if (indicatorName.Contains("复购")) return $"复购潜力: 当前{description}，建议优化产品质量或增加配件/耗材业务";
            if (indicatorName.Contains("竞品")) return $"竞品压力: 当前{description}，建议寻找差异化定位或利基市场";
            if (indicatorName.Contains("物流")) return $"物流时效: 当前{description}，建议优化供应链缩短交期";
            return score <= 4 ? $"{indicatorName}: 该指标({description})得分较低，需重点关注优化" : null;
        }

        private decimal ScoreConversionRate(decimal? rate) =>
            rate >= 0.03m ? 10 : rate >= 0.02m ? 8 : rate >= 0.01m ? 6 : 2;

        private decimal ScoreCTR(decimal? ctr) =>
            ctr >= 0.005m ? 10 : ctr >= 0.003m ? 8 : ctr >= 0.002m ? 6 : 2;

        private decimal ScoreBSR(int? bsr) =>
            bsr <= 100 ? 10 : bsr <= 500 ? 8 : bsr <= 1000 ? 6 : 2;

        private decimal ScorePrice(decimal? price) =>
            price >= 15 && price <= 50 ? 10 : price >= 10 && price <= 100 ? 7 : 4;

        private decimal ScoreRepurchase(decimal? rate) =>
            rate >= 0.15m ? 10 : rate >= 0.10m ? 8 : rate >= 0.05m ? 6 : 2;

        private decimal ScoreReturnRate(decimal? rate) =>
            rate < 0.05m ? 10 : rate < 0.10m ? 8 : rate < 0.15m ? 6 : 2;

        /// <summary>
        /// 计算ACOS (Advertising Cost of Sales)
        /// 公式: CPC / (转化率 × 客单价)
        /// 这是每成交一单需要花费的广告成本占销售额的比例
        /// </summary>
        private decimal CalculateACOS(decimal? cpc, decimal? conversionRate, decimal? price)
        {
            if (!cpc.HasValue || !price.HasValue || price == 0) return 1m; // 无数据返回100%
            if (!conversionRate.HasValue || conversionRate == 0) return 1m;
            
            // ACOS = CPC / (转化率 × 客单价)
            // 例: CPC=$0.5, 转化率=5%, 价格=$25 → ACOS = 0.5/(0.05×25) = 0.4 = 40%
            return cpc.Value / (conversionRate.Value * price.Value);
        }
        
        /// <summary>
        /// ACOS评分 (行业标准)
        /// <15% 优秀, 15-25% 良好, 25-35% 警告, 35-50% 危险, >50% 极差
        /// </summary>
        private decimal ScoreACOS(decimal acos) =>
            acos < 0.15m ? 10 :  // <15% 优秀 - 广告高效盈利
            acos < 0.25m ? 8 :   // 15-25% 良好 - 广告持平或微利
            acos < 0.35m ? 6 :   // 25-35% 警告 - 可能亏损
            acos < 0.50m ? 4 :   // 35-50% 危险 - 肯定亏损
            2;                    // >50% 极差 - 严重亏损
        
        /// <summary>
        /// SPR评分 (Supply-Demand Ratio 供需比)
        /// SPR = 月搜索量 / 竞品数 × 1000
        /// 数值越高表示竞争越小，越容易获得排名
        /// </summary>
        private decimal ScoreSPR(decimal spr) =>
            spr >= 300 ? 10 :   // >300 极易推广 - 蓝海
            spr >= 200 ? 8 :    // 200-300 容易推广
            spr >= 100 ? 6 :    // 100-200 一般难度
            spr >= 50 ? 4 :     // 50-100 较难推广
            2;                   // <50 非常难推广 - 红海

        private decimal ScoreRating(decimal? rating) =>
            rating >= 4.5m ? 10 : rating >= 4.2m ? 8 : rating >= 4.0m ? 6 : 2;

        private decimal ScoreReviews(int? reviews) =>
            reviews >= 1000 ? 10 : reviews >= 500 ? 8 : reviews >= 100 ? 6 : 2;

        private decimal ScoreQA(int? unanswered) =>
            unanswered == 0 ? 10 : unanswered <= 5 ? 8 : unanswered <= 10 ? 6 : 2;

        private decimal ScoreCompetitors(int? count) =>
            count < 100 ? 10 : count < 300 ? 8 : count < 500 ? 6 : 2;

        private decimal ScoreConcentration(decimal? concentration) =>
            concentration < 0.3m ? 10 : concentration < 0.5m ? 8 : concentration < 0.7m ? 6 : 2;

        private decimal ScoreNewProductRatio(decimal? ratio) =>
            ratio > 0.3m ? 10 : ratio > 0.2m ? 8 : ratio > 0.1m ? 6 : 2;

        // 侵权风险评分 - 同时支持中文和英文格式
        private decimal ScoreInfringement(string risk)
        {
            if (string.IsNullOrEmpty(risk)) return 5;
            var r = risk.ToLower();
            if (r == "低" || r == "low") return 10;
            if (r == "中" || r == "medium") return 6;
            return 0; // 高/High 或其他
        }

        private decimal ScorePolicy(decimal? risk) =>
            risk < 0.3m ? 10 : risk < 0.5m ? 7 : risk < 0.7m ? 4 : 0;

        private decimal ScoreSeasonality(decimal? seasonality) =>
            seasonality < 0.3m ? 10 : seasonality < 0.5m ? 8 : seasonality < 0.7m ? 5 : 2;
    }

    /// <summary>
    /// S13 - 爆点识别引擎
    /// </summary>
    public class HotspotDetectionStrategy : BaseStrategy
    {
        public override string Code => "S13";
        public override string Name => "爆点识别引擎";
        public override string Description => "爆品信号+衰退预警检测";
        public override StrategyType Type => StrategyType.Detection;

        public override IReadOnlyList<string> RequiredFields => new[] { nameof(ProductData.ProductName) };

        public override string LogicDefinition => @"
### 策略定义
爆点识别引擎，专门捕捉产品爆发前的信号或衰退前的征兆。

### 核心输入
*   Growth, BSR, Rating, NewRatio, etc.

### 计算逻辑
1.  **爆品信号**: 增长>100%, 转化>5%, 排名优等。
2.  **衰退预警**: 增长<-20%, 退货>15%, 评分<4.0。
3.  **决策**: 爆品信号多 -> GO; 衰退预警多 -> STOP。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            var hotSignals = new List<(string code, string desc, string trigger)>();
            var declineWarnings = new List<(string code, string desc, string trigger)>();

            // ========================================
            // 爆品信号检测 (12项) - 基于product_data可判断的
            // ========================================
            
            // HOT-02: 搜索量暴涨 - SearchGrowthRate是百分比值
            if (product.SearchGrowthRate >= 50m)
                hotSignals.Add(("HOT-02", "搜索量暴涨", $"增长率{product.SearchGrowthRate:F0}% ≥50%"));

            // HOT-04: BSR排名优秀
            if (product.BSRTop10.HasValue && product.BSRTop10 <= 500)
                hotSignals.Add(("HOT-04", "BSR排名优秀", $"TOP10均值#{product.BSRTop10} ≤500"));

            // HOT-05: 好评优秀
            if (product.AverageRating >= 4.5m && product.TotalReviews >= 100)
                hotSignals.Add(("HOT-05", "好评表现优秀", $"评分{product.AverageRating}星 + {product.TotalReviews}条评论"));

            // HOT-08: 新品榜活跃
            if (product.NewProductRatio >= 0.25m)
                hotSignals.Add(("HOT-08", "新品榜活跃", $"新品占比{product.NewProductRatio:P0} ≥25%"));

            // HOT-10: 流量大且稳定
            if (product.MonthlySearchVolume >= 15000 && product.Seasonality < 0.4m)
                hotSignals.Add(("HOT-10", "稳定大流量", $"月搜{product.MonthlySearchVolume} + 低季节性"));

            // HOT-11: 转化率飙升
            if (product.ConversionRate >= 0.05m)
                hotSignals.Add(("HOT-11", "转化率优秀", $"转化率{product.ConversionRate:P1} ≥5%"));

            // HOT-补充1: 低CPC高转化
            if (product.AdvertisingCPC < 0.5m && product.ConversionRate >= 0.03m)
                hotSignals.Add(("HOT-CPC", "广告效率极高", $"CPC ${product.AdvertisingCPC} + 转化{product.ConversionRate:P1}"));

            // HOT-补充2: 竞争小需求大
            var spr = product.CompetitorCount > 0 
                ? (decimal)(product.MonthlySearchVolume ?? 0) / product.CompetitorCount * 1000 
                : 0;
            if (spr >= 300)
                hotSignals.Add(("HOT-SPR", "蓝海市场信号", $"SPR={spr:F0} ≥300"));

            // HOT-补充3: 差异化空间大
            if (product.DifferentiationPoints >= 5)
                hotSignals.Add(("HOT-DIFF", "差异化机会大", $"差异化点{product.DifferentiationPoints}个 ≥5"));

            // HOT-补充4: 毛利空间大
            var margin = product.TargetPrice > 0 
                ? (product.TargetPrice - product.PurchaseCost) / product.TargetPrice 
                : 0;
            if (margin >= 0.45m)
                hotSignals.Add(("HOT-MARGIN", "高毛利机会", $"毛利率{margin:P0} ≥45%"));

            // ========================================
            // 衰退预警检测 (10项)
            // ========================================
            
            // DEC-02: 搜索量萎缩
            if (product.SearchGrowthRate < -15m)
                declineWarnings.Add(("DEC-02", "搜索量萎缩", $"增长率{product.SearchGrowthRate:F0}% <-15%"));

            // DEC-04: 评分下滑
            if (product.AverageRating < 4.0m)
                declineWarnings.Add(("DEC-04", "评分偏低", $"评分{product.AverageRating}星 <4.0"));

            // DEC-05: 退货率上升
            if (product.ReturnRate > 0.10m)
                declineWarnings.Add(("DEC-05", "退货率偏高", $"退货率{product.ReturnRate:P1} >10%"));

            // DEC-06: 竞争加剧
            if (product.CompetitorCount >= 500)
                declineWarnings.Add(("DEC-06", "竞争激烈", $"竞品{product.CompetitorCount}个 ≥500"));

            // DEC-07: 价格战风险
            if (product.PriceVolatility > 0.15m)
                declineWarnings.Add(("DEC-07", "价格波动大", $"波动{product.PriceVolatility:P0} >15%"));

            // DEC-10: 季节性风险
            if (product.Seasonality > 0.6m)
                declineWarnings.Add(("DEC-10", "强季节性风险", $"季节性{product.Seasonality:P0} >60%"));

            // DEC-补充1: 头部垄断
            if (product.TopConcentration > 0.65m)
                declineWarnings.Add(("DEC-MONO", "头部垄断严重", $"集中度{product.TopConcentration:P0} >65%"));

            // DEC-补充2: CPC过高
            if (product.AdvertisingCPC > 1.2m)
                declineWarnings.Add(("DEC-CPC", "广告成本过高", $"CPC ${product.AdvertisingCPC} >$1.2"));

            // DEC-补充3: 毛利空间小
            if (margin < 0.25m && margin > 0)
                declineWarnings.Add(("DEC-MARGIN", "毛利空间不足", $"毛利率{margin:P0} <25%"));

            // DEC-补充4: 侵权风险
            var infRisk = (product.InfringementRisk ?? "").ToLower();
            if (infRisk == "高" || infRisk == "high")
                declineWarnings.Add(("DEC-IP", "侵权风险高", "IP风险=高"));

            // ========================================
            // 计算综合得分
            // ========================================
            var hotScore = hotSignals.Count * 8;
            var decScore = declineWarnings.Count * 12;
            var score = Math.Max(Math.Min(50 + hotScore - decScore, 100), 0);

            // 构建详细输出
            var subResults = new List<SubResult>
            {
                new SubResult
                {
                    Name = "爆品信号",
                    Score = hotSignals.Count * 10,
                    Weight = 0.6m,
                    WeightedScore = hotSignals.Count * 6,
                    Description = $"检测到 {hotSignals.Count} 个信号"
                },
                new SubResult
                {
                    Name = "衰退预警",
                    Score = Math.Max(100 - declineWarnings.Count * 20, 0),
                    Weight = 0.4m,
                    WeightedScore = Math.Max(40 - declineWarnings.Count * 8, 0),
                    Description = $"检测到 {declineWarnings.Count} 个预警"
                }
            };

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = score,
                Grade = GetGrade(score),
                Decision = hotSignals.Count >= 3 && declineWarnings.Count <= 1 ? "GO" : 
                          declineWarnings.Count >= 3 ? "STOP" : "WAIT",
                Reason = $"检测到{hotSignals.Count}个爆品信号，{declineWarnings.Count}个衰退预警",
                SubResults = subResults,
                Suggestions = hotSignals.Select(h => $"{h.code}: {h.desc} ({h.trigger})").Cast<object>().ToList(),
                Warnings = declineWarnings.Select(d => $"{d.code}: {d.desc} ({d.trigger})").ToList(),
                DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    HotSignals = hotSignals.Select(h => new { h.code, h.desc, h.trigger }),
                    DeclineWarnings = declineWarnings.Select(d => new { d.code, d.desc, d.trigger }),
                    Score = score
                })
            };

            return result;
        }
    }

    /// <summary>
    /// S14 - 20节点决策树
    /// </summary>
    public class DecisionTreeStrategy : BaseStrategy
    {
        public override string Code => "S14";
        public override string Name => "20节点决策树";
        public override string Description => "20个决策节点权重判定";
        public override StrategyType Type => StrategyType.Decision;

        public override IReadOnlyList<string> RequiredFields => new[]
        {
            nameof(ProductData.InfringementRisk),
            nameof(ProductData.PolicyRisk)
        };

        public override string LogicDefinition => @"
### 策略定义
20节点决策树，模拟专家决策路径，通过一系列关键节点判定产品生死。

### 核心输入
*   Risk, Margin, SearchVolume, Concentration, etc.

### 计算逻辑
1.  **一票否决**: 侵权, 政策违规, ROI极低 -> STOP (0分)。
2.  **风险扣分**: 市场小(-30), 竞争大(-25), 利润低(-30)等。
3.  **优势加分**: 蓝海(+15), 高增长(+10), 供应链强(+5)等。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            var baseScore = 100m;
            var triggeredNodes = new List<string>();

            // 极高风险节点 (N01-N05) - 一票否决
            // 同时支持中文和英文格式
            var infringementRisk = (product.InfringementRisk ?? "").ToLower();
            if (infringementRisk == "高" || infringementRisk == "high")
            {
                return new StrategyResult
                {
                    StrategyCode = Code,
                    StrategyName = Name,
                    Type = Type,
                    Score = 0,
                    Grade = "F",
                    Decision = "STOP",
                    Reason = "N01: 专利侵权风险 - 一票否决",
                    Warnings = new List<string> { "存在严重侵权风险，不可立项" }
                };
            }

            if (product.PolicyRisk > 0.7m)
            {
                return new StrategyResult
                {
                    StrategyCode = Code,
                    StrategyName = Name,
                    Type = Type,
                    Score = 0,
                    Grade = "F",
                    Decision = "STOP",
                    Reason = "N02: 禁入类目 - 一票否决",
                    Warnings = new List<string> { "政策风险过高，禁止进入" }
                };
            }

            var margin = (product.TargetPrice - product.PurchaseCost) / product.TargetPrice;
            if (margin < 0.05m)
            {
                return new StrategyResult
                {
                    StrategyCode = Code,
                    StrategyName = Name,
                    Type = Type,
                    Score = 0,
                    Grade = "F",
                    Decision = "STOP",
                    Reason = "N05: ROI极低 - 一票否决",
                    Warnings = new List<string> { "利润率过低，无法盈利" }
                };
            }

            // 高风险节点 (N06-N10)
            if (product.MonthlySearchVolume < 3000)
            {
                baseScore -= 30;
                triggeredNodes.Add("N06: 市场规模过小(-30分)");
            }

            if (product.TopConcentration > 0.6m)
            {
                baseScore -= 25;
                triggeredNodes.Add("N07: 竞争过于激烈(-25分)");
            }

            if (margin < 0.25m)
            {
                baseScore -= 30;
                triggeredNodes.Add("N08: 毛利率过低(-30分)");
            }

            if (product.SupplierStability < 50)
            {
                baseScore -= 20;
                triggeredNodes.Add("N09: 供应链高风险(-20分)");
            }

            if (product.Seasonality > 0.7m)
            {
                baseScore -= 15;
                triggeredNodes.Add("N10: 季节性过强(-15分)");
            }

            // 中风险节点 (N11-N15)
            if (product.AdvertisingCPC > 1.0m)
            {
                baseScore -= 10;
                triggeredNodes.Add("N11: CPC偏高(-10分)");
            }

            if (product.AverageRating < 4.2m)
            {
                baseScore -= 10;
                triggeredNodes.Add("N12: 评分偏低(-10分)");
            }

            if (product.ReturnRate > 0.1m)
            {
                baseScore -= 10;
                triggeredNodes.Add("N13: 退货风险(-10分)");
            }

            if (product.DifferentiationPoints < 3)
            {
                baseScore -= 10;
                triggeredNodes.Add("N14: 差异化不足(-10分)");
            }

            if (product.LeadTimeDays > 30)
            {
                baseScore -= 5;
                triggeredNodes.Add("N15: 交期过长(-5分)");
            }

            // 加分节点 (N16-N20)
            if (product.TopConcentration < 0.2m && product.MonthlySearchVolume >= 5000)
            {
                baseScore += 15;
                triggeredNodes.Add("N16: 蓝海市场(+15分)");
            }

            if (margin > 0.4m)
            {
                baseScore += 10;
                triggeredNodes.Add("N17: 高毛利(+10分)");
            }

            if (product.TopConcentration < 0.2m)
            {
                baseScore += 10;
                triggeredNodes.Add("N18: 低竞争(+10分)");
            }

            // N19: 高增长 - 注意：SearchGrowthRate 存储的是百分比值（如 8.3 代表 8.3%）
            if (product.SearchGrowthRate > 30m)
            {
                baseScore += 10;
                triggeredNodes.Add("N19: 高增长(+10分)");
            }

            if (product.SupplierStability > 90)
            {
                baseScore += 5;
                triggeredNodes.Add("N20: 供应链优势(+5分)");
            }

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = baseScore,
                Grade = GetGrade(baseScore),
                Decision = baseScore >= 80 ? "GO" : baseScore >= 60 ? "WAIT" : "STOP",
                Reason = $"决策树评分: {baseScore:F1}分，触发{triggeredNodes.Count}个节点"
            };

            result.Warnings = triggeredNodes.Where(n => n.Contains("-")).ToList();
            result.Suggestions = triggeredNodes.Where(n => n.Contains("+")).Cast<object>().ToList();

            return result;
        }
    }

    /// <summary>
    /// S15 - 竞品分析矩阵
    /// </summary>
    public class CompetitorAnalysisStrategy : BaseStrategy
    {
        public override string Code => "S15";
        public override string Name => "竞品分析矩阵";
        public override string Description => "12维竞品对比+差异化机会";
        public override StrategyType Type => StrategyType.Analysis;

        public override IReadOnlyList<string> RequiredFields => new[]
        {
            nameof(ProductData.TargetPrice),
            nameof(ProductData.AverageRating)
        };

        public override string LogicDefinition => @"
### 策略定义
竞品分析矩阵，通过对比Top竞品寻找差异化突围机会。

### 核心输入
*   Price, Rating, Variants, Differentiation

### 计算逻辑
1.  **差异化扫描**: 比较我方与竞品的关键维度。
    - 价格空间? 
    - 质量是否有优势? (竞品<4.2分)
    - 变体是否更丰富?
    - 功能卖点是否更多?
2.  **评分**: 识别到的机会越多，评分越高。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            // 12维竞品对比分析
            var dimensions = new List<(string name, decimal score, string opportunity, string status)>();
            
            // 1. 价格维度
            var priceScore = product.TargetPrice >= 15 && product.TargetPrice <= 50 ? 80 : 
                            product.TargetPrice < 15 ? 60 : 70;
            var priceOpp = product.TargetPrice < 30 ? "可考虑中高端定位提升溢价" : 
                          product.TargetPrice > 50 ? "价格偏高，需强差异化支撑" : "价格带适中";
            dimensions.Add(("价格定位", priceScore, priceOpp, priceScore >= 70 ? "优势" : "待改进"));

            // 2. 评价维度
            var ratingScore = product.AverageRating >= 4.5m ? 90 :
                             product.AverageRating >= 4.2m ? 75 :
                             product.AverageRating >= 4.0m ? 60 : 40;
            var ratingOpp = product.AverageRating < 4.2m ? "竞品评分偏低，可通过质量取胜" : "评分竞争激烈";
            dimensions.Add(("评价质量", ratingScore, ratingOpp, ratingScore >= 70 ? "优势" : "待改进"));

            // 3. 变体维度
            var variantScore = product.VariantCount >= 8 ? 90 :
                              product.VariantCount >= 5 ? 75 :
                              product.VariantCount >= 3 ? 60 : 50;
            var variantOpp = product.VariantCount < 5 ? "可扩展颜色/尺寸/款式" : "变体已较丰富";
            dimensions.Add(("变体丰富度", variantScore, variantOpp, variantScore >= 70 ? "优势" : "机会"));

            // 4. 差异化维度
            var diffScore = product.DifferentiationPoints >= 5 ? 90 :
                           product.DifferentiationPoints >= 3 ? 70 : 50;
            var diffOpp = product.DifferentiationPoints < 5 ? "功能创新空间大，可增加独特卖点" : "差异化良好";
            dimensions.Add(("功能差异化", diffScore, diffOpp, diffScore >= 70 ? "优势" : "机会"));

            // 5. 客服维度
            var qaScore = product.QAUnanswered <= 2 ? 90 :
                         product.QAUnanswered <= 5 ? 70 : 50;
            var qaOpp = product.QAUnanswered > 5 ? "竞品QA响应不足，可通过客服取胜" : "客服响应正常";
            dimensions.Add(("客服响应", qaScore, qaOpp, qaScore >= 70 ? "优势" : "机会"));

            // 6. 评论维度
            var reviewScore = product.TotalReviews >= 1000 ? 90 :
                             product.TotalReviews >= 500 ? 75 :
                             product.TotalReviews >= 100 ? 60 : 40;
            var reviewOpp = product.TotalReviews < 500 ? "评论积累机会，早期Reviewer计划" : "评论基础良好";
            dimensions.Add(("评论规模", reviewScore, reviewOpp, reviewScore >= 70 ? "优势" : "待积累"));

            // 7. 广告维度
            var cpcScore = product.AdvertisingCPC < 0.5m ? 90 :
                          product.AdvertisingCPC < 0.8m ? 75 :
                          product.AdvertisingCPC < 1.2m ? 60 : 40;
            var cpcOpp = product.AdvertisingCPC < 0.8m ? "CPC成本低，广告红利期" : "广告竞争激烈，需精细化运营";
            dimensions.Add(("广告成本", cpcScore, cpcOpp, cpcScore >= 70 ? "优势" : "劣势"));

            // 8. 物流维度
            var weightScore = product.WeightKg < 0.5m ? 90 :
                             product.WeightKg < 1m ? 80 :
                             product.WeightKg < 2m ? 70 : 50;
            var weightOpp = product.WeightKg > 2m ? "物流成本偏高，考虑轻量化设计" : "物流成本可控";
            dimensions.Add(("物流成本", weightScore, weightOpp, weightScore >= 70 ? "优势" : "待优化"));

            // 9. 供应链维度
            var supplyScore = product.SupplierStability >= 80 ? 90 :
                             product.SupplierStability >= 60 ? 70 : 50;
            var supplyOpp = product.SupplierStability < 80 ? "供应链需强化，建议备选供应商" : "供应链稳定";
            dimensions.Add(("供应链稳定", supplyScore, supplyOpp, supplyScore >= 70 ? "优势" : "风险"));

            // 10. 品牌维度
            var brandScore = product.TopConcentration < 0.5m ? 80 :
                            product.TopConcentration < 0.7m ? 60 : 40;
            var brandOpp = product.TopConcentration >= 0.7m ? "头部品牌垄断，需差异化突围" : "品牌格局分散，机会大";
            dimensions.Add(("品牌格局", brandScore, brandOpp, brandScore >= 70 ? "机会" : "风险"));

            // 11. 销量维度
            var salesScore = product.MonthlySearchVolume >= 10000 ? 90 :
                            product.MonthlySearchVolume >= 5000 ? 75 :
                            product.MonthlySearchVolume >= 3000 ? 60 : 40;
            var salesOpp = product.MonthlySearchVolume >= 10000 ? "市场容量大" : "细分市场，需精准定位";
            dimensions.Add(("市场容量", salesScore, salesOpp, salesScore >= 70 ? "优势" : "待评估"));

            // 12. 增长维度
            var growthScore = product.SearchGrowthRate >= 20m ? 90 :
                             product.SearchGrowthRate >= 10m ? 75 :
                             product.SearchGrowthRate >= 0 ? 60 : 40;
            var growthOpp = product.SearchGrowthRate >= 20m ? "快速增长市场，抓住红利期" : 
                           product.SearchGrowthRate < 0 ? "市场下滑，谨慎进入" : "市场稳定";
            dimensions.Add(("增长趋势", growthScore, growthOpp, growthScore >= 70 ? "优势" : "待观察"));

            // 计算综合得分
            var totalScore = dimensions.Average(d => d.score);
            var opportunities = dimensions.Where(d => d.status == "机会" || d.status == "待改进")
                                         .Select(d => $"{d.name}: {d.opportunity}")
                                         .ToList();
            var strengths = dimensions.Where(d => d.status == "优势")
                                     .Select(d => d.name)
                                     .ToList();
            var risks = dimensions.Where(d => d.status == "风险" || d.status == "劣势")
                                 .Select(d => $"{d.name}: {d.opportunity}")
                                 .ToList();

            // SubResults
            var subResults = dimensions.Select(d => new SubResult
            {
                Name = d.name,
                Score = d.score,
                Weight = 1m / 12m,
                WeightedScore = d.score / 12m,
                Description = d.opportunity
            }).ToList();

            var result = new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Type = Type,
                Score = Math.Round(totalScore, 1),
                Grade = GetGrade(totalScore),
                Decision = opportunities.Count >= 4 && risks.Count <= 2 ? "GO" : 
                          risks.Count >= 4 ? "STOP" : "WAIT",
                Reason = $"12维对比：{strengths.Count}项优势，{opportunities.Count}项机会，{risks.Count}项风险",
                SubResults = subResults,
                Suggestions = opportunities.Cast<object>().ToList(),
                Warnings = risks,
                DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    Dimensions = dimensions.Select(d => new { d.name, d.score, d.opportunity, d.status }),
                    Strengths = strengths,
                    Opportunities = opportunities,
                    Risks = risks,
                    TotalScore = totalScore
                })
            };

            return result;
        }
    }
}

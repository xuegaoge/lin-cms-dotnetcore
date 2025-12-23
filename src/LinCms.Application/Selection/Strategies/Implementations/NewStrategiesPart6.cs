using LinCms.Application.Selection.Models;
using LinCms.Application.Selection.Strategies;
using LinCms.Entities.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LinCms.Application.Selection.Strategies.Implementations
{
    // ==========================================
    // S19 - 关键词研究策略 (Sheet21)
    // ==========================================
    public class KeywordResearchStrategy : BaseStrategy
    {
        public override string Code => "S19";
        public override string Name => "关键词研究策略";
        public override string Description => "挖掘高机会关键词 (SPR/机会指数)";
        public override StrategyType Type => StrategyType.Analysis;
        public override IReadOnlyList<string> RequiredFields => new[] { nameof(ProductData.ProductName) };

        private readonly IFreeSql _fsql;

        public KeywordResearchStrategy(IFreeSql fsql)
        {
            _fsql = fsql;
        }

        public override string LogicDefinition => @"
### 策略定义
关键词研究策略，通过挖掘关键词供需比和市场机会，寻找高性价比流量入口。

### 核心输入
*   ProductKeyword表数据 (搜索量, 竞品数, CPC, 转化率)

### 计算逻辑
1.  **供需比(SPR)**: Search / Comp * 1000。
2.  **机会指数**: Search / Comp * (100 / Bid)。
3.  **分级评估**: 筛选出""高机会""关键词。
4.  **综合评分**: 高机会词占比(60%) + 市场总盘子大小(40%)。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            List<ProductKeyword> keywords;
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"[S19] 开始执行 ProductId={product.Id}");
                
                keywords = _fsql.Select<ProductKeyword>()
                    .Where(k => k.ProductId == product.Id)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[S19] 查询到关键词数量: {keywords.Count}");

                if (keywords.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[S19] 触发Fallback逻辑");
                    // 如果没有关键词数据，使用产品基础数据进行模拟分析
                    var fallbackResult = GenerateFallbackResult(product);
                    System.Diagnostics.Debug.WriteLine($"[S19] Fallback结果: Score={fallbackResult.Score}, Decision={fallbackResult.Decision}");
                    return fallbackResult;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[S19] 数据库查询异常: {ex.Message}");
                // 发生异常时也使用fallback
                return GenerateFallbackResult(product);
            }

            // 1. 计算指标 (Sheet21逻辑)
            int highPriorityCount = 0;
            int totalVolume = 0;

            foreach (var kw in keywords)
            {
                // SPR 计算
                if (kw.CompetitorCount > 0)
                {
                    kw.SPR = (decimal)kw.SearchVolume / kw.CompetitorCount * 1000;
                }
                
                // 竞争度评级
                if (kw.SPR > 300) kw.CompetitionLevel = "低竞争";
                else if (kw.SPR > 100) kw.CompetitionLevel = "中竞争";
                else kw.CompetitionLevel = "高竞争";

                // 机会指数
                if (kw.CompetitorCount > 0)
                {
                    kw.OpportunityScore = (decimal)kw.SearchVolume / kw.CompetitorCount * (100 / (kw.BidPrice + 0.1m));
                }

                // 优先级
                if (kw.OpportunityScore > 50) 
                {
                    kw.Priority = "高优先";
                    highPriorityCount++;
                }
                else if (kw.OpportunityScore > 20) kw.Priority = "中优先";
                else kw.Priority = "低优先";

                totalVolume += kw.SearchVolume;
            }

            // 更新数据库中的计算结果
            _fsql.Update<ProductKeyword>().SetSource(keywords).ExecuteAffrows();

            // 2. 策略评分
            decimal highPriorityRatio = (decimal)highPriorityCount / keywords.Count;
            
            // 基础分：占比得分 (满分60)
            decimal ratioScore = highPriorityRatio * 60;
            
            // 规模分：搜索量得分 (满分40) - 假设目标是 50000
            decimal volumeScore = Math.Min(totalVolume / 50000m * 40, 40);

            decimal finalScore = ratioScore + volumeScore;

            string decision = DecisionType.WAIT;
            if (finalScore >= 80) decision = DecisionType.GO;
            else if (finalScore < 50) decision = DecisionType.STOP;

            var subResults = new List<SubResult>();
            
            // 维度1: 机会词占比 (60%)
            // ratioScore is raw score out of 60. Normalized to 100: (ratioScore / 60) * 100 = highPriorityRatio * 100
            subResults.Add(new SubResult 
            { 
                Name = "机会词占比", 
                Score = highPriorityRatio * 100, 
                Weight = 0.6m, 
                WeightedScore = ratioScore,
                Description = $"高优先词占比 {highPriorityRatio:P0}"
            });

            // 维度2: 市场容量 (40%)
            // volumeScore is raw score out of 40. Normalized to 100: (volumeScore / 40) * 100
            decimal volumeNormalized = Math.Min(totalVolume / 50000m * 100, 100);
            subResults.Add(new SubResult 
            { 
                Name = "市场搜索容量", 
                Score = volumeNormalized, 
                Weight = 0.4m, 
                WeightedScore = volumeScore,
                Description = $"总搜索量 {totalVolume}"
            });

            return new StrategyResult
            {
                Score = finalScore,
                Decision = decision,
                Reason = $"分析{keywords.Count}个关键词，高优先词{highPriorityCount}个(占比{highPriorityRatio:P0})，总搜索量{totalVolume}。发现{(highPriorityRatio > 0.3m ? "充足" : "不足")}的市场机会。",
                Data = new { HighPriorityCount = highPriorityCount, TotalVolume = totalVolume },
                SubResults = subResults,
                DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(new { SubResults = subResults })
            };
        }

        /// <summary>
        /// 当没有关键词数据时，使用产品基础数据生成模拟分析结果
        /// </summary>
        private StrategyResult GenerateFallbackResult(ProductData product)
        {
            // 使用产品的搜索量和竞争数据进行估算
            var searchVolume = product.MonthlySearchVolume ?? 10000;
            var competitorCount = product.CompetitorCount ?? 200;
            var cpc = product.AdvertisingCPC ?? 0.5m;

            // 模拟计算 SPR 和机会指数
            decimal spr = competitorCount > 0 ? (decimal)searchVolume / competitorCount * 1000 : 0;
            decimal opportunityIndex = competitorCount > 0 ? (decimal)searchVolume / competitorCount * (100 / (cpc + 0.1m)) : 0;

            // 评估机会等级
            string opportunityLevel = opportunityIndex > 50 ? "高机会" : opportunityIndex > 20 ? "中机会" : "低机会";
            decimal highPriorityRatio = opportunityIndex > 50 ? 0.6m : opportunityIndex > 20 ? 0.3m : 0.1m;

            // 计算得分
            decimal ratioScore = highPriorityRatio * 60;
            decimal volumeScore = Math.Min(searchVolume / 50000m * 40, 40);
            decimal finalScore = ratioScore + volumeScore;

            string decision = DecisionType.WAIT;
            if (finalScore >= 75) decision = DecisionType.GO;
            else if (finalScore < 45) decision = DecisionType.STOP;

            var subResults = new List<SubResult>
            {
                new SubResult
                {
                    Name = "机会词占比",
                    Score = highPriorityRatio * 100,
                    Weight = 0.6m,
                    WeightedScore = ratioScore,
                    Description = $"预估高优先词占比 {highPriorityRatio:P0} ({opportunityLevel})"
                },
                new SubResult
                {
                    Name = "市场搜索容量",
                    Score = Math.Min(searchVolume / 50000m * 100, 100),
                    Weight = 0.4m,
                    WeightedScore = volumeScore,
                    Description = $"月搜索量约 {searchVolume:N0}"
                },
                new SubResult
                {
                    Name = "供需比 (SPR)",
                    Score = spr > 300 ? 90 : spr > 100 ? 70 : 40,
                    Weight = 0m,
                    WeightedScore = 0,
                    Description = $"SPR = {spr:F0} (竞争{(spr > 300 ? "低" : spr > 100 ? "中" : "高")})"
                },
                new SubResult
                {
                    Name = "机会指数",
                    Score = Math.Min(opportunityIndex, 100),
                    Weight = 0m,
                    WeightedScore = 0,
                    Description = $"机会指数 = {opportunityIndex:F1}"
                }
            };

            return new StrategyResult
            {
                StrategyCode = Code,
                StrategyName = Name,
                Score = finalScore,
                Decision = decision,
                Reason = $"基于产品基础数据估算关键词机会。预估机会等级: {opportunityLevel}，市场容量{(searchVolume > 20000 ? "较大" : "适中")}。建议补充详细关键词数据以获得更精准分析。",
                Data = new { EstimatedOpportunityLevel = opportunityLevel, SPR = spr, OpportunityIndex = opportunityIndex },
                SubResults = subResults,
                DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(new { SubResults = subResults, Note = "基于产品基础数据估算，非实际关键词分析" })
            };
        }
    }

    // ==========================================
    // S20 - 市场趋势分析策略 (Sheet23)
    // ==========================================
    public class MarketTrendStrategy : BaseStrategy
    {
        public override string Code => "S20";
        public override string Name => "市场趋势分析策略";
        public override string Description => "识别产品全生命周期走势与季节性特征";
        public override StrategyType Type => StrategyType.Analysis;
        public override IReadOnlyList<string> RequiredFields => new[] { nameof(ProductData.ProductName) };

        private readonly IFreeSql _fsql;

        public MarketTrendStrategy(IFreeSql fsql)
        {
            _fsql = fsql;
        }

        public override string LogicDefinition => @"
### 策略定义
市场趋势分析策略，识别产品全生命周期走势与季节性特征，避免踩坑。

### 核心输入
*   ProductTrend表数据 (最近12个月的销量、搜索、价格趋势)

### 计算逻辑
1.  **趋势判定**: 计算同比增长率 ((End-Start)/Start)。>10%为上升。
2.  **季节性**: 计算波动系数 (Range/Mean)。
3.  **综合评分**: 上升指标数量越多分数越高，下降指标倒扣分。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            var trends = _fsql.Select<ProductTrend>()
                .Where(t => t.ProductId == product.Id)
                .ToList();

            if (trends.Count == 0 || !trends.Any(t => t.MetricName == "月销量"))
            {
                // 如果没有详细趋势数据，尝试退回到 ProductData 的简单字段
                if (product.SearchGrowthRate.HasValue)
                {
                    // SearchGrowthRate是百分比值（如 8.3 代表 8.3%）
                    // 调整计算：基础50分 + 增长率贡献（每1%贡献1分，最多25分）
                    decimal growthContribution = Math.Min(product.SearchGrowthRate.Value, 25);
                    decimal score = Math.Max(0, Math.Min(50 + growthContribution, 100));
                    
                    // 评估增长趋势
                    string trendDesc = product.SearchGrowthRate.Value >= 20 ? "强劲上升" :
                                       product.SearchGrowthRate.Value >= 10 ? "稳健增长" :
                                       product.SearchGrowthRate.Value >= 0 ? "平稳" : "下滑";
                    
                    var fallbackSubResults = new List<SubResult> 
                    {
                        new SubResult 
                        { 
                            Name = "搜索增长预测", 
                            Score = Math.Min(50 + product.SearchGrowthRate.Value * 2, 100), 
                            Weight = 0.5m, 
                            WeightedScore = Math.Min(25 + product.SearchGrowthRate.Value, 50), 
                            Description = $"年增长率 {product.SearchGrowthRate.Value:F1}%" 
                        },
                        new SubResult
                        {
                            Name = "趋势判定",
                            Score = score,
                            Weight = 0.5m,
                            WeightedScore = score * 0.5m,
                            Description = $"市场趋势: {trendDesc}"
                        }
                    };

                    return new StrategyResult 
                    { 
                        Score = Math.Round(score, 1), 
                        Decision = score >= 60 ? DecisionType.GO : score >= 45 ? DecisionType.WAIT : DecisionType.STOP, 
                        Reason = $"缺少详细月度趋势数据，基于搜索增长率({product.SearchGrowthRate.Value:F1}%)评估，市场{trendDesc}。",
                        SubResults = fallbackSubResults,
                        DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(new { SubResults = fallbackSubResults, Note = "基于产品基础数据估算" })
                    };
                }

                return new StrategyResult 
                { 
                    Score = 50, // 缺少数据给中性分 
                    Decision = DecisionType.WAIT, 
                    Reason = "缺少趋势数据，暂无法评估，建议补充月度销售数据" 
                };
            }

            // 1. 分析核心指标趋势 (Sheet23逻辑)
            int positiveTrends = 0;
            int negativeTrends = 0;
            decimal maxSeasonality = 0;

            foreach (var t in trends)
            {
                decimal start = t.Month1 == 0 ? 1 : t.Month1; 
                decimal end = t.Month12;
                decimal change = (end - start) / start;

                string trendStatus = "稳定";
                if (change > 0.1m) { trendStatus = "上升"; if (IsPositiveMetric(t.MetricName)) positiveTrends++; else negativeTrends++; }
                else if (change < -0.1m) { trendStatus = "下降"; if (IsPositiveMetric(t.MetricName)) negativeTrends++; else positiveTrends++; }

                t.Trend = trendStatus;
                t.YearMean = (t.Month1 + t.Month2 + t.Month3 + t.Month4 + t.Month5 + t.Month6 + 
                              t.Month7 + t.Month8 + t.Month9 + t.Month10 + t.Month11 + t.Month12) / 12;

                // 季节性
                List<decimal> vals = new List<decimal> { t.Month1, t.Month2, t.Month3, t.Month4, t.Month5, t.Month6, t.Month7, t.Month8, t.Month9, t.Month10, t.Month11, t.Month12 };
                decimal range = vals.Max() - vals.Min();
                if (t.YearMean > 0)
                {
                    t.SeasonalityIndex = range / t.YearMean;
                    if (t.MetricName == "月销量") maxSeasonality = t.SeasonalityIndex;
                }
            }

            // 更新分析结果
            _fsql.Update<ProductTrend>().SetSource(trends).ExecuteAffrows();

            // 2. 综合评分
            decimal trendScore = 50 + (positiveTrends * 10) - (negativeTrends * 10);
            trendScore = Math.Min(Math.Max(trendScore, 0), 100);

            string decision = DecisionType.WAIT;
            if (trendScore >= 75) decision = DecisionType.GO;
            else if (trendScore < 40) decision = DecisionType.STOP;

            string seasonDesc = maxSeasonality > 0.5m ? "强季节性" : (maxSeasonality > 0.2m ? "中季节性" : "弱季节性");

            // 3. 构造子结果
            var subResults = new List<SubResult>();

            // 维度1: 增长趋势
            decimal growthScore = 50 + (positiveTrends * 15); // 每有一个正向指标+15分
            growthScore = Math.Min(growthScore, 100);
            subResults.Add(new SubResult
            {
                Name = "增长态势",
                Score = growthScore,
                Weight = 0.4m,
                WeightedScore = growthScore * 0.4m,
                Description = $"正向增长指标: {positiveTrends}个"
            });

            // 维度2: 稳定性 (基于负向指标)
            decimal stabilityScore = 100 - (negativeTrends * 20); // 每有一个负向指标-20分
            stabilityScore = Math.Max(stabilityScore, 0);
            subResults.Add(new SubResult
            {
                Name = "趋势稳定性",
                Score = stabilityScore,
                Weight = 0.3m,
                WeightedScore = stabilityScore * 0.3m,
                Description = $"负向衰退指标: {negativeTrends}个"
            });

            // 维度3: 季节性健康度 (季节性越弱分数越高)
            // maxSeasonality: 0~1+
            decimal seasonScore = Math.Max(0, 100 - (maxSeasonality * 100));
            subResults.Add(new SubResult
            {
                Name = "非季节性",
                Score = seasonScore,
                Weight = 0.3m,
                WeightedScore = seasonScore * 0.3m,
                Description = $"季节性指数: {maxSeasonality:F2} ({seasonDesc})"
            });

            return new StrategyResult
            {
                Score = trendScore,
                Decision = decision,
                Reason = $"市场整体呈{(trendScore > 60 ? "上升" : "波动")}趋势 ({positiveTrends}项利好, {negativeTrends}项利空)。产品属于{seasonDesc}。",
                Data = new { PositiveTrends = positiveTrends, Seasonality = seasonDesc },
                SubResults = subResults,
                DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(new { SubResults = subResults })
            };
        }

        private bool IsPositiveMetric(string metricName)
        {
            // 这些指标上升是好事
            return metricName.Contains("销量") || metricName.Contains("搜索") || metricName.Contains("均价");
            // 竞品数、退货率、CPC上升与否取决于具体，通常竞品数上升是负面的
        }
    }

    // ==========================================
    // S21 - 综合选品决策策略 (Sheet24)
    // ==========================================
    public class ComprehensiveDecisionStrategy : BaseStrategy
    {
        public override string Code => "S21";
        public override string Name => "综合选品决策";
        public override string Description => "汇总核心策略结果，输出P0-P3立项评级";
        public override StrategyType Type => StrategyType.Decision;
        public override IReadOnlyList<string> RequiredFields => new[] { nameof(ProductData.ProductName) };

        private readonly IFreeSql _fsql;

        public ComprehensiveDecisionStrategy(IFreeSql fsql)
        {
            _fsql = fsql;
        }

        public override string LogicDefinition => @"
### 策略定义
综合选品决策，作为最终裁判，汇总所有核心策略结果，输出P0-P3立项评级。

### 核心输入
*   S02(自诊), S03(财务), S04(风险), S05(综合评分) 的历史执行结果

### 计算逻辑
1.  **加权模型**: 
    - 11维度评估(S05): 35%
    - 40题自诊(S02): 25%
    - 风险评级(S04): 20%
    - 财务模型(S03): 20%
2.  **等级划分**: 
    - P0 (必做): >85分
    - P1 (重点): 75-85分
    - P2 (储备): 65-75分
    - P3 (放弃): <65分";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            // 1. 获取依赖策略的最新执行结果
            var history = _fsql.Select<StrategyExecution>()
                .Where(e => e.ProductId == product.Id && e.IsLatest == true)
                .Where(e => e.StrategyCode == "S02" || e.StrategyCode == "S03" || e.StrategyCode == "S04" || e.StrategyCode == "S05")
                .ToList();
            
            //Console.WriteLine($"[S21] Found dependencies: {history.Count}");

            decimal scoreS05 = history.FirstOrDefault(e => e.StrategyCode == "S05")?.Score ?? 60; // 11维度
            decimal scoreS02 = history.FirstOrDefault(e => e.StrategyCode == "S02")?.Score ?? 60; // 40题

            // 归一化 S02 (自诊系统现在使用1000分制)
            if (scoreS02 > 100) 
            {
                scoreS02 = scoreS02 / 10;
            }
            decimal scoreRisk = history.FirstOrDefault(e => e.StrategyCode == "S04")?.Score ?? 60; // 风险
            
            // S03(利润)结果作为财务分及其它分数的代理
            var execS03 = history.FirstOrDefault(e => e.StrategyCode == "S03");
            decimal scoreROI = 60;
            decimal scoreCycle = 60;

            if (execS03 != null)
            {
                // 默认使用总分作为回退
                scoreROI = execS03.Score ?? 60;
                scoreCycle = execS03.Score ?? 60;

                // 尝试从 DetailJson 中提取更精细的指标
                if (!string.IsNullOrEmpty(execS03.DetailJson))
                {
                    try
                    {
                        var detail = Newtonsoft.Json.Linq.JObject.Parse(execS03.DetailJson);
                        var finance = detail["Finance"];
                        if (finance != null)
                        {
                            var roi = finance["Roi"]?.Value<decimal>() ?? 0;
                            var payback = finance["PaybackPeriod"]?.Value<int>() ?? 999;

                            // 重新评分 (ROI > 30% Excellent, > 15% Good)
                            scoreROI = roi >= 0.30m ? 100 : roi >= 0.15m ? 75 : roi > 0 ? 60 : 40;

                            // 重新评分 (回本 < 6月 Excellent, < 12月 Good)
                            scoreCycle = payback <= 6 ? 100 : payback <= 12 ? 75 : payback <= 18 ? 60 : 40;
                        }
                    }
                    catch
                    {
                        // 解析失败，保持默认
                    }
                }
            }
            
            // 如果历史数据不存在（例如首次运行），尝试使用一些ProductData字段进行修正
            if (history.Count == 0)
            {
                 // 保持原有的简单兜底逻辑 - 兼容中英文格式
                 var infRisk = (product.InfringementRisk ?? "").ToLower();
                 if ((infRisk == "无风险" || infRisk == "low" || infRisk == "低") && product.PolicyRisk < 0.2m) scoreRisk = 100;
                 else if (infRisk == "高风险" || infRisk == "high" || infRisk == "高") scoreRisk = 20;
            }

            // 计算综合得分
            decimal weightScore = 
                scoreS05 * 0.35m + 
                scoreS02 * 0.25m + 
                scoreRisk * 0.20m + 
                scoreROI * 0.15m + 
                scoreCycle * 0.05m;

            // 优先级判定
            string priority = "P4-暂缓";
            string suggestion = "暂不立项，继续优化";
            string allocation = "0%";

            if (weightScore >= 85) { priority = "P0-最高"; suggestion = "立即立项，优先资源"; allocation = "40%"; }
            else if (weightScore >= 75) { priority = "P1-高"; suggestion = "优先立项，重点关注"; allocation = "30%"; }
            else if (weightScore >= 65) { priority = "P2-中"; suggestion = "评审后立项，正常资源"; allocation = "20%"; }
            else if (weightScore >= 55) { priority = "P3-低"; suggestion = "小额测试，观察表现"; allocation = "10%"; }

            // 回写优先级到产品主表
            try 
            {
                product.PriorityLevel = priority;
                // 使用 SetSource + UpdateColumns 方式更新，通常更稳定
                _fsql.Update<ProductData>()
                    .SetSource(product)
                    .UpdateColumns(p => p.PriorityLevel)
                    .ExecuteAffrows();
            }
            catch (Exception ex)
            {
                // 仅打印日志，不要阻断策略结果返回
                Console.WriteLine($"[S21] Update PriorityLevel Failed: {ex.Message}");
            }

            // 构造 SubResults 列表
            var subResults = new List<SubResult>
            {
                new SubResult { Name = "S05-维度评分", Score = scoreS05, Weight = 0.35m, WeightedScore = scoreS05 * 0.35m },
                new SubResult { Name = "S02-40题自诊", Score = scoreS02, Weight = 0.25m, WeightedScore = scoreS02 * 0.25m },
                new SubResult { Name = "S04-风险评级", Score = scoreRisk, Weight = 0.20m, WeightedScore = scoreRisk * 0.20m },
                new SubResult { Name = "S03-投资回报", Score = scoreROI, Weight = 0.15m, WeightedScore = scoreROI * 0.15m },
                new SubResult { Name = "S03-资金周期", Score = scoreCycle, Weight = 0.05m, WeightedScore = scoreCycle * 0.05m }
            };

            // 构造 Suggestions
            var suggestions = new List<string> { suggestion };
            if (priority == "P0-最高") suggestions.Add("建议投入最大资源进行推广");
            else if (priority == "P3-低") suggestions.Add("建议重新评估或放弃");

            var detailData = new 
            {
                SubResults = subResults,
                Suggestions = suggestions,
                Priority = priority,
                Allocation = allocation
            };
            
            return new StrategyResult
            {
                Score = weightScore,
                Decision = weightScore >= 65 ? DecisionType.GO : DecisionType.WAIT,
                Reason = $"综合评分{weightScore:F1} (P等级: {priority})。建议: {suggestion}。资源分配: {allocation}。",
                Data = new { Priority = priority, Allocation = allocation },
                SubResults = subResults,
                Suggestions = suggestions.Cast<object>().ToList(),
                DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(detailData)
            };
        }
    }
}

using LinCms.Application.Selection.Models;
using LinCms.Application.Selection.Strategies;
using LinCms.Entities.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            var keywords = _fsql.Select<ProductKeyword>()
                .Where(k => k.ProductId == product.Id)
                .ToList();

            if (keywords.Count == 0)
            {
                return new StrategyResult 
                { 
                    Score = 0, 
                    Decision = DecisionType.WAIT, 
                    Reason = "缺少关键词数据，无法进行分析" 
                };
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

            return new StrategyResult
            {
                Score = finalScore,
                Decision = decision,
                Reason = $"分析{keywords.Count}个关键词，高优先词{highPriorityCount}个(占比{highPriorityRatio:P0})，总搜索量{totalVolume}。发现{(highPriorityRatio > 0.3m ? "充足" : "不足")}的市场机会。",
                Data = new { HighPriorityCount = highPriorityCount, TotalVolume = totalVolume }
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
                    decimal score = 50 + product.SearchGrowthRate.Value; // 简单映射
                    return new StrategyResult 
                    { 
                        Score = Math.Min(score, 100), 
                        Decision = score > 60 ? DecisionType.GO : DecisionType.WAIT, 
                        Reason = "缺少详细月度趋势数据，仅依据搜索增长率评估。" 
                    };
                }

                return new StrategyResult 
                { 
                    Score = 0, 
                    Decision = DecisionType.WAIT, 
                    Reason = "缺少趋势数据，无法评估" 
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

            return new StrategyResult
            {
                Score = trendScore,
                Decision = decision,
                Reason = $"市场整体呈{(trendScore > 60 ? "上升" : "波动")}趋势 ({positiveTrends}项利好, {negativeTrends}项利空)。产品属于{seasonDesc}。",
                Data = new { PositiveTrends = positiveTrends, Seasonality = seasonDesc }
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
                .Where(e => new[] { "S02", "S03", "S04", "S05" }.Contains(e.StrategyCode))
                .ToList();

            decimal scoreS05 = history.FirstOrDefault(e => e.StrategyCode == "S05")?.Score ?? 60; // 11维度
            decimal scoreS02 = history.FirstOrDefault(e => e.StrategyCode == "S02")?.Score ?? 60; // 40题
            decimal scoreRisk = history.FirstOrDefault(e => e.StrategyCode == "S04")?.Score ?? 60; // 风险
            
            // S03(利润)结果作为财务分及其它分数的代理
            var execS03 = history.FirstOrDefault(e => e.StrategyCode == "S03");
            decimal scoreROI = execS03?.Score ?? 60;
            decimal scoreCycle = execS03?.Score ?? 60;
            
            // 如果历史数据不存在（例如首次运行），尝试使用一些ProductData字段进行修正
            if (history.Count == 0)
            {
                 // 保持原有的简单兜底逻辑
                 if (product.InfringementRisk == "无风险" && product.PolicyRisk < 0.2m) scoreRisk = 100;
                 else if (product.InfringementRisk == "高风险") scoreRisk = 20;
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
            product.PriorityLevel = priority;
            _fsql.Update<ProductData>(product.Id)
                .Set(p => p.PriorityLevel, priority)
                .ExecuteAffrows();

            return new StrategyResult
            {
                Score = weightScore,
                Decision = weightScore >= 65 ? DecisionType.GO : DecisionType.WAIT,
                Reason = $"综合评分{weightScore:F1} (P等级: {priority})。建议: {suggestion}。资源分配: {allocation}。",
                Data = new { Priority = priority, Allocation = allocation }
            };
        }
    }
}

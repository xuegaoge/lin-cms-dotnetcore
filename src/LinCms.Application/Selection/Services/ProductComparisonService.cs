using AutoMapper;
using FreeSql;
using IGeekFan.FreeKit.Extras.FreeSql;
using LinCms.Application.Contracts.Selection;
using LinCms.Entities.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinCms.Application.Selection.Services
{
    /// <summary>
    /// 产品对比服务
    /// </summary>
    public class ProductComparisonService
    {
        private readonly IAuditBaseRepository<ProductComparison> _comparisonRepository;
        private readonly IAuditBaseRepository<ProductData> _productRepository;
        private readonly StrategyExecutionService _strategyService;
        private readonly IMapper _mapper;

        public ProductComparisonService(
            IAuditBaseRepository<ProductComparison> comparisonRepository,
            IAuditBaseRepository<ProductData> productRepository,
            StrategyExecutionService strategyService,
            IMapper mapper)
        {
            _comparisonRepository = comparisonRepository;
            _productRepository = productRepository;
            _strategyService = strategyService;
            _mapper = mapper;
        }

        /// <summary>
        /// 创建产品对比
        /// </summary>
        public async Task<ProductComparisonDto> CreateComparisonAsync(CreateComparisonDto dto)
        {
            // 获取产品信息
            var products = await _productRepository.Select
                .Where(p => dto.ProductIds.Contains(p.Id))
                .ToListAsync();

            if (products.Count != dto.ProductIds.Count)
            {
                throw new Exception("部分产品不存在");
            }

            // 计算对比矩阵和排名
            var ranking = await CalculateRankingAsync(products);
            var matrix = BuildComparisonMatrix(products, ranking);
            var recommendation = GenerateRecommendation(ranking);

            // 保存对比记录
            var comparison = new ProductComparison
            {
                ComparisonName = dto.ComparisonName,
                ProductIds = string.Join(",", dto.ProductIds),
                ProductCount = products.Count,
                ComparisonMatrix = System.Text.Json.JsonSerializer.Serialize(matrix),
                PriorityRanking = System.Text.Json.JsonSerializer.Serialize(ranking),
                Recommendation = recommendation,
                
            };

            await _comparisonRepository.InsertAsync(comparison);

            return new ProductComparisonDto
            {
                Id = comparison.Id,
                ComparisonName = comparison.ComparisonName,
                ProductCount = comparison.ProductCount,
                ComparisonMatrix = matrix,
                PriorityRanking = ranking,
                Recommendation = comparison.Recommendation,
                CreatedAt = comparison.CreateTime
            };
        }

        /// <summary>
        /// 获取对比详情
        /// </summary>
        public async Task<ProductComparisonDto> GetComparisonAsync(long id)
        {
            var comparison = await _comparisonRepository.Select.Where(c => c.Id == id).FirstAsync();
            if (comparison == null) return null;

            return new ProductComparisonDto
            {
                Id = comparison.Id,
                ComparisonName = comparison.ComparisonName,
                ProductCount = comparison.ProductCount,
                ComparisonMatrix = System.Text.Json.JsonSerializer.Deserialize<object>(comparison.ComparisonMatrix),
                PriorityRanking = System.Text.Json.JsonSerializer.Deserialize<List<ProductRankingDto>>(comparison.PriorityRanking),
                Recommendation = comparison.Recommendation,
                CreatedAt = comparison.CreateTime
            };
        }

        /// <summary>
        /// 获取对比列表
        /// </summary>
        public async Task<List<ProductComparisonDto>> GetComparisonsAsync(int page = 1, int size = 20)
        {
            var list = await _comparisonRepository.Select
                .OrderByDescending(c => c.CreateTime)
                .Page(page, size)
                .ToListAsync();

            return list.Select(c => new ProductComparisonDto
            {
                Id = c.Id,
                ComparisonName = c.ComparisonName,
                ProductCount = c.ProductCount,
                Recommendation = c.Recommendation,
                CreatedAt = c.CreateTime
            }).ToList();
        }

        /// <summary>
        /// 删除对比
        /// </summary>
        public async Task<bool> DeleteComparisonAsync(long id)
        {
            var rows = await _comparisonRepository.DeleteAsync(c => c.Id == id);
            return rows > 0;
        }

        #region 私有方法

        /// <summary>
        /// 计算产品排名
        /// </summary>
        private async Task<List<ProductRankingDto>> CalculateRankingAsync(List<ProductData> products)
        {
            var rankings = new List<ProductRankingDto>();

            foreach (var product in products)
            {
                // 计算综合评分（简化版，实际应调用多个策略）
                var scores = new Dictionary<string, decimal>
                {
                    ["market"] = CalculateMarketScore(product),
                    ["profit"] = CalculateProfitScore(product),
                    ["risk"] = CalculateRiskScore(product),
                    ["competition"] = CalculateCompetitionScore(product)
                };

                var finalScore = scores.Values.Average();

                rankings.Add(new ProductRankingDto
                {
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    FinalScore = finalScore,
                    PriorityLevel = DeterminePriorityLevel(finalScore),
                    Scores = scores
                });
            }

            // 排序并设置排名
            rankings = rankings.OrderByDescending(r => r.FinalScore).ToList();
            for (int i = 0; i < rankings.Count; i++)
            {
                rankings[i].Rank = i + 1;
            }

            return rankings;
        }

        /// <summary>
        /// 构建对比矩阵
        /// </summary>
        private object BuildComparisonMatrix(List<ProductData> products, List<ProductRankingDto> rankings)
        {
            return new
            {
                products = products.Select(p => new
                {
                    id = p.Id,
                    name = p.ProductName,
                    category = p.Category,
                    price = p.TargetPrice,
                    monthlySearchVolume = p.MonthlySearchVolume,
                    competitorCount = p.CompetitorCount,
                    averageRating = p.AverageRating
                }),
                rankings = rankings
            };
        }

        /// <summary>
        /// 生成推荐建议
        /// </summary>
        private string GenerateRecommendation(List<ProductRankingDto> rankings)
        {
            if (!rankings.Any()) return "无产品数据";

            var top = rankings.First();
            if (top.FinalScore >= 80)
                return $"强烈推荐 {top.ProductName} 优先立项（评分：{top.FinalScore:F1}）";
            else if (top.FinalScore >= 60)
                return $"建议优先考虑 {top.ProductName}（评分：{top.FinalScore:F1}），但需进一步评估";
            else
                return $"所有产品评分偏低，建议重新筛选";
        }

        /// <summary>
        /// 计算市场评分
        /// </summary>
        private decimal CalculateMarketScore(ProductData product)
        {
            decimal score = 50; // 基础分

            if (product.MonthlySearchVolume.HasValue)
            {
                if (product.MonthlySearchVolume >= 10000) score += 20;
                else if (product.MonthlySearchVolume >= 5000) score += 15;
                else if (product.MonthlySearchVolume >= 1000) score += 10;
            }

            if (product.SearchGrowthRate.HasValue && product.SearchGrowthRate > 0)
            {
                score += Math.Min(product.SearchGrowthRate.Value * 10, 20);
            }

            if (product.CompetitorCount.HasValue)
            {
                if (product.CompetitorCount < 50) score += 10;
                else if (product.CompetitorCount > 200) score -= 10;
            }

            return Math.Min(Math.Max(score, 0), 100);
        }

        /// <summary>
        /// 计算利润评分
        /// </summary>
        private decimal CalculateProfitScore(ProductData product)
        {
            if (!product.TargetPrice.HasValue || !product.PurchaseCost.HasValue)
                return 50;

            var grossProfit = product.TargetPrice.Value - product.PurchaseCost.Value - 
                             (product.ShippingCost ?? 0) - (product.FBACost ?? 0);
            var margin = grossProfit / product.TargetPrice.Value;

            if (margin >= 0.4m) return 90;
            if (margin >= 0.3m) return 75;
            if (margin >= 0.2m) return 60;
            if (margin >= 0.1m) return 40;
            return 20;
        }

        /// <summary>
        /// 计算风险评分
        /// </summary>
        private decimal CalculateRiskScore(ProductData product)
        {
            decimal score = 80; // 基础分（低风险）

            if (product.InfringementRisk == "高") score -= 30;
            else if (product.InfringementRisk == "中") score -= 15;

            if (product.PolicyRisk.HasValue)
            {
                score -= product.PolicyRisk.Value * 20;
            }

            if (product.ReturnRate.HasValue && product.ReturnRate > 0.1m)
            {
                score -= 20;
            }

            return Math.Min(Math.Max(score, 0), 100);
        }

        /// <summary>
        /// 计算竞争评分
        /// </summary>
        private decimal CalculateCompetitionScore(ProductData product)
        {
            decimal score = 50;

            if (product.TopConcentration.HasValue)
            {
                // CR3越低越好（市场分散）
                if (product.TopConcentration < 0.3m) score += 20;
                else if (product.TopConcentration > 0.7m) score -= 20;
            }

            if (product.NewProductRatio.HasValue && product.NewProductRatio > 0.3m)
            {
                score += 15; // 新品占比高说明市场活跃
            }

            if (product.AverageRating.HasValue && product.AverageRating < 4.0m)
            {
                score += 15; // 平均评分低说明有改进空间
            }

            return Math.Min(Math.Max(score, 0), 100);
        }

        /// <summary>
        /// 确定优先级
        /// </summary>
        private string DeterminePriorityLevel(decimal score)
        {
            if (score >= 80) return "P1";
            if (score >= 60) return "P2";
            if (score >= 40) return "P3";
            return "P4";
        }

        #endregion
    }
}

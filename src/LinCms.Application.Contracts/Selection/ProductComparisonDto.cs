using System;
using System.Collections.Generic;

namespace LinCms.Application.Contracts.Selection
{
    /// <summary>
    /// 创建产品对比DTO
    /// </summary>
    public class CreateComparisonDto
    {
        public string ComparisonName { get; set; }
        public List<long> ProductIds { get; set; }
    }

    /// <summary>
    /// 产品对比DTO
    /// </summary>
    public class ProductComparisonDto
    {
        public long Id { get; set; }
        public string ComparisonName { get; set; }
        public int ProductCount { get; set; }
        public object ComparisonMatrix { get; set; }
        public List<ProductRankingDto> PriorityRanking { get; set; }
        public string Recommendation { get; set; }
        public DateTime CreatedAt { get; set; }
        public long? CreatedBy { get; set; }
    }

    /// <summary>
    /// 产品排名DTO
    /// </summary>
    public class ProductRankingDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal FinalScore { get; set; }
        public string PriorityLevel { get; set; }
        public int Rank { get; set; }
        public Dictionary<string, decimal> Scores { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LinCms.Application.Contracts.Selection
{
    /// <summary>
    /// 企业定位评估DTO
    /// </summary>
    public class EnterpriseProfileDto
    {
        public long Id { get; set; }
        public long OrganizationId { get; set; }

        // 8个维度评分
        public decimal FundingCapacity { get; set; }
        public decimal TeamExperience { get; set; }
        public decimal SupplyChainDepth { get; set; }
        public decimal OperationCapability { get; set; }
        public decimal RiskTolerance { get; set; }
        public decimal MarketInsight { get; set; }
        public decimal TechCapability { get; set; }
        public decimal BrandAwareness { get; set; }

        // 计算结果
        public decimal TotalScore { get; set; }
        public string Grade { get; set; }
        public Dictionary<string, decimal> WeightConfig { get; set; }
        public List<string> Recommendations { get; set; }

        public DateTime EvaluatedAt { get; set; }
        public long? EvaluatedBy { get; set; }
        public bool IsCurrent { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>
    /// 创建企业定位评估DTO
    /// </summary>
    public class CreateEnterpriseProfileDto
    {
        public long OrganizationId { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal FundingCapacity { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal TeamExperience { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal SupplyChainDepth { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal OperationCapability { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal RiskTolerance { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal MarketInsight { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal TechCapability { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal BrandAwareness { get; set; }

        public Dictionary<string, decimal> WeightConfig { get; set; }
        public string Notes { get; set; }
    }
}

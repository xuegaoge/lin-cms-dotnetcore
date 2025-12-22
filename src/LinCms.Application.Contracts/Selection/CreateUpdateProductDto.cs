using System.ComponentModel.DataAnnotations;

namespace LinCms.Application.Contracts.Selection
{
    /// <summary>
    /// 创建/更新产品DTO
    /// </summary>
    public class CreateUpdateProductDto
    {
        // 基础信息
        [Required(ErrorMessage = "产品名称不能为空")]
        [StringLength(500)]
        public string ProductName { get; set; }

        [StringLength(20)]
        public string ASIN { get; set; }

        [StringLength(200)]
        public string Category { get; set; }

        [StringLength(200)]
        public string Brand { get; set; }

        // 市场数据
        public int? MonthlySearchVolume { get; set; }
        public decimal? SearchGrowthRate { get; set; }
        public int? CompetitorCount { get; set; }
        public decimal? TopConcentration { get; set; }
        public decimal? NewProductRatio { get; set; }
        public decimal? AverageRating { get; set; }
        public int? TotalReviews { get; set; }
        public decimal? Seasonality { get; set; }

        // 成本财务
        public decimal? TargetPrice { get; set; }
        public decimal? PurchaseCost { get; set; }
        public decimal? ShippingCost { get; set; }
        public decimal? FBACost { get; set; }
        public decimal? WeightKg { get; set; }
        public decimal? VolumeCbm { get; set; }
        public decimal? AdvertisingCPC { get; set; }
        public decimal? ConversionRate { get; set; }
        public decimal? ClickThroughRate { get; set; }

        // 供应链
        public int? SupplierCount { get; set; }
        public int? SupplierStability { get; set; }
        public int? LeadTimeDays { get; set; }
        public int? MOQ { get; set; }
        public decimal? PriceVolatility { get; set; }

        // 风险合规
        public string InfringementRisk { get; set; }
        public string CertificationLevel { get; set; }
        public decimal? PolicyRisk { get; set; }
        public decimal? ReturnRate { get; set; }

        // 产品属性
        public string Material { get; set; }
        public int? VariantCount { get; set; }
        public bool? IsFragile { get; set; }
        public bool? IsLiquid { get; set; }
        public bool? IsDangerous { get; set; }
        public int? ProductLifecycle { get; set; }
        public decimal? RepurchaseRate { get; set; }

        // 竞争分析
        public bool? HasAmazonChoice { get; set; }
        public int? BSRTop10 { get; set; }
        public int? BSRTop50 { get; set; }
        public decimal? LongTailKeywordRatio { get; set; }
        public decimal? NewProductSuccessRate { get; set; }
        public int? QAUnanswered { get; set; }

        // 差异化
        public int? DifferentiationPoints { get; set; }
        public int? FunctionDiff { get; set; }
        public int? MaterialDiff { get; set; }
        public int? DesignDiff { get; set; }
        public int? PackagingDiff { get; set; }

        // 元数据
        public string Status { get; set; }
        public string PriorityLevel { get; set; }
        public long? AssignedTo { get; set; }
    }
}

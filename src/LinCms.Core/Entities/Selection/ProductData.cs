using FreeSql.DataAnnotations;
using IGeekFan.FreeKit.Extras.AuditEntity;
using System;

namespace LinCms.Entities.Selection
{
    /// <summary>
    /// 产品数据主表 - 核心表，包含52个字段
    /// </summary>
    [Table(Name = "selection_product_data")]
    public class ProductData : FullAuditEntity<long, long>
    {
        // ========================================
        // 基础信息 (4字段)
        // ========================================

        /// <summary>
        /// 产品名称
        /// </summary>
        [Column(StringLength = 500, IsNullable = false)]
        public string ProductName { get; set; }

        /// <summary>
        /// Amazon ASIN
        /// </summary>
        [Column(StringLength = 20)]
        public string ASIN { get; set; }

        /// <summary>
        /// 产品类目
        /// </summary>
        [Column(StringLength = 200)]
        public string Category { get; set; }

        /// <summary>
        /// 品牌
        /// </summary>
        [Column(StringLength = 200)]
        public string Brand { get; set; }

        // ========================================
        // 市场数据 (8字段)
        // ========================================

        /// <summary>
        /// 月搜索量
        /// </summary>
        public int? MonthlySearchVolume { get; set; }
        
        /// <summary>
        /// 预估月销量
        /// </summary>
        public int? EstimatedMonthlySales { get; set; }

        /// <summary>
        /// 搜索增长率 (%)
        /// </summary>
        [Column(Precision = 5, Scale = 2)]
        public decimal? SearchGrowthRate { get; set; }

        /// <summary>
        /// 竞品数量
        /// </summary>
        public int? CompetitorCount { get; set; }

        /// <summary>
        /// 头部集中度CR3 (0-1)
        /// </summary>
        [Column(Precision = 5, Scale = 4)]
        public decimal? TopConcentration { get; set; }

        /// <summary>
        /// 新品占比 (0-1)
        /// </summary>
        [Column(Precision = 5, Scale = 4)]
        public decimal? NewProductRatio { get; set; }

        /// <summary>
        /// 平均评分 (1-5)
        /// </summary>
        [Column(Precision = 3, Scale = 2)]
        public decimal? AverageRating { get; set; }

        /// <summary>
        /// Review总数
        /// </summary>
        public int? TotalReviews { get; set; }

        /// <summary>
        /// 季节性指数 (峰值/平均)
        /// </summary>
        [Column(Precision = 5, Scale = 2)]
        public decimal? Seasonality { get; set; }

        // ========================================
        // 成本财务 (9字段)
        // ========================================

        /// <summary>
        /// 目标售价 ($)
        /// </summary>
        [Column(Precision = 10, Scale = 2)]
        public decimal? TargetPrice { get; set; }

        /// <summary>
        /// 采购成本 ($)
        /// </summary>
        [Column(Precision = 10, Scale = 2)]
        public decimal? PurchaseCost { get; set; }

        /// <summary>
        /// 头程运费 ($)
        /// </summary>
        [Column(Precision = 10, Scale = 2)]
        public decimal? ShippingCost { get; set; }

        /// <summary>
        /// FBA费用 ($)
        /// </summary>
        [Column(Precision = 10, Scale = 2)]
        public decimal? FBACost { get; set; }

        /// <summary>
        /// 重量 (kg)
        /// </summary>
        [Column(Precision = 8, Scale = 3)]
        public decimal? WeightKg { get; set; }

        /// <summary>
        /// 体积 (cbm)
        /// </summary>
        [Column(Precision = 8, Scale = 4)]
        public decimal? VolumeCbm { get; set; }

        /// <summary>
        /// 广告CPC ($)
        /// </summary>
        [Column(Precision = 8, Scale = 2)]
        public decimal? AdvertisingCPC { get; set; }

        /// <summary>
        /// 预期转化率 (0-1)
        /// </summary>
        [Column(Precision = 5, Scale = 4)]
        public decimal? ConversionRate { get; set; }

        /// <summary>
        /// 预期点击率 (0-1)
        /// </summary>
        [Column(Precision = 5, Scale = 4)]
        public decimal? ClickThroughRate { get; set; }

        // ========================================
        // 供应链 (5字段)
        // ========================================

        /// <summary>
        /// 供应商数量
        /// </summary>
        public int? SupplierCount { get; set; }

        /// <summary>
        /// 供应商稳定性评分 (0-100)
        /// </summary>
        public int? SupplierStability { get; set; }

        /// <summary>
        /// 交期 (天)
        /// </summary>
        public int? LeadTimeDays { get; set; }

        /// <summary>
        /// 最小起订量
        /// </summary>
        public int? MOQ { get; set; }

        /// <summary>
        /// 价格波动率 (0-1)
        /// </summary>
        [Column(Precision = 5, Scale = 4)]
        public decimal? PriceVolatility { get; set; }

        // ========================================
        // 风险合规 (4字段)
        // ========================================

        /// <summary>
        /// 侵权风险 (高/中/低)
        /// </summary>
        [Column(StringLength = 20)]
        public string InfringementRisk { get; set; }

        /// <summary>
        /// 认证要求 (严/松)
        /// </summary>
        [Column(StringLength = 20)]
        public string CertificationLevel { get; set; }

        /// <summary>
        /// 政策风险评分 (0-1)
        /// </summary>
        [Column(Precision = 3, Scale = 2)]
        public decimal? PolicyRisk { get; set; }

        /// <summary>
        /// 预期退货率 (0-1)
        /// </summary>
        [Column(Precision = 5, Scale = 4)]
        public decimal? ReturnRate { get; set; }

        // ========================================
        // 产品属性 (7字段) - Phase 2
        // ========================================

        /// <summary>
        /// 材质
        /// </summary>
        [Column(StringLength = 200)]
        public string Material { get; set; }

        /// <summary>
        /// 变体数
        /// </summary>
        public int? VariantCount { get; set; }

        /// <summary>
        /// 是否易碎
        /// </summary>
        public bool? IsFragile { get; set; }

        /// <summary>
        /// 是否液体
        /// </summary>
        public bool? IsLiquid { get; set; }

        /// <summary>
        /// 是否危险品
        /// </summary>
        public bool? IsDangerous { get; set; }

        /// <summary>
        /// 产品生命周期 (月)
        /// </summary>
        public int? ProductLifecycle { get; set; }

        /// <summary>
        /// 复购率潜力 (0-1)
        /// </summary>
        [Column(Precision = 5, Scale = 4)]
        public decimal? RepurchaseRate { get; set; }

        // ========================================
        // 竞争分析 (6字段) - Phase 2
        // ========================================

        /// <summary>
        /// TOP10是否有亚马逊自营
        /// </summary>
        public bool? HasAmazonChoice { get; set; }

        /// <summary>
        /// BSR TOP10排名
        /// </summary>
        public int? BSRTop10 { get; set; }

        /// <summary>
        /// BSR TOP50排名
        /// </summary>
        public int? BSRTop50 { get; set; }

        /// <summary>
        /// 长尾关键词占比 (0-1)
        /// </summary>
        [Column(Precision = 5, Scale = 4)]
        public decimal? LongTailKeywordRatio { get; set; }

        /// <summary>
        /// 新品成功率 (0-1)
        /// </summary>
        [Column(Precision = 5, Scale = 4)]
        public decimal? NewProductSuccessRate { get; set; }

        /// <summary>
        /// 未回答的QA数量
        /// </summary>
        public int? QAUnanswered { get; set; }

        // ========================================
        // 差异化 (5字段) - Phase 2
        // ========================================

        /// <summary>
        /// 差异化卖点数量 (3-10)
        /// </summary>
        public int? DifferentiationPoints { get; set; }

        /// <summary>
        /// 功能差异评分 (0-10)
        /// </summary>
        public int? FunctionDiff { get; set; }

        /// <summary>
        /// 材质差异评分 (0-10)
        /// </summary>
        public int? MaterialDiff { get; set; }

        /// <summary>
        /// 设计差异评分 (0-10)
        /// </summary>
        public int? DesignDiff { get; set; }

        /// <summary>
        /// 包装差异评分 (0-10)
        /// </summary>
        public int? PackagingDiff { get; set; }

        // ========================================
        // 元数据
        // ========================================

        /// <summary>
        /// 状态 (draft/active/archived)
        /// </summary>
        [Column(StringLength = 20)]
        public string Status { get; set; } = "draft";

        /// <summary>
        /// 优先级 (P1/P2/P3/P4)
        /// </summary>
        [Column(StringLength = 10)]
        public string PriorityLevel { get; set; }

        /// <summary>
        /// 分配给谁
        /// </summary>
        public long? AssignedTo { get; set; }

        /// <summary>
        /// 最新决策 (GO/WAIT/STOP)
        /// </summary>
        [Column(StringLength = 20)]
        public string LatestDecision { get; set; }
    }
}

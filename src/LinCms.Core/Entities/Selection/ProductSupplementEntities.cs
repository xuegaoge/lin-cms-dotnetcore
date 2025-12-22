using FreeSql.DataAnnotations;
using IGeekFan.FreeKit.Extras.AuditEntity;
using System;

namespace LinCms.Entities.Selection
{
    /// <summary>
    /// S19 关键词研究数据
    /// </summary>
    [Table(Name = "selection_product_keywords")]
    public class ProductKeyword : FullAuditEntity<long, long>
    {
        public long ProductId { get; set; }

        /// <summary>
        /// 关键词
        /// </summary>
        [Column(StringLength = 200)]
        public string Keyword { get; set; }

        /// <summary>
        /// 类型 (核心词/长尾词/品牌词/场景词/属性词)
        /// </summary>
        [Column(StringLength = 50)]
        public string Type { get; set; }

        /// <summary>
        /// 月搜索量
        /// </summary>
        public int SearchVolume { get; set; }

        /// <summary>
        /// 竞品数
        /// </summary>
        public int CompetitorCount { get; set; }

        /// <summary>
        /// SPR值 (=搜索量/竞品数*1000)
        /// </summary>
        [Column(Precision = 10, Scale = 2)]
        public decimal SPR { get; set; }

        /// <summary>
        /// 建议出价 ($)
        /// </summary>
        [Column(Precision = 8, Scale = 2)]
        public decimal BidPrice { get; set; }

        /// <summary>
        /// 竞争度 (低/中/高)
        /// </summary>
        [Column(StringLength = 20)]
        public string CompetitionLevel { get; set; }

        /// <summary>
        /// 机会指数 (0-100)
        /// </summary>
        [Column(Precision = 5, Scale = 2)]
        public decimal OpportunityScore { get; set; }

        /// <summary>
        /// 优先级 (高/中/低)
        /// </summary>
        [Column(StringLength = 20)]
        public string Priority { get; set; }

        /// <summary>
        /// 当前排名
        /// </summary>
        public int? CurrentRank { get; set; }

        /// <summary>
        /// 目标排名
        /// </summary>
        public int? TargetRank { get; set; }
    }

    /// <summary>
    /// S20 市场趋势数据 (按指标存储12个月数据)
    /// </summary>
    [Table(Name = "selection_product_trends")]
    public class ProductTrend : FullAuditEntity<long, long>
    {
        public long ProductId { get; set; }

        /// <summary>
        /// 指标名称 (月搜索量/月销量/平均售价/竞品数量/新品数量/平均评分/广告CPC/退货率)
        /// </summary>
        [Column(StringLength = 100)]
        public string MetricName { get; set; }

        // 1-12月数据
        [Column(Precision = 18, Scale = 4)]
        public decimal Month1 { get; set; }
        [Column(Precision = 18, Scale = 4)]
        public decimal Month2 { get; set; }
        [Column(Precision = 18, Scale = 4)]
        public decimal Month3 { get; set; }
        [Column(Precision = 18, Scale = 4)]
        public decimal Month4 { get; set; }
        [Column(Precision = 18, Scale = 4)]
        public decimal Month5 { get; set; }
        [Column(Precision = 18, Scale = 4)]
        public decimal Month6 { get; set; }
        [Column(Precision = 18, Scale = 4)]
        public decimal Month7 { get; set; }
        [Column(Precision = 18, Scale = 4)]
        public decimal Month8 { get; set; }
        [Column(Precision = 18, Scale = 4)]
        public decimal Month9 { get; set; }
        [Column(Precision = 18, Scale = 4)]
        public decimal Month10 { get; set; }
        [Column(Precision = 18, Scale = 4)]
        public decimal Month11 { get; set; }
        [Column(Precision = 18, Scale = 4)]
        public decimal Month12 { get; set; }

        /// <summary>
        /// 年均值
        /// </summary>
        [Column(Precision = 18, Scale = 4)]
        public decimal YearMean { get; set; }

        /// <summary>
        /// 趋势判定 (上升/下降/稳定)
        /// </summary>
        [Column(StringLength = 50)]
        public string Trend { get; set; }

        /// <summary>
        /// 季节性指数
        /// </summary>
        [Column(Precision = 8, Scale = 4)]
        public decimal SeasonalityIndex { get; set; }
    }
}

using FreeSql.DataAnnotations;
using System;

namespace LinCms.Entities.Selection
{
    /// <summary>
    /// 产品指标历史数据表 - 用于趋势分析
    /// </summary>
    [Table(Name = "selection_product_metrics_history")]
    public class ProductMetricsHistory
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Column(IsIdentity = true, IsPrimary = true)]
        public long Id { get; set; }

        /// <summary>
        /// 关联产品ID
        /// </summary>
        [Column(IsNullable = false)]
        public long ProductId { get; set; }

        /// <summary>
        /// 指标日期
        /// </summary>
        [Column(IsNullable = false)]
        public DateTime MetricDate { get; set; }

        /// <summary>
        /// 日销量
        /// </summary>
        public int? DailySales { get; set; }

        /// <summary>
        /// 平均价格
        /// </summary>
        [Column(Precision = 10, Scale = 2)]
        public decimal? AveragePrice { get; set; }

        /// <summary>
        /// 搜索量
        /// </summary>
        public int? SearchVolume { get; set; }

        /// <summary>
        /// BSR排名
        /// </summary>
        public int? BSRRank { get; set; }

        /// <summary>
        /// 平均评分
        /// </summary>
        [Column(Precision = 3, Scale = 2)]
        public decimal? AverageRating { get; set; }

        /// <summary>
        /// Review数量
        /// </summary>
        public int? ReviewCount { get; set; }

        /// <summary>
        /// 转化率
        /// </summary>
        [Column(Precision = 5, Scale = 4)]
        public decimal? ConversionRate { get; set; }

        /// <summary>
        /// 点击率
        /// </summary>
        [Column(Precision = 5, Scale = 4)]
        public decimal? ClickThroughRate { get; set; }

        /// <summary>
        /// ACOS (广告成本销售比)
        /// </summary>
        [Column(Precision = 5, Scale = 4)]
        public decimal? ACOS { get; set; }

        /// <summary>
        /// 库存数量
        /// </summary>
        public int? StockQuantity { get; set; }

        /// <summary>
        /// 数据来源 (manual/api/scraper)
        /// </summary>
        [Column(StringLength = 50)]
        public string DataSource { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        // 导航属性
        [Navigate(nameof(ProductId))]
        public ProductData Product { get; set; }
    }
}

using FreeSql.DataAnnotations;
using IGeekFan.FreeKit.Extras.AuditEntity;
using System;

namespace LinCms.Entities.Selection
{
    /// <summary>
    /// 多产品对比表 - M04综合决策表使用
    /// </summary>
    [Table(Name = "selection_product_comparison")]
    public class ProductComparison : FullAuditEntity<long, long>
    {
        /// <summary>
        /// 对比名称
        /// </summary>
        [Column(StringLength = 200, IsNullable = false)]
        public string ComparisonName { get; set; }

        /// <summary>
        /// 产品ID列表 (JSON数组)
        /// </summary>
        [Column(DbType = "text")]
        public string ProductIds { get; set; }

        /// <summary>
        /// 产品数量
        /// </summary>
        public int ProductCount { get; set; }

        /// <summary>
        /// 对比矩阵 (JSON)
        /// </summary>
        [Column(DbType = "mediumtext")]
        public string ComparisonMatrix { get; set; }

        /// <summary>
        /// 优先级排名 (JSON)
        /// </summary>
        [Column(DbType = "text")]
        public string PriorityRanking { get; set; }

        /// <summary>
        /// 推荐意见
        /// </summary>
        [Column(DbType = "text")]
        public string Recommendation { get; set; }

        /// <summary>
        /// 创建人ID
        /// </summary>
        public long? CreatedBy { get; set; }
    }
}

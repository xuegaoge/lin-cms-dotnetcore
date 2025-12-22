using FreeSql.DataAnnotations;
using System;

namespace LinCms.Entities.Selection
{
    /// <summary>
    /// 策略执行记录表 - 存储每次策略执行的结果
    /// </summary>
    [Table(Name = "selection_strategy_execution")]
    [Index("idx_product_latest", "ProductId, IsLatest, ExecutedAt")]
    public class StrategyExecution
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
        /// 策略代码 (S01/S02/...)
        /// </summary>
        [Column(StringLength = 50, IsNullable = false)]
        public string StrategyCode { get; set; }

        /// <summary>
        /// 策略名称
        /// </summary>
        [Column(StringLength = 200)]
        public string StrategyName { get; set; }

        /// <summary>
        /// 策略类型
        /// </summary>
        [Column(StringLength = 50)]
        public string StrategyType { get; set; }

        // ========== 主要结果 ==========

        /// <summary>
        /// 评分 (0-100)
        /// </summary>
        [Column(Precision = 5, Scale = 2)]
        public decimal? Score { get; set; }

        /// <summary>
        /// 等级 (GO/WAIT/STOP 或 A/B/C/D)
        /// </summary>
        [Column(StringLength = 20)]
        public string Grade { get; set; }

        /// <summary>
        /// 决策建议
        /// </summary>
        [Column(StringLength = 50)]
        public string Decision { get; set; }

        /// <summary>
        /// 判定理由
        /// </summary>
        [Column(DbType = "text")]
        public string Reason { get; set; }

        // ========== 详细结果 (JSON格式) ==========

        /// <summary>
        /// 详细计算明细 (JSON)
        /// </summary>
        [Column(DbType = "mediumtext")]
        public string DetailJson { get; set; }

        /// <summary>
        /// 子项结果 (JSON)
        /// </summary>
        [Column(DbType = "text")]
        public string SubResultsJson { get; set; }

        /// <summary>
        /// 警告列表 (JSON)
        /// </summary>
        [Column(DbType = "text")]
        public string WarningsJson { get; set; }

        /// <summary>
        /// 建议列表 (JSON)
        /// </summary>
        [Column(DbType = "text")]
        public string SuggestionsJson { get; set; }

        // ========== 执行元数据 ==========

        /// <summary>
        /// 执行时间
        /// </summary>
        public DateTime ExecutedAt { get; set; }

        /// <summary>
        /// 执行人ID
        /// </summary>
        public long? ExecutedBy { get; set; }

        /// <summary>
        /// 执行耗时 (毫秒)
        /// </summary>
        public int? ExecutionTimeMs { get; set; }

        /// <summary>
        /// 是否最新记录
        /// </summary>
        public bool IsLatest { get; set; } = true;

        // 导航属性
        [Navigate(nameof(ProductId))]
        public ProductData Product { get; set; }
    }
}

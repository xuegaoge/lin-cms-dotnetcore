using FreeSql.DataAnnotations;
using System;

namespace LinCms.Entities.Selection
{
    /// <summary>
    /// 策略推荐记录表 - S08 TOP20打法推荐使用
    /// </summary>
    [Table(Name = "selection_strategy_recommendations")]
    public class StrategyRecommendation
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
        /// 关联策略执行ID
        /// </summary>
        public long? ExecutionId { get; set; }

        /// <summary>
        /// 推荐代码 (T01/T02/...)
        /// </summary>
        [Column(StringLength = 50)]
        public string RecommendationCode { get; set; }

        /// <summary>
        /// 推荐名称
        /// </summary>
        [Column(StringLength = 200)]
        public string RecommendationName { get; set; }

        /// <summary>
        /// 推荐类型 (关键词断层/差评优化/断货窗口等)
        /// </summary>
        [Column(StringLength = 50)]
        public string RecommendationType { get; set; }

        /// <summary>
        /// 优先级 (高/中/低)
        /// </summary>
        [Column(StringLength = 20)]
        public string Priority { get; set; }

        /// <summary>
        /// 推荐描述
        /// </summary>
        [Column(DbType = "text")]
        public string Description { get; set; }

        /// <summary>
        /// 执行步骤 (JSON)
        /// </summary>
        [Column(DbType = "text")]
        public string ActionSteps { get; set; }

        /// <summary>
        /// 预期效果
        /// </summary>
        [Column(DbType = "text")]
        public string ExpectedImpact { get; set; }

        /// <summary>
        /// 匹配度评分 (0-100)
        /// </summary>
        [Column(Precision = 5, Scale = 2)]
        public decimal? MatchScore { get; set; }

        /// <summary>
        /// 推荐时间
        /// </summary>
        public DateTime RecommendedAt { get; set; }

        /// <summary>
        /// 是否已采纳
        /// </summary>
        public bool IsAdopted { get; set; } = false;

        /// <summary>
        /// 采纳时间
        /// </summary>
        public DateTime? AdoptedAt { get; set; }

        /// <summary>
        /// 采纳人ID
        /// </summary>
        public long? AdoptedBy { get; set; }

        /// <summary>
        /// 执行备注
        /// </summary>
        [Column(DbType = "text")]
        public string ExecutionNotes { get; set; }

        // 导航属性
        [Navigate(nameof(ProductId))]
        public ProductData Product { get; set; }

        [Navigate(nameof(ExecutionId))]
        public StrategyExecution Execution { get; set; }
    }
}

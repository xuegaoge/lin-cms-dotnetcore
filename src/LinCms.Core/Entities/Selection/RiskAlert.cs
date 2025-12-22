using FreeSql.DataAnnotations;
using System;

namespace LinCms.Entities.Selection
{
    /// <summary>
    /// 风险预警记录表 - S04策略使用
    /// </summary>
    [Table(Name = "selection_risk_alerts")]
    public class RiskAlert
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
        /// 风险代码 (R01/R02/...)
        /// </summary>
        [Column(StringLength = 50)]
        public string RiskCode { get; set; }

        /// <summary>
        /// 风险名称
        /// </summary>
        [Column(StringLength = 200)]
        public string RiskName { get; set; }

        /// <summary>
        /// 风险等级 (高/中/低)
        /// </summary>
        [Column(StringLength = 20)]
        public string RiskLevel { get; set; }

        /// <summary>
        /// 风险类型 (市场/财务/供应链/合规)
        /// </summary>
        [Column(StringLength = 50)]
        public string RiskType { get; set; }

        /// <summary>
        /// 风险描述
        /// </summary>
        [Column(DbType = "text")]
        public string Description { get; set; }

        /// <summary>
        /// 触发值
        /// </summary>
        [Column(StringLength = 100)]
        public string TriggerValue { get; set; }

        /// <summary>
        /// 阈值
        /// </summary>
        [Column(StringLength = 100)]
        public string ThresholdValue { get; set; }

        /// <summary>
        /// 建议措施
        /// </summary>
        [Column(DbType = "text")]
        public string Suggestions { get; set; }

        /// <summary>
        /// 检测时间
        /// </summary>
        public DateTime DetectedAt { get; set; }

        /// <summary>
        /// 是否已处理
        /// </summary>
        public bool IsResolved { get; set; } = false;

        /// <summary>
        /// 处理时间
        /// </summary>
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// 处理人ID
        /// </summary>
        public long? ResolvedBy { get; set; }

        /// <summary>
        /// 处理备注
        /// </summary>
        [Column(DbType = "text")]
        public string ResolveNotes { get; set; }

        // 导航属性
        [Navigate(nameof(ProductId))]
        public ProductData Product { get; set; }

        [Navigate(nameof(ExecutionId))]
        public StrategyExecution Execution { get; set; }
    }
}

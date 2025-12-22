using FreeSql.DataAnnotations;
using IGeekFan.FreeKit.Extras.AuditEntity;
using System;

namespace LinCms.Entities.Selection
{
    /// <summary>
    /// 产品审批表 - 审批流程使用
    /// </summary>
    [Table(Name = "selection_product_approval")]
    public class ProductApproval : FullAuditEntity<long, long>
    {
        /// <summary>
        /// 关联产品ID
        /// </summary>
        [Column(IsNullable = false)]
        public long ProductId { get; set; }

        /// <summary>
        /// 关联策略执行ID
        /// </summary>
        public long? StrategyExecutionId { get; set; }

        /// <summary>
        /// 当前审批阶段 (product/operation/finance/ceo)
        /// </summary>
        [Column(StringLength = 50)]
        public string CurrentStage { get; set; }

        /// <summary>
        /// 审批历史 (JSON数组)
        /// </summary>
        [Column(DbType = "mediumtext")]
        public string ApprovalHistory { get; set; }

        /// <summary>
        /// 审批链配置 (JSON数组)
        /// </summary>
        [Column(DbType = "text")]
        public string ApprovalChain { get; set; }

        /// <summary>
        /// 是否完成
        /// </summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>
        /// 最终结果 (approved/rejected/pending)
        /// </summary>
        [Column(StringLength = 20)]
        public string FinalResult { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// 提交人ID
        /// </summary>
        public long? SubmittedBy { get; set; }

        /// <summary>
        /// 提交时间
        /// </summary>
        public DateTime? SubmittedAt { get; set; }

        // 导航属性
        [Navigate(nameof(ProductId))]
        public ProductData Product { get; set; }

        [Navigate(nameof(StrategyExecutionId))]
        public StrategyExecution StrategyExecution { get; set; }
    }
}

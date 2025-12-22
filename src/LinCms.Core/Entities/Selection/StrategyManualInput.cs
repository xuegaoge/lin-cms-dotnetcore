using FreeSql.DataAnnotations;
using System;

namespace LinCms.Entities.Selection
{
    /// <summary>
    /// 手填型策略输入表 - 存储需要手动填写的策略数据(如40题自诊)
    /// </summary>
    [Table(Name = "selection_strategy_manual_input")]
    public class StrategyManualInput
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
        /// 策略代码 (如S02)
        /// </summary>
        [Column(StringLength = 50, IsNullable = false)]
        public string StrategyCode { get; set; }

        /// <summary>
        /// 完整输入数据 (JSON格式，如40题答案)
        /// </summary>
        [Column(DbType = "mediumtext")]
        public string InputJson { get; set; }

        /// <summary>
        /// 关联的执行记录ID
        /// </summary>
        public long? ExecutionId { get; set; }

        /// <summary>
        /// 提交时间
        /// </summary>
        public DateTime SubmittedAt { get; set; }

        /// <summary>
        /// 提交人ID
        /// </summary>
        public long? SubmittedBy { get; set; }

        // 导航属性
        [Navigate(nameof(ProductId))]
        public ProductData Product { get; set; }

        [Navigate(nameof(ExecutionId))]
        public StrategyExecution Execution { get; set; }
    }
}

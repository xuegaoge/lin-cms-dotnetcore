using LinCms.Entities.Selection;
using System.Collections.Generic;

namespace LinCms.Application.Selection.Models
{
    /// <summary>
    /// 策略执行上下文 - 包含执行策略所需的外部依赖
    /// </summary>
    public class ExecutionContext
    {
        /// <summary>
        /// 企业定位评估数据
        /// </summary>
        public EnterpriseProfile EnterpriseProfile { get; set; }

        /// <summary>
        /// 全局配置数据
        /// </summary>
        public GlobalConfig GlobalConfig { get; set; }

        /// <summary>
        /// 产品数据
        /// </summary>
        public ProductData Product { get; set; }

        /// <summary>
        /// 执行人ID
        /// </summary>
        public long? ExecutedBy { get; set; }

        /// <summary>
        /// 用户手动填写的答案（用于自诊等策略）
        /// </summary>
        public Dictionary<string, int?> ManualAnswers { get; set; }
    }
}
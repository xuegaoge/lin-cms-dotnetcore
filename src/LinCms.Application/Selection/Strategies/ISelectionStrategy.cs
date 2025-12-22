using LinCms.Application.Selection.Models;
using LinCms.Entities.Selection;
using System.Collections.Generic;

namespace LinCms.Application.Selection.Strategies
{
    /// <summary>
    /// 选品策略接口 - 所有策略必须实现此接口
    /// </summary>
    public interface ISelectionStrategy
    {
        /// <summary>
        /// 策略代码（唯一标识）如 S01, S02
        /// </summary>
        string Code { get; }

        /// <summary>
        /// 策略名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 策略描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 策略类型
        /// </summary>
        StrategyType Type { get; }

        /// <summary>
        /// 必需字段列表（用于验证）
        /// </summary>
        IReadOnlyList<string> RequiredFields { get; }

        /// <summary>
        /// 验证产品数据是否满足执行条件
        /// </summary>
        ValidationResult Validate(ProductData product);

        /// <summary>
        /// 执行策略计算
        /// </summary>
        StrategyResult Execute(ProductData product, ExecutionContext context);
    }
}

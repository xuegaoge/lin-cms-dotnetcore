using LinCms.Entities.Selection;
using System.Collections.Generic;
using System.Linq;

namespace LinCms.Application.Selection.Strategies
{
    /// <summary>
    /// 策略注册表 - 管理所有策略实例
    /// </summary>
    public class StrategyRegistry
    {
        private readonly Dictionary<string, ISelectionStrategy> _strategies = new Dictionary<string, ISelectionStrategy>();

        public StrategyRegistry(IEnumerable<ISelectionStrategy> strategies)
        {
            foreach (var strategy in strategies)
            {
                Register(strategy);
            }
        }

        /// <summary>
        /// 注册策略
        /// </summary>
        public void Register(ISelectionStrategy strategy)
        {
            _strategies[strategy.Code] = strategy;
        }

        /// <summary>
        /// 获取策略
        /// </summary>
        public ISelectionStrategy Get(string code)
        {
            return _strategies.TryGetValue(code, out var strategy) ? strategy : null;
        }

        /// <summary>
        /// 获取所有策略
        /// </summary>
        public IEnumerable<ISelectionStrategy> GetAll()
        {
            return _strategies.Values;
        }

        /// <summary>
        /// 按类型获取策略
        /// </summary>
        public IEnumerable<ISelectionStrategy> GetByType(StrategyType type)
        {
            return _strategies.Values.Where(s => s.Type == type);
        }

        /// <summary>
        /// 检查策略是否存在
        /// </summary>
        public bool Exists(string code)
        {
            return _strategies.ContainsKey(code);
        }

        /// <summary>
        /// 获取策略数量
        /// </summary>
        public int Count => _strategies.Count;
    }
}

using LinCms.Application.Selection.Models;
using LinCms.Entities.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace LinCms.Application.Selection.Strategies
{
    /// <summary>
    /// 策略基类 - 提供通用功能
    /// </summary>
    public abstract class BaseStrategy : ISelectionStrategy
    {
        public abstract string Code { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract StrategyType Type { get; }
        public abstract IReadOnlyList<string> RequiredFields { get; }
        
        /// <summary>
        /// 策略计算逻辑定义与字段说明
        /// </summary>
        public virtual string LogicDefinition => "未定义计算逻辑";

        /// <summary>
        /// 验证产品数据
        /// </summary>
        public virtual ValidationResult Validate(ProductData product)
        {
            if (product == null)
            {
                return ValidationResult.Fail("产品数据不能为空");
            }

            var missingFields = new List<string>();

            foreach (var fieldName in RequiredFields)
            {
                var value = GetFieldValue(product, fieldName);
                if (value == null)
                {
                    missingFields.Add(fieldName);
                }
            }

            if (missingFields.Any())
            {
                return ValidationResult.Fail($"缺少必需字段: {string.Join(", ", missingFields)}");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// 执行策略 - 带计时和异常处理
        /// </summary>
        public StrategyResult Execute(ProductData product, ExecutionContext context)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                // 验证
                var validationResult = Validate(product);
                if (!validationResult.IsValid)
                {
                    return new StrategyResult
                    {
                        StrategyCode = Code,
                        StrategyName = Name,
                        Type = Type,
                        IsSuccess = false,
                        ErrorMessage = validationResult.GetErrorMessage(),
                        ExecutedAt = DateTime.Now,
                        ExecutionTimeMs = sw.ElapsedMilliseconds
                    };
                }

                // 执行策略
                var result = ExecuteCore(product, context);

                // 设置基础信息
                result.StrategyCode = Code;
                result.StrategyName = Name;
                result.Type = Type;
                result.IsSuccess = true;
                result.ExecutedAt = DateTime.Now;
                result.ExecutionTimeMs = sw.ElapsedMilliseconds;

                return result;
            }
            catch (Exception ex)
            {
                return new StrategyResult
                {
                    StrategyCode = Code,
                    StrategyName = Name,
                    Type = Type,
                    IsSuccess = false,
                    ErrorMessage = $"策略执行异常: {ex.Message}",
                    ExecutedAt = DateTime.Now,
                    ExecutionTimeMs = sw.ElapsedMilliseconds
                };
            }
            finally
            {
                sw.Stop();
            }
        }

        /// <summary>
        /// 核心执行逻辑 - 子类实现
        /// </summary>
        protected abstract StrategyResult ExecuteCore(ProductData product, ExecutionContext context);

        /// <summary>
        /// 获取字段值（通过反射）
        /// </summary>
        protected object GetFieldValue(ProductData product, string fieldName)
        {
            var property = typeof(ProductData).GetProperty(fieldName, 
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            
            return property?.GetValue(product);
        }

        /// <summary>
        /// 计算分级评分（根据阈值数组）
        /// </summary>
        protected decimal CalculateGradeScore(decimal? value, decimal[] thresholds, bool higherIsBetter = true)
        {
            if (!value.HasValue) return 0;

            var val = value.Value;
            var grades = new[] { 2m, 4m, 6m, 8m, 10m };

            if (!higherIsBetter)
            {
                // 越低越好，反转逻辑
                for (int i = thresholds.Length - 1; i >= 0; i--)
                {
                    if (val <= thresholds[i])
                    {
                        return grades[thresholds.Length - 1 - i];
                    }
                }
                return 0;
            }
            else
            {
                // 越高越好
                for (int i = 0; i < thresholds.Length; i++)
                {
                    if (val >= thresholds[i])
                    {
                        return grades[i];
                    }
                }
                return grades[0];
            }
        }

        /// <summary>
        /// 计算分级评分（整数版本）
        /// </summary>
        protected decimal CalculateGradeScore(int? value, int[] thresholds, bool higherIsBetter = true)
        {
            if (!value.HasValue) return 0;
            return CalculateGradeScore((decimal)value.Value, 
                thresholds.Select(t => (decimal)t).ToArray(), higherIsBetter);
        }

        /// <summary>
        /// 获取等级（根据分数）
        /// </summary>
        protected string GetGrade(decimal score)
        {
            if (score >= 85) return "S";
            if (score >= 70) return "A";
            if (score >= 55) return "B";
            if (score >= 40) return "C";
            return "D";
        }

        /// <summary>
        /// 获取决策（根据分数和阈值）
        /// </summary>
        protected string GetDecision(decimal score, decimal goThreshold, decimal waitThreshold)
        {
            if (score >= goThreshold) return "GO";
            if (score >= waitThreshold) return "WAIT";
            return "STOP";
        }
    }
}

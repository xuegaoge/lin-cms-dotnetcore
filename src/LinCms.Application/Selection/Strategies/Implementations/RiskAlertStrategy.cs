using LinCms.Application.Selection.Config;
using LinCms.Application.Selection.Models;
using LinCms.Entities.Selection;
using System.Collections.Generic;

namespace LinCms.Application.Selection.Strategies.Implementations
{
    /// <summary>
    /// S04 - 36项风险预警策略
    /// </summary>
    public class RiskAlertStrategy : BaseStrategy
    {
        public override string Code => "S04";
        public override string Name => "36项风险预警";
        public override string Description => "全方位风险检测和预警";
        public override StrategyType Type => StrategyType.RiskDetection;

        public override IReadOnlyList<string> RequiredFields => new[]
        {
            "MonthlySearchVolume", "TopConcentration", "CompetitorCount",
            "TargetPrice", "PurchaseCost", "SupplierStability", "PolicyRisk"
        };

        public override string LogicDefinition => @"
### 策略定义
多维度风险预警系统，覆盖市场、财务、供应链、合规四大维度，旨在早期发现潜在雷区。

### 核心输入
*   **市场**: 搜索量, 垄断度, 竞品数
*   **财务**: 售价, 成本
*   **供应链**: 稳定性, 交期, MOQ
*   **合规**: 侵权风险, 政策风险

### 计算逻辑
1.  **风险检测**: 逐项检查指标是否触发红线（如垄断度>0.6, 毛利<0.2, 侵权=高）。
2.  **风险计数**: 统计低/中/高风险项数量。
3.  **评分公式**: 100 - (风险数*5) - (高风险数*15)。
4.  **决策**: 存在高风险项 -> STOP; 风险数>5 -> WAIT; 否则 GO。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            var result = new StrategyResult();
            var riskCount = 0;
            var highRiskCount = 0;

            // 市场风险检测
            CheckMarketRisks(product, result, ref riskCount, ref highRiskCount);

            // 财务风险检测
            CheckFinancialRisks(product, result, ref riskCount, ref highRiskCount);

            // 供应链风险检测
            CheckSupplyChainRisks(product, result, ref riskCount, ref highRiskCount);

            // 合规风险检测
            CheckComplianceRisks(product, result, ref riskCount, ref highRiskCount);

            // 计算风险等级
            if (highRiskCount > 0)
            {
                result.Grade = "高风险";
                result.Decision = "STOP";
                result.Reason = $"检测到{highRiskCount}个高风险项";
            }
            else if (riskCount > 5)
            {
                result.Grade = "中风险";
                result.Decision = "WAIT";
                result.Reason = $"检测到{riskCount}个风险项";
            }
            else if (riskCount > 0)
            {
                result.Grade = "低风险";
                result.Decision = "GO";
                result.Reason = $"检测到{riskCount}个低风险项，可控";
            }
            else
            {
                result.Grade = "无风险";
                result.Decision = "GO";
                result.Reason = "未检测到明显风险";
            }

            result.Score = 100 - (riskCount * 5) - (highRiskCount * 15);
            if (result.Score < 0) result.Score = 0;

            return result;
        }

        private void CheckMarketRisks(ProductData product, StrategyResult result, ref int riskCount, ref int highRiskCount)
        {
            // R01: 搜索量过低
            if (product.MonthlySearchVolume.HasValue && product.MonthlySearchVolume < StrategyConfig.RiskAlert.MinSearchVolume)
            {
                result.RiskAlerts.Add(new RiskAlertItem
                {
                    RiskCode = "R01",
                    RiskName = "搜索量过低",
                    RiskLevel = "中",
                    RiskType = "市场",
                    Description = $"月搜索量{product.MonthlySearchVolume}低于最低要求{StrategyConfig.RiskAlert.MinSearchVolume}",
                    TriggerValue = product.MonthlySearchVolume.ToString(),
                    ThresholdValue = StrategyConfig.RiskAlert.MinSearchVolume.ToString(),
                    Suggestions = new List<string> { "建议重新评估市场容量", "考虑更换产品类目" }
                });
                riskCount++;
            }

            // R02: 竞争过于激烈
            if (product.TopConcentration.HasValue && product.TopConcentration > StrategyConfig.RiskAlert.MaxConcentration)
            {
                result.RiskAlerts.Add(new RiskAlertItem
                {
                    RiskCode = "R02",
                    RiskName = "头部集中度过高",
                    RiskLevel = "高",
                    RiskType = "市场",
                    Description = $"头部集中度{product.TopConcentration:P}超过警戒线{StrategyConfig.RiskAlert.MaxConcentration:P}",
                    TriggerValue = product.TopConcentration.Value.ToString("P"),
                    ThresholdValue = StrategyConfig.RiskAlert.MaxConcentration.ToString("P"),
                    Suggestions = new List<string> { "市场被头部垄断，建议避开", "寻找细分市场机会" }
                });
                riskCount++;
                highRiskCount++;
            }

            // R03: 竞品数量过多
            if (product.CompetitorCount.HasValue && product.CompetitorCount > StrategyConfig.RiskAlert.MaxCompetitorCount)
            {
                result.RiskAlerts.Add(new RiskAlertItem
                {
                    RiskCode = "R03",
                    RiskName = "竞品数量过多",
                    RiskLevel = "中",
                    RiskType = "市场",
                    Description = $"竞品数量{product.CompetitorCount}超过警戒线{StrategyConfig.RiskAlert.MaxCompetitorCount}",
                    TriggerValue = product.CompetitorCount.ToString(),
                    ThresholdValue = StrategyConfig.RiskAlert.MaxCompetitorCount.ToString(),
                    Suggestions = new List<string> { "竞争激烈，需要强差异化", "考虑蓝海策略" }
                });
                riskCount++;
            }
        }

        private void CheckFinancialRisks(ProductData product, StrategyResult result, ref int riskCount, ref int highRiskCount)
        {
            // R11: 毛利率过低
            if (product.TargetPrice.HasValue && product.PurchaseCost.HasValue)
            {
                var totalCost = (product.PurchaseCost ?? 0) + (product.ShippingCost ?? 0) + (product.FBACost ?? 0);
                var margin = product.TargetPrice > 0 ? (product.TargetPrice.Value - totalCost) / product.TargetPrice.Value : 0;

                if (margin < StrategyConfig.RiskAlert.MinMargin)
                {
                    result.RiskAlerts.Add(new RiskAlertItem
                    {
                        RiskCode = "R11",
                        RiskName = "毛利率过低",
                        RiskLevel = "高",
                        RiskType = "财务",
                        Description = $"毛利率{margin:P}低于最低要求{StrategyConfig.RiskAlert.MinMargin:P}",
                        TriggerValue = margin.ToString("P"),
                        ThresholdValue = StrategyConfig.RiskAlert.MinMargin.ToString("P"),
                        Suggestions = new List<string> { "优化成本结构", "提高售价", "寻找更优质供应商" }
                    });
                    riskCount++;
                    highRiskCount++;
                }
            }

            // R12: 价格区间风险
            if (product.TargetPrice.HasValue)
            {
                if (product.TargetPrice < StrategyConfig.RiskAlert.MinPrice)
                {
                    result.RiskAlerts.Add(new RiskAlertItem
                    {
                        RiskCode = "R12",
                        RiskName = "售价过低",
                        RiskLevel = "中",
                        RiskType = "财务",
                        Description = $"售价${product.TargetPrice:F2}低于建议最低价${StrategyConfig.RiskAlert.MinPrice:F2}",
                        Suggestions = new List<string> { "低价产品利润空间小", "建议选择中高价位产品" }
                    });
                    riskCount++;
                }
                else if (product.TargetPrice > StrategyConfig.RiskAlert.MaxPrice)
                {
                    result.RiskAlerts.Add(new RiskAlertItem
                    {
                        RiskCode = "R13",
                        RiskName = "售价过高",
                        RiskLevel = "中",
                        RiskType = "财务",
                        Description = $"售价${product.TargetPrice:F2}高于建议最高价${StrategyConfig.RiskAlert.MaxPrice:F2}",
                        Suggestions = new List<string> { "高价产品市场容量小", "需要强品牌支撑" }
                    });
                    riskCount++;
                }
            }
        }

        private void CheckSupplyChainRisks(ProductData product, StrategyResult result, ref int riskCount, ref int highRiskCount)
        {
            // R21: 供应商稳定性差
            if (product.SupplierStability.HasValue && product.SupplierStability < StrategyConfig.RiskAlert.MinSupplierStability)
            {
                result.RiskAlerts.Add(new RiskAlertItem
                {
                    RiskCode = "R21",
                    RiskName = "供应商稳定性差",
                    RiskLevel = "高",
                    RiskType = "供应链",
                    Description = $"供应商稳定性{product.SupplierStability}分低于要求{StrategyConfig.RiskAlert.MinSupplierStability}分",
                    TriggerValue = product.SupplierStability.ToString(),
                    ThresholdValue = StrategyConfig.RiskAlert.MinSupplierStability.ToString(),
                    Suggestions = new List<string> { "更换更稳定的供应商", "建立备用供应商", "签订长期合作协议" }
                });
                riskCount++;
                highRiskCount++;
            }

            // R22: 交期过长
            if (product.LeadTimeDays.HasValue && product.LeadTimeDays > StrategyConfig.RiskAlert.MaxLeadTime)
            {
                result.RiskAlerts.Add(new RiskAlertItem
                {
                    RiskCode = "R22",
                    RiskName = "交期过长",
                    RiskLevel = "中",
                    RiskType = "供应链",
                    Description = $"交期{product.LeadTimeDays}天超过警戒线{StrategyConfig.RiskAlert.MaxLeadTime}天",
                    Suggestions = new List<string> { "缩短交期", "增加库存备货", "寻找本地供应商" }
                });
                riskCount++;
            }

            // R23: MOQ过高
            if (product.MOQ.HasValue && product.MOQ > StrategyConfig.RiskAlert.MaxMOQ)
            {
                result.RiskAlerts.Add(new RiskAlertItem
                {
                    RiskCode = "R23",
                    RiskName = "起订量过高",
                    RiskLevel = "中",
                    RiskType = "供应链",
                    Description = $"MOQ{product.MOQ}超过警戒线{StrategyConfig.RiskAlert.MaxMOQ}",
                    Suggestions = new List<string> { "协商降低MOQ", "寻找小批量供应商", "评估资金压力" }
                });
                riskCount++;
            }
        }

        private void CheckComplianceRisks(ProductData product, StrategyResult result, ref int riskCount, ref int highRiskCount)
        {
            // R31: 侵权风险
            if (product.InfringementRisk == "高")
            {
                result.RiskAlerts.Add(new RiskAlertItem
                {
                    RiskCode = "R31",
                    RiskName = "侵权风险高",
                    RiskLevel = "高",
                    RiskType = "合规",
                    Description = "产品存在高侵权风险",
                    Suggestions = new List<string> { "进行专利检索", "咨询法律顾问", "避免侵权产品" }
                });
                riskCount++;
                highRiskCount++;
            }

            // R32: 政策风险
            if (product.PolicyRisk.HasValue && product.PolicyRisk > StrategyConfig.RiskAlert.MaxPolicyRisk)
            {
                result.RiskAlerts.Add(new RiskAlertItem
                {
                    RiskCode = "R32",
                    RiskName = "政策风险高",
                    RiskLevel = "高",
                    RiskType = "合规",
                    Description = $"政策风险评分{product.PolicyRisk:P}超过警戒线{StrategyConfig.RiskAlert.MaxPolicyRisk:P}",
                    Suggestions = new List<string> { "关注政策变化", "准备应对方案", "考虑更换类目" }
                });
                riskCount++;
                highRiskCount++;
            }
        }
    }
}

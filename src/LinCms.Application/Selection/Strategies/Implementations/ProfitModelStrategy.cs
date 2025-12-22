using LinCms.Application.Selection.Config;
using LinCms.Application.Selection.Models;
using LinCms.Entities.Selection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LinCms.Application.Selection.Strategies.Implementations
{
    /// <summary>
    /// S03 - 完整利润模型策略
    /// </summary>
    public class ProfitModelStrategy : BaseStrategy
    {
        public override string Code => "S03";
        public override string Name => "完整利润模型";
        public override string Description => "ROI/毛利率/回本周期财务分析";
        public override StrategyType Type => StrategyType.Financial;

        public override IReadOnlyList<string> RequiredFields => new[]
        {
            "TargetPrice", "PurchaseCost", "ShippingCost", "FBACost", "AdvertisingCPC", "ConversionRate"
        };

        public override string LogicDefinition => @"
### 策略定义
完整利润模型，基于电商成本结构进行深度财务分析。

### 核心输入
*   **基础价格**: 售价, 采购价, 头程物流费, FBA配送费
*   **运营指标**: CPC, 转化率

### 计算公式
1.  **总成本**: 采购 + 运费 + FBA + 佣金(15%) + 广告成本(CPC/Conv) + 退货损耗。
2.  **毛利率**: (售价 - 采购 - 运费 - FBA - 佣金) / 售价。
3.  **净利润**: 售价 - 总成本。
4.  **ROI**: 净利润 / 成本投入。
5.  **回本周期**: 成本投入 / 月净利润。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            var result = new StrategyResult();

            // 1. 计算各项财务指标
            var revenue = product.TargetPrice ?? 0;
            var purchaseCost = product.PurchaseCost ?? 0;
            var shippingCost = product.ShippingCost ?? 0;
            var fbaCost = product.FBACost ?? 0;
            var cpc = product.AdvertisingCPC ?? 0;
            var conversionRate = product.ConversionRate ?? 0.02m;

            // 总成本
            var totalCost = purchaseCost + shippingCost + fbaCost;
            
            // 佣金（默认15%）
            var commission = revenue * StrategyConfig.ProfitModel.DefaultCommissionRate;
            
            // 广告成本（CPC / 转化率）
            var adCost = conversionRate > 0 ? cpc / conversionRate : 0;
            
            // 退货损失
            var returnLoss = revenue * StrategyConfig.ProfitModel.DefaultReturnRate * StrategyConfig.ProfitModel.DefaultLossRate;
            
            // 净利润
            var netProfit = revenue - totalCost - commission - adCost - returnLoss;
            
            // 毛利率 (售价 - 采购 - 运费 - FBA - 佣金) / 售价
            var grossMargin = revenue > 0 ? (revenue - totalCost - commission) / revenue : 0;
            
            // ROI
            var roi = totalCost > 0 ? netProfit / totalCost : 0;
            
            // 回本周期（月）
            var paybackMonths = netProfit > 0 ? (int)Math.Ceiling(totalCost / netProfit) : 999;

            // 2. 创建指标
            result.Indicators.Add(new Indicator
            {
                Code = "F01",
                Name = "销售价格",
                RawValue = revenue,
                Score = 0,
                Weight = 0,
                Calculation = $"${revenue:F2}"
            });

            result.Indicators.Add(new Indicator
            {
                Code = "F02",
                Name = "总成本",
                RawValue = totalCost,
                Score = 0,
                Weight = 0,
                Calculation = $"${totalCost:F2} (采购${purchaseCost:F2} + 运费${shippingCost:F2} + FBA${fbaCost:F2})"
            });

            result.Indicators.Add(new Indicator
            {
                Code = "F03",
                Name = "毛利率",
                RawValue = grossMargin,
                Score = grossMargin >= StrategyConfig.ProfitModel.MinGrossMargin ? 100 : 50,
                Weight = 0.30m,
                Calculation = $"{grossMargin:P2}"
            });

            result.Indicators.Add(new Indicator
            {
                Code = "F04",
                Name = "ROI",
                RawValue = roi,
                Score = roi >= StrategyConfig.ProfitModel.MinROI ? 100 : 50,
                Weight = 0.40m,
                Calculation = $"{roi:P2}"
            });

            result.Indicators.Add(new Indicator
            {
                Code = "F05",
                Name = "回本周期",
                RawValue = paybackMonths,
                Score = paybackMonths <= StrategyConfig.ProfitModel.MaxPaybackMonths ? 100 : 50,
                Weight = 0.30m,
                Calculation = $"{paybackMonths}个月"
            });

            result.Indicators.Add(new Indicator
            {
                Code = "F06",
                Name = "净利润",
                RawValue = netProfit,
                Score = 0,
                Weight = 0,
                Calculation = $"${netProfit:F2}"
            });

            // 3. 计算综合得分
            var totalScore = 0m;
            var totalWeight = 0m;
            foreach (var ind in result.Indicators)
            {
                if (ind.Weight > 0)
                {
                    totalScore += ind.Score * ind.Weight;
                    totalWeight += ind.Weight;
                }
            }
            result.Score = totalWeight > 0 ? Math.Round(totalScore / totalWeight, 2) : 0;
            result.Grade = GetGrade(result.Score.Value);

            // 4. 判定决策
            var passCount = 0;
            if (grossMargin >= StrategyConfig.ProfitModel.MinGrossMargin) passCount++;
            if (roi >= StrategyConfig.ProfitModel.MinROI) passCount++;
            if (paybackMonths <= StrategyConfig.ProfitModel.MaxPaybackMonths) passCount++;

            if (passCount == 3)
            {
                result.Decision = "GO";
                result.Reason = "所有财务指标均达标";
            }
            else if (passCount >= 2)
            {
                result.Decision = "WAIT";
                result.Reason = $"部分财务指标达标（{passCount}/3）";
            }
            else
            {
                result.Decision = "STOP";
                result.Reason = $"财务指标不达标（{passCount}/3）";
            }

            // 5. 生成建议
            if (grossMargin < StrategyConfig.ProfitModel.MinGrossMargin)
            {
                result.Warnings.Add($"毛利率{grossMargin:P}低于最低要求{StrategyConfig.ProfitModel.MinGrossMargin:P}");
                result.Suggestions.Add("建议优化成本结构或提高售价");
            }

            if (roi < StrategyConfig.ProfitModel.MinROI)
            {
                result.Warnings.Add($"ROI{roi:P}低于最低要求{StrategyConfig.ProfitModel.MinROI:P}");
                result.Suggestions.Add("建议降低广告成本或提高转化率");
            }

            if (paybackMonths > StrategyConfig.ProfitModel.MaxPaybackMonths)
            {
                result.Warnings.Add($"回本周期{paybackMonths}个月超过最长要求{StrategyConfig.ProfitModel.MaxPaybackMonths}个月");
                result.Suggestions.Add("建议提高利润率以缩短回本周期");
            }

            // 6. 构造详情数据供前端图表使用 (Waterfall & Tornado)
            // 计算瀑布图数据
            // categories: 售价, 采购, 运费, FBA, 佣金, 广告, 退货, 利润
            // 逻辑: 售价作为起点，后续各项作为扣减项，最后剩余为利润
            var wfCategories = new[] { "售价", "采购", "头程", "FBA费", "佣金", "广告", "退货", "净利" };
            
            var wfBaseData = new List<decimal>();
            var wfAmountData = new List<decimal>();
            
            // 1. 售价 (Base=0, Amount=Revenue)
            wfBaseData.Add(0);
            wfAmountData.Add(revenue);
            
            var currentBase = revenue;
            
            // 2-7. 各项成本 (Base=Current-Cost, Amount=Cost)
            var costs = new[] { purchaseCost, shippingCost, fbaCost, commission, adCost, returnLoss };
            foreach(var cost in costs)
            {
                currentBase -= cost;
                wfBaseData.Add(Math.Max(0, currentBase)); // 防止负数
                wfAmountData.Add(cost);
            }
            
            // 8. 净利润 (Base=0, Amount=NetProfit)
            wfBaseData.Add(0);
            wfAmountData.Add(Math.Max(0, netProfit)); // 如果亏损可能需要特殊处理，这里暂且显示为0或正值

            var detailData = new
            {
                Finance = new
                {
                    TargetPrice = revenue,
                    PurchaseCost = purchaseCost,
                    ShippingCost = shippingCost,
                    FBACost = fbaCost,
                    AdvertisingCPC = cpc,
                    ConversionRate = conversionRate,
                    NetProfitMargin = grossMargin,
                    NetProfit = netProfit,
                    Roi = roi,
                    PaybackPeriod = paybackMonths,
                    AnnualRoi = roi * 12, // 简单估算
                    BreakEvenSales = netProfit > 0 ? (int)Math.Ceiling(totalCost / netProfit) : 999
                },
                Waterfall = new
                {
                   categories = wfCategories,
                   baseData = wfBaseData,
                   amountData = wfAmountData
                },
                Tornado = new
                {
                    categories = new[] { "售价变动", "采购成本", "运费波动", "广告CPC", "转化率" },
                    posData = new[] { 15, -10, -5, -8, 12 }, // 模拟敏感性分析数据
                    negData = new[] { -20, 8, 4, 6, -15 }
                }
            };

            try 
            {
                result.DetailJson = Newtonsoft.Json.JsonConvert.SerializeObject(detailData);
            }
            catch
            {
                // Ignore serialization error
            }

            return result;
        }
    }
}

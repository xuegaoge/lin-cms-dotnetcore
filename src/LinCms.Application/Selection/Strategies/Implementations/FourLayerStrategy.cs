using LinCms.Application.Selection.Config;
using LinCms.Application.Selection.Models;
using LinCms.Entities.Selection;
using System;
using System.Collections.Generic;

namespace LinCms.Application.Selection.Strategies.Implementations
{
    /// <summary>
    /// S01 - 四层评估体系策略
    /// </summary>
    public class FourLayerStrategy : BaseStrategy
    {
        public override string Code => "S01";
        public override string Name => "四层评估体系";
        public override string Description => "市场层/产品层/运营层/财务层四维度综合评估";
        public override StrategyType Type => StrategyType.Scoring;

        public override IReadOnlyList<string> RequiredFields => new[]
        {
            "MonthlySearchVolume", "SearchGrowthRate", "CompetitorCount", "TopConcentration",
            "TargetPrice", "PurchaseCost", "FBACost", "AdvertisingCPC", "ConversionRate",
            "SupplierStability", "ReturnRate"
        };

        public override string LogicDefinition => @"
### 策略定义
四层评估体系是初步筛选的核心策略，涵盖市场、产品、运营、财务四个维度。

### 核心输入
*   **市场数据**: 月搜索量, 增长率, 竞品数, 垄断度
*   **产品数据**: 售价, 采购成本, FBA费
*   **运营数据**: CPC, 转化率
*   **风险数据**: 供应商稳定性, 退货率

### 计算公式
1.  **各层得分**: 将原始数据映射到 0-100 分区间。
2.  **加权总分**: 市场分*Weight + 产品分*Weight + 运营分*Weight + 财务分*Weight。
3.  **风险调整**: 根据供应商稳定性和退货率对总分进行 0.9~0.95 的折扣。
4.  **红线检查**: 财务分过低或 ROI 过低直接一票否决(STOP)。";

        protected override StrategyResult ExecuteCore(ProductData product, ExecutionContext context)
        {
            var result = new StrategyResult();

            // 1. 计算四层得分
            var marketResult = CalculateMarketLayer(product);
            var productResult = CalculateProductLayer(product);
            var operationResult = CalculateOperationLayer(product);
            var financialResult = CalculateFinancialLayer(product);

            result.SubResults.Add(marketResult);
            result.SubResults.Add(productResult);
            result.SubResults.Add(operationResult);
            result.SubResults.Add(financialResult);

            // 2. 加权总分
            var totalScore = 
                marketResult.Score * StrategyConfig.FourLayer.MarketWeight +
                productResult.Score * StrategyConfig.FourLayer.ProductWeight +
                operationResult.Score * StrategyConfig.FourLayer.OperationWeight +
                financialResult.Score * StrategyConfig.FourLayer.FinancialWeight;

            // 3. 风险调整
            var riskAdjustment = CalculateRiskAdjustment(product);
            var adjustedScore = totalScore * riskAdjustment;

            result.Score = Math.Round(adjustedScore, 2);
            result.Grade = GetGrade(adjustedScore);

            // 4. 红线检查
            if (CheckRedLines(product, financialResult, out string redLineReason))
            {
                result.Decision = "STOP";
                result.Reason = redLineReason;
                result.Warnings.Add(redLineReason);
                return result;
            }

            // 5. 判定决策
            var enterpriseGrade = context.EnterpriseProfile?.Grade ?? "C";
            var threshold = StrategyConfig.FourLayer.GoThreshold.ContainsKey(enterpriseGrade) 
                ? StrategyConfig.FourLayer.GoThreshold[enterpriseGrade] 
                : 75;

            result.Decision = GetDecision(adjustedScore, threshold, threshold - 10);
            result.Reason = $"综合评分{adjustedScore:F1}分，{enterpriseGrade}级企业阈值{threshold}分";

            // 6. 生成建议
            GenerateSuggestions(result, marketResult, productResult, operationResult, financialResult);

            return result;
        }

        private SubResult CalculateMarketLayer(ProductData product)
        {
            var indicators = new List<Indicator>();

            // M01: 搜索量
            var searchScore = CalculateGradeScore(product.MonthlySearchVolume, 
                StrategyConfig.FourLayer.MarketLayer.SearchVolumeGrades) * 10;
            indicators.Add(new Indicator
            {
                Code = "M01",
                Name = "月搜索量",
                RawValue = product.MonthlySearchVolume,
                Score = searchScore,
                Weight = 0.18m
            });

            // M02: 增长率
            var growthScore = CalculateGradeScore(product.SearchGrowthRate, 
                StrategyConfig.FourLayer.MarketLayer.GrowthRateGrades) * 10;
            indicators.Add(new Indicator
            {
                Code = "M02",
                Name = "搜索增长率",
                RawValue = product.SearchGrowthRate,
                Score = growthScore,
                Weight = 0.14m
            });

            // M03: 竞争度（越低越好）
            var concentrationScore = CalculateGradeScore(product.TopConcentration, 
                StrategyConfig.FourLayer.MarketLayer.ConcentrationGrades, false) * 10;
            indicators.Add(new Indicator
            {
                Code = "M03",
                Name = "头部集中度",
                RawValue = product.TopConcentration,
                Score = concentrationScore,
                Weight = 0.12m
            });

            // M04: 竞品数量（越少越好）
            var compScore = product.CompetitorCount < 100 ? 90 :
                           product.CompetitorCount < 300 ? 75 :
                           product.CompetitorCount < 500 ? 60 : 40;
            indicators.Add(new Indicator
            {
                Code = "M04",
                Name = "竞品数量",
                RawValue = product.CompetitorCount,
                Score = compScore,
                Weight = 0.12m
            });

            // M05: 新品占比（越高越友好）
            var newProductScore = product.NewProductRatio >= 0.30m ? 90 :
                                  product.NewProductRatio >= 0.20m ? 75 :
                                  product.NewProductRatio >= 0.10m ? 60 : 40;
            indicators.Add(new Indicator
            {
                Code = "M05",
                Name = "新品友好度",
                RawValue = product.NewProductRatio,
                Score = newProductScore,
                Weight = 0.12m
            });

            // M06: 季节性（越低越稳定）
            var seasonScore = product.Seasonality < 0.3m ? 90 :
                             product.Seasonality < 0.5m ? 75 :
                             product.Seasonality < 0.7m ? 60 : 40;
            indicators.Add(new Indicator
            {
                Code = "M06",
                Name = "季节性风险",
                RawValue = product.Seasonality,
                Score = seasonScore,
                Weight = 0.10m
            });

            // M07: 政策风险（越低越好）
            var policyScore = product.PolicyRisk < 0.3m ? 90 :
                             product.PolicyRisk < 0.5m ? 70 :
                             product.PolicyRisk < 0.7m ? 50 : 20;
            indicators.Add(new Indicator
            {
                Code = "M07",
                Name = "政策风险",
                RawValue = product.PolicyRisk,
                Score = policyScore,
                Weight = 0.10m
            });

            // M08: SPR供需比（行业核心指标）
            var spr = product.CompetitorCount > 0 
                ? (decimal)(product.MonthlySearchVolume ?? 0) / product.CompetitorCount * 1000 
                : 0;
            var sprScore = spr >= 300 ? 90 :   // 蓝海
                          spr >= 200 ? 75 :
                          spr >= 100 ? 60 :
                          spr >= 50 ? 45 : 30; // 红海
            indicators.Add(new Indicator
            {
                Code = "M08",
                Name = "供需比SPR",
                RawValue = spr,
                Score = sprScore,
                Weight = 0.12m
            });

            var totalScore = 0m;
            var totalWeight = 0m;
            foreach (var ind in indicators)
            {
                totalScore += ind.Score * ind.Weight;
                totalWeight += ind.Weight;
            }
            var layerScore = totalWeight > 0 ? totalScore / totalWeight : 0;

            return new SubResult
            {
                Name = "市场层(M)",
                Score = Math.Round(layerScore, 2),
                Weight = StrategyConfig.FourLayer.MarketWeight,
                WeightedScore = Math.Round(layerScore * StrategyConfig.FourLayer.MarketWeight, 2),
                Grade = GetGrade(layerScore),
                Indicators = indicators
            };
        }

        private SubResult CalculateProductLayer(ProductData product)
        {
            var indicators = new List<Indicator>();

            // P01: 毛利率
            var margin = CalculateMargin(product);
            var marginScore = CalculateGradeScore(margin, 
                StrategyConfig.FourLayer.ProductLayer.MarginGrades) * 10;
            indicators.Add(new Indicator
            {
                Code = "P01",
                Name = "毛利率",
                RawValue = margin,
                Score = marginScore,
                Weight = 0.30m
            });

            // P02: 差异化
            var diffScore = (product.DifferentiationPoints ?? 0) * 10m;
            indicators.Add(new Indicator
            {
                Code = "P02",
                Name = "差异化卖点",
                RawValue = product.DifferentiationPoints,
                Score = Math.Min(diffScore, 100),
                Weight = 0.20m
            });

            var totalScore = 0m;
            var totalWeight = 0m;
            foreach (var ind in indicators)
            {
                totalScore += ind.Score * ind.Weight;
                totalWeight += ind.Weight;
            }
            var layerScore = totalWeight > 0 ? totalScore / totalWeight : 0;

            return new SubResult
            {
                Name = "产品层(P)",
                Score = Math.Round(layerScore, 2),
                Weight = StrategyConfig.FourLayer.ProductWeight,
                WeightedScore = Math.Round(layerScore * StrategyConfig.FourLayer.ProductWeight, 2),
                Grade = GetGrade(layerScore),
                Indicators = indicators
            };
        }

        private SubResult CalculateOperationLayer(ProductData product)
        {
            var indicators = new List<Indicator>();

            // O01: CPC（越低越好）
            var cpcScore = CalculateGradeScore(product.AdvertisingCPC, 
                StrategyConfig.FourLayer.OperationLayer.CPCGrades, false) * 10;
            indicators.Add(new Indicator
            {
                Code = "O01",
                Name = "广告CPC",
                RawValue = product.AdvertisingCPC,
                Score = cpcScore,
                Weight = 0.25m
            });

            // O02: 转化率
            var conversionScore = CalculateGradeScore(product.ConversionRate, 
                StrategyConfig.FourLayer.OperationLayer.ConversionGrades) * 10;
            indicators.Add(new Indicator
            {
                Code = "O02",
                Name = "转化率",
                RawValue = product.ConversionRate,
                Score = conversionScore,
                Weight = 0.25m
            });

            var totalScore = 0m;
            var totalWeight = 0m;
            foreach (var ind in indicators)
            {
                totalScore += ind.Score * ind.Weight;
                totalWeight += ind.Weight;
            }
            var layerScore = totalWeight > 0 ? totalScore / totalWeight : 0;

            return new SubResult
            {
                Name = "运营层(O)",
                Score = Math.Round(layerScore, 2),
                Weight = StrategyConfig.FourLayer.OperationWeight,
                WeightedScore = Math.Round(layerScore * StrategyConfig.FourLayer.OperationWeight, 2),
                Grade = GetGrade(layerScore),
                Indicators = indicators
            };
        }

        private SubResult CalculateFinancialLayer(ProductData product)
        {
            var indicators = new List<Indicator>();

            // F01: ROI
            var roi = CalculateROI(product);
            var roiScore = CalculateGradeScore(roi, 
                StrategyConfig.FourLayer.FinancialLayer.ROIGrades) * 10;
            indicators.Add(new Indicator
            {
                Code = "F01",
                Name = "ROI",
                RawValue = roi,
                Score = roiScore,
                Weight = 0.40m
            });

            var totalScore = 0m;
            var totalWeight = 0m;
            foreach (var ind in indicators)
            {
                totalScore += ind.Score * ind.Weight;
                totalWeight += ind.Weight;
            }
            var layerScore = totalWeight > 0 ? totalScore / totalWeight : 0;

            return new SubResult
            {
                Name = "财务层(F)",
                Score = Math.Round(layerScore, 2),
                Weight = StrategyConfig.FourLayer.FinancialWeight,
                WeightedScore = Math.Round(layerScore * StrategyConfig.FourLayer.FinancialWeight, 2),
                Grade = GetGrade(layerScore),
                Indicators = indicators
            };
        }

        private decimal CalculateRiskAdjustment(ProductData product)
        {
            var adjustment = 1.0m;

            // 供应链风险
            if (product.SupplierStability.HasValue && product.SupplierStability < 60)
                adjustment *= 0.95m;

            // 退货率风险
            if (product.ReturnRate.HasValue && product.ReturnRate > 0.10m)
                adjustment *= 0.90m;

            return adjustment;
        }

        private bool CheckRedLines(ProductData product, SubResult financialResult, out string reason)
        {
            // 财务红线
            if (financialResult.Score < StrategyConfig.FourLayer.FinancialScoreRedLine)
            {
                reason = $"触发财务红线：财务层得分{financialResult.Score}分，低于{StrategyConfig.FourLayer.FinancialScoreRedLine}分";
                return true;
            }

            // ROI红线
            var roi = CalculateROI(product);
            if (roi < StrategyConfig.FourLayer.ROIRedLine)
            {
                reason = $"触发ROI红线：ROI{roi:P}，低于{StrategyConfig.FourLayer.ROIRedLine:P}";
                return true;
            }

            reason = null;
            return false;
        }

        private decimal CalculateMargin(ProductData product)
        {
            if (!product.TargetPrice.HasValue || !product.PurchaseCost.HasValue)
                return 0;

            var totalCost = (product.PurchaseCost ?? 0) + (product.ShippingCost ?? 0) + (product.FBACost ?? 0);
            return product.TargetPrice > 0 ? (product.TargetPrice.Value - totalCost) / product.TargetPrice.Value : 0;
        }

        private decimal CalculateROI(ProductData product)
        {
            if (!product.TargetPrice.HasValue || !product.PurchaseCost.HasValue)
                return 0;

            var revenue = product.TargetPrice.Value;
            var cost = (product.PurchaseCost ?? 0) + (product.ShippingCost ?? 0) + (product.FBACost ?? 0);
            var profit = revenue - cost - (revenue * 0.15m); // 扣除佣金

            return cost > 0 ? profit / cost : 0;
        }

        private void GenerateSuggestions(StrategyResult result, SubResult market, SubResult product, SubResult operation, SubResult financial)
        {
            if (market.Score < 60)
                result.Suggestions.Add("市场层得分较低，建议重新评估市场容量和竞争态势");

            if (product.Score < 60)
                result.Suggestions.Add("产品层得分较低，建议增强产品差异化");

            if (operation.Score < 60)
                result.Suggestions.Add("运营层得分较低，建议优化广告投放和转化率");

            if (financial.Score < 60)
                result.Suggestions.Add("财务层得分较低，建议优化成本结构");
        }
    }
}

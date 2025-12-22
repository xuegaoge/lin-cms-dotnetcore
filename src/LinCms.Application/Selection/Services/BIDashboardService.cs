using AutoMapper;
using FreeSql;
using IGeekFan.FreeKit.Extras.FreeSql;
using LinCms.Application.Contracts.Selection;
using LinCms.Entities.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinCms.Application.Selection.Services
{
    /// <summary>
    /// BI仪表板服务
    /// </summary>
    public class BIDashboardService
    {
        private readonly IAuditBaseRepository<ProductData> _productRepository;
        private readonly IAuditBaseRepository<StrategyExecution> _executionRepository;
        private readonly IAuditBaseRepository<RiskAlert> _alertRepository;

        public BIDashboardService(
            IAuditBaseRepository<ProductData> productRepository,
            IAuditBaseRepository<StrategyExecution> executionRepository,
            IAuditBaseRepository<RiskAlert> alertRepository)
        {
            _productRepository = productRepository;
            _executionRepository = executionRepository;
            _alertRepository = alertRepository;
        }

        /// <summary>
        /// 获取BI仪表板数据
        /// </summary>
        public async Task<BIDashboardDto> GetDashboardDataAsync()
        {
            var products = await _productRepository.Select.ToListAsync();
            var executions = await _executionRepository.Select.Where(e => e.IsLatest).ToListAsync();
            var alerts = await _alertRepository.Select.OrderByDescending(a => a.DetectedAt).Take(10).ToListAsync();

            var strategyDistribution = executions.GroupBy(e => e.StrategyCode)
                .ToDictionary(g => g.Key, g => g.Count());

            return new BIDashboardDto
            {
                TotalProducts = products.Count,
                ActiveProducts = products.Count(p => !string.IsNullOrEmpty(p.Status)),
                AverageScore = executions.Any() ? executions.Average(e => e.Score ?? 0) : 0,
                HighRiskCount = alerts.Count(a => a.RiskLevel == "高"),
                TopProducts = await GetTopProductsAsync(products, executions),
                RecentAlerts = alerts.Select(a => new AlertDto
                {
                    Id = a.Id,
                    ProductId = a.ProductId,
                    ProductName = products.FirstOrDefault(p => p.Id == a.ProductId)?.ProductName,
                    AlertType = a.RiskType,
                    Severity = a.RiskLevel,
                    Message = a.RiskName + ": " + a.Description,
                    CreatedAt = a.DetectedAt
                }).ToList(),
                StrategyDistribution = strategyDistribution
            };
        }

        /// <summary>
        /// 获取单个产品KPI
        /// </summary>
        public async Task<ProductKPIDto> GetProductKPIAsync(long productId)
        {
            var product = await _productRepository.Select.Where(p => p.Id == productId).FirstAsync();
            if (product == null) return null;

            var executions = await _executionRepository.Select
                .Where(e => e.ProductId == productId && e.IsLatest)
                .ToListAsync();

            var estimatedProfit = (product.EstimatedMonthlySales ?? 0) * 
                                 ((product.TargetPrice ?? 0) - (product.PurchaseCost ?? 0) - 
                                  (product.ShippingCost ?? 0) - (product.FBACost ?? 0));

            var roi = product.PurchaseCost > 0 
                ? ((product.TargetPrice ?? 0) - (product.PurchaseCost ?? 0)) / product.PurchaseCost 
                : null;

            return new ProductKPIDto
            {
                ProductId = product.Id,
                ProductName = product.ProductName,
                TotalScore = executions.Any() ? executions.Average(e => e.Score) : null,
                EstimatedProfit = estimatedProfit,
                ROI = roi,
                RiskLevel = DetermineRiskLevel(product, executions),
                StrategyCount = executions.Count
            };
        }

        /// <summary>
        /// 获取预警列表
        /// </summary>
        public async Task<List<AlertDto>> GetAlertsAsync(int page = 1, int size = 20)
        {
            var alerts = await _alertRepository.Select
                .OrderByDescending(a => a.DetectedAt)
                .Page(page, size)
                .ToListAsync();

            var productIds = alerts.Select(a => a.ProductId).Distinct().ToList();
            var products = await _productRepository.Select.Where(p => productIds.Contains(p.Id)).ToListAsync();

            return alerts.Select(a => new AlertDto
            {
                Id = a.Id,
                ProductId = a.ProductId,
                ProductName = products.FirstOrDefault(p => p.Id == a.ProductId)?.ProductName,
                AlertType = a.RiskType,
                Severity = a.RiskLevel,
                Message = a.RiskName + ": " + a.Description,
                CreatedAt = a.DetectedAt
            }).ToList();
        }

        #region 私有方法

        private async Task<List<ProductKPIDto>> GetTopProductsAsync(List<ProductData> products, List<StrategyExecution> executions)
        {
            return products
                .OrderByDescending(p => executions.Where(e => e.ProductId == p.Id).Average(e => (decimal?)e.Score) ?? 0)
                .Take(5)
                .Select(p =>
                {
                    var productExecutions = executions.Where(e => e.ProductId == p.Id).ToList();
                    var estimatedProfit = (p.EstimatedMonthlySales ?? 0) * 
                                         ((p.TargetPrice ?? 0) - (p.PurchaseCost ?? 0));

                    return new ProductKPIDto
                    {
                        ProductId = p.Id,
                        ProductName = p.ProductName,
                        TotalScore = productExecutions.Any() ? productExecutions.Average(e => e.Score) : null,
                        EstimatedProfit = estimatedProfit,
                        RiskLevel = DetermineRiskLevel(p, productExecutions),
                        StrategyCount = productExecutions.Count
                    };
                })
                .ToList();
        }

        private string DetermineRiskLevel(ProductData product, List<StrategyExecution> executions)
        {
            var avgScore = executions.Any() ? executions.Average(e => e.Score ?? 0) : 0;
            
            if (avgScore >= 80) return "low";
            if (avgScore >= 60) return "medium";
            return "high";
        }

        #endregion
    }
}

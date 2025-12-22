using System;
using System.Collections.Generic;

namespace LinCms.Application.Contracts.Selection
{
    /// <summary>
    /// BI仪表板DTO
    /// </summary>
    public class BIDashboardDto
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public decimal AverageScore { get; set; }
        public int HighRiskCount { get; set; }
        public List<ProductKPIDto> TopProducts { get; set; }
        public List<AlertDto> RecentAlerts { get; set; }
        public Dictionary<string, int> StrategyDistribution { get; set; }
    }

    /// <summary>
    /// 产品KPI DTO
    /// </summary>
    public class ProductKPIDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal? TotalScore { get; set; }
        public decimal? EstimatedProfit { get; set; }
        public decimal? ROI { get; set; }
        public string RiskLevel { get; set; }
        public int StrategyCount { get; set; }
    }

    /// <summary>
    /// 预警DTO
    /// </summary>
    public class AlertDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public string AlertType { get; set; }
        public string Severity { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

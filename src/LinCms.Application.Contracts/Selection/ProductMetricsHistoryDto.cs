using System;
using System.Collections.Generic;

namespace LinCms.Application.Contracts.Selection
{
    /// <summary>
    /// 添加历史数据DTO
    /// </summary>
    public class AddMetricsHistoryDto
    {
        public DateTime MetricDate { get; set; }
        public int? DailySales { get; set; }
        public decimal? AveragePrice { get; set; }
        public int? SearchVolume { get; set; }
        public int? BSRRank { get; set; }
        public decimal? AverageRating { get; set; }
        public int? ReviewCount { get; set; }
        public decimal? ConversionRate { get; set; }
        public decimal? ACOS { get; set; }
        public decimal? ClickThroughRate { get; set; }
    }

    /// <summary>
    /// 产品指标历史DTO
    /// </summary>
    public class ProductMetricsHistoryDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public DateTime MetricDate { get; set; }
        public int? DailySales { get; set; }
        public decimal? AveragePrice { get; set; }
        public int? SearchVolume { get; set; }
        public int? BSRRank { get; set; }
        public decimal? AverageRating { get; set; }
        public int? ReviewCount { get; set; }
        public decimal? ConversionRate { get; set; }
        public decimal? ACOS { get; set; }
        public decimal? ClickThroughRate { get; set; }
    }

    /// <summary>
    /// 趋势分析结果DTO
    /// </summary>
    public class TrendAnalysisDto
    {
        public string ProductName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DataPoints { get; set; }
        
        public TrendMetric SalesTrend { get; set; }
        public TrendMetric PriceTrend { get; set; }
        public TrendMetric SearchTrend { get; set; }
        public TrendMetric RatingTrend { get; set; }
        
        public List<string> Insights { get; set; }
        public List<string> Warnings { get; set; }
    }

    /// <summary>
    /// 趋势指标
    /// </summary>
    public class TrendMetric
    {
        public string MetricName { get; set; }
        public decimal? CurrentValue { get; set; }
        public decimal? PreviousValue { get; set; }
        public decimal? ChangeRate { get; set; } // 变化率
        public string Trend { get; set; } // 上升/下降/稳定
        public List<DataPoint> History { get; set; }
    }

    /// <summary>
    /// 数据点
    /// </summary>
    public class DataPoint
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
    }
}

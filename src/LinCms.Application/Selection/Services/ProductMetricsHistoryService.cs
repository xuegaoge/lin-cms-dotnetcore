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
    /// 产品指标历史服务
    /// </summary>
    public class ProductMetricsHistoryService
    {
        private readonly IAuditBaseRepository<ProductMetricsHistory> _historyRepository;
        private readonly IAuditBaseRepository<ProductData> _productRepository;
        private readonly IMapper _mapper;

        public ProductMetricsHistoryService(
            IAuditBaseRepository<ProductMetricsHistory> historyRepository,
            IAuditBaseRepository<ProductData> productRepository,
            IMapper mapper)
        {
            _historyRepository = historyRepository;
            _productRepository = productRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// 添加历史数据
        /// </summary>
        public async Task<ProductMetricsHistoryDto> AddMetricsAsync(long productId, AddMetricsHistoryDto dto)
        {
            var metrics = new ProductMetricsHistory
            {
                ProductId = productId,
                MetricDate = dto.MetricDate,
                DailySales = dto.DailySales,
                AveragePrice = dto.AveragePrice,
                SearchVolume = dto.SearchVolume,
                BSRRank = dto.BSRRank,
                AverageRating = dto.AverageRating,
                ReviewCount = dto.ReviewCount,
                ConversionRate = dto.ConversionRate,
                ACOS = dto.ACOS,
                ClickThroughRate = dto.ClickThroughRate
            };

            await _historyRepository.InsertAsync(metrics);
            return _mapper.Map<ProductMetricsHistoryDto>(metrics);
        }

        /// <summary>
        /// 获取历史数据
        /// </summary>
        public async Task<List<ProductMetricsHistoryDto>> GetHistoryAsync(long productId, DateTime? start, DateTime? end, int page = 1, int size = 100)
        {
            var query = _historyRepository.Select.Where(h => h.ProductId == productId);
            
            if (start.HasValue) query = query.Where(h => h.MetricDate >= start.Value);
            if (end.HasValue) query = query.Where(h => h.MetricDate <= end.Value);
            
            var list = await query
                .OrderBy(h => h.MetricDate)
                .Page(page, size)
                .ToListAsync();

            return _mapper.Map<List<ProductMetricsHistoryDto>>(list);
        }

        /// <summary>
        /// 获取趋势分析
        /// </summary>
        public async Task<TrendAnalysisDto> GetTrendAnalysisAsync(long productId, int days = 30)
        {
            var product = await _productRepository.Select.Where(p => p.Id == productId).FirstAsync();
            if (product == null) throw new Exception("产品不存在");

            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-days);
            
            var history = await _historyRepository.Select
                .Where(h => h.ProductId == productId && h.MetricDate >= startDate && h.MetricDate <= endDate)
                .OrderBy(h => h.MetricDate)
                .ToListAsync();

            if (!history.Any())
            {
                return new TrendAnalysisDto 
                { 
                    ProductName = product.ProductName,
                    StartDate = startDate,
                    EndDate = endDate,
                    DataPoints = 0,
                    Insights = new List<string> { "暂无历史数据" },
                    Warnings = new List<string>()
                };
            }

            return new TrendAnalysisDto
            {
                ProductName = product.ProductName,
                StartDate = startDate,
                EndDate = endDate,
                DataPoints = history.Count,
                SalesTrend = CalculateTrend("销量", history.Where(h => h.DailySales.HasValue)
                    .Select(h => new DataPoint { Date = h.MetricDate, Value = h.DailySales.Value }).ToList()),
                PriceTrend = CalculateTrend("价格", history.Where(h => h.AveragePrice.HasValue)
                    .Select(h => new DataPoint { Date = h.MetricDate, Value = h.AveragePrice.Value }).ToList()),
                SearchTrend = CalculateTrend("搜索量", history.Where(h => h.SearchVolume.HasValue)
                    .Select(h => new DataPoint { Date = h.MetricDate, Value = h.SearchVolume.Value }).ToList()),
                RatingTrend = CalculateTrend("评分", history.Where(h => h.AverageRating.HasValue)
                    .Select(h => new DataPoint { Date = h.MetricDate, Value = h.AverageRating.Value }).ToList()),
                Insights = GenerateInsights(history),
                Warnings = GenerateWarnings(history)
            };
        }

        /// <summary>
        /// 批量添加历史数据
        /// </summary>
        public async Task<bool> BatchAddMetricsAsync(long productId, List<AddMetricsHistoryDto> list)
        {
            var entities = list.Select(dto => new ProductMetricsHistory
            {
                ProductId = productId,
                MetricDate = dto.MetricDate,
                DailySales = dto.DailySales,
                AveragePrice = dto.AveragePrice,
                SearchVolume = dto.SearchVolume,
                BSRRank = dto.BSRRank,
                AverageRating = dto.AverageRating,
                ReviewCount = dto.ReviewCount,
                ConversionRate = dto.ConversionRate,
                ACOS = dto.ACOS,
                ClickThroughRate = dto.ClickThroughRate
            }).ToList();

            await _historyRepository.InsertAsync(entities);
            return true;
        }

        #region 私有方法

        /// <summary>
        /// 计算趋势指标
        /// </summary>
        private TrendMetric CalculateTrend(string name, List<DataPoint> data)
        {
            if (!data.Any())
            {
                return new TrendMetric 
                { 
                    MetricName = name,
                    Trend = "无数据",
                    History = new List<DataPoint>()
                };
            }

            var current = data.Last().Value;
            var previous = data.Count > 1 ? data[data.Count - 2].Value : current;
            var changeRate = previous != 0 ? ((current - previous) / previous) * 100 : 0;

            return new TrendMetric
            {
                MetricName = name,
                CurrentValue = current,
                PreviousValue = previous,
                ChangeRate = changeRate,
                Trend = changeRate > 5 ? "上升" : changeRate < -5 ? "下降" : "稳定",
                History = data
            };
        }

        /// <summary>
        /// 生成洞察
        /// </summary>
        private List<string> GenerateInsights(List<ProductMetricsHistory> history)
        {
            var insights = new List<string>();
            if (history.Count < 2) return insights;

            var latest = history.Last();
            var previous = history[history.Count - 2];

            // 销量分析
            if (latest.DailySales.HasValue && previous.DailySales.HasValue)
            {
                if (latest.DailySales > previous.DailySales * 1.5m)
                    insights.Add("💡 销量出现显著增长（+50%以上），可能存在爆款潜力");
                else if (latest.DailySales > previous.DailySales * 1.2m)
                    insights.Add("📈 销量稳步增长，市场反响良好");
            }

            // 评分分析
            if (latest.AverageRating.HasValue && latest.ReviewCount.HasValue)
            {
                if (latest.AverageRating > 4.5m && latest.ReviewCount > 100)
                    insights.Add("⭐ 产品口碑优秀（评分>4.5，评论>100），适合加大推广力度");
            }

            // 搜索量分析
            if (latest.SearchVolume.HasValue && previous.SearchVolume.HasValue)
            {
                if (latest.SearchVolume > previous.SearchVolume * 1.3m)
                    insights.Add("🔥 搜索热度上升，市场需求增长");
            }

            // 转化率分析
            if (latest.ConversionRate.HasValue && latest.ConversionRate > 0.03m)
            {
                insights.Add("✅ 转化率表现优秀（>3%），产品与市场匹配度高");
            }

            return insights;
        }

        /// <summary>
        /// 生成预警
        /// </summary>
        private List<string> GenerateWarnings(List<ProductMetricsHistory> history)
        {
            var warnings = new List<string>();
            if (history.Count < 2) return warnings;

            var latest = history.Last();
            var previous = history[history.Count - 2];

            // 销量预警
            if (latest.DailySales.HasValue && latest.DailySales < 10)
                warnings.Add("⚠️ 销量持续低迷（<10单/天），需要检查产品定位和推广策略");
            else if (latest.DailySales.HasValue && previous.DailySales.HasValue && 
                     latest.DailySales < previous.DailySales * 0.7m)
                warnings.Add("⚠️ 销量下降超过30%，需要分析原因");

            // 评分预警
            if (latest.AverageRating.HasValue && latest.AverageRating < 3.5m)
                warnings.Add("⚠️ 评分偏低（<3.5），需要改进产品质量或客户服务");

            // ACOS预警
            if (latest.ACOS.HasValue && latest.ACOS > 0.3m)
                warnings.Add("⚠️ 广告成本过高（ACOS>30%），需要优化广告投放");

            // BSR预警
            if (latest.BSRRank.HasValue && latest.BSRRank > 10000)
                warnings.Add("⚠️ BSR排名较低（>10000），竞争力不足");

            return warnings;
        }

        #endregion
    }
}

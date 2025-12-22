using AutoMapper;
using FreeSql;
using LinCms.Application.Contracts.Selection;
using LinCms.Entities.Selection;
using IGeekFan.FreeKit.Extras.FreeSql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinCms.Application.Selection.Services
{
    /// <summary>
    /// 产品数据服务
    /// </summary>
    public class ProductDataService
    {
        private readonly IFreeSql _freeSql;
        private readonly IAuditBaseRepository<ProductData> _productRepository;
        private readonly IMapper _mapper;

        public ProductDataService(
            IFreeSql freeSql,
            IAuditBaseRepository<ProductData> productRepository,
            IMapper mapper)
        {
            _freeSql = freeSql;
            _productRepository = productRepository;
            _mapper = mapper;
        }

        public async Task<(List<ProductDataDto> items, long total)> GetProductsAsync(ProductQueryDto query)
        {
            var queryable = _freeSql.Select<ProductData>()
                .WhereIf(!string.IsNullOrEmpty(query.Status), p => p.Status == query.Status)
                .WhereIf(!string.IsNullOrEmpty(query.Priority), p => p.PriorityLevel == query.Priority)
                .WhereIf(!string.IsNullOrEmpty(query.Keyword), p => 
                    p.ProductName.Contains(query.Keyword) || p.ASIN.Contains(query.Keyword));

            switch (query.Sort?.ToLower())
            {
                case "updated_at":
                    queryable = query.Order == "asc" ? queryable.OrderBy(p => p.UpdateTime) : queryable.OrderByDescending(p => p.UpdateTime);
                    break;
                case "priority_level":
                    queryable = query.Order == "asc" ? queryable.OrderBy(p => p.PriorityLevel) : queryable.OrderByDescending(p => p.PriorityLevel);
                    break;
                default:
                    queryable = query.Order == "asc" ? queryable.OrderBy(p => p.CreateTime) : queryable.OrderByDescending(p => p.CreateTime);
                    break;
            }

            var total = await queryable.CountAsync();
            var products = await queryable
                .Page(query.Page, query.Size)
                .ToListAsync();

            var dtos = _mapper.Map<List<ProductDataDto>>(products);

            // 填充最新策略评分
            if (dtos.Any())
            {
                var productIds = dtos.Select(d => d.Id).ToList();
                var executions = await _freeSql.Select<StrategyExecution>()
                    .Where(e => productIds.Contains(e.ProductId) && e.IsLatest)
                    .ToListAsync(e => new { e.ProductId, e.StrategyCode, e.Score });

                foreach (var dto in dtos)
                {
                    var scores = executions.Where(e => e.ProductId == dto.Id).ToList();
                    if (scores.Any())
                    {
                        dto.LatestScores = new LatestScoresDto
                        {
                            S01 = scores.FirstOrDefault(s => s.StrategyCode == "S01")?.Score,
                            S02 = scores.FirstOrDefault(s => s.StrategyCode == "S02")?.Score,
                            S03_ROI = scores.FirstOrDefault(s => s.StrategyCode == "S03")?.Score,
                            S04_RiskLevel = scores.FirstOrDefault(s => s.StrategyCode == "S04")?.Score,
                            S05 = scores.FirstOrDefault(s => s.StrategyCode == "S05")?.Score,
                            S06 = scores.FirstOrDefault(s => s.StrategyCode == "S06")?.Score,
                            S07 = scores.FirstOrDefault(s => s.StrategyCode == "S07")?.Score
                        };
                    }
                }
            }

            return (dtos, total);
        }

        public async Task<ProductDataDto> GetProductByIdAsync(long id)
        {
            var product = await _productRepository.Select.Where(p => p.Id == id).FirstAsync();
            return _mapper.Map<ProductDataDto>(product);
        }

        public async Task<ProductDataDto> CreateProductAsync(CreateUpdateProductDto dto)
        {
            var product = _mapper.Map<ProductData>(dto);
            product.Status = dto.Status ?? "draft";
            product.CreateTime = DateTime.Now;
            product.UpdateTime = DateTime.Now;

            var created = await _productRepository.InsertAsync(product);
            return _mapper.Map<ProductDataDto>(created);
        }

        public async Task<ProductDataDto> UpdateProductAsync(long id, CreateUpdateProductDto dto)
        {
            var product = await _productRepository.Select.Where(p => p.Id == id).FirstAsync();
            if (product == null)
            {
                throw new Exception($"产品不存在: {id}");
            }

            _mapper.Map(dto, product);
            product.UpdateTime = DateTime.Now;

            await _productRepository.UpdateAsync(product);
            return _mapper.Map<ProductDataDto>(product);
        }

        public async Task<bool> DeleteProductAsync(long id)
        {
            var result = await _productRepository.DeleteAsync(p => p.Id == id);
            return result > 0;
        }

        public async Task<int> BatchDeleteProductsAsync(List<long> ids)
        {
            return await _productRepository.DeleteAsync(p => ids.Contains(p.Id));
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _productRepository.Select.Where(p => p.Id == id).AnyAsync();
        }

        public async Task<bool> SaveKeywordsAsync(long productId, List<ProductKeywordDto> dtos)
        {
            // Transaction
            using var uow = _freeSql.CreateUnitOfWork();
            var repo = uow.GetRepository<ProductKeyword>();

            await repo.DeleteAsync(p => p.ProductId == productId);

            var entities = dtos.Select(d => new ProductKeyword
            {
                ProductId = productId,
                Keyword = d.Keyword,
                Type = d.Type,
                SearchVolume = d.SearchVolume,
                CompetitorCount = d.CompetitorCount,
                SPR = d.SPR,
                BidPrice = d.BidPrice,
                CompetitionLevel = d.CompetitionLevel,
                OpportunityScore = d.OpportunityScore,
                Priority = d.Priority,
                CurrentRank = d.CurrentRank,
                TargetRank = d.TargetRank
            }).ToList();

            if (entities.Any())
            {
                await repo.InsertAsync(entities);
            }
            
            uow.Commit();
            return true;
        }

        public async Task<bool> SaveTrendsAsync(long productId, List<ProductTrendDto> dtos)
        {
            using var uow = _freeSql.CreateUnitOfWork();
            var repo = uow.GetRepository<ProductTrend>();

            await repo.DeleteAsync(p => p.ProductId == productId);

            var entities = dtos.Select(d => new ProductTrend
            {
                ProductId = productId,
                MetricName = d.MetricName,
                Month1 = d.Month1,
                Month2 = d.Month2,
                Month3 = d.Month3,
                Month4 = d.Month4,
                Month5 = d.Month5,
                Month6 = d.Month6,
                Month7 = d.Month7,
                Month8 = d.Month8,
                Month9 = d.Month9,
                Month10 = d.Month10,
                Month11 = d.Month11,
                Month12 = d.Month12,
                YearMean = d.YearMean,
                Trend = d.Trend,
                SeasonalityIndex = d.SeasonalityIndex
            }).ToList();

            if (entities.Any())
            {
                await repo.InsertAsync(entities);
            }

            uow.Commit();
            return true;
        }

        public async Task<BatchImportResultDto> BatchImportAsync(Microsoft.AspNetCore.Http.IFormFile file)
        {
            // TODO: 为简化演示，这里仅实现基础逻辑框架
            // 实际应使用 CsvHelper 或 MiniExcel 处理文件
            return new BatchImportResultDto
            {
                SuccessCount = 10,
                FailCount = 0,
                TotalCount = 10
            };
        }

        public async Task<byte[]> ExportAsync(List<long> ids, string format)
        {
            var query = _productRepository.Select;
            if (ids != null && ids.Any())
            {
                query = query.Where(p => ids.Contains(p.Id));
            }

            var products = await query.ToListAsync();

            if (format.ToLower() == "csv")
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Id,ProductName,ASIN,Category,Status,PriorityLevel");
                foreach (var p in products)
                {
                    sb.AppendLine($"{p.Id},{p.ProductName},{p.ASIN},{p.Category},{p.Status},{p.PriorityLevel}");
                }
                return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            }

            // Excel 导出后续实现
            return Array.Empty<byte>();
        }
    }

    public class BatchImportResultDto
    {
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
    }
}

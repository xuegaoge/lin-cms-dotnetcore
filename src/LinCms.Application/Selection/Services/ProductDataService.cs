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
    }
}

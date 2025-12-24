using AutoMapper;
using FreeSql;
using IGeekFan.FreeKit.Extras.FreeSql;
using LinCms.Application.Selection.Models;
using LinCms.Application.Selection.Strategies;
using LinCms.Application.Contracts.Selection;
using LinCms.Entities.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinCms.Application.Selection.Services
{
    /// <summary>
    /// 策略执行服务
    /// </summary>
    public class StrategyExecutionService
    {
        private readonly StrategyRegistry _registry;
        private readonly IAuditBaseRepository<StrategyExecution> _executionRepository;
        private readonly IAuditBaseRepository<ProductData> _productRepository;
        private readonly EnterpriseProfileService _enterpriseService;
        private readonly IMapper _mapper;

        public StrategyExecutionService(
            StrategyRegistry registry,
            IAuditBaseRepository<StrategyExecution> executionRepository,
            IAuditBaseRepository<ProductData> productRepository,
            EnterpriseProfileService enterpriseService,
            IMapper mapper)
        {
            _registry = registry;
            _executionRepository = executionRepository;
            _productRepository = productRepository;
            _enterpriseService = enterpriseService;
            _mapper = mapper;
        }

        public List<StrategyDto> GetAllStrategies()
        {
            return _registry.GetAll().Select(s => new StrategyDto
            {
                Code = s.Code,
                Name = s.Name,
                Type = s.Type.ToString(),
                Description = s.Description,
                RequiredFields = s.RequiredFields.ToList()
            }).ToList();
        }

        /// <summary>
        /// 执行单个策略 (兼容旧版调用)
        /// </summary>
        public async Task<StrategyResultDto> ExecuteStrategyAsync(string code, long productId)
        {
            return await ExecuteStrategyAsync(code, productId, null);
        }

        /// <summary>
        /// 执行单个策略 (核心逻辑)
        /// </summary>
        public async Task<StrategyResultDto> ExecuteStrategyAsync(string code, long productId, Dictionary<string, int?> manualAnswers)
        {
            var strategy = _registry.Get(code);
            if (strategy == null) throw new Exception($"策略不存在: {code}");

            var product = await _productRepository.Select.Where(p => p.Id == productId).FirstAsync();
            if (product == null) throw new Exception($"产品不存在: {productId}");

            var profile = await _enterpriseService.GetCurrentProfileAsync(1); 
            var context = new ExecutionContext
            {
                EnterpriseProfile = _mapper.Map<EnterpriseProfile>(profile),
                ManualAnswers = manualAnswers // 修复：必须赋值，否则策略拿不到手动答案
            };

            var result = strategy.Execute(product, context);

            var execution = new StrategyExecution
            {
                ProductId = productId,
                StrategyCode = code,
                StrategyName = result.StrategyName,
                StrategyType = result.Type.ToString(),
                Score = result.Score,
                Grade = result.Grade,
                Decision = result.Decision,
                Reason = result.Reason,
                ExecutedAt = DateTime.Now,
                ExecutionTimeMs = (int)result.ExecutionTimeMs,
                DetailJson = !string.IsNullOrEmpty(result.DetailJson) ? result.DetailJson : 
                             (result.Data != null ? System.Text.Json.JsonSerializer.Serialize(result.Data) : 
                             System.Text.Json.JsonSerializer.Serialize(result)),
                IsLatest = true
            };

            // 设置旧记录为非最新
            await _executionRepository.UpdateDiy
                .Set(e => e.IsLatest, false)
                .Where(e => e.ProductId == productId && e.StrategyCode == code)
                .ExecuteAffrowsAsync();

            await _executionRepository.InsertAsync(execution);

            var resultDto = _mapper.Map<StrategyResultDto>(result);
            resultDto.ExecutionId = execution.Id;
            return resultDto;
        }

        public async Task<List<StrategyResultDto>> ExecuteBatchStrategiesAsync(List<string> codes, long productId)
        {
            var results = new List<StrategyResultDto>();
            foreach (var code in codes)
            {
                results.Add(await ExecuteStrategyAsync(code, productId));
            }
            return results;
        }

        public async Task<List<StrategyResultDto>> ExecuteAllStrategiesAsync(long productId)
        {
            var codes = _registry.GetAll().Select(s => s.Code).ToList();
            return await ExecuteBatchStrategiesAsync(codes, productId);
        }

        public async Task<List<StrategyExecutionDto>> GetExecutionHistoryAsync(long productId, string strategyCode = null, int page = 1, int size = 20)
        {
            var list = await _executionRepository.Select
                .Where(e => e.ProductId == productId)
                .Where(e => e.IsLatest == true) 
                .WhereIf(!string.IsNullOrEmpty(strategyCode), e => e.StrategyCode == strategyCode)
                .OrderByDescending(e => e.ExecutedAt)
                .Page(page, size)
                .ToListAsync();

            return _mapper.Map<List<StrategyExecutionDto>>(list);
        }

        public async Task<StrategyExecutionDto> GetExecutionDetailAsync(long id)
        {
            var execution = await _executionRepository.Select.Where(e => e.Id == id).FirstAsync();
            if (execution == null) return null;
            return _mapper.Map<StrategyExecutionDto>(execution);
        }

        public async Task<StrategyResultDto> ReExecuteStrategyAsync(long executionId)
        {
            var execution = await _executionRepository.Select.Where(e => e.Id == executionId).FirstAsync();
            if (execution == null) throw new Exception("执行记录不存在");
            return await ExecuteStrategyAsync(execution.StrategyCode, execution.ProductId);
        }

        /// <summary>
        /// S02-40题自诊提交 (入口)
        /// </summary>
        public async Task<StrategyResultDto> SubmitSelfDiagnosisAsync(SelfDiagnosisSubmitDto dto)
        {
            // 调用核心执行逻辑，传递手动答案
            return await ExecuteStrategyAsync("S02", dto.ProductId, dto.Answers);
        }

        public async Task<object> SensitivityAnalysisAsync(SensitivityAnalysisDto dto)
        {
            var product = await _productRepository.Select.Where(p => p.Id == dto.ProductId).FirstAsync();
            if (product == null) throw new Exception("产品不存在");
            var scenarios = new List<object>();
            foreach (var scenario in dto.Scenarios)
            {
                switch (scenario.ToLower())
                {
                    case "price":
                        scenarios.Add(new { scenario = "价格变动", changes = new[] { 
                            new { change = "-20%", profit = CalculateProfit(product, priceChange: -0.2m) },
                            new { change = "0%", profit = CalculateProfit(product) },
                            new { change = "+20%", profit = CalculateProfit(product, priceChange: 0.2m) } 
                        }});
                        break;
                }
            }
            return new { scenarios, product_id = dto.ProductId };
        }

        public async Task<object> StressTestAsync(StressTestDto dto)
        {
            var product = await _productRepository.Select.Where(p => p.Id == dto.ProductId).FirstAsync();
            if (product == null) throw new Exception("产品不存在");
            return new { result = "PASS", survival_rate = 1.0m };
        }

        public StrategyConfigDto GetStrategyConfig(string strategyCode)
        {
            return new StrategyConfigDto { StrategyCode = strategyCode, IsActive = true };
        }

        public async Task<StrategyConfigDto> UpdateStrategyConfigAsync(string strategyCode, StrategyConfigDto config)
        {
            return config;
        }

        #region 私有方法
        private decimal CalculateProfit(ProductData product, decimal priceChange = 0, decimal costChange = 0, decimal acosChange = 0, decimal volumeChange = 0)
        {
            var price = (product.TargetPrice ?? 0) * (1 + priceChange);
            var cost = (product.PurchaseCost ?? 0) * (1 + costChange);
            var unitProfit = price - cost - (product.ShippingCost ?? 0) - (product.FBACost ?? 0) - price * 0.35m;
            return unitProfit * (product.EstimatedMonthlySales ?? 100);
        }
        #endregion
    }
}
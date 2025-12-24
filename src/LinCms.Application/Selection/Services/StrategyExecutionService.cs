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

        public async Task<StrategyResultDto> ExecuteStrategyAsync(string code, long productId, Dictionary<string, int?> manualAnswers = null)
        {
            var strategy = _registry.Get(code);
            if (strategy == null) throw new Exception($"策略不存在: {code}");

            var product = await _productRepository.Select.Where(p => p.Id == productId).FirstAsync();
            if (product == null) throw new Exception($"产品不存在: {productId}");

            var profile = await _enterpriseService.GetCurrentProfileAsync(1); // 模拟
            var context = new ExecutionContext
            {
                EnterpriseProfile = _mapper.Map<EnterpriseProfile>(profile)
            };

            var result = strategy.Execute(product, context);

            //Console.WriteLine($"[StrategyExecution] Strategy: {code}, StrategyResult.DetailJson: {result.DetailJson?.Substring(0, Math.Min(result.DetailJson?.Length ?? 0, 100))}");

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
                // Prefer specific detail JSON if available, otherwise fallback to Data object, otherwise full result
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
                .Where(e => e.IsLatest == true) // 只返回每个策略的最新记录
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

        /// <summary>
        /// 重新执行历史策略
        /// </summary>
        public async Task<StrategyResultDto> ReExecuteStrategyAsync(long executionId)
        {
            var execution = await _executionRepository.Select.Where(e => e.Id == executionId).FirstAsync();
            if (execution == null) throw new Exception("执行记录不存在");

            // 使用相同的策略和产品重新执行
            return await ExecuteStrategyAsync(execution.StrategyCode, execution.ProductId);
        }

        /// <summary>
        /// S02-40题自诊提交
        /// </summary>
        public async Task<StrategyResultDto> SubmitSelfDiagnosisAsync(SelfDiagnosisSubmitDto dto)
        {
            var product = await _productRepository.Select.Where(p => p.Id == dto.ProductId).FirstAsync();
            if (product == null) throw new Exception("产品不存在");

            // 计算得分（基于用户手动填写的答案）
            var passCount = dto.Answers.Values.Count(v => v);
            var totalCount = dto.Answers.Count;
            var passRate = (decimal)passCount / totalCount;
            var score = passRate * 1000; // 1000分制

            var result = new StrategyResult
            {
                StrategyCode = "S02",
                StrategyName = "40题自诊系统",
                Type = StrategyType.Decision,
                Score = score,
                Grade = score >= 800 ? "A" : score >= 600 ? "B" : score >= 400 ? "C" : "D",
                Decision = score >= 800 ? "GO" : score >= 600 ? "WAIT" : "STOP",
                Reason = $"手动诊断: {score:F0}/1000分 ({passCount}/{totalCount}题通过)",
                ExecutionTimeMs = 50
            };

            // 先将该产品该策略的旧记录设为非最新
            await _executionRepository.UpdateDiy
                .Set(e => e.IsLatest, false)
                .Where(e => e.ProductId == dto.ProductId && e.StrategyCode == "S02")
                .ExecuteAffrowsAsync();

            // 保存执行记录（包含用户手动填写的答案）
            var execution = new StrategyExecution
            {
                ProductId = dto.ProductId,
                StrategyCode = "S02",
                StrategyName = result.StrategyName,
                StrategyType = result.Type.ToString(),
                Score = result.Score,
                Grade = result.Grade,
                Decision = result.Decision,
                Reason = result.Reason,
                ExecutedAt = DateTime.Now,
                ExecutionTimeMs = (int)result.ExecutionTimeMs,
                DetailJson = System.Text.Json.JsonSerializer.Serialize(new 
                { 
                    answers = dto.Answers, 
                    manualSubmit = true,
                    passCount = passCount,
                    totalCount = totalCount,
                    score = score
                }),
                IsLatest = true
            };

            await _executionRepository.InsertAsync(execution);

            return _mapper.Map<StrategyResultDto>(result);
        }

        /// <summary>
        /// S03-敏感性分析
        /// </summary>
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
                        scenarios.Add(new
                        {
                            scenario = "价格变动",
                            changes = new[]
                            {
                                new { change = "-20%", profit = CalculateProfit(product, priceChange: -0.2m) },
                                new { change = "-10%", profit = CalculateProfit(product, priceChange: -0.1m) },
                                new { change = "0%", profit = CalculateProfit(product) },
                                new { change = "+10%", profit = CalculateProfit(product, priceChange: 0.1m) },
                                new { change = "+20%", profit = CalculateProfit(product, priceChange: 0.2m) }
                            }
                        });
                        break;
                    case "cost":
                        scenarios.Add(new
                        {
                            scenario = "成本变动",
                            changes = new[]
                            {
                                new { change = "-10%", profit = CalculateProfit(product, costChange: -0.1m) },
                                new { change = "0%", profit = CalculateProfit(product) },
                                new { change = "+10%", profit = CalculateProfit(product, costChange: 0.1m) },
                                new { change = "+20%", profit = CalculateProfit(product, costChange: 0.2m) }
                            }
                        });
                        break;
                    case "acos":
                        scenarios.Add(new
                        {
                            scenario = "广告成本变动",
                            changes = new[]
                            {
                                new { change = "15%", profit = CalculateProfit(product, acosChange: 0.15m) },
                                new { change = "20%", profit = CalculateProfit(product, acosChange: 0.20m) },
                                new { change = "25%", profit = CalculateProfit(product, acosChange: 0.25m) },
                                new { change = "30%", profit = CalculateProfit(product, acosChange: 0.30m) }
                            }
                        });
                        break;
                    case "volume":
                        scenarios.Add(new
                        {
                            scenario = "销量变动",
                            changes = new[]
                            {
                                new { change = "-50%", profit = CalculateProfit(product, volumeChange: -0.5m) },
                                new { change = "-25%", profit = CalculateProfit(product, volumeChange: -0.25m) },
                                new { change = "0%", profit = CalculateProfit(product) },
                                new { change = "+25%", profit = CalculateProfit(product, volumeChange: 0.25m) },
                                new { change = "+50%", profit = CalculateProfit(product, volumeChange: 0.5m) }
                            }
                        });
                        break;
                }
            }

            return new { scenarios, product_id = dto.ProductId };
        }

        /// <summary>
        /// S18-压力测试
        /// </summary>
        public async Task<object> StressTestAsync(StressTestDto dto)
        {
            var product = await _productRepository.Select.Where(p => p.Id == dto.ProductId).FirstAsync();
            if (product == null) throw new Exception("产品不存在");

            var scenarios = new[]
            {
                new { name = "场景1-正常", priceChange = 0m, costChange = 0m, volumeChange = 0m },
                new { name = "场景2-价格下降10%", priceChange = -0.1m, costChange = 0m, volumeChange = 0m },
                new { name = "场景3-成本上升15%", priceChange = 0m, costChange = 0.15m, volumeChange = 0m },
                new { name = "场景4-销量下降30%", priceChange = 0m, costChange = 0m, volumeChange = -0.3m },
                new { name = "场景5-价格降+成本升", priceChange = -0.1m, costChange = 0.1m, volumeChange = 0m },
                new { name = "场景6-价格降+销量降", priceChange = -0.1m, costChange = 0m, volumeChange = -0.2m },
                new { name = "场景7-成本升+销量降", priceChange = 0m, costChange = 0.15m, volumeChange = -0.2m },
                new { name = "场景8-极端情况", priceChange = -0.15m, costChange = 0.2m, volumeChange = -0.3m }
            };

            var results = scenarios.Select(s => new
            {
                scenario_name = s.name,
                net_profit = CalculateProfit(product, s.priceChange, s.costChange, volumeChange: s.volumeChange),
                result = CalculateProfit(product, s.priceChange, s.costChange, volumeChange: s.volumeChange) > 0 ? "PASS" : "FAIL"
            }).ToList();

            var passCount = results.Count(r => r.result == "PASS");
            var survivalRate = (decimal)passCount / results.Count;
            var worstCaseProfit = results.Min(r => r.net_profit);

            var decision = survivalRate >= 0.75m ? "GO" : survivalRate >= 0.5m ? "WAIT" : "STOP";

            return new
            {
                scenarios = results,
                survival_rate = survivalRate,
                worst_case_profit = worstCaseProfit,
                decision
            };
        }

        /// <summary>
        /// 获取策略配置
        /// </summary>
        public StrategyConfigDto GetStrategyConfig(string strategyCode)
        {
            var strategy = _registry.Get(strategyCode);
            if (strategy == null) throw new Exception($"策略不存在: {strategyCode}");

            // TODO: 从配置表读取实际配置
            return new StrategyConfigDto
            {
                StrategyCode = strategyCode,
                Thresholds = new Dictionary<string, object>
                {
                    ["minScore"] = 60,
                    ["maxRisk"] = 0.3,
                    ["targetMargin"] = 0.25
                },
                IsActive = true
            };
        }

        /// <summary>
        /// 更新策略配置
        /// </summary>
        public async Task<StrategyConfigDto> UpdateStrategyConfigAsync(string strategyCode, StrategyConfigDto config)
        {
            var strategy = _registry.Get(strategyCode);
            if (strategy == null) throw new Exception($"策略不存在: {strategyCode}");

            // TODO: 保存到配置表
            return config;
        }

        #region 私有方法

        /// <summary>
        /// 计算利润
        /// </summary>
        private decimal CalculateProfit(ProductData product, decimal priceChange = 0, decimal costChange = 0, decimal acosChange = 0, decimal volumeChange = 0)
        {
            var price = (product.TargetPrice ?? 0) * (1 + priceChange);
            var cost = (product.PurchaseCost ?? 0) * (1 + costChange);
            var shipping = product.ShippingCost ?? 0;
            var fba = product.FBACost ?? 0;
            var referralFee = price * 0.15m; // 假设15%
            var acos = price * (acosChange > 0 ? acosChange : 0.2m); // 默认20%
            var volume = (product.EstimatedMonthlySales ?? 100) * (1 + volumeChange);

            var unitProfit = price - cost - shipping - fba - referralFee - acos;
            return unitProfit * volume;
        }

        #endregion
    }
}

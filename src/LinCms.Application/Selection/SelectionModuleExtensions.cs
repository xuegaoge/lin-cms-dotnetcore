using LinCms.Application.Selection.Services;
using LinCms.Application.Selection.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace LinCms.Application.Selection
{
    /// <summary>
    /// 选品模块依赖注入配置
    /// </summary>
    public static class SelectionModuleExtensions
    {
        /// <summary>
        /// 添加选品模块服务
        /// </summary>
        public static IServiceCollection AddSelectionModule(this IServiceCollection services)
        {
            // 注册所有选品策略 (Scoping: Scoped allows injecting Context/Repositories)
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.FourLayerStrategy>();          // S01
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.ProfitModelStrategy>();        // S03
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.RiskAlertStrategy>();          // S04
            
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.SelfDiagnosisStrategy>();      // S02
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.ElevenDimensionStrategy>();    // S05
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.FiveDimensionStrategy>();      // S06
            
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.MarketEvaluationStrategy>();   // S07
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.Top20StrategyLibrary>();       // S08
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.BlueOceanDetectionStrategy>(); // S09
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.TrackHeatRatingStrategy>();    // S10
            
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.EnterpriseProfileStrategy>();  // S11
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.A9IndicatorStrategy>();        // S12
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.HotspotDetectionStrategy>();   // S13
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.DecisionTreeStrategy>();       // S14
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.CompetitorAnalysisStrategy>(); // S15
            
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.SupplyChainEvaluationStrategy>(); // S16
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.InnovationMatrixStrategy>();      // S17
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.StressTestStrategy>();            // S18
            
            // S19-S21
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.KeywordResearchStrategy>();       // S19
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.MarketTrendStrategy>();           // S20
            services.AddScoped<ISelectionStrategy, Strategies.Implementations.ComprehensiveDecisionStrategy>(); // S21

            // 注册策略注册表（Scoped，因为它依赖于策略集合）
            services.AddScoped<StrategyRegistry>();

            // 注册服务（Scoped）
            services.AddScoped<ProductDataService>();
            services.AddScoped<EnterpriseProfileService>();
            services.AddScoped<GlobalConfigService>();
            services.AddScoped<StrategyExecutionService>();
            services.AddScoped<ProductComparisonService>();
            services.AddScoped<ProductApprovalService>();
            services.AddScoped<ProductMetricsHistoryService>();
            services.AddScoped<BIDashboardService>();

            return services;
        }
    }
}

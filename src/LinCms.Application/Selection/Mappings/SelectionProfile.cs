using AutoMapper;
using LinCms.Application.Contracts.Selection;
using LinCms.Entities.Selection;
using LinCms.Application.Selection.Models;
using System;
using System.Collections.Generic;

namespace LinCms.Application.Selection.Mappings
{
    /// <summary>
    /// AutoMapper配置 - 选品模块
    /// </summary>
    public class SelectionProfile : Profile
    {
        public SelectionProfile()
        {
            // ProductData映射
            CreateMap<ProductData, ProductDataDto>()
                .ForMember(dest => dest.AssignedName, opt => opt.Ignore())
                .ForMember(dest => dest.LatestScores, opt => opt.Ignore());
            
            CreateMap<CreateUpdateProductDto, ProductData>();

            // EnterpriseProfile映射
            CreateMap<EnterpriseProfile, EnterpriseProfileDto>()
                .ForMember(dest => dest.WeightConfig, opt => opt.Ignore())
                .ForMember(dest => dest.Recommendations, opt => opt.Ignore());
            
            CreateMap<CreateEnterpriseProfileDto, EnterpriseProfile>();

            // GlobalConfig映射
            CreateMap<GlobalConfig, GlobalConfigDto>();
            CreateMap<CreateUpdateGlobalConfigDto, GlobalConfig>();

            // StrategyExecution映射
            CreateMap<StrategyExecution, StrategyExecutionDto>()
                .ForMember(dest => dest.SubResults, opt => opt.Ignore())
                .ForMember(dest => dest.Warnings, opt => opt.Ignore())
                .ForMember(dest => dest.Suggestions, opt => opt.Ignore());

            CreateMap<StrategyResult, StrategyResultDto>();
            CreateMap<SubResult, SubResultDto>();
            CreateMap<Indicator, IndicatorDto>();

            // ProductMetricsHistory映射
            CreateMap<ProductMetricsHistory, ProductMetricsHistoryDto>();
            CreateMap<AddMetricsHistoryDto, ProductMetricsHistory>();

            // ProductComparison映射
            CreateMap<ProductComparison, ProductComparisonDto>();

            // ProductApproval映射
            CreateMap<ProductApproval, ProductApprovalDto>();
        }
    }
}

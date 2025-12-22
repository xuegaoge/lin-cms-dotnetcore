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
                .ForMember(dest => dest.WeightConfig, opt => opt.MapFrom(src => 
                    string.IsNullOrEmpty(src.WeightConfig) ? new Dictionary<string, decimal>() : 
                    Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, decimal>>(src.WeightConfig)))
                .ForMember(dest => dest.Recommendations, opt => opt.Ignore());
            
            // 添加反向映射: EnterpriseProfileDto -> EnterpriseProfile
            CreateMap<EnterpriseProfileDto, EnterpriseProfile>()
                .ForMember(dest => dest.WeightConfig, opt => opt.MapFrom(src => 
                    src.WeightConfig == null ? null : Newtonsoft.Json.JsonConvert.SerializeObject(src.WeightConfig)))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.DeleteTime, opt => opt.Ignore())
                .ForMember(dest => dest.DeleteUserId, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());
            
            CreateMap<CreateEnterpriseProfileDto, EnterpriseProfile>()
                .ForMember(dest => dest.WeightConfig, opt => opt.MapFrom(src => 
                    src.WeightConfig == null ? null : Newtonsoft.Json.JsonConvert.SerializeObject(src.WeightConfig)));

            // GlobalConfig映射
            CreateMap<GlobalConfig, GlobalConfigDto>();
            CreateMap<CreateUpdateGlobalConfigDto, GlobalConfig>();

            // StrategyExecution映射
            CreateMap<StrategyExecution, StrategyExecutionDto>()
                .ForMember(dest => dest.SubResults, opt => opt.Ignore())
                .ForMember(dest => dest.Warnings, opt => opt.Ignore())
                .ForMember(dest => dest.Suggestions, opt => opt.Ignore())
                .ForMember(dest => dest.DetailJson, opt => opt.MapFrom(src => 
                    string.IsNullOrEmpty(src.DetailJson) ? null : 
                    Newtonsoft.Json.JsonConvert.DeserializeObject<object>(src.DetailJson)));

            CreateMap<StrategyResult, StrategyResultDto>()
                .ForMember(dest => dest.DetailJson, opt => opt.MapFrom(src => 
                    string.IsNullOrEmpty(src.DetailJson) ? null : 
                    Newtonsoft.Json.JsonConvert.DeserializeObject<object>(src.DetailJson)));
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

using AutoMapper;
using FreeSql;
using IGeekFan.FreeKit.Extras.FreeSql;
using LinCms.Application.Contracts.Selection;
using LinCms.Entities.Selection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinCms.Application.Selection.Services
{
    /// <summary>
    /// 企业定位评估服务
    /// </summary>
    public class EnterpriseProfileService
    {
        private readonly IAuditBaseRepository<EnterpriseProfile> _profileRepository;
        private readonly IMapper _mapper;

        public EnterpriseProfileService(IAuditBaseRepository<EnterpriseProfile> profileRepository, IMapper mapper)
        {
            _profileRepository = profileRepository;
            _mapper = mapper;
        }

        public async Task<EnterpriseProfileDto> GetCurrentProfileAsync(long organizationId)
        {
            var profile = await _profileRepository.Select
                .Where(p => p.OrganizationId == organizationId && p.IsCurrent)
                .FirstAsync();

            return _mapper.Map<EnterpriseProfileDto>(profile);
        }

        public async Task<EnterpriseProfileDto> CreateProfileAsync(long organizationId, CreateEnterpriseProfileDto dto)
        {
            // 将该组织下的历史配置设为非当前
            await _profileRepository.UpdateDiy
                .Set(p => p.IsCurrent, false)
                .Where(p => p.OrganizationId == organizationId)
                .ExecuteAffrowsAsync();

            var profile = _mapper.Map<EnterpriseProfile>(dto);
            profile.OrganizationId = organizationId;
            profile.IsCurrent = true;
            profile.EvaluatedAt = DateTime.Now;

            // 简单的评分计算
            profile.TotalScore = (dto.FundingCapacity + dto.TeamExperience + dto.SupplyChainDepth + 
                                 dto.OperationCapability + dto.RiskTolerance + dto.MarketInsight + 
                                 dto.TechCapability + dto.BrandAwareness) / 8;

            profile.Grade = CalculateGrade(profile.TotalScore);

            await _profileRepository.InsertAsync(profile);
            return _mapper.Map<EnterpriseProfileDto>(profile);
        }

        public async Task<List<EnterpriseProfileDto>> GetHistoryAsync(long organizationId, int page = 1, int size = 20)
        {
            var list = await _profileRepository.Select
                .Where(p => p.OrganizationId == organizationId)
                .OrderByDescending(p => p.EvaluatedAt)
                .Page(page, size)
                .ToListAsync();

            return _mapper.Map<List<EnterpriseProfileDto>>(list);
        }

        public async Task<bool> ActivateProfileAsync(long id, long organizationId)
        {
            var profile = await _profileRepository.Select.Where(p => p.Id == id && p.OrganizationId == organizationId).FirstAsync();
            if (profile == null) return false;

            await _profileRepository.UpdateDiy
                .Set(p => p.IsCurrent, false)
                .Where(p => p.OrganizationId == organizationId)
                .ExecuteAffrowsAsync();

            profile.IsCurrent = true;
            await _profileRepository.UpdateAsync(profile);
            return true;
        }

        private string CalculateGrade(decimal score)
        {
            if (score >= 90) return "A";
            if (score >= 80) return "B";
            if (score >= 70) return "C";
            if (score >= 60) return "D";
            return "E";
        }
    }
}

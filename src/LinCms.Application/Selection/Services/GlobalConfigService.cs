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
    /// 全局配置服务
    /// </summary>
    public class GlobalConfigService
    {
        private readonly IAuditBaseRepository<GlobalConfig> _configRepository;
        private readonly IMapper _mapper;

        public GlobalConfigService(IAuditBaseRepository<GlobalConfig> configRepository, IMapper mapper)
        {
            _configRepository = configRepository;
            _mapper = mapper;
        }

        public async Task<List<GlobalConfigDto>> GetConfigsAsync(string group = null, int page = 1, int size = 50)
        {
            var configs = await _configRepository.Select
                .WhereIf(!string.IsNullOrEmpty(group), c => c.ConfigGroup == group)
                .Page(page, size)
                .ToListAsync();

            return _mapper.Map<List<GlobalConfigDto>>(configs);
        }

        public async Task<GlobalConfigDto> GetConfigAsync(string group, string key)
        {
            var config = await _configRepository.Select
                .Where(c => c.ConfigGroup == group && c.ConfigKey == key)
                .FirstAsync();

            return _mapper.Map<GlobalConfigDto>(config);
        }

        public async Task<GlobalConfigDto> CreateConfigAsync(CreateUpdateGlobalConfigDto dto)
        {
            var config = _mapper.Map<GlobalConfig>(dto);
            await _configRepository.InsertAsync(config);
            return _mapper.Map<GlobalConfigDto>(config);
        }

        public async Task<GlobalConfigDto> UpdateConfigAsync(long id, CreateUpdateGlobalConfigDto dto)
        {
            var config = await _configRepository.Select.Where(c => c.Id == id).FirstAsync();
            if (config == null) throw new Exception("配置不存在");

            _mapper.Map(dto, config);
            await _configRepository.UpdateAsync(config);
            return _mapper.Map<GlobalConfigDto>(config);
        }

        public async Task<bool> DeleteConfigAsync(long id)
        {
            var rows = await _configRepository.DeleteAsync(p => p.Id == id);
            return rows > 0;
        }
    }
}

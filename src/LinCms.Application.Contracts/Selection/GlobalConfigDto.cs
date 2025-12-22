using System;

namespace LinCms.Application.Contracts.Selection
{
    /// <summary>
    /// 全局配置DTO
    /// </summary>
    public class GlobalConfigDto
    {
        public long Id { get; set; }
        public string ConfigGroup { get; set; }
        public string ConfigKey { get; set; }
        public string ConfigValue { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public long? UpdatedBy { get; set; }
    }

    /// <summary>
    /// 创建/更新全局配置DTO
    /// </summary>
    public class CreateUpdateGlobalConfigDto
    {
        public string ConfigGroup { get; set; }
        public string ConfigKey { get; set; }
        public string ConfigValue { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

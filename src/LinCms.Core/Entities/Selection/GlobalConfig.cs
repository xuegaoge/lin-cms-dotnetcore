using FreeSql.DataAnnotations;
using IGeekFan.FreeKit.Extras.AuditEntity;
using System;

namespace LinCms.Entities.Selection
{
    /// <summary>
    /// 全局配置表
    /// </summary>
    [Table(Name = "selection_global_config")]
    public class GlobalConfig : FullAuditEntity<long, long>
    {
        /// <summary>
        /// 配置分组 (tax/shipping/fba/commission/exchange/threshold)
        /// </summary>
        [Column(StringLength = 50)]
        public string ConfigGroup { get; set; }

        /// <summary>
        /// 配置键
        /// </summary>
        [Column(StringLength = 100)]
        public string ConfigKey { get; set; }

        /// <summary>
        /// 配置值 (JSON格式)
        /// </summary>
        [Column(DbType = "text")]
        public string ConfigValue { get; set; }

        /// <summary>
        /// 说明
        /// </summary>
        [Column(StringLength = 500)]
        public string Description { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 更新人ID
        /// </summary>
        public long? UpdatedBy { get; set; }
    }
}

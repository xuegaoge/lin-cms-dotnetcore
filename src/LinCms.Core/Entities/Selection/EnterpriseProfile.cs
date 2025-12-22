using FreeSql.DataAnnotations;
using IGeekFan.FreeKit.Extras.AuditEntity;
using System;

namespace LinCms.Entities.Selection
{
    /// <summary>
    /// 企业定位评估表 - S11策略使用
    /// </summary>
    [Table(Name = "selection_enterprise_profile")]
    [Index("idx_org_current", "OrganizationId, IsCurrent")]
    public class EnterpriseProfile : FullAuditEntity<long, long>
    {
        /// <summary>
        /// 关联组织/团队ID
        /// </summary>
        public long OrganizationId { get; set; }

        // ========== 基础信息 (Raw Data) ==========
        /// <summary>
        /// 企业层级 (上市/集团/中小/初创)
        /// </summary>
        [Column(StringLength = 50)]
        public string EnterpriseLevel { get; set; }

        /// <summary>
        /// 资金规模 (万)
        /// </summary>
        [Column(Precision = 18, Scale = 2)]
        public decimal CapitalScale { get; set; }

        /// <summary>
        /// 团队规模 (人)
        /// </summary>
        public int TeamSize { get; set; }

        /// <summary>
        /// 运营年限 (年)
        /// </summary>
        public int OperationYears { get; set; }

        /// <summary>
        /// 供应链能力 (强/中/弱)
        /// </summary>
        [Column(StringLength = 50)]
        public string SupplyChainCapability { get; set; }

        /// <summary>
        /// 品牌能力 (强/中/弱)
        /// </summary>
        [Column(StringLength = 50)]
        public string BrandCapability { get; set; }

        /// <summary>
        /// 技术能力 (强/中/弱)
        /// </summary>
        [Column(StringLength = 50)]
        public string TechnologyCapability { get; set; }

        /// <summary>
        /// 市场资源 (强/中/弱)
        /// </summary>
        [Column(StringLength = 50)]
        public string MarketResources { get; set; }

        /// <summary>
        /// 风险偏好 (高/中/低)
        /// </summary>
        [Column(StringLength = 50)]
        public string RiskPreference { get; set; }

        // ========== 8个维度评分 (0-100) ==========

        /// <summary>
        /// 资金体量评分
        /// </summary>
        [Column(Precision = 5, Scale = 2)]
        public decimal FundingCapacity { get; set; }

        /// <summary>
        /// 团队经验评分
        /// </summary>
        [Column(Precision = 5, Scale = 2)]
        public decimal TeamExperience { get; set; }

        /// <summary>
        /// 供应链深度评分
        /// </summary>
        [Column(Precision = 5, Scale = 2)]
        public decimal SupplyChainDepth { get; set; }

        /// <summary>
        /// 运营能力评分
        /// </summary>
        [Column(Precision = 5, Scale = 2)]
        public decimal OperationCapability { get; set; }

        /// <summary>
        /// 风险承受度评分
        /// </summary>
        [Column(Precision = 5, Scale = 2)]
        public decimal RiskTolerance { get; set; }

        /// <summary>
        /// 市场洞察评分
        /// </summary>
        [Column(Precision = 5, Scale = 2)]
        public decimal MarketInsight { get; set; }

        /// <summary>
        /// 技术能力评分
        /// </summary>
        [Column(Precision = 5, Scale = 2)]
        public decimal TechCapability { get; set; }

        /// <summary>
        /// 品牌意识评分
        /// </summary>
        [Column(Precision = 5, Scale = 2)]
        public decimal BrandAwareness { get; set; }

        // ========== 计算字段 ==========

        /// <summary>
        /// 综合得分 (加权平均)
        /// </summary>
        [Column(Precision = 5, Scale = 2)]
        public decimal TotalScore { get; set; }

        /// <summary>
        /// 等级 (A/B/C/D/E)
        /// </summary>
        [Column(StringLength = 10)]
        public string Grade { get; set; }

        /// <summary>
        /// 权重配置 (JSON格式)
        /// </summary>
        [Column(DbType = "text")]
        public string WeightConfig { get; set; }

        /// <summary>
        /// 评估时间
        /// </summary>
        public DateTime EvaluatedAt { get; set; }

        /// <summary>
        /// 评估人ID
        /// </summary>
        public long? EvaluatedBy { get; set; }

        /// <summary>
        /// 是否当前有效
        /// </summary>
        public bool IsCurrent { get; set; } = true;

        /// <summary>
        /// 备注
        /// </summary>
        [Column(DbType = "text")]
        public string Notes { get; set; }
    }
}

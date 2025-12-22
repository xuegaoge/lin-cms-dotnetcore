namespace LinCms.Entities.Selection
{
    /// <summary>
    /// 策略类型枚举
    /// </summary>
    public enum StrategyType
    {
        /// <summary>
        /// 评分型：输出0-100分 (S01/S05/S06/S07/S10/S11/S12/S16)
        /// </summary>
        Scoring,

        /// <summary>
        /// 判定型：输出GO/WAIT/STOP (S02/S14)
        /// </summary>
        Decision,

        /// <summary>
        /// 财务型：输出ROI/利润率 (S03)
        /// </summary>
        Financial,

        /// <summary>
        /// 风险型：输出风险列表 (S04)
        /// </summary>
        RiskDetection,

        /// <summary>
        /// 推荐型：输出打法列表 (S08/S09/S17)
        /// </summary>
        Recommendation,

        /// <summary>
        /// 识别型：输出信号识别 (S13爆点识别)
        /// </summary>
        Detection,

        /// <summary>
        /// 分析型：输出对比分析 (S15竞品分析)
        /// </summary>
        Analysis
    }
}

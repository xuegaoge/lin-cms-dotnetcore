namespace LinCms.Application.Contracts.Selection
{
    /// <summary>
    /// 产品查询DTO
    /// </summary>
    public class ProductQueryDto
    {
        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页数量
        /// </summary>
        public int Size { get; set; } = 20;

        /// <summary>
        /// 状态筛选 (draft/active/archived)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 优先级筛选 (P1/P2/P3/P4)
        /// </summary>
        public string Priority { get; set; }

        /// <summary>
        /// 关键词搜索（产品名/ASIN）
        /// </summary>
        public string Keyword { get; set; }

        /// <summary>
        /// 排序字段 (created_at/updated_at/priority_level)
        /// </summary>
        public string Sort { get; set; } = "created_at";

        /// <summary>
        /// 排序方向 (asc/desc)
        /// </summary>
        public string Order { get; set; } = "desc";
    }
}

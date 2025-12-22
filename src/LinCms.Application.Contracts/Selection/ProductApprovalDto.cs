using System;
using System.Collections.Generic;

namespace LinCms.Application.Contracts.Selection
{
    /// <summary>
    /// 提交审批DTO
    /// </summary>
    public class SubmitApprovalDto
    {
        public long StrategyExecutionId { get; set; }
        public List<ApproverDto> Approvers { get; set; }
    }

    /// <summary>
    /// 审批人DTO
    /// </summary>
    public class ApproverDto
    {
        public string Role { get; set; }
        public long ApproverId { get; set; }
    }

    /// <summary>
    /// 审批操作DTO
    /// </summary>
    public class ApproveActionDto
    {
        public string Opinion { get; set; } // 赞成/不赞成/待定
        public string Comments { get; set; }
    }

    /// <summary>
    /// 产品审批DTO
    /// </summary>
    public class ProductApprovalDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public long? StrategyExecutionId { get; set; }
        public string CurrentStage { get; set; }
        public List<ApprovalHistoryDto> ApprovalHistory { get; set; }
        public bool IsCompleted { get; set; }
        public string FinalResult { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// 审批历史DTO
    /// </summary>
    public class ApprovalHistoryDto
    {
        public string ApproverRole { get; set; }
        public string ApproverName { get; set; }
        public string Opinion { get; set; }
        public string Comments { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}

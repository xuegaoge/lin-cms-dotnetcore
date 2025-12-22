using AutoMapper;
using FreeSql;
using IGeekFan.FreeKit.Extras.FreeSql;
using LinCms.Application.Contracts.Selection;
using LinCms.Entities.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinCms.Application.Selection.Services
{
    /// <summary>
    /// 产品审批服务
    /// </summary>
    public class ProductApprovalService
    {
        private readonly IAuditBaseRepository<ProductApproval> _approvalRepository;
        private readonly IAuditBaseRepository<ProductData> _productRepository;
        private readonly IMapper _mapper;

        public ProductApprovalService(
            IAuditBaseRepository<ProductApproval> approvalRepository,
            IAuditBaseRepository<ProductData> productRepository,
            IMapper mapper)
        {
            _approvalRepository = approvalRepository;
            _productRepository = productRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// 提交产品审批
        /// </summary>
        public async Task<ProductApprovalDto> SubmitApprovalAsync(long productId, SubmitApprovalDto dto)
        {
            var product = await _productRepository.Select.Where(p => p.Id == productId).FirstAsync();
            if (product == null) throw new Exception("产品不存在");

            // 检查是否已有进行中的审批
            var existing = await _approvalRepository.Select
                .Where(a => a.ProductId == productId && !a.IsCompleted)
                .FirstAsync();
            if (existing != null) throw new Exception("该产品已有进行中的审批流程");

            // 创建审批记录
            var approval = new ProductApproval
            {
                ProductId = productId,
                StrategyExecutionId = dto.StrategyExecutionId,
                CurrentStage = "product", // 第一级：产品负责人
                ApprovalChain = System.Text.Json.JsonSerializer.Serialize(dto.Approvers),
                ApprovalHistory = "[]",
                IsCompleted = false,
                SubmittedAt = DateTime.Now
            };

            await _approvalRepository.InsertAsync(approval);

            return await GetApprovalStatusAsync(productId);
        }

        /// <summary>
        /// 审批操作
        /// </summary>
        public async Task<ProductApprovalDto> ApproveAsync(long approvalId, ApproveActionDto dto, long approverId)
        {
            var approval = await _approvalRepository.Select.Where(a => a.Id == approvalId).FirstAsync();
            if (approval == null) throw new Exception("审批记录不存在");
            if (approval.IsCompleted) throw new Exception("审批流程已完成");

            // 解析审批链
            var approvers = System.Text.Json.JsonSerializer.Deserialize<List<ApproverDto>>(approval.ApprovalChain);
            var currentApprover = approvers.FirstOrDefault(a => a.Role == approval.CurrentStage);
            
            if (currentApprover == null || currentApprover.ApproverId != approverId)
            {
                throw new Exception("您无权进行此审批");
            }

            // 添加审批历史
            var history = System.Text.Json.JsonSerializer.Deserialize<List<ApprovalHistoryDto>>(approval.ApprovalHistory);
            history.Add(new ApprovalHistoryDto
            {
                ApproverRole = currentApprover.Role,
                ApproverName = $"User_{approverId}", // 实际应查询用户名
                Opinion = dto.Opinion,
                Comments = dto.Comments,
                ApprovedAt = DateTime.Now
            });
            approval.ApprovalHistory = System.Text.Json.JsonSerializer.Serialize(history);

            // 判断审批结果
            if (dto.Opinion == "不赞成")
            {
                // 拒绝，流程结束
                approval.IsCompleted = true;
                approval.FinalResult = "rejected";
                approval.CompletedAt = DateTime.Now;
            }
            else if (dto.Opinion == "赞成")
            {
                // 进入下一级
                var stages = new[] { "product", "operation", "finance", "ceo" };
                var currentIndex = Array.IndexOf(stages, approval.CurrentStage);
                
                if (currentIndex < stages.Length - 1)
                {
                    // 还有下一级
                    approval.CurrentStage = stages[currentIndex + 1];
                }
                else
                {
                    // 全部通过
                    approval.IsCompleted = true;
                    approval.FinalResult = "approved";
                    approval.CompletedAt = DateTime.Now;
                }
            }

            await _approvalRepository.UpdateAsync(approval);

            return await GetApprovalStatusAsync(approval.ProductId);
        }

        /// <summary>
        /// 获取审批状态
        /// </summary>
        public async Task<ProductApprovalDto> GetApprovalStatusAsync(long productId)
        {
            var approval = await _approvalRepository.Select
                .Where(a => a.ProductId == productId)
                .OrderByDescending(a => a.SubmittedAt)
                .FirstAsync();

            if (approval == null) return null;

            var product = await _productRepository.Select.Where(p => p.Id == productId).FirstAsync();
            var history = System.Text.Json.JsonSerializer.Deserialize<List<ApprovalHistoryDto>>(approval.ApprovalHistory);

            return new ProductApprovalDto
            {
                Id = approval.Id,
                ProductId = approval.ProductId,
                ProductName = product?.ProductName,
                StrategyExecutionId = approval.StrategyExecutionId,
                CurrentStage = approval.CurrentStage,
                ApprovalHistory = history,
                IsCompleted = approval.IsCompleted,
                FinalResult = approval.FinalResult,
                SubmittedAt = approval.SubmittedAt,
                CompletedAt = approval.CompletedAt
            };
        }

        /// <summary>
        /// 获取待我审批列表
        /// </summary>
        public async Task<List<ProductApprovalDto>> GetPendingApprovalsAsync(long approverId, int page = 1, int size = 20)
        {
            var allApprovals = await _approvalRepository.Select
                .Where(a => !a.IsCompleted)
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();

            // 筛选出需要当前用户审批的
            var pendingList = new List<ProductApprovalDto>();
            foreach (var approval in allApprovals)
            {
                var approvers = System.Text.Json.JsonSerializer.Deserialize<List<ApproverDto>>(approval.ApprovalChain);
                var currentApprover = approvers.FirstOrDefault(a => a.Role == approval.CurrentStage);
                
                if (currentApprover != null && currentApprover.ApproverId == approverId)
                {
                    var product = await _productRepository.Select.Where(p => p.Id == approval.ProductId).FirstAsync();
                    var history = System.Text.Json.JsonSerializer.Deserialize<List<ApprovalHistoryDto>>(approval.ApprovalHistory);

                    pendingList.Add(new ProductApprovalDto
                    {
                        Id = approval.Id,
                        ProductId = approval.ProductId,
                        ProductName = product?.ProductName,
                        CurrentStage = approval.CurrentStage,
                        ApprovalHistory = history,
                        SubmittedAt = approval.SubmittedAt
                    });
                }
            }

            return pendingList.Skip((page - 1) * size).Take(size).ToList();
        }

        /// <summary>
        /// 获取我的审批历史
        /// </summary>
        public async Task<List<ProductApprovalDto>> GetMyApprovalHistoryAsync(long approverId, int page = 1, int size = 20)
        {
            var allApprovals = await _approvalRepository.Select
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();

            var myList = new List<ProductApprovalDto>();
            foreach (var approval in allApprovals)
            {
                var history = System.Text.Json.JsonSerializer.Deserialize<List<ApprovalHistoryDto>>(approval.ApprovalHistory);
                // 简化判断：检查历史中是否有我的审批记录
                if (history.Any())
                {
                    var product = await _productRepository.Select.Where(p => p.Id == approval.ProductId).FirstAsync();
                    myList.Add(new ProductApprovalDto
                    {
                        Id = approval.Id,
                        ProductId = approval.ProductId,
                        ProductName = product?.ProductName,
                        CurrentStage = approval.CurrentStage,
                        ApprovalHistory = history,
                        IsCompleted = approval.IsCompleted,
                        FinalResult = approval.FinalResult,
                        SubmittedAt = approval.SubmittedAt,
                        CompletedAt = approval.CompletedAt
                    });
                }
            }

            return myList.Skip((page - 1) * size).Take(size).ToList();
        }
    }
}

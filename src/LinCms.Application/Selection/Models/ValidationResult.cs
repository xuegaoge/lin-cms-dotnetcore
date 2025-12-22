using System.Collections.Generic;
using System.Linq;

namespace LinCms.Application.Selection.Models
{
    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        public static ValidationResult Fail(string error)
        {
            return new ValidationResult 
            { 
                IsValid = false, 
                Errors = new List<string> { error } 
            };
        }

        public static ValidationResult Fail(List<string> errors)
        {
            return new ValidationResult 
            { 
                IsValid = false, 
                Errors = errors 
            };
        }

        public string GetErrorMessage()
        {
            return string.Join("; ", Errors);
        }
    }
}

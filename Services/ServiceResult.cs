using System.Collections.Generic;

namespace NMB_HLabSys.Services
{
    /// <summary>
    /// Standardized response pattern for service operations
    /// </summary>
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new List<string>();

        public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
        public static ServiceResult<T> Fail(string error) => new() { Success = false, ErrorMessage = error };
        public static ServiceResult<T> ValidationFail(List<string> errors) => new() { Success = false, ValidationErrors = errors };
        public static ServiceResult<T> ValidationFail(string error) => new() { Success = false, ValidationErrors = new List<string> { error } };
    }

    public class ServiceResult : ServiceResult<object> { }
}
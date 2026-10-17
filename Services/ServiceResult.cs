using System.Collections.Generic;
using System.Linq;

namespace NMB_HLabSys.Services
{
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new List<string>();

        public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
        public static ServiceResult<T> Fail(string error) => new() { Success = false, ErrorMessage = error };
        public static ServiceResult<T> ValidationFail(List<string> errors) =>
            new() { Success = false, ValidationErrors = errors };
        public static ServiceResult<T> ValidationFail(string error) =>
            new() { Success = false, ValidationErrors = new List<string> { error } };

        /// <summary>
        /// Returns the most meaningful error message available,
        /// preferring ValidationErrors list over ErrorMessage string.
        /// </summary>
        public string GetMessage()
        {
            if (ValidationErrors != null && ValidationErrors.Any())
                return string.Join(" ", ValidationErrors);
            if (!string.IsNullOrWhiteSpace(ErrorMessage))
                return ErrorMessage;
            return "An unknown error occurred.";
        }
    }

    public class ServiceResult : ServiceResult<object> { }
}
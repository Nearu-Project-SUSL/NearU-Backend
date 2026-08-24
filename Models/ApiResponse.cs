namespace NearU_Backend_Revised.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> SuccessResponse(string message, T? data) =>
            new ApiResponse<T> { Success = true, Message = message, Data = data };


        public static ApiResponse<T> FailResponse(string message) =>
            new ApiResponse<T> { Success = false, Message = message, Data = default };
    }
}
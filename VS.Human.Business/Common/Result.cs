namespace VS.Human.Business.Common
{
    /// <summary>
    /// Represents the result of an operation with success/failure status and optional data
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; protected set; }
        public string Message { get; protected set; } = string.Empty;
        public List<string> Errors { get; protected set; } = new();

        protected Result(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        protected Result(bool isSuccess, string message, List<string> errors)
        {
            IsSuccess = isSuccess;
            Message = message;
            Errors = errors;
        }

        public static Result Success() => new(true, "Operation completed successfully");
        
        public static Result Success(string message) => new(true, message);
        
        public static Result Failure(string message) => new(false, message);
        
        public static Result Failure(string message, List<string> errors) => new(false, message, errors);
    }

    /// <summary>
    /// Represents the result of an operation with typed data
    /// </summary>
    public class Result<T> : Result
    {
        public T? Data { get; private set; }

        private Result(bool isSuccess, string message, T? data) : base(isSuccess, message)
        {
            Data = data;
        }

        private Result(bool isSuccess, string message, T? data, List<string> errors) : base(isSuccess, message, errors)
        {
            Data = data;
        }

        public static Result<T> Success(T data) => new(true, "Operation completed successfully", data);
        
        public static Result<T> Success(T data, string message) => new(true, message, data);
        
        public new static Result<T> Failure(string message) => new(false, message, default);
        
        public new static Result<T> Failure(string message, List<string> errors) => new(false, message, default, errors);
    }
}


namespace KeystoneCommerce.Application.Common.Result_Pattern
{
    public class Result<T>
    {
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public bool IsSuccess => !Errors.Any();

        public Result()
        {
        }

        public static Result<T> Success(T? data = default) => new Result<T> { Data = data };
        public static Result<T> Failure(List<string> errors) => new Result<T> { Errors = errors };

        public static Result<T> Failure(string error) => new Result<T>() { Errors = new List<string>() { error } };
        
    }
}

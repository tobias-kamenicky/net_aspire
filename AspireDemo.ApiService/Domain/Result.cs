namespace AspireDemo.ApiService.Domain;

public enum ErrorType
{
    NotFound,
    ValidationProblem,
    Conflict
}
public record Error(ErrorType Type, string Message, IDictionary<string, string[]>? Errors = null);

public record struct Unit;

public sealed record Result : Result<Unit>
{
    private Result() : base(new Unit()) { }
    private Result(Error error) : base(error) { }

    public TResult Match<TResult>(Func<TResult> some, Func<Error, TResult> none) => IsSuccess ? some() : none(Error!);

    public static Result Ok() => new();
    public new static Result Fail(Error error) => new(error);

    public static implicit operator Result(Error error) => Fail(error);
};

public record Result<T>
{
    public T? Value { get; }
    public bool IsSuccess { get; }
    public Error? Error { get; }
    protected Result(T value)
    {
        Value = value;
        IsSuccess = true;
        Error = default;
    }
    protected Result(Error error)
    {
        Value = default!;
        IsSuccess = false;
        Error = error;
    }

    public TResult Match<TResult>(Func<T, TResult> some, Func<Error, TResult> none) => IsSuccess ? some(Value!) : none(Error!);
    public Task<TResult> Match<TResult>(Func<T, Task<TResult>> some, Func<Error, Task<TResult>> none) => IsSuccess ? some(Value!) : none(Error!);

    public async Task<Result> IfOk(Func<T, Task> some)
    {
        if (!IsSuccess) return Result.Fail(Error!);
        await some(Value!);
        return Result.Ok();
    }

    public async Task<Result<TResult>> IfOk<TResult>(Func<T, Task<TResult>> some) => IsSuccess ? (await some(Value!)).Ok() : Error!.Fail<TResult>();

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Ok(value);
    public static implicit operator Result<T>(Error error) => Fail(error);
}

public static class ResultExtensions
{
    public static Result<T> Ok<T>(this T value) => Result<T>.Ok(value);
    public static Result<T> Fail<T>(this Error error) => Result<T>.Fail(error);
}

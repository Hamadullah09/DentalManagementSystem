namespace DentalSurgery.Application.Common;

/// <summary>Outcome of an operation that can fail for expected, reportable reasons.</summary>
public class Result
{
    protected Result(bool succeeded, IReadOnlyList<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public bool Succeeded { get; }
    public bool Failed => !Succeeded;
    public IReadOnlyList<string> Errors { get; }
    public string ErrorMessage => string.Join(" ", Errors);

    public static Result Success() => new(true, Array.Empty<string>());
    public static Result Failure(params string[] errors) => new(false, errors);
    public static Result Failure(IEnumerable<string> errors) => new(false, errors.ToList());
}

public class Result<T> : Result
{
    private Result(bool succeeded, T? value, IReadOnlyList<string> errors)
        : base(succeeded, errors) => Value = value;

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, Array.Empty<string>());
    public static new Result<T> Failure(params string[] errors) => new(false, default, errors);
    public static new Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors.ToList());

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<string>, TOut> onFailure) =>
        Succeeded ? onSuccess(Value!) : onFailure(Errors);
}

/// <summary>One page of results plus the totals needed to render paging controls.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public int FirstItemIndex => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int LastItemIndex => Math.Min(Page * PageSize, TotalCount);

    public static PagedResult<T> Empty(int pageSize = 25) => new() { PageSize = pageSize };

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize) =>
        new() { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
}

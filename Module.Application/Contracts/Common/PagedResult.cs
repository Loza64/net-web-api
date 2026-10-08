namespace Module.Application.Contracts.Common;

public record PagedResult<T>(IReadOnlyList<T> Data, Pagination Pagination);

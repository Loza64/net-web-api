namespace Module.Application.Contracts.Common;

public record Pagination(int Page, int PageSize, int PageCount, int Total);

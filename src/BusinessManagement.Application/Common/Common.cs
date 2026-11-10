namespace BusinessManagement.Application.Common;

public record PageResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPage => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
public class NotFoundException(string message) : Exception(message);
public class ConflictException(string message) : Exception(message);
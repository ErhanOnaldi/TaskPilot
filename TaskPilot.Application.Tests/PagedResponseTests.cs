using TaskPilot.Application.Common.Pagination;

namespace TaskPilot.Application.Tests;

public class PagedResponseTests
{
    [Fact]
    public void Create_calculates_metadata_for_middle_page()
    {
        var response = PagedResponse<int>.Create([3, 4], pageNumber: 2, pageSize: 2, totalCount: 5);

        Assert.Equal([3, 4], response.Items);
        Assert.Equal(2, response.PageNumber);
        Assert.Equal(2, response.PageSize);
        Assert.Equal(5, response.TotalCount);
        Assert.Equal(3, response.TotalPages);
        Assert.True(response.HasPreviousPage);
        Assert.True(response.HasNextPage);
    }

    [Fact]
    public void Create_returns_zero_total_pages_for_empty_result()
    {
        var response = PagedResponse<int>.Create([], pageNumber: 1, pageSize: 20, totalCount: 0);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalPages);
        Assert.False(response.HasPreviousPage);
        Assert.False(response.HasNextPage);
    }

    [Fact]
    public void Create_marks_last_page_without_next_page()
    {
        var response = PagedResponse<int>.Create([5], pageNumber: 3, pageSize: 2, totalCount: 5);

        Assert.True(response.HasPreviousPage);
        Assert.False(response.HasNextPage);
    }
}

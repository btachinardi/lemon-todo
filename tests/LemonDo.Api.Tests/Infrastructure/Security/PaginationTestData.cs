namespace LemonDo.Api.Tests.Infrastructure.Security;

/// <summary>
/// Parameterized pagination values for abuse testing.
/// </summary>
public static class PaginationTestData
{
    /// <summary>Abusive pagination parameter combinations as (page, pageSize, description) tuples.</summary>
    public static IEnumerable<object[]> AbusiveValues =>
    [
        [-1, 20, "NegativePage"],
        [0, 20, "ZeroPage"],
        [1, 0, "ZeroPageSize"],
        [1, -1, "NegativePageSize"],
        [1, 999999, "HugePageSize"],
        [int.MaxValue, 20, "MaxIntPage"],
    ];
}

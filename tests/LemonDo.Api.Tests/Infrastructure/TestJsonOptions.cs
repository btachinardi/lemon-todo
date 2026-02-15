namespace LemonDo.Api.Tests.Infrastructure;

using System.Text.Json;

public static class TestJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

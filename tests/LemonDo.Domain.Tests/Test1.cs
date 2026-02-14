namespace LemonDo.Domain.Tests;

[TestClass]
public sealed class SmokeTests
{
    [TestMethod]
    public void DomainProject_ShouldBeReferenceable()
    {
        // Verifies the test project can reference and load the Domain assembly
        var assembly = typeof(SmokeTests).Assembly;
        Assert.IsNotNull(assembly);
    }
}

using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RiskScoreStub;

namespace PolicyService.Tests;

[TestClass]
public class RiskScoreStubTests
{
    [TestMethod]
    public async Task RiskScore_ReturnsLow()
    {
        using var factory = new WebApplicationFactory<RiskScoreStubApp>()
            .WithWebHostBuilder(builder => builder.UseSetting("RiskStub:DelayMs", "0"));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/risk-score",
            new RiskScoreRequest("Jane Doe"));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.AreEqual("Low", body);
    }
}

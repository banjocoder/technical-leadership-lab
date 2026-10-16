using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using EmailServiceStub;

namespace PolicyService.Tests;

[TestClass]
public class EmailServiceStubTests
{
    private static WebApplicationFactory<EmailServiceStubApp> CreateFactory(string? result)
        => new WebApplicationFactory<EmailServiceStubApp>()
            .WithWebHostBuilder(builder =>
            {
                if (result is not null)
                {
                    builder.UseSetting("EmailService:Result", result);
                }
            });

    private static EmailPolicyRequest SampleRequest()
        => new("Jane Doe", "1", Guid.NewGuid());

    [TestMethod]
    public async Task EmailService_ReturnsOk_WhenConfiguredForSuccess()
    {
        using var factory = CreateFactory("success");
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/email-service", SampleRequest());

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task EmailService_Returns503_WhenConfiguredForFailure()
    {
        using var factory = CreateFactory("failure");
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/email-service", SampleRequest());

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [TestMethod]
    public async Task EmailService_Returns503_WhenConfigurationMissing()
    {
        using var factory = CreateFactory(null);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/email-service", SampleRequest());

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}

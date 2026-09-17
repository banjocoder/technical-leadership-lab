using PolicyService.Services;

namespace PolicyService.Tests;

[TestClass]
public class PolicyServiceTests
{
    [TestMethod]
    public void CreatePolicy_AssignsFirstPolicyId()
    {
        var service = new Services.PolicyService();

        var request = new CreatePolicyRequest(
            "Jane Doe",
            "Homeowners");

        var policy = service.CreatePolicy(request);

        Assert.AreEqual(1, policy.Id);
        Assert.AreEqual("Jane Doe", policy.PolicyHolderName);
        Assert.AreEqual("Homeowners", policy.PolicyType);
    }

    [TestMethod]
    public void GetPolicy_ReturnsPreviouslyCreatedPolicy()
    {
        var service = new Services.PolicyService();

        var created = service.CreatePolicy(
            new CreatePolicyRequest(
                "Jane Doe",
                "Auto"));

        var retrieved = service.GetPolicy(created.Id);

        Assert.IsNotNull(retrieved);
        Assert.AreEqual(created.Id, retrieved.Id);
    }

    [TestMethod]
    public void GetPolicy_ReturnsNull_WhenPolicyDoesNotExist()
    {
        var service = new Services.PolicyService();

        var result = service.GetPolicy(999);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IntentionalFailure()
    {
        Assert.AreEqual(1, 2);
    }
}
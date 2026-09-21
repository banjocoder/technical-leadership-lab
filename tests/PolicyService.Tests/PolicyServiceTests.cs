using PolicyService.Models;
using PolicyService.Services;

namespace PolicyService.Tests;

[TestClass]
public class PolicyServiceTests
{
    [TestMethod]
    public void CreatePolicy_AssignsFirstPolicyId()
    {
        var store = new PolicyStore();

        var request = new CreatePolicyRequest(
            "Jane Doe",
            "Homeowners");

        var policy = store.CreatePolicy(request);

        Assert.AreEqual(1, policy.Id);
        Assert.AreEqual("Jane Doe", policy.PolicyHolderName);
        Assert.AreEqual("Homeowners", policy.PolicyType);
    }

    [TestMethod]
    public void GetPolicy_ReturnsPreviouslyCreatedPolicy()
    {
        var store = new PolicyStore();

        var created = store.CreatePolicy(
            new CreatePolicyRequest(
                "Jane Doe",
                "Auto"));

        var retrieved = store.GetPolicy(created.Id);

        Assert.IsNotNull(retrieved);
        Assert.AreEqual(created.Id, retrieved.Id);
    }

    [TestMethod]
    public void GetPolicy_ReturnsNull_WhenPolicyDoesNotExist()
    {
        var store = new PolicyStore();

        var result = store.GetPolicy(999);

        Assert.IsNull(result);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Application.Administration.DTOs;
using LemonDo.Domain.Common;

namespace LemonDo.Api.Tests.Admin;

[TestClass]
public sealed class AdminEndpointsTests
{
    private static CustomWebApplicationFactory _factory = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomWebApplicationFactory();

    [ClassCleanup]
    public static void ClassCleanup() => _factory.Dispose();

    // --- Authorization ---

    [TestMethod]
    public async Task Should_Return401_When_UnauthenticatedUserAccessesAdminEndpoints()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/users");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return403_When_RegularUserAccessesAdminEndpoints()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/admin/users");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return200_When_AdminAccessesUserList()
    {
        var client = await _factory.CreateAdminClientAsync();

        var response = await client.GetAsync("/api/admin/users");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    // --- List Users ---

    [TestMethod]
    public async Task Should_ReturnPagedUsers_When_AdminListsUsers()
    {
        var client = await _factory.CreateAdminClientAsync();

        var response = await client.GetAsync("/api/admin/users");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(TestJsonOptions.Default);
        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.TotalCount);
        Assert.IsNotEmpty(result.Items);
    }

    [TestMethod]
    public async Task Should_DefaultPageSizeTo10_When_AdminListsUsersWithoutPageSize()
    {
        var client = await _factory.CreateAdminClientAsync();

        var response = await client.GetAsync("/api/admin/users");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(TestJsonOptions.Default);
        Assert.IsNotNull(result);
        Assert.AreEqual(10, result.PageSize);
    }

    [TestMethod]
    public async Task Should_DefaultPageSizeTo10_When_AdminSearchesAuditLogWithoutPageSize()
    {
        var client = await _factory.CreateAdminClientAsync();

        var response = await client.GetAsync("/api/admin/audit");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuditEntryDto>>(TestJsonOptions.Default);
        Assert.IsNotNull(result);
        Assert.AreEqual(10, result.PageSize);
    }

    [TestMethod]
    public async Task Should_ReturnRedactedProtectedData_When_AdminListsUsers()
    {
        var client = await _factory.CreateAdminClientAsync();

        var response = await client.GetAsync("/api/admin/users");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(TestJsonOptions.Default);

        // All emails should be redacted (contain ***)
        foreach (var user in result!.Items)
        {
            Assert.Contains("***", user.Email, $"Email '{user.Email}' should be redacted");
            Assert.Contains("***", user.DisplayName, $"DisplayName '{user.DisplayName}' should be redacted");
        }
    }

    // --- Get Single User ---

    [TestMethod]
    public async Task Should_Return200_When_AdminGetsExistingUser()
    {
        var client = await _factory.CreateAdminClientAsync();

        // List users to get a valid ID
        var listResponse = await client.GetAsync("/api/admin/users");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(TestJsonOptions.Default);
        var userId = list!.Items[0].Id;

        var response = await client.GetAsync($"/api/admin/users/{userId}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<AdminUserDto>(TestJsonOptions.Default);
        Assert.IsNotNull(user);
        Assert.AreEqual(userId, user.Id);
    }

    [TestMethod]
    public async Task Should_Return404_When_AdminGetsNonExistentUser()
    {
        var client = await _factory.CreateAdminClientAsync();

        var response = await client.GetAsync($"/api/admin/users/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Role Assignment (SystemAdmin only) ---

    [TestMethod]
    public async Task Should_Return403_When_AdminTriesToAssignRole()
    {
        var client = await _factory.CreateAdminClientAsync();

        // Admin can list but not assign roles
        var listResponse = await client.GetAsync("/api/admin/users");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(TestJsonOptions.Default);
        var userId = list!.Items[0].Id;

        var response = await client.PostAsJsonAsync($"/api/admin/users/{userId}/roles", new { RoleName = "Admin" });
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_AssignRole_When_SystemAdminAssignsValidRole()
    {
        var client = await _factory.CreateSystemAdminClientAsync();

        // Create a new user to assign role to
        var email = $"roletest-{Guid.NewGuid():N}@example.com";
        var registerClient = _factory.CreateClient();
        await registerClient.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = "Test1234!",
            DisplayName = "Role Test"
        });

        // Find that user by exact email (hash-based lookup)
        var listResponse = await client.GetAsync($"/api/admin/users?search={Uri.EscapeDataString(email)}");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(TestJsonOptions.Default);
        Assert.IsNotEmpty(list!.Items);
        var userId = list.Items[0].Id;

        // Assign Admin role
        var response = await client.PostAsJsonAsync($"/api/admin/users/{userId}/roles", new { RoleName = "Admin" });
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    // --- Deactivation (SystemAdmin only) ---

    [TestMethod]
    public async Task Should_Return403_When_AdminTriesToDeactivateUser()
    {
        var client = await _factory.CreateAdminClientAsync();

        var listResponse = await client.GetAsync("/api/admin/users");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(TestJsonOptions.Default);
        var userId = list!.Items[0].Id;

        var response = await client.PostAsync($"/api/admin/users/{userId}/deactivate", null);
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_DeactivateAndReactivate_When_SystemAdminManagesUser()
    {
        var client = await _factory.CreateSystemAdminClientAsync();

        // Create a test user
        var email = $"deactivate-{Guid.NewGuid():N}@example.com";
        var registerClient = _factory.CreateClient();
        await registerClient.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = "Test1234!",
            DisplayName = "Deactivate Test"
        });

        var listResponse = await client.GetAsync($"/api/admin/users?search={Uri.EscapeDataString(email)}");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(TestJsonOptions.Default);
        var userId = list!.Items[0].Id;

        // Deactivate
        var deactivateResponse = await client.PostAsync($"/api/admin/users/{userId}/deactivate", null);
        Assert.AreEqual(HttpStatusCode.OK, deactivateResponse.StatusCode);

        // Verify deactivated
        var getResponse = await client.GetAsync($"/api/admin/users/{userId}");
        var user = await getResponse.Content.ReadFromJsonAsync<AdminUserDto>(TestJsonOptions.Default);
        Assert.IsFalse(user!.IsActive);

        // Reactivate
        var reactivateResponse = await client.PostAsync($"/api/admin/users/{userId}/reactivate", null);
        Assert.AreEqual(HttpStatusCode.OK, reactivateResponse.StatusCode);

        // Verify reactivated
        getResponse = await client.GetAsync($"/api/admin/users/{userId}");
        user = await getResponse.Content.ReadFromJsonAsync<AdminUserDto>(TestJsonOptions.Default);
        Assert.IsTrue(user!.IsActive);
    }

    // --- Protected Data Reveal (SystemAdmin only, break-the-glass) ---

    [TestMethod]
    public async Task Should_Return403_When_AdminTriesToRevealProtectedData()
    {
        var client = await _factory.CreateAdminClientAsync();

        var listResponse = await client.GetAsync("/api/admin/users");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(TestJsonOptions.Default);
        var userId = list!.Items[0].Id;

        var response = await client.PostAsJsonAsync($"/api/admin/users/{userId}/reveal", new
        {
            Reason = "SupportTicket",
            Password = "anything"
        });
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnUnredactedProtectedData_When_SystemAdminReveals()
    {
        var client = await _factory.CreateSystemAdminClientAsync();

        // Create a user whose protected data we'll reveal
        var email = $"reveal-{Guid.NewGuid():N}@example.com";
        var displayName = "Reveal Test User";
        var registerClient = _factory.CreateClient();
        await registerClient.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = "Test1234!",
            DisplayName = displayName
        });

        // Find the user by exact email (hash-based lookup)
        var listResponse = await client.GetAsync($"/api/admin/users?search={Uri.EscapeDataString(email)}");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(TestJsonOptions.Default);
        var userId = list!.Items[0].Id;

        // Verify list shows redacted protected data
        Assert.Contains("***", list.Items[0].Email);

        // Reveal protected data with break-the-glass controls
        var revealResponse = await client.PostAsJsonAsync($"/api/admin/users/{userId}/reveal", new
        {
            Reason = "SupportTicket",
            Password = CustomWebApplicationFactory.SystemAdminUserPassword
        });
        Assert.AreEqual(HttpStatusCode.OK, revealResponse.StatusCode);

        // RevealedField is decrypted to plain strings by the server's RevealedFieldConverter,
        // so we deserialize into a JsonDocument to read strings directly.
        var json = await revealResponse.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.IsNotNull(json);

        // The revealed email should be unredacted
        Assert.AreEqual(email, json.RootElement.GetProperty("email").GetString());
        Assert.AreEqual(displayName, json.RootElement.GetProperty("displayName").GetString());
    }

    [TestMethod]
    public async Task Should_Return404_When_SystemAdminRevealsNonExistentUser()
    {
        var client = await _factory.CreateSystemAdminClientAsync();

        var response = await client.PostAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}/reveal", new
        {
            Reason = "SupportTicket",
            Password = CustomWebApplicationFactory.SystemAdminUserPassword
        });
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return400_When_ReasonIsOtherButNoDetails()
    {
        var client = await _factory.CreateSystemAdminClientAsync();

        var listResponse = await client.GetAsync("/api/admin/users");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(TestJsonOptions.Default);
        var userId = list!.Items[0].Id;

        var response = await client.PostAsJsonAsync($"/api/admin/users/{userId}/reveal", new
        {
            Reason = "Other",
            Password = CustomWebApplicationFactory.SystemAdminUserPassword
        });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_PasswordIsWrong()
    {
        var client = await _factory.CreateSystemAdminClientAsync();

        var listResponse = await client.GetAsync("/api/admin/users");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(TestJsonOptions.Default);
        var userId = list!.Items[0].Id;

        var response = await client.PostAsJsonAsync($"/api/admin/users/{userId}/reveal", new
        {
            Reason = "SupportTicket",
            Password = "WrongPassword123!"
        });
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return400_When_ReasonIsInvalid()
    {
        var client = await _factory.CreateSystemAdminClientAsync();

        var listResponse = await client.GetAsync("/api/admin/users");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(TestJsonOptions.Default);
        var userId = list!.Items[0].Id;

        var response = await client.PostAsJsonAsync($"/api/admin/users/{userId}/reveal", new
        {
            Reason = "InvalidReason",
            Password = CustomWebApplicationFactory.SystemAdminUserPassword
        });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnUnredactedProtectedData_When_ReasonIsOtherWithDetails()
    {
        var client = await _factory.CreateSystemAdminClientAsync();

        var email = $"reveal-other-{Guid.NewGuid():N}@example.com";
        var displayName = "Reveal Other User";
        var registerClient = _factory.CreateClient();
        await registerClient.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = "Test1234!",
            DisplayName = displayName
        });

        var listResponse = await client.GetAsync($"/api/admin/users?search={Uri.EscapeDataString(email)}");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(TestJsonOptions.Default);
        var userId = list!.Items[0].Id;

        var response = await client.PostAsJsonAsync($"/api/admin/users/{userId}/reveal", new
        {
            Reason = "Other",
            ReasonDetails = "Manual identity verification for phone-based support request",
            Comments = "Ticket #12345",
            Password = CustomWebApplicationFactory.SystemAdminUserPassword
        });
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.IsNotNull(json);
        Assert.AreEqual(email, json.RootElement.GetProperty("email").GetString());
        Assert.AreEqual(displayName, json.RootElement.GetProperty("displayName").GetString());
    }

    // --- Deactivated User Login Rejection ---

    [TestMethod]
    public async Task Should_RejectLogin_When_UserIsDeactivated()
    {
        var sysClient = await _factory.CreateSystemAdminClientAsync();

        // Create a user
        var email = $"deactivate-login-{Guid.NewGuid():N}@example.com";
        var registerClient = _factory.CreateClient();
        await registerClient.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = "Test1234!",
            DisplayName = "Deactivate Login Test"
        });

        // Verify login works before deactivation
        var loginClient = _factory.CreateClient();
        var loginBefore = await loginClient.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = "Test1234!"
        });
        Assert.AreEqual(HttpStatusCode.OK, loginBefore.StatusCode);

        // Find and deactivate via admin
        var listResponse = await sysClient.GetAsync($"/api/admin/users?search={Uri.EscapeDataString(email)}");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(TestJsonOptions.Default);
        var userId = list!.Items[0].Id;

        await sysClient.PostAsync($"/api/admin/users/{userId}/deactivate", null);

        // Attempt login after deactivation — should be rejected
        var loginAfter = await loginClient.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = "Test1234!"
        });
        Assert.AreNotEqual(HttpStatusCode.OK, loginAfter.StatusCode,
            "Deactivated user should not be able to login");
    }

    // --- Protected Data Reveal Audit Trail ---

    [TestMethod]
    public async Task Should_CreateAuditEntry_When_ProtectedDataIsRevealed()
    {
        var client = await _factory.CreateSystemAdminClientAsync();

        // Create a user whose protected data we'll reveal
        var email = $"audit-reveal-{Guid.NewGuid():N}@example.com";
        var registerClient = _factory.CreateClient();
        await registerClient.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = "Test1234!",
            DisplayName = "Audit Reveal User"
        });

        var listResponse = await client.GetAsync($"/api/admin/users?search={Uri.EscapeDataString(email)}");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(TestJsonOptions.Default);
        var userId = list!.Items[0].Id;

        // Reveal protected data
        var revealResponse = await client.PostAsJsonAsync($"/api/admin/users/{userId}/reveal", new
        {
            Reason = "SupportTicket",
            Password = CustomWebApplicationFactory.SystemAdminUserPassword
        });
        Assert.AreEqual(HttpStatusCode.OK, revealResponse.StatusCode);

        // Check audit log for ProtectedDataRevealed action
        var auditResponse = await client.GetAsync("/api/admin/audit?action=ProtectedDataRevealed&resourceType=User");
        Assert.AreEqual(HttpStatusCode.OK, auditResponse.StatusCode);

        var audit = await auditResponse.Content.ReadFromJsonAsync<PagedResult<AuditEntryDto>>(TestJsonOptions.Default);
        Assert.IsNotNull(audit);

        // Find the audit entry for this specific user
        var entry = audit.Items.FirstOrDefault(e => e.ResourceId == userId.ToString());
        Assert.IsNotNull(entry, "Protected data reveal should create an audit entry for the target user");
    }
}

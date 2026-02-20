namespace LemonDo.Api.Tests.Admin;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Application.Administration.DTOs;
using LemonDo.Domain.Common;

/// <summary>
/// Security hardening tests for AdminEndpoints.
/// A PASSING test means the endpoint correctly REJECTED the attack.
/// A FAILING test means the endpoint is VULNERABLE (it accepted or mishandled the attack).
/// </summary>
[TestClass]
public sealed class AdminSecurityHardeningTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions JsonOpts = TestJsonOptions.Default;

    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomWebApplicationFactory();

    [ClassCleanup]
    public static void ClassCleanup() => _factory.Dispose();

    // ============================================================
    // CATEGORY 1: Authentication Bypass
    // All admin endpoints must reject unauthenticated requests.
    // ============================================================

    [TestMethod]
    public async Task Should_Return401_When_NoTokenOnListUsers()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/users");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_MalformedTokenOnListUsers()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this.is.not.a.valid.jwt");
        var response = await client.GetAsync("/api/admin/users");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_EmptyBearerTokenOnListUsers()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer ");
        var response = await client.GetAsync("/api/admin/users");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_TokenSignedWithWrongKeyOnListUsers()
    {
        var client = _factory.CreateClient();
        // Build a JWT signed with a DIFFERENT key than what the test app uses.
        // The test app expects: "test-secret-key-at-least-32-characters-long!!"
        var wrongKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("wrong-secret-key-at-least-32-characters-long!!"));
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"alg":"HS256","typ":"JWT"}""")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{{\"sub\":\"{Guid.NewGuid()}\",\"email\":\"fake@fake.com\",\"role\":\"SystemAdmin\",\"exp\":{DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()}}}")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var fakeSig = Convert.ToBase64String(Encoding.UTF8.GetBytes("fakesignature")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", $"{header}.{payload}.{fakeSig}");
        var response = await client.GetAsync("/api/admin/users");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_NoTokenOnGetUser()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/admin/users/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_NoTokenOnAssignRole()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{Guid.NewGuid()}/roles",
            new { RoleName = "Admin" });
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_NoTokenOnRemoveRole()
    {
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync($"/api/admin/users/{Guid.NewGuid()}/roles/Admin");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_NoTokenOnDeactivateUser()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/api/admin/users/{Guid.NewGuid()}/deactivate", null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_NoTokenOnReactivateUser()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/api/admin/users/{Guid.NewGuid()}/reactivate", null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_NoTokenOnRevealProtectedData()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}/reveal",
            new { Reason = "SupportTicket", Password = "anything" });
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_NoTokenOnRevealTaskNote()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/admin/tasks/{Guid.NewGuid()}/reveal-note",
            new { Reason = "SupportTicket", Password = "anything" });
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_NoTokenOnAuditLog()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/audit");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ============================================================
    // CATEGORY 2: Privilege Escalation — Regular User → Admin
    // Users with only the "User" role must be blocked from all
    // admin endpoints (expect 403).
    // ============================================================

    [TestMethod]
    public async Task Should_Return403_When_RegularUserListsUsers()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/admin/users");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return403_When_RegularUserGetsSpecificUser()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync($"/api/admin/users/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return403_When_RegularUserAssignsRole()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{Guid.NewGuid()}/roles",
            new { RoleName = "Admin" });
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return403_When_RegularUserRemovesRole()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.DeleteAsync($"/api/admin/users/{Guid.NewGuid()}/roles/Admin");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return403_When_RegularUserDeactivatesUser()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.PostAsync($"/api/admin/users/{Guid.NewGuid()}/deactivate", null);
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return403_When_RegularUserReactivatesUser()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.PostAsync($"/api/admin/users/{Guid.NewGuid()}/reactivate", null);
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return403_When_RegularUserRevealsProtectedData()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}/reveal",
            new { Reason = "SupportTicket", Password = CustomWebApplicationFactory.TestUserPassword });
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return403_When_RegularUserRevealsTaskNote()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync($"/api/admin/tasks/{Guid.NewGuid()}/reveal-note",
            new { Reason = "SupportTicket", Password = CustomWebApplicationFactory.TestUserPassword });
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return403_When_RegularUserAccessesAuditLog()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/admin/audit");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ============================================================
    // CATEGORY 3: Privilege Escalation — Admin → SystemAdmin
    // Admin role can read users and audit log, but MUST be blocked
    // from all SystemAdmin-only write operations (expect 403).
    // ============================================================

    [TestMethod]
    public async Task Should_Return403_When_AdminAssignsRole()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{Guid.NewGuid()}/roles",
            new { RoleName = "Admin" });
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return403_When_AdminRemovesRole()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.DeleteAsync($"/api/admin/users/{Guid.NewGuid()}/roles/Admin");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return403_When_AdminDeactivatesUser()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.PostAsync($"/api/admin/users/{Guid.NewGuid()}/deactivate", null);
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return403_When_AdminReactivatesUser()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.PostAsync($"/api/admin/users/{Guid.NewGuid()}/reactivate", null);
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return403_When_AdminRevealsProtectedData()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}/reveal",
            new { Reason = "SupportTicket", Password = CustomWebApplicationFactory.AdminUserPassword });
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return403_When_AdminRevealsTaskNote()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync($"/api/admin/tasks/{Guid.NewGuid()}/reveal-note",
            new { Reason = "SupportTicket", Password = CustomWebApplicationFactory.AdminUserPassword });
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ============================================================
    // CATEGORY 4: Invalid Role Injection
    // Assigning non-existent or dangerous role names must fail.
    // ============================================================

    [TestMethod]
    public async Task Should_RejectNonExistentRole_When_SystemAdminAssignsSuperAdmin()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{userId}/roles",
            new { RoleName = "SuperAdmin" });

        // Must not succeed — role does not exist
        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
            "Assigning non-existent role 'SuperAdmin' should not return 200");
    }

    [TestMethod]
    public async Task Should_RejectEmptyRoleName_When_SystemAdminAssignsRole()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{userId}/roles",
            new { RoleName = "" });

        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
            "Assigning empty role name should not succeed");
    }

    [TestMethod]
    public async Task Should_RejectSqlInjectionInRoleName_When_SystemAdminAssignsRole()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{userId}/roles",
            new { RoleName = "'; DROP TABLE AspNetRoles; --" });

        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
            "SQL injection in role name should not succeed");
    }

    [TestMethod]
    public async Task Should_RejectSqlInjectionInRoleNameDelete_When_SystemAdminRemovesRole()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        // SQL injection in the roleName path segment
        var response = await client.DeleteAsync(
            $"/api/admin/users/{userId}/roles/%27%3B DROP TABLE AspNetRoles%3B --");

        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
            "SQL injection in role name path segment should not succeed");
    }

    [TestMethod]
    public async Task Should_RejectNullRoleName_When_SystemAdminAssignsRole()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{userId}/roles",
            new { RoleName = (string?)null });

        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
            "Null role name should not succeed");
    }

    [TestMethod]
    public async Task Should_RejectExcessivelyLongRoleName_When_SystemAdminAssignsRole()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{userId}/roles",
            new { RoleName = new string('A', 5000) });

        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
            "Extremely long role name should not succeed");
    }

    // ============================================================
    // CATEGORY 5: Self-Role Escalation
    // A SystemAdmin must not be able to escalate privileges via
    // the role assignment endpoint targeting themselves.
    // ============================================================

    [TestMethod]
    public async Task Should_NotAllowSelfEscalation_When_SystemAdminAssignsSystemAdminToSelf()
    {
        var sysAdminClient = await _factory.CreateSystemAdminClientAsync();

        // Find the sysadmin user by searching for the known email
        var listResponse = await sysAdminClient.GetAsync(
            $"/api/admin/users?search={Uri.EscapeDataString(CustomWebApplicationFactory.SystemAdminUserEmail)}");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(JsonOpts);
        Assert.IsNotNull(list);
        Assert.IsNotEmpty(list.Items, "Should find the sysadmin user");
        var sysAdminId = list.Items[0].Id;

        // SystemAdmin already has SystemAdmin role — assigning again must conflict
        var response = await sysAdminClient.PostAsJsonAsync(
            $"/api/admin/users/{sysAdminId}/roles",
            new { RoleName = "SystemAdmin" });

        // Should return conflict (409) since they already have the role
        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
            "A SystemAdmin assigning SystemAdmin to themselves (already assigned) must not return 200");
    }

    // ============================================================
    // CATEGORY 6: IDOR on User Management
    // Operations on non-existent user IDs must return 404, not 500
    // or other error that leaks internal state.
    // ============================================================

    [TestMethod]
    public async Task Should_Return404_When_AdminGetsNonExistentUserId()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.GetAsync($"/api/admin/users/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return4xx_When_SystemAdminAssignsRoleToNonExistentUser()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{Guid.NewGuid()}/roles",
            new { RoleName = "Admin" });

        // Must be 4xx (404 not found), not 200 or 500
        Assert.IsTrue(
            (int)response.StatusCode >= 400 && (int)response.StatusCode < 500,
            $"Expected 4xx for non-existent user, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_SystemAdminRemovesRoleFromNonExistentUser()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var response = await client.DeleteAsync($"/api/admin/users/{Guid.NewGuid()}/roles/Admin");

        Assert.IsTrue(
            (int)response.StatusCode >= 400 && (int)response.StatusCode < 500,
            $"Expected 4xx for non-existent user, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_SystemAdminDeactivatesNonExistentUser()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var response = await client.PostAsync($"/api/admin/users/{Guid.NewGuid()}/deactivate", null);

        Assert.IsTrue(
            (int)response.StatusCode >= 400 && (int)response.StatusCode < 500,
            $"Expected 4xx for non-existent user, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_SystemAdminReactivatesNonExistentUser()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var response = await client.PostAsync($"/api/admin/users/{Guid.NewGuid()}/reactivate", null);

        Assert.IsTrue(
            (int)response.StatusCode >= 400 && (int)response.StatusCode < 500,
            $"Expected 4xx for non-existent user, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return404_When_SystemAdminRevealsDataForNonExistentUser()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var response = await client.PostAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}/reveal",
            new
            {
                Reason = "SupportTicket",
                Password = CustomWebApplicationFactory.SystemAdminUserPassword
            });
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return404_When_SystemAdminRevealsTaskNoteForNonExistentTask()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var response = await client.PostAsJsonAsync($"/api/admin/tasks/{Guid.NewGuid()}/reveal-note",
            new
            {
                Reason = "SupportTicket",
                Password = CustomWebApplicationFactory.SystemAdminUserPassword
            });
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ============================================================
    // CATEGORY 7: Protected Data Reveal — Password Re-Authentication
    // Reveal endpoints must reject empty, missing, or wrong passwords.
    // ============================================================

    [TestMethod]
    public async Task Should_Return401_When_WrongPasswordOnRevealProtectedData()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        var response = await client.PostAsJsonAsync($"/api/admin/users/{userId}/reveal",
            new
            {
                Reason = "SupportTicket",
                Password = "TotallyWrongPassword999!"
            });
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "Wrong password must be rejected with 401");
    }

    [TestMethod]
    public async Task Should_Return401_When_EmptyPasswordOnRevealProtectedData()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        var response = await client.PostAsJsonAsync($"/api/admin/users/{userId}/reveal",
            new
            {
                Reason = "SupportTicket",
                Password = ""
            });
        // Empty password must be rejected — 401 or 400
        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
            "Empty password must not allow data reveal");
    }

    [TestMethod]
    public async Task Should_Return400_When_InvalidReasonOnRevealProtectedData()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        var response = await client.PostAsJsonAsync($"/api/admin/users/{userId}/reveal",
            new
            {
                Reason = "HackerReason",
                Password = CustomWebApplicationFactory.SystemAdminUserPassword
            });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Invalid reason enum value should return 400");
    }

    [TestMethod]
    public async Task Should_Return400_When_OtherReasonHasNoDetailsOnRevealProtectedData()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        var response = await client.PostAsJsonAsync($"/api/admin/users/{userId}/reveal",
            new
            {
                Reason = "Other",
                ReasonDetails = (string?)null,
                Password = CustomWebApplicationFactory.SystemAdminUserPassword
            });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Reason=Other without ReasonDetails must return 400");
    }

    [TestMethod]
    public async Task Should_Return401_When_WrongPasswordOnRevealTaskNote()
    {
        var client = await _factory.CreateSystemAdminClientAsync();

        var response = await client.PostAsJsonAsync($"/api/admin/tasks/{Guid.NewGuid()}/reveal-note",
            new
            {
                Reason = "SupportTicket",
                Password = "TotallyWrongPassword999!"
            });
        // Will be 404 (no task) or 401 (wrong password rejected before task lookup)
        // Either response is acceptable as long as it is NOT 200 with data
        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
            "Wrong password with non-existent task must not return 200 with data");
    }

    [TestMethod]
    public async Task Should_Return400_When_InvalidReasonOnRevealTaskNote()
    {
        var client = await _factory.CreateSystemAdminClientAsync();

        var response = await client.PostAsJsonAsync($"/api/admin/tasks/{Guid.NewGuid()}/reveal-note",
            new
            {
                Reason = "NotARealReason",
                Password = CustomWebApplicationFactory.SystemAdminUserPassword
            });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Invalid reason enum should return 400 before any task lookup");
    }

    [TestMethod]
    public async Task Should_Return400_When_OtherReasonHasNoDetailsOnRevealTaskNote()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        var response = await client.PostAsJsonAsync($"/api/admin/tasks/{userId}/reveal-note",
            new
            {
                Reason = "Other",
                ReasonDetails = (string?)null,
                Password = CustomWebApplicationFactory.SystemAdminUserPassword
            });
        // 400 from validation OR 404 from no such task — must not be 200
        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
            "Other reason with no details must not reveal data");
    }

    // ============================================================
    // CATEGORY 8: Audit Log Bypass / Manipulation
    // The audit log is read-only. There must be no endpoint that
    // allows writing, updating, or deleting audit entries directly.
    // ============================================================

    [TestMethod]
    public async Task Should_Return405_When_SystemAdminPostsToAuditLog()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var response = await client.PostAsJsonAsync("/api/admin/audit",
            new { Action = "TaskCreated", ResourceType = "Task", ResourceId = Guid.NewGuid().ToString() });

        // 405 Method Not Allowed or 404 (route doesn't exist)
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.MethodNotAllowed
            || response.StatusCode == HttpStatusCode.NotFound,
            $"POST to audit log should not exist. Got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return405_When_SystemAdminDeletesFromAuditLog()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var response = await client.DeleteAsync($"/api/admin/audit/{Guid.NewGuid()}");

        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.MethodNotAllowed
            || response.StatusCode == HttpStatusCode.NotFound,
            $"DELETE on audit entry should not exist. Got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return405_When_SystemAdminPutsToAuditLog()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var response = await client.PutAsJsonAsync($"/api/admin/audit/{Guid.NewGuid()}",
            new { Details = "Tampered" });

        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.MethodNotAllowed
            || response.StatusCode == HttpStatusCode.NotFound,
            $"PUT on audit entry should not exist. Got {(int)response.StatusCode}");
    }

    // ============================================================
    // CATEGORY 9: Pagination Abuse
    // Extremely large pageSize values must not crash the server or
    // return excessively large responses (resource exhaustion).
    // ============================================================

    [TestMethod]
    public async Task Should_NotCrash_When_AdminRequestsHugePageSizeOnListUsers()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.GetAsync("/api/admin/users?pageSize=999999&page=1");

        // Must not return 500 — either returns data capped or 400
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "pageSize=999999 must not cause a 500 Internal Server Error");
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK || (int)response.StatusCode == 400,
            $"Expected 200 or 400 for huge pageSize, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotCrash_When_AdminRequestsHugePageSizeOnAuditLog()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.GetAsync("/api/admin/audit?pageSize=999999&page=1");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "pageSize=999999 on audit log must not cause a 500");
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK || (int)response.StatusCode == 400,
            $"Expected 200 or 400 for huge pageSize, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotCrash_When_AdminRequestsNegativePageSize()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.GetAsync("/api/admin/users?pageSize=-1&page=1");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "pageSize=-1 must not cause a 500");
    }

    [TestMethod]
    public async Task Should_NotCrash_When_AdminRequestsZeroPageSize()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.GetAsync("/api/admin/users?pageSize=0&page=1");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "pageSize=0 must not cause a 500");
    }

    [TestMethod]
    public async Task Should_NotCrash_When_AdminRequestsNegativePage()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.GetAsync("/api/admin/users?page=-1&pageSize=10");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "page=-1 must not cause a 500");
    }

    // ============================================================
    // CATEGORY 10: Information Leakage
    // Error responses must not expose stack traces, internal IDs,
    // database schemas, or unredacted PII in non-reveal contexts.
    // ============================================================

    [TestMethod]
    public async Task Should_NotLeakStackTrace_When_AdminGetsNonExistentUser()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.GetAsync($"/api/admin/users/{Guid.NewGuid()}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.IsFalse(body.Contains("StackTrace", StringComparison.OrdinalIgnoreCase),
            "404 response must not include StackTrace");
        Assert.IsFalse(body.Contains("System.", StringComparison.OrdinalIgnoreCase),
            "404 response must not include .NET type names");
    }

    [TestMethod]
    public async Task Should_NotLeakStackTrace_When_RevealFailsDueToWrongPassword()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        var response = await client.PostAsJsonAsync($"/api/admin/users/{userId}/reveal",
            new { Reason = "SupportTicket", Password = "WrongPass999!" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.IsFalse(body.Contains("StackTrace", StringComparison.OrdinalIgnoreCase),
            "Error response on wrong password must not expose StackTrace");
        Assert.IsFalse(body.Contains("System.", StringComparison.OrdinalIgnoreCase),
            "Error response must not expose .NET type names");
    }

    [TestMethod]
    public async Task Should_ReturnRedactedEmail_When_AdminListsUsers()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.GetAsync("/api/admin/users");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(JsonOpts);

        Assert.IsNotNull(result);
        foreach (var user in result.Items)
        {
            Assert.IsTrue(user.Email.Contains("***"),
                $"Email '{user.Email}' must be redacted (contain '***') in list view");
            Assert.IsTrue(user.DisplayName.Contains("***"),
                $"DisplayName '{user.DisplayName}' must be redacted in list view");
        }
    }

    [TestMethod]
    public async Task Should_ReturnRedactedEmail_When_AdminGetsSingleUser()
    {
        var client = await _factory.CreateAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        var response = await client.GetAsync($"/api/admin/users/{userId}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<AdminUserDto>(JsonOpts);
        Assert.IsNotNull(user);
        Assert.IsTrue(user.Email.Contains("***"),
            $"Email '{user.Email}' must be redacted (contain '***') in single-user GET");
        Assert.IsTrue(user.DisplayName.Contains("***"),
            $"DisplayName '{user.DisplayName}' must be redacted in single-user GET");
    }

    [TestMethod]
    public async Task Should_NotExposeXPoweredByHeader_When_AdminListsUsers()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.GetAsync("/api/admin/users");

        Assert.IsFalse(response.Headers.Contains("X-Powered-By"),
            "Response must not expose X-Powered-By header");
        Assert.IsFalse(response.Headers.Contains("Server")
            && (response.Headers.GetValues("Server").Any(v => v.Contains("Kestrel", StringComparison.OrdinalIgnoreCase))),
            "Response must not expose internal server version");
    }

    // ============================================================
    // CATEGORY 11: Input Validation — Reveal Endpoint Payloads
    // Malicious / malformed bodies must not crash the server.
    // ============================================================

    [TestMethod]
    public async Task Should_NotCrash_When_XssPayloadInRevealReason()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        var response = await client.PostAsJsonAsync($"/api/admin/users/{userId}/reveal",
            new
            {
                Reason = "<script>alert('xss')</script>",
                Password = CustomWebApplicationFactory.SystemAdminUserPassword
            });

        // Must be 400 (invalid enum) — never 200 or 500
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "XSS in Reason field should be rejected as invalid enum (400), not crash (500) or succeed (200)");
    }

    [TestMethod]
    public async Task Should_NotCrash_When_SqlInjectionInRevealReason()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        var response = await client.PostAsJsonAsync($"/api/admin/users/{userId}/reveal",
            new
            {
                Reason = "'; DROP TABLE Users; --",
                Password = CustomWebApplicationFactory.SystemAdminUserPassword
            });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "SQL injection in Reason field should be rejected as invalid enum (400)");
    }

    [TestMethod]
    public async Task Should_NotCrash_When_TemplatInjectionInRevealReasonDetails()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        var response = await client.PostAsJsonAsync($"/api/admin/users/{userId}/reveal",
            new
            {
                Reason = "Other",
                ReasonDetails = "{{7*7}} ${7*7} #{7*7}",
                Comments = "Test",
                Password = CustomWebApplicationFactory.SystemAdminUserPassword
            });

        // Template injection in a string field. Must not crash — 200 (data revealed) or 401 (wrong pass via ProtectedValue deserialisation)
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Template injection in ReasonDetails must not cause a 500 crash");
    }

    [TestMethod]
    public async Task Should_NotCrash_When_OversizedReasonDetailsInReveal()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var userId = await GetAnyExistingUserIdAsync(client);

        var response = await client.PostAsJsonAsync($"/api/admin/users/{userId}/reveal",
            new
            {
                Reason = "Other",
                ReasonDetails = new string('A', 1_000_000), // 1 MB string
                Password = CustomWebApplicationFactory.SystemAdminUserPassword
            });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Oversized ReasonDetails must not cause a 500 crash");
    }

    [TestMethod]
    public async Task Should_NotCrash_When_AuditLogFilterHasSqlInjection()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.GetAsync(
            "/api/admin/audit?resourceType=%27%3B+DROP+TABLE+AuditEntries%3B+--");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "SQL injection in resourceType filter must not crash the server");
    }

    [TestMethod]
    public async Task Should_NotCrash_When_SearchFilterHasSqlInjection()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.GetAsync(
            $"/api/admin/users?search={Uri.EscapeDataString("' OR '1'='1")}");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "SQL injection in search filter must not crash the server");
    }

    // ============================================================
    // CATEGORY 12: Business Logic Abuse
    // ============================================================

    [TestMethod]
    public async Task Should_Return4xx_When_DeactivatingAlreadyDeactivatedUser()
    {
        var sysClient = await _factory.CreateSystemAdminClientAsync();

        // Register a fresh user to deactivate
        var email = $"double-deactivate-{Guid.NewGuid():N}@lemondo.dev";
        var registerClient = _factory.CreateClient();
        await registerClient.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = "Test1234!",
            DisplayName = "Double Deactivate Test"
        });

        var listResponse = await sysClient.GetAsync(
            $"/api/admin/users?search={Uri.EscapeDataString(email)}");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(JsonOpts);
        var userId = list!.Items[0].Id;

        // First deactivation
        var first = await sysClient.PostAsync($"/api/admin/users/{userId}/deactivate", null);
        Assert.AreEqual(HttpStatusCode.OK, first.StatusCode, "First deactivation should succeed");

        // Second deactivation — must not succeed silently (business rule: already deactivated)
        var second = await sysClient.PostAsync($"/api/admin/users/{userId}/deactivate", null);
        Assert.AreNotEqual(HttpStatusCode.OK, second.StatusCode,
            "Double-deactivating the same user should not return 200 — business rule violation");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_ReactivatingAlreadyActiveUser()
    {
        var sysClient = await _factory.CreateSystemAdminClientAsync();

        // Reactivate an already-active user (the admin itself)
        var listResponse = await sysClient.GetAsync(
            $"/api/admin/users?search={Uri.EscapeDataString(CustomWebApplicationFactory.AdminUserEmail)}");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(JsonOpts);
        Assert.IsNotEmpty(list!.Items, "Should find the admin user");
        var adminId = list.Items[0].Id;

        var response = await sysClient.PostAsync($"/api/admin/users/{adminId}/reactivate", null);
        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
            "Reactivating an already-active user should not return 200");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_AssigningSameRoleTwice()
    {
        var sysClient = await _factory.CreateSystemAdminClientAsync();

        // Register a fresh user, assign Admin, then try to assign Admin again
        var email = $"double-role-{Guid.NewGuid():N}@lemondo.dev";
        var registerClient = _factory.CreateClient();
        await registerClient.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = "Test1234!",
            DisplayName = "Double Role Test"
        });

        var listResponse = await sysClient.GetAsync(
            $"/api/admin/users?search={Uri.EscapeDataString(email)}");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(JsonOpts);
        var userId = list!.Items[0].Id;

        // First assignment
        var first = await sysClient.PostAsJsonAsync(
            $"/api/admin/users/{userId}/roles", new { RoleName = "Admin" });
        Assert.AreEqual(HttpStatusCode.OK, first.StatusCode, "First role assignment should succeed");

        // Second assignment of the same role
        var second = await sysClient.PostAsJsonAsync(
            $"/api/admin/users/{userId}/roles", new { RoleName = "Admin" });
        Assert.AreNotEqual(HttpStatusCode.OK, second.StatusCode,
            "Assigning the same role twice must not succeed (conflict expected)");
    }

    // ============================================================
    // CATEGORY 13: Token Revocation on Deactivation (VULNERABILITY)
    // When a user is deactivated, their existing tokens MUST be revoked.
    // A deactivated user must NOT be able to:
    //   1. Continue using their existing access token
    //   2. Refresh their tokens to get new access tokens
    // FAILING TEST: This exposes a vulnerability where deactivated users
    // can continue accessing the API with existing tokens.
    // ============================================================

    [TestMethod]
    public async Task Should_RevokeRefreshToken_When_UserIsDeactivated()
    {
        // Create client with cookie handling enabled to maintain refresh token cookie
        var userClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        // 1. Register a new user and get their tokens
        var email = $"deactivate-token-test-{Guid.NewGuid():N}@lemondo.dev";
        var password = "TestPass123!";
        var registerResponse = await userClient.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            DisplayName = "Deactivate Token Test"
        });
        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode, "Registration should succeed");

        // 2. Login to get tokens (refresh token is in HttpOnly cookie)
        var loginResponse = await userClient.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });
        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode, "Login should succeed");
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        Assert.IsNotNull(loginResult);
        var accessToken = loginResult.AccessToken;

        // 3. Get user ID via admin
        var sysClient = await _factory.CreateSystemAdminClientAsync();
        var listResponse = await sysClient.GetAsync($"/api/admin/users?search={Uri.EscapeDataString(email)}");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(JsonOpts);
        Assert.IsNotEmpty(list!.Items, "Should find the newly registered user");
        var userId = list.Items[0].Id;

        // 4. Verify the user can access protected endpoints before deactivation
        userClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        var beforeDeactivateResponse = await userClient.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.OK, beforeDeactivateResponse.StatusCode,
            "User should be able to access tasks before deactivation");

        // 5. Deactivate the user via SystemAdmin
        var deactivateResponse = await sysClient.PostAsync($"/api/admin/users/{userId}/deactivate", null);
        Assert.AreEqual(HttpStatusCode.OK, deactivateResponse.StatusCode, "Deactivation should succeed");

        // 6. VULNERABILITY: Try to refresh tokens - this should FAIL but currently PASSES
        // A deactivated user should NOT be able to refresh their tokens
        // The refresh token cookie should be automatically sent with the request
        var refreshResponse = await userClient.PostAsync("/api/auth/refresh", null);

        // SECURITY BREACH: If this returns 200, the vulnerability exists!
        // The correct behavior should be 401 Unauthorized
        Assert.AreEqual(
            HttpStatusCode.Unauthorized,
            refreshResponse.StatusCode,
            "VULNERABILITY EXPOSED: Deactivated user was able to refresh tokens! " +
            $"Got {(int)refreshResponse.StatusCode} instead of 401. " +
            "RefreshTokenAsync should check if the user is deactivated before issuing new tokens.");
    }

    [TestMethod]
    public async Task Should_RevokeAccessToken_When_UserIsDeactivated()
    {
        // This test verifies that access tokens are immediately invalidated on deactivation.
        // In JWT-based systems, this requires either:
        // 1. Short token expiry + refresh token revocation, OR
        // 2. A token blacklist / invalidation mechanism
        //
        // Note: This test may pass if the endpoint checks user status on every request,
        // but the refresh token vulnerability above is the more critical issue.

        // 1. Register a new user and get their tokens
        var email = $"deactivate-access-test-{Guid.NewGuid():N}@lemondo.dev";
        var password = "TestPass123!";
        var userClient = _factory.CreateClient();
        await userClient.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            DisplayName = "Deactivate Access Test"
        });

        // 2. Login to get access token
        var loginResponse = await userClient.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        var accessToken = loginResult!.AccessToken;

        // 3. Get user ID via admin
        var sysClient = await _factory.CreateSystemAdminClientAsync();
        var listResponse = await sysClient.GetAsync($"/api/admin/users?search={Uri.EscapeDataString(email)}");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(JsonOpts);
        var userId = list!.Items[0].Id;

        // 4. Deactivate the user
        await sysClient.PostAsync($"/api/admin/users/{userId}/deactivate", null);

        // 5. Try to use the access token - should be rejected
        userClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await userClient.GetAsync("/api/tasks");

        // Ideally this should be 401, but many JWT implementations allow
        // the token to live until expiry. The critical issue is refresh token validation.
        // We'll check if the endpoint validates user status on each request.
        Assert.AreEqual(
            HttpStatusCode.Unauthorized,
            response.StatusCode,
            "VULNERABILITY: Deactivated user's access token still works! " +
            "Endpoints should validate that the user is still active.");
    }

    /// <summary>Authentication response from login/register endpoints.</summary>
    private sealed record AuthResponse(string AccessToken, UserResponse User);
    private sealed record UserResponse(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles);

    // ============================================================
    // CATEGORY 14: HTTP Method Enforcement
    // ============================================================

    [TestMethod]
    public async Task Should_Return405_When_PutSentToListUsers()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var response = await client.PutAsJsonAsync("/api/admin/users", new { });

        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.MethodNotAllowed
            || response.StatusCode == HttpStatusCode.NotFound,
            $"PUT /api/admin/users should not be routable. Got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return405_When_DeleteSentToDeactivate()
    {
        var client = await _factory.CreateSystemAdminClientAsync();
        var response = await client.DeleteAsync($"/api/admin/users/{Guid.NewGuid()}/deactivate");

        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.MethodNotAllowed
            || response.StatusCode == HttpStatusCode.NotFound,
            $"DELETE /deactivate should not work. Got {(int)response.StatusCode}");
    }

    // ============================================================
    // Helper
    // ============================================================

    /// <summary>
    /// Returns the ID of any user visible in the admin user list.
    /// Used to get a valid user ID for subsequent operations without creating a new user.
    /// </summary>
    private static async Task<Guid> GetAnyExistingUserIdAsync(HttpClient sysAdminClient)
    {
        var response = await sysAdminClient.GetAsync("/api/admin/users?pageSize=1");
        response.EnsureSuccessStatusCode();
        var list = await response.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(JsonOpts);
        Assert.IsNotNull(list);
        Assert.IsNotEmpty(list.Items, "No users found in the test database");
        return list.Items[0].Id;
    }
}

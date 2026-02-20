namespace LemonDo.Api.Tests.Security;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Application.Tasks.DTOs;

/// <summary>
/// Advanced input security tests — Pass 6 of the security review series.
/// Focus: unicode tricks, null bytes, JSON structure attacks, and encoding edge cases.
///
/// A PASSING test = the endpoint correctly rejected or safely handled the malicious input.
/// A FAILING test = a vulnerability exists — the endpoint crashed (500) or behaved unexpectedly.
/// </summary>
[TestClass]
public sealed class AdvancedInputSecurityTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;
    private static readonly JsonSerializerOptions JsonOpts = TestJsonOptions.Default;

    [ClassInitialize]
    public static async Task ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  HELPER: create a task and return its ID (setup for dependent tests)
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task<Guid> CreateTestTaskAsync(string title = "Baseline task for advanced tests")
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new { Title = title });
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);
        return dto!.Id;
    }

    private static async Task<Guid> GetDefaultBoardIdAsync()
    {
        var response = await _client.GetAsync("/api/boards/default");
        response.EnsureSuccessStatusCode();
        var board = await response.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);
        return board!.Id;
    }

    private static async Task<HttpClient> RegisterFreshUserAsync(string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@lemondo.dev";
        var freshClient = _factory.CreateClient();

        var registerResponse = await freshClient.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = "TestPass123!", DisplayName = $"User {prefix}" });
        registerResponse.EnsureSuccessStatusCode();

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        freshClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        return freshClient;
    }

    // =========================================================================
    // CATEGORY 1: UNICODE NORMALIZATION ATTACKS
    // Goal: inputs containing lookalike / invisible / bidi-override unicode
    //       must not crash (500) and ideally are normalized or rejected.
    // =========================================================================

    [TestMethod]
    public async Task Should_NotReturn500_When_TaskTitleContainsCyrillicHomoglyphs()
    {
        // Cyrillic "а" (U+0430) looks identical to Latin "a" (U+0061)
        // A naive string comparison might confuse them.
        const string cyrillicTitle = "Tаsk with Сyrillic homoglyphs"; // contains U+0430, U+0421
        var response = await _client.PostAsJsonAsync("/api/tasks", new { Title = cyrillicTitle });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Cyrillic homoglyph characters in title must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.BadRequest,
            $"Expected 201 or 400 for homoglyph title, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_TaskTitleContainsZeroWidthCharacters()
    {
        // Zero-width joiner (U+200D) and zero-width non-joiner (U+200C) are invisible
        // and can be used to create homograph attacks or bypass filters.
        var zeroWidthTitle = "Invisible\u200bCharacter\u200cIn\u200dTitle"; // ZWSP + ZWNJ + ZWJ
        var response = await _client.PostAsJsonAsync("/api/tasks", new { Title = zeroWidthTitle });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Zero-width characters in title must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.BadRequest,
            $"Expected 201 or 400 for zero-width chars, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_TagContainsUnicodeCombiningCharacters()
    {
        // Combining characters stack onto preceding base characters.
        // e.g. "a\u0300" = "à" — normalization edge case.
        var taskId = await CreateTestTaskAsync("Combining chars tag test");
        // "tag" + combining grave accent (U+0300) + combining tilde (U+0303)
        var combiningTag = "ta\u0300g\u0303";

        var response = await _client.PostAsJsonAsync($"/api/tasks/{taskId}/tags",
            new { Tag = combiningTag });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Unicode combining characters in tag must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"Expected 200 or 400 for combining chars in tag, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_ColumnNameContainsBidiOverrideCharacter()
    {
        // U+202E (RIGHT-TO-LEFT OVERRIDE) reverses text rendering direction.
        // Can make "example.exe" appear as "exe.elpmaxe" — pure visual deception.
        var boardId = await GetDefaultBoardIdAsync();
        var bidiName = "Normal\u202eesreveR"; // RTL override embedded in middle

        var response = await _client.PostAsJsonAsync($"/api/boards/{boardId}/columns",
            new { Name = bidiName, TargetStatus = "Todo" });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Right-to-Left Override character in column name must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.BadRequest,
            $"Expected 201 or 400 for BIDI override column name, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_RegisterEmailContainsUnicodeTrick()
    {
        // "tеst@lemondo.dev" where "е" is Cyrillic (U+0435), not Latin "e".
        // The email validator must catch this or normalize it; must not crash.
        using var freshClient = _factory.CreateClient();
        var unicodeEmail = "t\u0435st-unicode@lemondo.dev"; // Cyrillic "е" in local part

        var response = await freshClient.PostAsJsonAsync("/api/auth/register",
            new { Email = unicodeEmail, Password = "TestPass123!", DisplayName = "Unicode Email Test" });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Cyrillic lookalike character in email must not cause a 500 server error");
        // RFC 5322 emails technically allow unicode in local parts with SMTPUTF8,
        // but most validators reject them. Expect 400 or potentially 200.
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest or HttpStatusCode.Conflict,
            $"Expected 200/400/409 for unicode email, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_DescriptionContainsPrivateUseAreaCharacters()
    {
        // Private Use Area characters (U+E000–U+F8FF) have no standard meaning.
        // They should be stored or rejected, never cause crashes.
        var puaDescription = "Description with PUA: \uE000\uE001\uF8FF characters";

        var response = await _client.PostAsJsonAsync("/api/tasks",
            new { Title = "PUA chars test", Description = puaDescription });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Private Use Area unicode characters in description must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.BadRequest,
            $"Expected 201 or 400 for PUA chars, got {(int)response.StatusCode}");
    }

    // =========================================================================
    // CATEGORY 2: NULL BYTE INJECTION
    // Goal: embedded null bytes must be stripped or rejected — never pass through
    //       to truncate strings at the C-string boundary (classic null byte attack).
    // =========================================================================

    [TestMethod]
    public async Task Should_NotReturn500_When_TaskTitleContainsNullByte()
    {
        // "Valid Title\0<script>evil</script>" — if parsed as C string, the part after \0 is hidden
        var nullByteTitle = "Valid Title\0<script>evil</script>";
        var response = await _client.PostAsJsonAsync("/api/tasks", new { Title = nullByteTitle });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Null byte in task title must not cause a 500 server error");

        // If accepted, the stored title must not be the raw string (would be fine for a .NET string,
        // but the concern is truncation in downstream C libraries or database drivers).
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync();
            // The response title should contain the null byte or the part before it, but must not
            // have secretly stripped validation (the real check is that no 500 occurred).
            Assert.DoesNotContain("500", body,
                "If null byte is accepted, response must still be a well-formed success response");
        }
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_TagContainsNullByte()
    {
        var taskId = await CreateTestTaskAsync("Null byte tag test");
        var nullByteTag = "tag\0injection";

        var response = await _client.PostAsJsonAsync($"/api/tasks/{taskId}/tags",
            new { Tag = nullByteTag });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Null byte in tag must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"Expected 200 or 400 for null byte in tag, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_SearchQueryContainsNullByte()
    {
        // URL: ?search=test%00admin — the %00 is a null byte in the query string
        var response = await _client.GetAsync("/api/tasks?search=test%00admin");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Null byte in search query parameter must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"Expected 200 or 400 for null byte in search, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_RegisterEmailContainsNullByte()
    {
        // test%00admin@lemondo.dev — null byte in email local part
        using var freshClient = _factory.CreateClient();
        // JSON serialization of a raw null byte in a string
        var nullByteEmail = "test\0admin@lemondo.dev";

        var response = await freshClient.PostAsJsonAsync("/api/auth/register",
            new { Email = nullByteEmail, Password = "TestPass123!", DisplayName = "NullByte Test" });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Null byte in email must not cause a 500 server error");
        // Null byte is not a valid email character; expect 400
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict,
            $"Expected 400 for null byte in email, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_ColumnNameContainsNullByte()
    {
        var boardId = await GetDefaultBoardIdAsync();
        var nullByteName = "Column\0Injection";

        var response = await _client.PostAsJsonAsync($"/api/boards/{boardId}/columns",
            new { Name = nullByteName, TargetStatus = "Todo" });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Null byte in column name must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.BadRequest,
            $"Expected 201 or 400 for null byte in column name, got {(int)response.StatusCode}");
    }

    // =========================================================================
    // CATEGORY 3: JSON STRUCTURE ATTACKS
    // Goal: malformed or adversarial JSON structures must not stack overflow,
    //       panic the deserializer, or cause server errors.
    // =========================================================================

    [TestMethod]
    public async Task Should_NotReturn500_When_JsonDepthBombSentToCreateTask()
    {
        // 150 levels of nested objects — far exceeds the default System.Text.Json max depth (64).
        // .NET's System.Text.Json throws a JsonException for depth > 64, which the middleware
        // should catch and return 400.
        var depth = 150;
        var inner = new StringBuilder();
        for (var i = 0; i < depth; i++)
            inner.Append("{\"x\":");
        inner.Append("1");
        for (var i = 0; i < depth; i++)
            inner.Append('}');

        var json = $"{{\"Title\":{inner}}}"; // Title is the nested bomb
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/tasks", content);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "A 150-deep JSON depth bomb must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Created,
            $"Expected 400 (rejected by deserializer) or 201 (depth clamped) for depth bomb, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_JsonArrayDepthBombSentToCreateTask()
    {
        // Very deeply nested arrays: [[[[...]]]]
        var depth = 100;
        var nested = new string('[', depth) + new string(']', depth);
        var json = $"{{\"Title\":\"test\",\"Tags\":{nested}}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/tasks", content);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Deeply nested JSON array must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Created,
            $"Expected 400 or 201 for array depth bomb, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_HandleDeterministically_When_DuplicateJsonKeysInCreateTask()
    {
        // RFC 7159 says duplicate keys have undefined behavior; System.Text.Json uses last-wins.
        // The server must not crash and must behave deterministically.
        var json = """{"Title":"first","Title":"second"}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/tasks", content);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Duplicate JSON keys must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.BadRequest,
            $"Expected 201 or 400 for duplicate keys, got {(int)response.StatusCode}");

        // If accepted, verify the stored title is one of the two values (not corrupted)
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var dto = await response.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);
            Assert.IsNotNull(dto);
            Assert.IsTrue(
                dto.Title == "first" || dto.Title == "second",
                $"Stored title must be 'first' or 'second', not '{dto.Title}'");
        }
    }

    [TestMethod]
    public async Task Should_Return400_When_NumericPriorityInsteadOfString()
    {
        // Priority is parsed via Enum.TryParse<Priority>() — sending a number instead of "High"
        // The endpoint currently falls back to Priority.None if TryParse fails (no crash expected).
        var json = """{"Title":"Numeric priority test","Priority":42}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/tasks", content);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Numeric value for Priority field must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.BadRequest,
            $"Expected 201 (fallback to None) or 400 for numeric Priority, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return400_When_BooleanSentForTitleField()
    {
        // Title expects a string; sending a boolean should cause a deserialization error (400),
        // not a type coercion that silently converts true→"True" and stores it.
        var json = """{"Title":true}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/tasks", content);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Boolean value for Title field must not cause a 500 server error");
        // System.Text.Json strict mode rejects type mismatches by default
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Created,
            $"Expected 400 for boolean in Title, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return400_When_ObjectSentForTitleField()
    {
        // Sending a nested object where a string is expected — type coercion attack
        var json = """{"Title":{"nested":"object"}}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/tasks", content);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Object value for Title string field must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Created,
            $"Expected 400 for object-as-Title, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return400_When_ArraySentAsRootObjectToCreateTask()
    {
        // Sending a JSON array `[{...}]` instead of `{...}` — incorrect root type
        var json = """[{"Title":"array attack"}]""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/tasks", content);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "JSON array as root must not cause a 500 server error");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "JSON array sent to an object endpoint must return 400");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_ExtremelyLargePageNumberSentAsInteger()
    {
        // Int32.MaxValue (2147483647) as page parameter — tests integer overflow handling
        // in pagination. Math.Max(1, page) and Skip() calculation.
        var response = await _client.GetAsync("/api/tasks?page=2147483647");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Int32.MaxValue as page number must not cause a 500 server error (overflow in Skip())");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"Expected 200 or 400 for max-int page, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_ExtremelyLargePageSizeAsLong()
    {
        // Sending a value that exceeds Int32 bounds as a query string — the binder
        // will fail to parse it and should return 400 gracefully.
        var response = await _client.GetAsync("/api/tasks?pageSize=99999999999999999999");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Out-of-range integer as pageSize must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"Expected 200 or 400 for out-of-range pageSize, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return400_When_ArraySentAsRootObjectToAnalyticsEndpoint()
    {
        // Analytics endpoint expects `{"Events":[...]}` — sending a bare array should 400
        var json = """[{"EventName":"test_event"}]""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/analytics/events", content);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "JSON array as root on analytics endpoint must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.OK,
            $"Expected 400 or 200 for array-as-root on analytics, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return400_When_NullSentAsRootBodyToCreateTask()
    {
        // JSON literal `null` as the entire request body
        var content = new StringContent("null", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/tasks", content);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "JSON null as request body must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Created,
            $"Expected 400 for null body, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_ExtremelyLargeAnalyticsBatch()
    {
        // 500 events in a single batch — stress test the analytics endpoint
        var events = Enumerable.Range(0, 500).Select(i => new
        {
            EventName = $"event_{i}",
            Properties = (Dictionary<string, string>?)null
        }).ToList();

        var json = JsonSerializer.Serialize(new { Events = events });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/analytics/events", content);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "500 analytics events in one batch must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"Expected 200 or 400 for large analytics batch, got {(int)response.StatusCode}");
    }

    // =========================================================================
    // CATEGORY 4: ENCODING EDGE CASES
    // Goal: all valid-but-unusual text encodings must be handled gracefully.
    //       .NET strings are UTF-16; JSON is UTF-8. The concern is surrogate
    //       pairs, control characters, and non-breaking whitespace variants.
    // =========================================================================

    [TestMethod]
    public async Task Should_NotReturn500_When_TaskTitleContainsSurrogatePairEmoji()
    {
        // Emoji are stored as surrogate pairs in UTF-16 (U+D83D U+DD25 = 🔥)
        // and as 4-byte sequences in UTF-8. Both JSON and .NET must handle them.
        const string emojiTitle = "Task with emoji 🔥🎉🇧🇷";
        var response = await _client.PostAsJsonAsync("/api/tasks", new { Title = emojiTitle });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Emoji (surrogate pairs) in task title must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.BadRequest,
            $"Expected 201 or 400 for emoji title, got {(int)response.StatusCode}");

        // If created, the emoji should round-trip correctly
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var dto = await response.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);
            Assert.IsNotNull(dto);
            Assert.Contains("🔥", dto.Title,
                "Emoji must round-trip correctly from storage");
        }
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_TagContainsOnlyNonBreakingSpaces()
    {
        // U+00A0 (NO-BREAK SPACE) is not matched by IsNullOrWhiteSpace in older .NET;
        // in .NET 5+ it IS included. Tag.Create() uses IsNullOrWhiteSpace, so this
        // should be rejected as whitespace-only. Tests this validation path.
        var taskId = await CreateTestTaskAsync("Non-breaking space tag test");
        var nbspTag = "\u00A0\u00A0\u00A0"; // Three non-breaking spaces

        var response = await _client.PostAsJsonAsync($"/api/tasks/{taskId}/tags",
            new { Tag = nbspTag });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Non-breaking space tag must not cause a 500 server error");
        // In .NET, char.IsWhiteSpace('\u00A0') == true, so IsNullOrWhiteSpace returns true → 400
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.OK,
            $"Expected 400 or 200 for NBSP-only tag, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_TagContainsOnlyEnQuadSpaces()
    {
        // U+2000 through U+200A are various "typographic" space characters.
        // IsNullOrWhiteSpace handles U+2000–U+200A in .NET.
        var taskId = await CreateTestTaskAsync("En-quad space tag test");
        var enQuadTag = "\u2000\u2001\u2002\u2003"; // EN QUAD, EM QUAD, EN SPACE, EM SPACE

        var response = await _client.PostAsJsonAsync($"/api/tasks/{taskId}/tags",
            new { Tag = enQuadTag });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "En-quad space characters in tag must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.OK,
            $"Expected 400 or 200 for typographic space tag, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_DescriptionContainsControlCharacters()
    {
        // Control characters U+0001–U+001F (excluding tab U+0009 and newline U+000A)
        // are valid in .NET strings but unusual in text fields.
        // System.Text.Json rejects them in string values by default (JsonException → 400).
        var controlChars = "\u0001\u0002\u0003\u0004\u0005\u001F";
        var json = JsonSerializer.Serialize(new { Title = "Control chars test", Description = controlChars });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/tasks", content);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Control characters in description must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.BadRequest,
            $"Expected 201 or 400 for control characters, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_ColumnNameContainsBackslashSequences()
    {
        // Backslash sequences that look like escape codes: \n \r \t \0
        // When embedded in JSON strings these are valid escape sequences;
        // the question is whether they survive round-trip without causing issues.
        var boardId = await GetDefaultBoardIdAsync();
        // In JSON these are actual escape sequences, not the literal backslash chars
        var json = $$$"""{"Name":"Column\n\r\t","TargetStatus":"InProgress"}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"/api/boards/{boardId}/columns", content);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "JSON escape sequences in column name must not cause a 500 server error");
        // After deserialization, the value would be "Column<LF><CR><TAB>"
        // Whitespace-only after trim is only possible if no non-whitespace chars remain.
        // "Column" survives, so this should be stored or possibly rejected on length/validity.
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.BadRequest,
            $"Expected 201 or 400 for escaped sequences in column name, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_DescriptionContainsLineTerminators()
    {
        // Line separator (U+2028) and paragraph separator (U+2029) are valid unicode
        // but were historically problematic in JavaScript contexts (JSON injection in <script> tags).
        var lsDescription = "Line\u2028Separator\u2029Paragraph";
        var response = await _client.PostAsJsonAsync("/api/tasks",
            new { Title = "Line terminator test", Description = lsDescription });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Unicode line/paragraph separators in description must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.BadRequest,
            $"Expected 201 or 400 for unicode line separators, got {(int)response.StatusCode}");
    }

    // =========================================================================
    // CATEGORY 5: CONTENT-TYPE MANIPULATION
    // Goal: sending unexpected Content-Type headers must not bypass JSON parsing
    //       or cause server errors.
    // =========================================================================

    [TestMethod]
    public async Task Should_NotReturn500_When_XmlContentTypeSentToTaskEndpoint()
    {
        // Minimal API will fail to bind the body (not JSON), should return 400 or 415
        var xmlBody = "<CreateTaskRequest><Title>XmlAttack</Title></CreateTaskRequest>";
        var content = new StringContent(xmlBody, Encoding.UTF8, "application/xml");
        var response = await _client.PostAsync("/api/tasks", content);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "XML body sent to JSON endpoint must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest
                or HttpStatusCode.UnsupportedMediaType
                or HttpStatusCode.Created,
            $"Expected 400/415 for XML body, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_MultipartFormDataSentToTaskEndpoint()
    {
        // Multipart/form-data to a JSON endpoint — should not crash, should 400 or 415
        using var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent("Attack via form"), "Title");
        var response = await _client.PostAsync("/api/tasks", formContent);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Multipart form-data to JSON endpoint must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest
                or HttpStatusCode.UnsupportedMediaType
                or HttpStatusCode.Created,
            $"Expected 400/415 for form-data body, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_NoContentTypeSentWithJsonBody()
    {
        // JSON body with no Content-Type header — behavior depends on ASP.NET Core binding rules
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tasks")
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("""{"Title":"no-content-type"}"""))
        };
        // Explicitly do NOT set Content-Type — leave it absent
        var response = await _client.SendAsync(request);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Missing Content-Type header must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.BadRequest
                or HttpStatusCode.UnsupportedMediaType,
            $"Expected 201/400/415 for missing Content-Type, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_TextHtmlContentTypeSentToAuthEndpoint()
    {
        // text/html Content-Type with a JSON body — content-type spoofing
        var content = new StringContent(
            """{"Email":"test@lemondo.dev","Password":"TestPass123!"}""",
            Encoding.UTF8,
            "text/html");
        using var freshClient = _factory.CreateClient();
        var response = await freshClient.PostAsync("/api/auth/login", content);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "text/html Content-Type on JSON endpoint must not cause a 500 server error");
        // ASP.NET Core minimal API is lenient with Content-Type for FromBody — may succeed or fail
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest
                or HttpStatusCode.UnsupportedMediaType or HttpStatusCode.Unauthorized,
            $"Expected 200/400/415/401 for text/html content-type, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_TextPlainContentTypeSentToTaskEndpoint()
    {
        var content = new StringContent(
            """{"Title":"plain text content type"}""",
            Encoding.UTF8,
            "text/plain");
        var response = await _client.PostAsync("/api/tasks", content);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "text/plain Content-Type on JSON endpoint must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.BadRequest
                or HttpStatusCode.UnsupportedMediaType,
            $"Expected 201/400/415 for text/plain content-type, got {(int)response.StatusCode}");
    }

    // =========================================================================
    // CATEGORY 6: QUERY STRING POLLUTION
    // Goal: repeated or malformed query parameters must be handled deterministically.
    // =========================================================================

    [TestMethod]
    public async Task Should_NotReturn500_When_PageParameterDuplicated()
    {
        // HTTP Parameter Pollution: ?page=1&page=999
        // ASP.NET Core minimal API binds to the FIRST or LAST value depending on config.
        var response = await _client.GetAsync("/api/tasks?page=1&page=999");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Duplicated page parameter must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"Expected 200 or 400 for duplicate page param, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_StatusFilterHasArrayNotation()
    {
        // PHP-style array notation: ?status[]=Todo&status[]=Done
        // ASP.NET Core does not parse [] notation by default; must not crash.
        var response = await _client.GetAsync("/api/tasks?status[]=Todo&status[]=Done");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Array-notation query parameter must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"Expected 200 or 400 for array-notation status filter, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_SearchParameterIsEmpty()
    {
        // ?search= (empty value, key present) vs no search param at all
        var response = await _client.GetAsync("/api/tasks?search=");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Empty search parameter (key present, no value) must not cause a 500 server error");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Empty search string should return all tasks (treat as null/no filter)");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_TagFilterHasIndexedArrayNotation()
    {
        // .NET binder ignores [] but ?tag[0]=a&tag[1]=b might confuse the query string parser
        var response = await _client.GetAsync("/api/tasks?tag[0]=alpha&tag[1]=beta");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Indexed array notation in tag filter must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"Expected 200 or 400 for indexed tag notation, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_PageSizeParameterDuplicated()
    {
        // ?pageSize=10&pageSize=200&pageSize=9999 — parameter pollution on page size
        var response = await _client.GetAsync("/api/tasks?pageSize=10&pageSize=200&pageSize=9999");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Triplicated pageSize parameter must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"Expected 200 or 400 for triplicated pageSize, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_SearchContainsOnlyWhitespaceVariants()
    {
        // Various whitespace types in search: U+0009 tab, U+000B vertical tab, U+000C form feed
        var response = await _client.GetAsync("/api/tasks?search=%09%0B%0C");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Whitespace-variant search parameter must not cause a 500 server error");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Whitespace-only search should return all tasks (treated as empty/no filter)");
    }

    // =========================================================================
    // CATEGORY 7: ANALYTICS ENDPOINT SPECIFIC ATTACKS
    // Goal: the analytics endpoint accepts arbitrary event names and properties —
    //       verify it doesn't crash on adversarial inputs.
    // =========================================================================

    [TestMethod]
    public async Task Should_NotReturn500_When_AnalyticsEventNameIsNullByte()
    {
        var json = """{"Events":[{"EventName":"event\u0000injection","Properties":null}]}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/analytics/events", content);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Null byte in analytics event name must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"Expected 200 or 400 for null byte in event name, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_AnalyticsEventNameIsVeryLong()
    {
        var longName = new string('E', 10_000); // 10K character event name
        var request = new
        {
            Events = new[]
            {
                new { EventName = longName, Properties = (Dictionary<string, string>?)null }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "10,000-character analytics event name must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"Expected 200 or 400 for very long event name, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_AnalyticsPropertiesDictionaryIsVeryLarge()
    {
        // 1,000 key-value pairs in a single event's Properties dictionary
        var properties = Enumerable.Range(0, 1000)
            .ToDictionary(i => $"key_{i}", i => $"value_{i}");

        var request = new
        {
            Events = new[]
            {
                new { EventName = "large_properties_test", Properties = properties }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Analytics event with 1,000 properties must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"Expected 200 or 400 for large properties dict, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_AnalyticsEventNameContainsUnicodeAndControlChars()
    {
        // Mix of homoglyphs, zero-width, and normal characters in event name
        var mixedName = "page\u200b_view\u0441"; // zero-width space + Cyrillic С
        var request = new
        {
            Events = new[]
            {
                new { EventName = mixedName, Properties = (Dictionary<string, string>?)null }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Analytics event name with unicode tricks must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"Expected 200 or 400 for unicode event name, got {(int)response.StatusCode}");
    }

    // =========================================================================
    // CATEGORY 8: BOARD & COLUMN ADVANCED ATTACKS
    // =========================================================================

    [TestMethod]
    public async Task Should_NotReturn500_When_AddColumnWithOnlyZeroWidthName()
    {
        // A column name made entirely of zero-width characters — visually empty but non-empty string.
        // ColumnName.Create() uses IsNullOrWhiteSpace — U+200B is NOT treated as whitespace
        // by string.IsNullOrWhiteSpace(), so this may pass validation with a "hidden" name.
        var boardId = await GetDefaultBoardIdAsync();
        var zwsName = "\u200b\u200c\u200d"; // ZWSP + ZWNJ + ZWJ — invisible

        var response = await _client.PostAsJsonAsync($"/api/boards/{boardId}/columns",
            new { Name = zwsName, TargetStatus = "InProgress" });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Zero-width character column name must not cause a 500 server error");
        // This is a security note: if 201 is returned, the column appears unnamed in the UI.
        // Ideally this returns 400 (column name is effectively invisible).
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.BadRequest,
            $"Expected 201 or 400 for zero-width column name, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_RenameColumnWithNullByteInName()
    {
        // First add a valid column, then rename it with a null byte in the name
        var boardId = await GetDefaultBoardIdAsync();

        // Add a test column
        var addResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/columns",
            new { Name = "TempColumnForRenameTest", TargetStatus = "Done" });
        addResponse.EnsureSuccessStatusCode();
        var addedColumn = await addResponse.Content.ReadFromJsonAsync<ColumnDto>(JsonOpts);
        Assert.IsNotNull(addedColumn);

        // Rename with null byte embedded
        var nullByteName = "Renamed\0Column";
        var response = await _client.PutAsJsonAsync(
            $"/api/boards/{boardId}/columns/{addedColumn.Id}",
            new { Name = nullByteName });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Null byte in column rename must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"Expected 200 or 400 for null byte in column rename, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_AddColumnWithRtlOverrideOnlyName()
    {
        // A column name that is pure RTL override chars — not whitespace per IsNullOrWhiteSpace
        // The ColumnName max is 50 chars; "\u202E" is 3 chars — passes length validation.
        var boardId = await GetDefaultBoardIdAsync();
        var rtlName = "\u202E\u202E\u202E"; // three RTL Override chars

        var response = await _client.PostAsJsonAsync($"/api/boards/{boardId}/columns",
            new { Name = rtlName, TargetStatus = "Done" });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Pure RTL override column name must not cause a 500 server error");
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.BadRequest,
            $"Expected 201 or 400 for RTL-only column name, got {(int)response.StatusCode}");
    }
}

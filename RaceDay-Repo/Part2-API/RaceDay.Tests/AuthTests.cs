using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Security;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private static object NewUser(string email, string role = "Participant", string password = "Password123!") => new
    {
        fullName = "Test User",
        email,
        password,
        role,
        contactNumber = "0820000000"
    };

    [Fact]
    public async Task Register_ValidParticipant_Returns201_AndDoesNotExposePassword()
    {
        var client = _factory.CreateClient();
        var email = $"p{Guid.NewGuid():N}@test.com";

        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser(email));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Password123!", body);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_ValidOrganiser_Returns201_WithOrganiserRole()
    {
        var client = _factory.CreateClient();
        var email = $"o{Guid.NewGuid():N}@test.com";

        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser(email, "Organiser"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("Organiser", user!.Role);
    }

    [Fact]
    public async Task Register_PasswordIsStoredHashed_NotInPlainText()
    {
        var client = _factory.CreateClient();
        var email = $"h{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", NewUser(email));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var stored = db.Users.Single(u => u.Email == email);

        Assert.NotEqual("Password123!", stored.PasswordHash);
        Assert.True(hasher.Verify("Password123!", stored.PasswordHash));
        Assert.False(hasher.Verify("WrongPassword!", stored.PasswordHash));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var email = $"d{Guid.NewGuid():N}@test.com";

        var first = await client.PostAsJsonAsync("/api/auth/register", NewUser(email));
        var second = await client.PostAsJsonAsync("/api/auth/register", NewUser(email));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser($"r{Guid.NewGuid():N}@test.com", "Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser($"s{Guid.NewGuid():N}@test.com", "Participant", "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200_AndReturnsRole()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = TestHelpers.Organiser1, password = TestHelpers.SeedPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal("Organiser", login!.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = TestHelpers.Participant1, password = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@nowhere.com", password = "Password123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WithoutLogin_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_AfterLogin_SessionIsRemembered_Returns200()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal(TestHelpers.Participant1, profile!.Email);
        Assert.Equal("Participant", profile.Role);
    }

    [Fact]
    public async Task UpdateProfile_AfterLogin_Returns200_AndSavesChanges()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant2);

        var update = await client.PutAsJsonAsync("/api/users/me", new { fullName = "Amahle Updated", contactNumber = "0799999999" });
        var profile = await (await client.GetAsync("/api/users/me")).Content.ReadFromJsonAsync<UserDto>();

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Amahle Updated", profile!.FullName);
    }

    [Fact]
    public async Task UpdateProfile_WithoutLogin_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/users/me", new { fullName = "Hacker", contactNumber = "0" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsSession_SoProfileReturns401Afterwards()
    {
        var client = await TestHelpers.LoginAsync(_factory, TestHelpers.Participant1);

        var logout = await client.PostAsync("/api/auth/logout", null);
        var profile = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
    }
}

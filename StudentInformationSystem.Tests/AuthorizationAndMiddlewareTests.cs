using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StudentInformationSystem.Helper;
using StudentInformationSystem.Models;
using StudentInformationSystem.Repository;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StudentInformationSystem.Tests;

public class AuthorizationAndMiddlewareTests
{
    [Fact]
    public void AuthorizeAttribute_SetsUnauthorizedResult_WhenUserMissing()
    {
        var attribute = new AuthorizeAttribute();
        var context = CreateAuthorizationContext();

        attribute.OnAuthorization(context);

        var result = Assert.IsType<JsonResult>(context.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public void AuthorizeAttribute_DoesNotSetResult_WhenAllowAnonymousPresent()
    {
        var attribute = new AuthorizeAttribute();
        var context = CreateAuthorizationContext(new AllowAnonymousAttribute());

        attribute.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void AuthorizeAttribute_DoesNotSetResult_WhenUserExists()
    {
        var attribute = new AuthorizeAttribute();
        var httpContext = new DefaultHttpContext();
        httpContext.Items["User"] = new ApplicationUser { Id = "1", UserName = "alice@example.com", FirstName = "Alice", LastName = "A" };
        var context = CreateAuthorizationContext(httpContext: httpContext);

        attribute.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task JwtMiddleware_InvokesNext_WhenNoTokenProvided()
    {
        var wasNextCalled = false;
        var middleware = CreateMiddleware(_ => wasNextCalled = true);
        var context = new DefaultHttpContext();
        var repository = new FakeUserRepository();

        await middleware.Invoke(context, repository);

        Assert.True(wasNextCalled);
        Assert.False(context.Items.ContainsKey("User"));
        Assert.Equal(0, repository.GetUserCallCount);
    }

    [Fact]
    public async Task JwtMiddleware_AttachesUser_WhenTokenIsValid()
    {
        var middleware = CreateMiddleware(_ => { });
        var context = new DefaultHttpContext();
        var expectedUser = new ApplicationUser { Id = "7", UserName = "alice@example.com", FirstName = "Alice", LastName = "A" };
        var repository = new FakeUserRepository(expectedUser);
        context.Request.Headers["Authorization"] = $"Bearer {CreateValidToken("7")}";

        await middleware.Invoke(context, repository);

        var attachedUser = Assert.IsType<ApplicationUser>(context.Items["User"]);
        Assert.Equal("7", attachedUser.Id);
        Assert.Equal(1, repository.GetUserCallCount);
        Assert.Equal("7", repository.LastRequestedId);
    }

    [Fact]
    public async Task JwtMiddleware_DoesNotAttachUser_WhenTokenInvalid()
    {
        var middleware = CreateMiddleware(_ => { });
        var context = new DefaultHttpContext();
        var repository = new FakeUserRepository(new ApplicationUser { Id = "7", UserName = "alice@example.com", FirstName = "Alice", LastName = "A" });
        context.Request.Headers["Authorization"] = "Bearer invalid-token";

        await middleware.Invoke(context, repository);

        Assert.False(context.Items.ContainsKey("User"));
        Assert.Equal(0, repository.GetUserCallCount);
    }

    private static AuthorizationFilterContext CreateAuthorizationContext(AllowAnonymousAttribute? allowAnonymousAttribute = null, HttpContext? httpContext = null)
    {
        var actionDescriptor = new ActionDescriptor();
        if (allowAnonymousAttribute != null)
        {
            actionDescriptor.EndpointMetadata = new List<object> { allowAnonymousAttribute };
        }

        var actionContext = new ActionContext(httpContext ?? new DefaultHttpContext(), new RouteData(), actionDescriptor);
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private static JwtMiddleware CreateMiddleware(Action<HttpContext> onNext)
    {
        var appSettings = Options.Create(new AppSettings
        {
            Key = "this-is-a-long-test-signing-key-123456",
            Issuer = "test-issuer",
            AccessTokenExpirationMinutes = 20
        });

        return new JwtMiddleware(
            context =>
            {
                onNext(context);
                return Task.CompletedTask;
            },
            appSettings);
    }

    private static string CreateValidToken(string userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("this-is-a-long-test-signing-key-123456");
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("id", userId)
            }),
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(descriptor);
        return tokenHandler.WriteToken(token);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly ApplicationUser? _returnUser;

        public FakeUserRepository(ApplicationUser? returnUser = null)
        {
            _returnUser = returnUser;
        }

        public int GetUserCallCount { get; private set; }
        public string? LastRequestedId { get; private set; }

        public ICollection<ApplicationUser> GetUsers() => new List<ApplicationUser>();

        public ApplicationUser GetUser(string id)
        {
            GetUserCallCount++;
            LastRequestedId = id;
            return _returnUser!;
        }

        public ApplicationUser UpdateUser(ApplicationUser user) => user;
    }
}

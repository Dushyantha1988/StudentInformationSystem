using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StudentInformationSystem.Controllers;
using StudentInformationSystem.Models;
using StudentInformationSystem.Models.RequestModel;
using StudentInformationSystem.Models.ResponseModel;
using StudentInformationSystem.Repository;

namespace StudentInformationSystem.Tests;

public class UserControllerTests
{
    [Fact]
    public async Task Register_ReturnsOk_WhenUserCreationSucceeds()
    {
        var userManager = CreateUserManager();
        var jwtService = new Mock<IJwtService>();
        userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "Password@123"))
            .ReturnsAsync(IdentityResult.Success);
        var controller = new UserController(userManager.Object, jwtService.Object);

        var result = await controller.Register(new RegisterModel
        {
            Email = "alice@example.com",
            Password = "Password@123",
            FirstName = "Alice",
            LastName = "A"
        });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenUserCreationFails()
    {
        var userManager = CreateUserManager();
        var jwtService = new Mock<IJwtService>();
        userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "creation failed" }));
        var controller = new UserController(userManager.Object, jwtService.Object);

        var result = await controller.Register(new RegisterModel
        {
            Email = "alice@example.com",
            Password = "Password@123",
            FirstName = "Alice",
            LastName = "A"
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsOkWithTokens_WhenCredentialsAreValid()
    {
        var user = new ApplicationUser { Id = "1", UserName = "alice@example.com", Email = "alice@example.com", FirstName = "Alice", LastName = "A" };
        var userManager = CreateUserManager();
        userManager.Setup(x => x.FindByNameAsync("alice@example.com")).ReturnsAsync(user);
        userManager.Setup(x => x.CheckPasswordAsync(user, "Password@123")).ReturnsAsync(true);

        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(x => x.GenerateTokens(user)).Returns(new LoginResponseModel
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            ExpiresDate = DateTime.UtcNow.AddMinutes(20)
        });

        var controller = new UserController(userManager.Object, jwtService.Object);

        var result = await controller.Login(new LoginModel
        {
            Username = "alice@example.com",
            Password = "Password@123"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserNotFound()
    {
        var userManager = CreateUserManager();
        userManager.Setup(x => x.FindByNameAsync("missing@example.com")).ReturnsAsync((ApplicationUser?)null);
        var jwtService = new Mock<IJwtService>();
        var controller = new UserController(userManager.Object, jwtService.Object);

        var result = await controller.Login(new LoginModel
        {
            Username = "missing@example.com",
            Password = "Password@123"
        });

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordCheckFails()
    {
        var user = new ApplicationUser { Id = "1", UserName = "alice@example.com", Email = "alice@example.com", FirstName = "Alice", LastName = "A" };
        var userManager = CreateUserManager();
        userManager.Setup(x => x.FindByNameAsync("alice@example.com")).ReturnsAsync(user);
        userManager.Setup(x => x.CheckPasswordAsync(user, "wrong-password")).ReturnsAsync(false);
        var jwtService = new Mock<IJwtService>();
        var controller = new UserController(userManager.Object, jwtService.Object);

        var result = await controller.Login(new LoginModel
        {
            Username = "alice@example.com",
            Password = "wrong-password"
        });

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }
}

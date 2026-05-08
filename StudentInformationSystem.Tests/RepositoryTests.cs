using Microsoft.EntityFrameworkCore;
using StudentInformationSystem.Models;
using StudentInformationSystem.Repository;

namespace StudentInformationSystem.Tests;

public class RepositoryTests
{
    [Fact]
    public void UserRepository_GetUsers_ReturnsAllUsers()
    {
        using var context = CreateContext();
        context.Users.AddRange(
            new ApplicationUser { Id = "1", UserName = "alice@example.com", Email = "alice@example.com", FirstName = "Alice", LastName = "A" },
            new ApplicationUser { Id = "2", UserName = "bob@example.com", Email = "bob@example.com", FirstName = "Bob", LastName = "B" });
        context.SaveChanges();

        var repository = new UserRepository(context);

        var users = repository.GetUsers();

        Assert.Equal(2, users.Count);
    }

    [Fact]
    public void UserRepository_GetUser_ReturnsMatchingUser()
    {
        using var context = CreateContext();
        context.Users.Add(new ApplicationUser { Id = "1", UserName = "alice@example.com", Email = "alice@example.com", FirstName = "Alice", LastName = "A" });
        context.SaveChanges();
        var repository = new UserRepository(context);

        var user = repository.GetUser("1");

        Assert.NotNull(user);
        Assert.Equal("alice@example.com", user!.Email);
    }

    [Fact]
    public void UserRepository_GetUser_ReturnsNullForMissingUser()
    {
        using var context = CreateContext();
        var repository = new UserRepository(context);

        var user = repository.GetUser("missing");

        Assert.Null(user);
    }

    [Fact]
    public void UserRepository_UpdateUser_PersistsChanges()
    {
        using var context = CreateContext();
        var existingUser = new ApplicationUser { Id = "1", UserName = "alice@example.com", Email = "alice@example.com", FirstName = "Alice", LastName = "A" };
        context.Users.Add(existingUser);
        context.SaveChanges();
        var repository = new UserRepository(context);
        existingUser.LastName = "Updated";

        var updatedUser = repository.UpdateUser(existingUser);
        var reloaded = context.Users.Single(u => u.Id == "1");

        Assert.Equal("Updated", updatedUser.LastName);
        Assert.Equal("Updated", reloaded.LastName);
    }

    [Fact]
    public void RoleRepository_GetRoles_ReturnsAllRoles()
    {
        using var context = CreateContext();
        context.Roles.AddRange(
            new ApplicationRole { Id = "1", Name = "Admin", NormalizedName = "ADMIN" },
            new ApplicationRole { Id = "2", Name = "Student", NormalizedName = "STUDENT" });
        context.SaveChanges();
        var repository = new RoleRepository(context);

        var roles = repository.GetRoles();

        Assert.Equal(2, roles.Count);
    }

    [Fact]
    public void UnitOfWork_ExposesInjectedRepositories()
    {
        using var context = CreateContext();
        var userRepository = new UserRepository(context);
        var roleRepository = new RoleRepository(context);

        var unitOfWork = new UnitOfWork(userRepository, roleRepository);

        Assert.Same(userRepository, unitOfWork.User);
        Assert.Same(roleRepository, unitOfWork.Role);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}

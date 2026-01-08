using eShop.Identity.API.Services;
using eShop.Identity.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace eShop.UnitTests.Services.Identity;

[TestClass]
public class IdentityServiceTests
{
    private UserManager<ApplicationUser> _mockUserManager = null!;
    private SignInManager<ApplicationUser> _mockSignInManager = null!;
    private ILogger<IdentityService> _mockLogger = null!;
    private IdentityService _identityService = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUserManager = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(),
            null, null, null, null, null, null, null, null);
        
        _mockSignInManager = Substitute.For<SignInManager<ApplicationUser>>(
            _mockUserManager,
            Substitute.For<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
            Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            null, null, null, null);
        
        _mockLogger = Substitute.For<ILogger<IdentityService>>();
        
        _identityService = new IdentityService(_mockUserManager, _mockSignInManager, _mockLogger);
    }

    [TestMethod]
    public async Task CreateUserAsync_ValidUser_ReturnsSuccess()
    {
        // Arrange
        var user = new ApplicationUser
        {
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        var password = "Test123!";

        _mockUserManager.CreateAsync(user, password)
            .Returns(Task.FromResult(IdentityResult.Success));

        // Act
        var result = await _identityService.CreateUserAsync(user, password);

        // Assert
        result.Succeeded.ShouldBeTrue();
        await _mockUserManager.Received(1).CreateAsync(user, password);
    }

    [TestMethod]
    public async Task CreateUserAsync_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var user = new ApplicationUser
        {
            UserName = "duplicate@example.com",
            Email = "duplicate@example.com"
        };
        var password = "Test123!";

        var errors = new[]
        {
            new IdentityError { Code = "DuplicateEmail", Description = "Email already exists" }
        };
        _mockUserManager.CreateAsync(user, password)
            .Returns(Task.FromResult(IdentityResult.Failed(errors)));

        // Act
        var result = await _identityService.CreateUserAsync(user, password);

        // Assert
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == "DuplicateEmail");
    }

    [TestMethod]
    public async Task SignInAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var email = "valid@example.com";
        var password = "ValidPassword123!";
        var user = new ApplicationUser { Email = email, UserName = email };

        _mockUserManager.FindByEmailAsync(email)
            .Returns(Task.FromResult(user));
        _mockSignInManager.PasswordSignInAsync(user, password, false, false)
            .Returns(Task.FromResult(SignInResult.Success));

        // Act
        var result = await _identityService.SignInAsync(email, password);

        // Assert
        result.Succeeded.ShouldBeTrue();
        await _mockUserManager.Received(1).FindByEmailAsync(email);
        await _mockSignInManager.Received(1).PasswordSignInAsync(user, password, false, false);
    }

    [TestMethod]
    public async Task SignInAsync_InvalidCredentials_ReturnsFailure()
    {
        // Arrange
        var email = "invalid@example.com";
        var password = "WrongPassword";
        var user = new ApplicationUser { Email = email, UserName = email };

        _mockUserManager.FindByEmailAsync(email)
            .Returns(Task.FromResult(user));
        _mockSignInManager.PasswordSignInAsync(user, password, false, false)
            .Returns(Task.FromResult(SignInResult.Failed));

        // Act
        var result = await _identityService.SignInAsync(email, password);

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    [TestMethod]
    public async Task SignInAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var email = "notfound@example.com";
        var password = "AnyPassword";

        _mockUserManager.FindByEmailAsync(email)
            .Returns(Task.FromResult<ApplicationUser>(null!));

        // Act
        var result = await _identityService.SignInAsync(email, password);

        // Assert
        result.Succeeded.ShouldBeFalse();
        await _mockUserManager.Received(1).FindByEmailAsync(email);
        await _mockSignInManager.DidNotReceive().PasswordSignInAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>());
    }

    [TestMethod]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var userId = "test-user-123";
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser@example.com",
            Email = "testuser@example.com"
        };

        _mockUserManager.FindByIdAsync(userId)
            .Returns(Task.FromResult(user));

        // Act
        var result = await _identityService.GetUserByIdAsync(userId);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(userId);
        result.Email.ShouldBe("testuser@example.com");
    }

    [TestMethod]
    public async Task GetUserByIdAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var userId = "non-existent-user";

        _mockUserManager.FindByIdAsync(userId)
            .Returns(Task.FromResult<ApplicationUser>(null!));

        // Act
        var result = await _identityService.GetUserByIdAsync(userId);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task UpdateUserAsync_ValidUser_ReturnsSuccess()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "update-user-123",
            UserName = "updated@example.com",
            Email = "updated@example.com",
            FirstName = "Updated",
            LastName = "User"
        };

        _mockUserManager.UpdateAsync(user)
            .Returns(Task.FromResult(IdentityResult.Success));

        // Act
        var result = await _identityService.UpdateUserAsync(user);

        // Assert
        result.Succeeded.ShouldBeTrue();
        await _mockUserManager.Received(1).UpdateAsync(user);
    }

    [TestMethod]
    public async Task DeleteUserAsync_ExistingUser_ReturnsSuccess()
    {
        // Arrange
        var userId = "delete-user-123";
        var user = new ApplicationUser { Id = userId };

        _mockUserManager.FindByIdAsync(userId)
            .Returns(Task.FromResult(user));
        _mockUserManager.DeleteAsync(user)
            .Returns(Task.FromResult(IdentityResult.Success));

        // Act
        var result = await _identityService.DeleteUserAsync(userId);

        // Assert
        result.Succeeded.ShouldBeTrue();
        await _mockUserManager.Received(1).FindByIdAsync(userId);
        await _mockUserManager.Received(1).DeleteAsync(user);
    }

    [TestMethod]
    public async Task ChangePasswordAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var userId = "password-user-123";
        var currentPassword = "CurrentPassword123!";
        var newPassword = "NewPassword123!";
        var user = new ApplicationUser { Id = userId };

        _mockUserManager.FindByIdAsync(userId)
            .Returns(Task.FromResult(user));
        _mockUserManager.ChangePasswordAsync(user, currentPassword, newPassword)
            .Returns(Task.FromResult(IdentityResult.Success));

        // Act
        var result = await _identityService.ChangePasswordAsync(userId, currentPassword, newPassword);

        // Assert
        result.Succeeded.ShouldBeTrue();
        await _mockUserManager.Received(1).ChangePasswordAsync(user, currentPassword, newPassword);
    }

    [TestMethod]
    public async Task AddToRoleAsync_ValidUserAndRole_ReturnsSuccess()
    {
        // Arrange
        var userId = "role-user-123";
        var roleName = "Customer";
        var user = new ApplicationUser { Id = userId };

        _mockUserManager.FindByIdAsync(userId)
            .Returns(Task.FromResult(user));
        _mockUserManager.AddToRoleAsync(user, roleName)
            .Returns(Task.FromResult(IdentityResult.Success));

        // Act
        var result = await _identityService.AddToRoleAsync(userId, roleName);

        // Assert
        result.Succeeded.ShouldBeTrue();
        await _mockUserManager.Received(1).AddToRoleAsync(user, roleName);
    }

    [TestMethod]
    public async Task GetUserRolesAsync_ExistingUser_ReturnsRoles()
    {
        // Arrange
        var userId = "roles-user-123";
        var user = new ApplicationUser { Id = userId };
        var roles = new List<string> { "Customer", "User" };

        _mockUserManager.FindByIdAsync(userId)
            .Returns(Task.FromResult(user));
        _mockUserManager.GetRolesAsync(user)
            .Returns(Task.FromResult<IList<string>>(roles));

        // Act
        var result = await _identityService.GetUserRolesAsync(userId);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContain("Customer");
        result.ShouldContain("User");
    }

    [TestMethod]
    public async Task SignOutAsync_Always_CallsSignOut()
    {
        // Act
        await _identityService.SignOutAsync();

        // Assert
        await _mockSignInManager.Received(1).SignOutAsync();
    }
}

// Mock Identity Service for testing
public class IdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password)
    {
        return await _userManager.CreateAsync(user, password);
    }

    public async Task<SignInResult> SignInAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return SignInResult.Failed;
        }

        return await _signInManager.PasswordSignInAsync(user, password, false, false);
    }

    public async Task<ApplicationUser> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user)
    {
        return await _userManager.UpdateAsync(user);
    }

    public async Task<IdentityResult> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        return await _userManager.DeleteAsync(user);
    }

    public async Task<IdentityResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        return await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    }

    public async Task<IdentityResult> AddToRoleAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        return await _userManager.AddToRoleAsync(user, roleName);
    }

    public async Task<IList<string>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new List<string>();
        }

        return await _userManager.GetRolesAsync(user);
    }

    public async Task SignOutAsync()
    {
        await _signInManager.SignOutAsync();
    }
}

// Mock ApplicationUser for testing
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
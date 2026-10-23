namespace eShop.Identity.UnitTests.Services;

[TestClass]
public class ProfileServiceTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ProfileService _profileService;

    public ProfileServiceTests()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            store, null, null, null, null, null, null, null, null);

        _profileService = new ProfileService(_userManager);
    }

    private static IsActiveContext CreateContext(string subjectId, IEnumerable<Claim>? extraClaims = null)
    {
        var claims = new List<Claim> { new Claim("sub", subjectId) };
        if (extraClaims != null)
            claims.AddRange(extraClaims);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        return new IsActiveContext(principal, new Client(), "test");
    }

    [TestMethod]
    public async Task IsActiveAsync_UserNotFound_SetsIsActiveFalse()
    {
        var context = CreateContext("unknown-id");
        _userManager.FindByIdAsync("unknown-id").Returns((ApplicationUser)null!);

        await _profileService.IsActiveAsync(context);

        Assert.IsFalse(context.IsActive);
    }

    [TestMethod]
    public async Task IsActiveAsync_UserFound_SecurityStampNotSupported_NotLockedOut_SetsIsActiveTrue()
    {
        var user = new ApplicationUser { Id = "user-1", LockoutEnabled = false };
        var context = CreateContext("user-1");

        _userManager.FindByIdAsync("user-1").Returns(user);
        _userManager.SupportsUserSecurityStamp.Returns(false);

        await _profileService.IsActiveAsync(context);

        Assert.IsTrue(context.IsActive);
    }

    [TestMethod]
    public async Task IsActiveAsync_UserFound_SecurityStampInToken_Matches_NotLockedOut_SetsIsActiveTrue()
    {
        var user = new ApplicationUser { Id = "user-1", LockoutEnabled = false };
        var context = CreateContext("user-1", [new Claim("security_stamp", "stamp-abc")]);

        _userManager.FindByIdAsync("user-1").Returns(user);
        _userManager.SupportsUserSecurityStamp.Returns(true);
        _userManager.GetSecurityStampAsync(user).Returns("stamp-abc");

        await _profileService.IsActiveAsync(context);

        Assert.IsTrue(context.IsActive);
    }

    [TestMethod]
    public async Task IsActiveAsync_UserFound_SecurityStampInToken_Mismatches_SetsIsActiveFalse()
    {
        var user = new ApplicationUser { Id = "user-1", LockoutEnabled = false };
        var context = CreateContext("user-1", [new Claim("security_stamp", "old-stamp")]);

        _userManager.FindByIdAsync("user-1").Returns(user);
        _userManager.SupportsUserSecurityStamp.Returns(true);
        _userManager.GetSecurityStampAsync(user).Returns("new-stamp");

        await _profileService.IsActiveAsync(context);

        Assert.IsFalse(context.IsActive);
    }

    [TestMethod]
    public async Task IsActiveAsync_UserFound_NoSecurityStampInToken_SkipsStampCheck_SetsIsActiveTrue()
    {
        var user = new ApplicationUser { Id = "user-1", LockoutEnabled = false };
        var context = CreateContext("user-1"); // no security_stamp claim

        _userManager.FindByIdAsync("user-1").Returns(user);
        _userManager.SupportsUserSecurityStamp.Returns(true);

        await _profileService.IsActiveAsync(context);

        Assert.IsTrue(context.IsActive);
        await _userManager.DidNotReceive().GetSecurityStampAsync(Arg.Any<ApplicationUser>());
    }

    [TestMethod]
    public async Task IsActiveAsync_UserFound_ValidStamp_CurrentlyLockedOut_SetsIsActiveFalse()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddHours(1)
        };
        var context = CreateContext("user-1");

        _userManager.FindByIdAsync("user-1").Returns(user);
        _userManager.SupportsUserSecurityStamp.Returns(false);

        await _profileService.IsActiveAsync(context);

        Assert.IsFalse(context.IsActive);
    }

    [TestMethod]
    public async Task IsActiveAsync_UserFound_ValidStamp_LockoutExpired_SetsIsActiveTrue()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddHours(-1)
        };
        var context = CreateContext("user-1");

        _userManager.FindByIdAsync("user-1").Returns(user);
        _userManager.SupportsUserSecurityStamp.Returns(false);

        await _profileService.IsActiveAsync(context);

        Assert.IsTrue(context.IsActive);
    }

    [TestMethod]
    public async Task IsActiveAsync_UserFound_ValidStamp_LockoutEnabledWithNoEndDate_SetsIsActiveTrue()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            LockoutEnabled = true,
            LockoutEnd = null
        };
        var context = CreateContext("user-1");

        _userManager.FindByIdAsync("user-1").Returns(user);
        _userManager.SupportsUserSecurityStamp.Returns(false);

        await _profileService.IsActiveAsync(context);

        Assert.IsTrue(context.IsActive);
    }
}

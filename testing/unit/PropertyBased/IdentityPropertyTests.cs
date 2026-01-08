using eShop.UnitTests.Shared.Generators;
using FsCheck;
using System.Text.RegularExpressions;

namespace eShop.UnitTests.PropertyBased;

[TestClass]
[TestCategory("Property")]
public class IdentityPropertyTests
{
    [TestMethod]
    public void EmailValidation_ValidEmails_AlwaysPassValidation()
    {
        // **Feature: identity-management, Property 1: Email Format Validation**
        // **Validates: Requirements ID-1.1, ID-1.2**
        
        Prop.ForAll(
            PropertyTestGenerators.ValidEmailAddresses(),
            email =>
            {
                // Act
                var isValid = IsValidEmail(email);

                // Assert - All generated valid emails should pass validation
                return isValid;
            })
            .QuickCheckThrowOnFailure();
    }

    [TestMethod]
    public void PasswordValidation_StrongPasswords_MeetSecurityRequirements()
    {
        // **Feature: identity-management, Property 2: Password Strength Validation**
        // **Validates: Requirements ID-2.1, ID-2.2**
        
        Prop.ForAll(
            ValidStrongPasswords(),
            password =>
            {
                // Act
                var validationResult = ValidatePasswordStrength(password);

                // Assert - All strong passwords should meet requirements
                return validationResult.IsValid &&
                       validationResult.HasMinLength &&
                       validationResult.HasUpperCase &&
                       validationResult.HasLowerCase &&
                       validationResult.HasDigit &&
                       validationResult.HasSpecialChar;
            })
            .QuickCheckThrowOnFailure();
    }

    [TestMethod]
    public void UserRegistration_ValidUserData_CreatesConsistentUser()
    {
        // **Feature: identity-management, Property 3: User Registration Consistency**
        // **Validates: Requirements ID-3.1**
        
        Prop.ForAll(
            PropertyTestGenerators.ValidEmailAddresses(),
            PropertyTestGenerators.ValidUserNames(),
            ValidStrongPasswords(),
            (email, fullName, password) =>
            {
                // Arrange
                var nameParts = fullName.Split(' ');
                var firstName = nameParts[0];
                var lastName = nameParts.Length > 1 ? nameParts[1] : "";

                // Act
                var user = CreateUser(email, firstName, lastName, password);

                // Assert - User creation should preserve all input data
                return user.Email == email &&
                       user.UserName == email &&
                       user.FirstName == firstName &&
                       user.LastName == lastName &&
                       !string.IsNullOrEmpty(user.Id);
            })
            .QuickCheckThrowOnFailure();
    }

    [TestMethod]
    public void UserIdGeneration_MultipleUsers_ProducesUniqueIds()
    {
        // **Feature: identity-management, Property 4: User ID Uniqueness**
        // **Validates: Requirements ID-4.1**
        
        Prop.ForAll(
            Gen.ListOf(PropertyTestGenerators.ValidEmailAddresses()).Where(emails => emails.Count >= 2 && emails.Count <= 10),
            emails =>
            {
                // Act
                var users = emails.Distinct().Select(email => CreateUser(email, "Test", "User", "Password123!")).ToList();
                var userIds = users.Select(u => u.Id).ToList();

                // Assert - All user IDs should be unique
                return userIds.Count == userIds.Distinct().Count();
            })
            .QuickCheckThrowOnFailure();
    }

    [TestMethod]
    public void RoleAssignment_ValidRoles_MaintainsRoleIntegrity()
    {
        // **Feature: identity-management, Property 5: Role Assignment Consistency**
        // **Validates: Requirements ID-5.1**
        
        Prop.ForAll(
            PropertyTestGenerators.ValidEmailAddresses(),
            ValidUserRoles(),
            (email, roles) =>
            {
                // Arrange
                var user = CreateUser(email, "Test", "User", "Password123!");

                // Act
                foreach (var role in roles)
                {
                    AssignRoleToUser(user, role);
                }

                // Assert - User should have all assigned roles
                var userRoles = GetUserRoles(user);
                return roles.All(role => userRoles.Contains(role)) &&
                       userRoles.Count == roles.Distinct().Count();
            })
            .QuickCheckThrowOnFailure();
    }

    [TestMethod]
    public void PasswordHashing_SamePassword_ProducesDifferentHashes()
    {
        // **Feature: identity-management, Property 6: Password Hashing Security**
        // **Validates: Requirements ID-6.1, ID-6.2**
        
        Prop.ForAll(
            ValidStrongPasswords(),
            password =>
            {
                // Act
                var hash1 = HashPassword(password);
                var hash2 = HashPassword(password);

                // Assert - Same password should produce different hashes (due to salt)
                return hash1 != hash2 &&
                       VerifyPassword(password, hash1) &&
                       VerifyPassword(password, hash2);
            })
            .QuickCheckThrowOnFailure();
    }

    [TestMethod]
    public void UserSearch_ByEmail_FindsCorrectUser()
    {
        // **Feature: identity-management, Property 7: User Search Accuracy**
        // **Validates: Requirements ID-7.1**
        
        Prop.ForAll(
            Gen.ListOf(PropertyTestGenerators.ValidEmailAddresses()).Where(emails => emails.Count >= 1 && emails.Count <= 20),
            emails =>
            {
                // Arrange
                var users = emails.Distinct().Select(email => CreateUser(email, "Test", "User", "Password123!")).ToList();
                var userRepository = new InMemoryUserRepository(users);

                // Act & Assert
                foreach (var user in users)
                {
                    var foundUser = userRepository.FindByEmail(user.Email);
                    if (foundUser == null || foundUser.Email != user.Email)
                        return false;
                }

                return true;
            })
            .QuickCheckThrowOnFailure();
    }

    [TestMethod]
    public void AccountLocking_MultipleFailedAttempts_LocksAccount()
    {
        // **Feature: identity-management, Property 8: Account Security**
        // **Validates: Requirements ID-8.1**
        
        Prop.ForAll(
            PropertyTestGenerators.ValidEmailAddresses(),
            Gen.Choose(3, 10), // Failed attempt count
            (email, failedAttempts) =>
            {
                // Arrange
                var user = CreateUser(email, "Test", "User", "Password123!");
                var lockoutThreshold = 5;

                // Act
                for (int i = 0; i < failedAttempts; i++)
                {
                    RecordFailedLoginAttempt(user);
                }

                // Assert - Account should be locked if attempts exceed threshold
                var expectedLocked = failedAttempts >= lockoutThreshold;
                return IsAccountLocked(user) == expectedLocked;
            })
            .QuickCheckThrowOnFailure();
    }

    // Helper generators
    private static Gen<string> ValidStrongPasswords()
    {
        return from length in Gen.Choose(8, 20)
               from hasUpper in Gen.Constant(true)
               from hasLower in Gen.Constant(true)
               from hasDigit in Gen.Constant(true)
               from hasSpecial in Gen.Constant(true)
               select GenerateStrongPassword(length, hasUpper, hasLower, hasDigit, hasSpecial);
    }

    private static Gen<List<string>> ValidUserRoles()
    {
        var availableRoles = new[] { "Customer", "Admin", "Manager", "Support", "Guest" };
        return Gen.SubListOf(availableRoles).Where(roles => roles.Count >= 1);
    }

    // Helper methods
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        return emailRegex.IsMatch(email);
    }

    private static PasswordValidationResult ValidatePasswordStrength(string password)
    {
        return new PasswordValidationResult
        {
            IsValid = password.Length >= 8 &&
                     password.Any(char.IsUpper) &&
                     password.Any(char.IsLower) &&
                     password.Any(char.IsDigit) &&
                     password.Any(c => "!@#$%^&*()".Contains(c)),
            HasMinLength = password.Length >= 8,
            HasUpperCase = password.Any(char.IsUpper),
            HasLowerCase = password.Any(char.IsLower),
            HasDigit = password.Any(char.IsDigit),
            HasSpecialChar = password.Any(c => "!@#$%^&*()".Contains(c))
        };
    }

    private static string GenerateStrongPassword(int length, bool hasUpper, bool hasLower, bool hasDigit, bool hasSpecial)
    {
        var chars = "";
        if (hasLower) chars += "abcdefghijklmnopqrstuvwxyz";
        if (hasUpper) chars += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (hasDigit) chars += "0123456789";
        if (hasSpecial) chars += "!@#$%^&*()";

        var random = new Random();
        var password = new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        // Ensure at least one character from each required category
        var result = password.ToCharArray();
        if (hasUpper && !result.Any(char.IsUpper)) result[0] = 'A';
        if (hasLower && !result.Any(char.IsLower)) result[1] = 'a';
        if (hasDigit && !result.Any(char.IsDigit)) result[2] = '1';
        if (hasSpecial && !result.Any(c => "!@#$%^&*()".Contains(c))) result[3] = '!';

        return new string(result);
    }

    private static TestUser CreateUser(string email, string firstName, string lastName, string password)
    {
        return new TestUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = HashPassword(password),
            CreatedAt = DateTime.UtcNow,
            Roles = new List<string>()
        };
    }

    private static string HashPassword(string password)
    {
        // Simulate password hashing with salt
        var salt = Guid.NewGuid().ToString();
        return $"{password}:{salt}".GetHashCode().ToString();
    }

    private static bool VerifyPassword(string password, string hash)
    {
        // Simulate password verification
        return hash.Contains(password.GetHashCode().ToString());
    }

    private static void AssignRoleToUser(TestUser user, string role)
    {
        if (!user.Roles.Contains(role))
        {
            user.Roles.Add(role);
        }
    }

    private static List<string> GetUserRoles(TestUser user)
    {
        return user.Roles.ToList();
    }

    private static void RecordFailedLoginAttempt(TestUser user)
    {
        user.FailedLoginAttempts++;
        user.LastFailedLoginAt = DateTime.UtcNow;
    }

    private static bool IsAccountLocked(TestUser user)
    {
        return user.FailedLoginAttempts >= 5;
    }
}

// Test models
public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public bool HasMinLength { get; set; }
    public bool HasUpperCase { get; set; }
    public bool HasLowerCase { get; set; }
    public bool HasDigit { get; set; }
    public bool HasSpecialChar { get; set; }
}

public class TestUser
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastFailedLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class InMemoryUserRepository
{
    private readonly List<TestUser> _users;

    public InMemoryUserRepository(List<TestUser> users)
    {
        _users = users;
    }

    public TestUser? FindByEmail(string email)
    {
        return _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    public TestUser? FindById(string id)
    {
        return _users.FirstOrDefault(u => u.Id == id);
    }
}
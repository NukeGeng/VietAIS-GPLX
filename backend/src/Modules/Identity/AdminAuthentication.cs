using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Gplx.Modules.Identity;

public static class PermissionNames
{
    public const string QuestionBankRead = "questionbank.read";
    public const string QuestionBankImport = "questionbank.import";
    public const string QuestionBankEdit = "questionbank.edit";
    public const string QuestionBankPublish = "questionbank.publish";
    public const string RegulationRead = "regulation.read";
    public const string RegulationManage = "regulation.manage";
    public const string RegulationPublish = "regulation.publish";
    public const string ExamBlueprintRead = "exam-blueprint.read";
    public const string ExamBlueprintManage = "exam-blueprint.manage";
    public const string ExamBlueprintPublish = "exam-blueprint.publish";
    public const string AnalyticsRead = "analytics.read";
    public const string ProjectionRead = "system.projection.read";
    public const string ProjectionRebuild = "system.projection.rebuild";

    public static readonly IReadOnlyList<string> All =
    [
        QuestionBankRead,
        QuestionBankImport,
        QuestionBankEdit,
        QuestionBankPublish,
        RegulationRead,
        RegulationManage,
        RegulationPublish,
        ExamBlueprintRead,
        ExamBlueprintManage,
        ExamBlueprintPublish,
        AnalyticsRead,
        ProjectionRead,
        ProjectionRebuild
    ];
}

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "vietais-gplx";
    public string Audience { get; set; } = "vietais-gplx-admin";
    public string SigningKey { get; set; } = string.Empty;
}

public sealed class AdminOptions
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}

public sealed record AdminLoginRequest(string Email, string Password);

public sealed record AdminTokenResponse(string AccessToken, DateTimeOffset ExpiresAt, IReadOnlyList<string> Permissions);

public static class AdminAuthentication
{
    public static void AddAdminAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var jwt = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 characters.");
        }

        var admin = configuration.GetSection("Admin").Get<AdminOptions>() ?? new AdminOptions();
        if (string.IsNullOrWhiteSpace(admin.Email))
        {
            throw new InvalidOperationException("Admin:Email is required.");
        }

        if (!environment.IsDevelopment() && string.IsNullOrWhiteSpace(admin.PasswordHash))
        {
            throw new InvalidOperationException("Admin:PasswordHash is required outside Development.");
        }

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<AdminOptions>(configuration.GetSection("Admin"));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization(options =>
        {
            foreach (var permission in PermissionNames.All)
            {
                options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
            }
        });
    }

    public static IResult Login(AdminLoginRequest request, IOptions<JwtOptions> jwtOptions, IOptions<AdminOptions> adminOptions, IHostEnvironment environment)
    {
        var admin = adminOptions.Value;
        var validEmail = string.Equals(request.Email.Trim(), admin.Email.Trim(), StringComparison.OrdinalIgnoreCase);
        var validPassword = VerifyPassword(request.Password, admin, environment);
        if (!validEmail || !validPassword)
        {
            return Results.Unauthorized();
        }

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddHours(8);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, admin.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.Name, admin.Email)
        };
        claims.AddRange(PermissionNames.All.Select(permission => new Claim("permission", permission)));
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(jwtOptions.Value.Issuer, jwtOptions.Value.Audience, claims, now.UtcDateTime, expiresAt.UtcDateTime, credentials);
        return Results.Ok(new AdminTokenResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt, PermissionNames.All));
    }

    private static bool VerifyPassword(string password, AdminOptions admin, IHostEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(admin.PasswordHash))
        {
            return VerifyPbkdf2(password, admin.PasswordHash);
        }

        return environment.IsDevelopment() && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(password),
            Encoding.UTF8.GetBytes(admin.Password));
    }

    private static bool VerifyPbkdf2(string password, string encodedHash)
    {
        try
        {
            var parts = encodedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations)) return false;
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

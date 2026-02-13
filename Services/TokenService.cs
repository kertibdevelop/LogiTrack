using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


public class TokenService
{
    private readonly IConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;


    public TokenService(IConfiguration config, UserManager<ApplicationUser> userManager)
    {
        _config = config;
        _userManager = userManager;
    }

    public async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? ""),
            new Claim(ClaimTypes.Email, user.Email ?? "")
            // Ide jöhetnek még role claim-ek később: await userManager.GetRolesAsync(user)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));   // ← ez a kulcs sor!
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _config["Jwt:Key"]
                ?? _config["LOGITRACK_JWT_SECRET"]
                ?? Environment.GetEnvironmentVariable("LOGITRACK_JWT_SECRET")
                ?? throw new InvalidOperationException("JWT signing key is missing. Set LOGITRACK_JWT_SECRET env var.")
            )
        );
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiryInMinutes"] ?? "60")),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
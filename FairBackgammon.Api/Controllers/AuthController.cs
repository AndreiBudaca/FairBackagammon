using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FairBackgammon.Api.Controllers
{
  [ApiController]
  [Route("api/auth")]
  public sealed class AuthController : ControllerBase
  {
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
      _configuration = configuration;
    }

    [HttpPost("guest")]
    [AllowAnonymous]
    public ActionResult<GuestTokenResponse> CreateGuestToken()
    {
      Guid userId = Guid.NewGuid();
      string token = CreateToken(userId);
      return Ok(new GuestTokenResponse(token, userId));
    }

    private string CreateToken(Guid userId)
    {
      string issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing.");
      string audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing.");
      string key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");

      string? expiresMinutesRaw = _configuration["Jwt:ExpiresMinutes"];
      int expiresMinutes = int.TryParse(expiresMinutesRaw, out int minutes) ? minutes : 60;

      List<Claim> claims =
      [
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim(ClaimTypes.Role, "guest")
      ];

      var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
      var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

      var token = new JwtSecurityToken(
          issuer: issuer,
          audience: audience,
          claims: claims,
          expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
          signingCredentials: credentials);

      return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public sealed class GuestTokenResponse(string token, Guid userId)
    {
      public string Token { get; } = token;

      public Guid UserId { get; } = userId;
    }
  }
}

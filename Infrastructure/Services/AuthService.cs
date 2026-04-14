using AiClinic.Application.DTOs;
using AiClinic.Application.Services;
using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AiClinic.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpRepository _otpRepository;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IOtpRepository otpRepository,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _otpRepository = otpRepository;
        _configuration = configuration;
    }

    public async Task<bool> SendOtpAsync(string email)
    {
        var code = GenerateOtpCode();
        var otp = new OtpToken
        {
            Id = Guid.NewGuid(),
            Email = email,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _otpRepository.AddAsync(otp);
        
        // TODO: Send email via email service
        Console.WriteLine($"OTP for {email}: {code}");
        
        return true;
    }

    public async Task<AuthResponse> VerifyOtpAsync(string email, string code)
    {
        var otp = await _otpRepository.GetValidOtpAsync(email, code);
        
        if (otp == null)
        {
            return new AuthResponse(false, null, "Invalid or expired OTP", null);
        }

        var user = await _userRepository.GetByEmailAsync(email);
        
        // Auto-registration for new users
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Role = UserRole.Patient,
                CreatedAt = DateTime.UtcNow
            };
            user = await _userRepository.AddAsync(user);
        }

        await _otpRepository.MarkAsUsedAsync(otp.Id);

        var token = await GenerateJwtTokenAsync(user.Id, user.Email, user.Role.ToString());
        
        var userDto = new UserDto(user.Id, user.Email, user.FullName, user.Role.ToString());
        
        return new AuthResponse(true, token, "Login successful", userDto);
    }

    public async Task<string> GenerateJwtTokenAsync(Guid userId, string email, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? "your-secret-key-min-32-characters-long"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return await Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    private string GenerateOtpCode()
    {
        return new Random().Next(100000, 999999).ToString();
    }
}

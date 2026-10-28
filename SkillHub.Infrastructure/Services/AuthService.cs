using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SkillHub.Application.Common.Exceptions;
using SkillHub.Application.DTOs.Auth;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Application.Interfaces.Services;
using SkillHub.Domain.Entities;
using SkillHub.Infrastructure.Identity;

namespace SkillHub.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IParticipantRepository _participantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IParticipantRepository participantRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _participantRepository = participantRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            throw new ConflictException($"A user with email '{request.Email}' already exists.");

        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = request.Email.Trim().ToLowerInvariant(),
            Email = request.Email.Trim().ToLowerInvariant(),
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim()
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BadRequestException($"Registration failed: {errors}");
        }

        await _userManager.AddToRoleAsync(user, Roles.Participant);

        var participant = new Participant(
            fullName: user.FullName,
            email: user.Email,
            phoneNumber: user.PhoneNumber,
            id: user.Id
        );

        await _participantRepository.AddAsync(participant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = GenerateJwtToken(user, Roles.Participant);

        return new AuthResponse(
            user.Id,
            token,
            user.Email,
            user.FullName,
            Roles.Participant,
            expiresAt
        );
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user == null)
            throw new BadRequestException("Invalid email or password.");

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            throw new BadRequestException("Invalid email or password.");

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? Roles.Participant;

        var (token, expiresAt) = GenerateJwtToken(user, primaryRole);

        return new AuthResponse(
            user.Id,
            token,
            user.Email ?? string.Empty,
            user.FullName,
            primaryRole,
            expiresAt
        );
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            throw new BadRequestException("La contraseña actual es obligatoria.");

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            throw new BadRequestException("La nueva contraseña es obligatoria.");

        if (request.NewPassword != request.ConfirmPassword)
            throw new BadRequestException("La nueva contraseña y su confirmación no coinciden.");

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            throw new NotFoundException("Usuario", userId);

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BadRequestException($"No se pudo cambiar la contraseña: {errors}");
        }
    }

    private (string Token, DateTime ExpiresAt) GenerateJwtToken(ApplicationUser user, string role)

    {
        var secret = _configuration["Jwt:SecretKey"]
                     ?? _configuration["Jwt:Secret"]
                     ?? "DefaultSuperSecretKeyForSkillHubProject202610!";
        var issuer = _configuration["Jwt:Issuer"] ?? "SkillHubAPI";
        var audience = _configuration["Jwt:Audience"] ?? "SkillHubClient";

        var expirationInMinutes = int.TryParse(_configuration["Jwt:ExpirationInMinutes"], out var mins)
            ? mins
            : 60 * 24; // default 24 hours

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, role)
        };

        var expiresAt = DateTime.UtcNow.AddMinutes(expirationInMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return (tokenHandler.WriteToken(token), expiresAt);
    }
}

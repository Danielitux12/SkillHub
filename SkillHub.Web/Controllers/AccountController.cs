using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.Common.Exceptions;
using SkillHub.Application.DTOs.Auth;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Application.Interfaces.Services;
using SkillHub.Infrastructure.Identity;
using SkillHub.Web.Models.Account;

namespace SkillHub.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthService _authService;
    private readonly IInstructorRepository _instructorRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuthService authService,
        IInstructorRepository instructorRepository,
        IParticipantRepository participantRepository,
        IWorkshopRepository workshopRepository,
        IEnrollmentRepository enrollmentRepository,
        IUnitOfWork unitOfWork)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _authService = authService;
        _instructorRepository = instructorRepository;
        _participantRepository = participantRepository;
        _workshopRepository = workshopRepository;
        _enrollmentRepository = enrollmentRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Workshops");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Correo electrónico o contraseña incorrectos.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName ?? user.Email!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false
        );

        if (result.Succeeded)
        {
            TempData["Success"] = $"¡Bienvenido de nuevo, {user.FullName}!";
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Workshops");
        }

        ModelState.AddModelError(string.Empty, "Correo electrónico o contraseña incorrectos.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Workshops");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var request = new RegisterRequest(
                FullName: model.FullName.Trim(),
                Email: model.Email.Trim(),
                Password: model.Password,
                PhoneNumber: model.PhoneNumber?.Trim()
            );

            await _authService.RegisterAsync(request, cancellationToken);

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user != null)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
            }

            TempData["Success"] = "¡Cuenta creada exitosamente! Bienvenido a SkillHub.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Workshops");
        }
        catch (ConflictException ex)
        {
            ModelState.AddModelError(nameof(model.Email), ex.Message);
            return View(model);
        }
        catch (BadRequestException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Ocurrió un error inesperado al registrar el usuario: {ex.Message}");
            return View(model);
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var isEmployee = roles.Contains(Roles.Employee);

        var model = new ProfileViewModel
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Role = isEmployee ? "Staff / Instructor" : "Participante",
            IsEmployee = isEmployee
        };

        if (isEmployee)
        {
            var instructor = await _instructorRepository.GetByIdAsync(user.Id, cancellationToken);
            model.Specialty = instructor?.Specialty ?? "General Coordinator";
            var allWorkshops = await _workshopRepository.GetAllAsync(cancellationToken);
            model.TotalWorkshopsTaughtCount = allWorkshops.Count(w => w.InstructorId == user.Id);
        }
        else
        {
            var activeEnrollments = await _enrollmentRepository.GetActiveByParticipantIdAsync(user.Id, cancellationToken);
            model.ActiveEnrollmentsCount = activeEnrollments.Count;
        }

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var isEmployee = roles.Contains(Roles.Employee);

        if (!ModelState.IsValid)
        {
            model.Email = user.Email ?? string.Empty;
            model.IsEmployee = isEmployee;
            model.Role = isEmployee ? "Staff / Instructor" : "Participante";
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = model.PhoneNumber?.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.Email = user.Email ?? string.Empty;
            model.IsEmployee = isEmployee;
            model.Role = isEmployee ? "Staff / Instructor" : "Participante";
            return View(model);
        }

        // Sincronizar con entidad de dominio
        if (isEmployee)
        {
            var instructor = await _instructorRepository.GetByIdAsync(user.Id, cancellationToken);
            if (instructor != null)
            {
                instructor.UpdateProfile(
                    fullName: user.FullName,
                    email: user.Email ?? instructor.Email,
                    specialty: string.IsNullOrWhiteSpace(model.Specialty) ? instructor.Specialty : model.Specialty.Trim()
                );
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            var participant = await _participantRepository.GetByIdAsync(user.Id, cancellationToken);
            if (participant != null)
            {
                participant.UpdateProfile(user.FullName, user.PhoneNumber);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "¡Tu información de perfil ha sido actualizada exitosamente!";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "¡Contraseña actualizada con éxito!";
        return RedirectToAction("Index", "Workshops");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["Info"] = "Has cerrado sesión correctamente.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}


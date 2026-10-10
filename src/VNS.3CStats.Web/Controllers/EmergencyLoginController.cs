using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNS.ThreeCStats.Infrastructure;
using VNS.ThreeCStats.Infrastructure.Identity;
using VNS.ThreeCStats.Web.Models;

namespace VNS.ThreeCStats.Web.Controllers;

[AllowAnonymous]
[Route("emergency-login")]
public sealed class EmergencyLoginController(IEmergencyLoginService emergencyLoginService) : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        var model = new EmergencyLoginViewModel();
        if (!emergencyLoginService.IsAvailable)
        {
            ViewData["EmergencyLoginUnavailable"] = true;
        }

        return View(model);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(EmergencyLoginViewModel model)
    {
        if (!emergencyLoginService.IsAvailable)
        {
            ModelState.AddModelError(
                string.Empty,
                "Emergency Login is not configured. Set EmergencyLogin__Enabled, EmergencyLogin__Username and EmergencyLogin__Password, then restart the application.");
            model.Password = string.Empty;
            ViewData["EmergencyLoginUnavailable"] = true;
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var principal = emergencyLoginService.Authenticate(model.Username, model.Password);
        if (principal is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid emergency credentials.");
            model.Password = string.Empty;
            return View(model);
        }

        await HttpContext.SignInAsync(
            DependencyInjection.EmergencyCookieScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            });

        return Redirect("/admin/ingestion/ceb");
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(DependencyInjection.EmergencyCookieScheme);
        return RedirectToAction(nameof(Index));
    }
}

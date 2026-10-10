using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Qala.Framework.Domain.Constants;
using Qala.Framework.Domain.Exceptions;
using Qala.Framework.Domain.Identity;

namespace Qala.Game.Auth.Host.Controllers;

/// <summary>Email + password registration (JSON).</summary>
public sealed class RegisterRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Minimal sign-in for the authorization code flow. TODO: phone OTP, Apple and Google sign-in, email confirmation,
/// password reset, a localized and branded login page.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
[Route("account")]
public sealed class AccountController(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    IAntiforgery antiforgery,
    IStringLocalizer localizer,
    TimeProvider timeProvider) : Controller
{
    private const string LoginCsp = "default-src 'none'; style-src 'unsafe-inline'; form-action 'self'; frame-ancestors 'none'";

    [HttpGet("login")]
    [AllowAnonymous]
    public ContentResult Login(string? returnUrl = null, bool failed = false)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        Response.Headers.ContentSecurityPolicy = LoginCsp;
        var language = Qala.Framework.Application.Localization.JsonStringLocalizer.CurrentLanguage == "ar" ? "ar" : "en";
        var direction = language == "ar" ? "rtl" : "ltr";
        var error = failed ? $"<p class=\"err\">{WebUtility.HtmlEncode(localizer["Auth:Login:Failed"])}</p>" : string.Empty;
        var html = $$"""
            <!doctype html>
            <html lang="{{language}}" dir="{{direction}}"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <title>Qal'a sign in</title>
            <style>body{font-family:system-ui,sans-serif;max-width:22rem;margin:4rem auto;padding:0 1rem}label,input,button{display:block;width:100%;margin:.4rem 0}input,button{padding:.6rem;font-size:1rem}.err{color:#b00020}</style>
            </head><body>
            <h1>{{WebUtility.HtmlEncode(localizer["Auth:Login:Title"])}}</h1>{{error}}
            <form method="post" action="/account/login">
            <input type="hidden" name="{{tokens.FormFieldName}}" value="{{WebUtility.HtmlEncode(tokens.RequestToken)}}">
            <input type="hidden" name="returnUrl" value="{{WebUtility.HtmlEncode(returnUrl ?? "/")}}">
            <label>{{WebUtility.HtmlEncode(localizer["Auth:Login:Email"])}}<input name="email" type="email" autocomplete="username" required></label>
            <label>{{WebUtility.HtmlEncode(localizer["Auth:Login:Password"])}}<input name="password" type="password" autocomplete="current-password" required></label>
            <button type="submit">{{WebUtility.HtmlEncode(localizer["Auth:Login:Submit"])}}</button>
            </form></body></html>
            """;
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginPost([FromForm] string email, [FromForm] string password, [FromForm] string? returnUrl)
    {
        var user = await userManager.FindByEmailAsync(email);
        var result = user is null
            ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);
        var target = Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
        if (!result.Succeeded)
        {
            return Redirect($"/account/login?failed=true&returnUrl={Uri.EscapeDataString(target)}");
        }

        return LocalRedirect(target);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var displayName = request.DisplayName.Trim();
        if (displayName.Length is < FieldDefinitions.MinDisplayNameLength or > FieldDefinitions.MaxDisplayNameLength)
        {
            throw new CustomValidationException(localizer["General:Fields:InvalidValue"]);
        }

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = displayName,
            CreationTime = timeProvider.GetUtcNow().UtcDateTime,
        };
        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            throw new CustomValidationException(created.Errors.Select(e => e.Description));
        }

        return Ok(new { id = user.Id });
    }

    [HttpPost("logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }
}

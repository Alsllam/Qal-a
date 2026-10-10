using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;

namespace Qala.Framework.Application.Services;

/// <summary>
/// Base class of every AppService. AppServices are exposed as controllers by
/// <see cref="DynamicControllers.DynamicControllerFeatureProvider"/>: the class needs an explicit
/// <c>[Route]</c> and each endpoint an explicit verb attribute. Everything requires a valid
/// OpenIddict access token unless marked <c>[AllowAnonymous]</c>.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public abstract class ApplicationService
{
}

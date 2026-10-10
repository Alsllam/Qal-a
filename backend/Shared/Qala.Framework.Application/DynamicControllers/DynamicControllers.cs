using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Qala.Framework.Application.Services;

namespace Qala.Framework.Application.DynamicControllers;

/// <summary>Marks a public AppService method that must not become an endpoint.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NonActionApiAttribute : Attribute
{
}

/// <summary>Treats every public, concrete <see cref="ApplicationService"/> whose name ends in <c>AppService</c> as a controller.</summary>
public sealed class DynamicControllerFeatureProvider : ControllerFeatureProvider
{
    public const string Suffix = "AppService";

    protected override bool IsController(TypeInfo typeInfo) => IsAppService(typeInfo);

    public static bool IsAppService(Type type) =>
        type is { IsClass: true, IsAbstract: false, IsPublic: true, ContainsGenericParameters: false }
        && type.Name.EndsWith(Suffix, StringComparison.Ordinal)
        && typeof(ApplicationService).IsAssignableFrom(type);
}

/// <summary>
/// Names AppService controllers without the suffix, drops <see cref="NonActionApiAttribute"/> methods, and gives
/// any action without an explicit route <c>POST {methodname}</c> (lower case, no <c>Async</c>).
/// </summary>
public sealed class DynamicControllerConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        ArgumentNullException.ThrowIfNull(application);
        foreach (var controller in application.Controllers.Where(c => DynamicControllerFeatureProvider.IsAppService(c.ControllerType)))
        {
            if (controller.ControllerName.EndsWith(DynamicControllerFeatureProvider.Suffix, StringComparison.Ordinal))
            {
                controller.ControllerName = controller.ControllerName[..^DynamicControllerFeatureProvider.Suffix.Length];
            }

            foreach (var action in controller.Actions.ToList())
            {
                if (action.ActionMethod.GetCustomAttribute<NonActionApiAttribute>(inherit: true) is not null)
                {
                    controller.Actions.Remove(action);
                    continue;
                }

                foreach (var selector in action.Selectors.Where(s => s.AttributeRouteModel is null))
                {
                    var name = action.ActionMethod.Name;
                    if (name.EndsWith("Async", StringComparison.Ordinal))
                    {
                        name = name[..^5];
                    }

#pragma warning disable CA1308 // Routes are lower case by convention.
                    selector.AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(name.ToLowerInvariant()));
#pragma warning restore CA1308
                    if (!selector.ActionConstraints.OfType<HttpMethodActionConstraint>().Any())
                    {
                        selector.ActionConstraints.Add(new HttpMethodActionConstraint([HttpMethods.Post]));
                        selector.EndpointMetadata.Add(new HttpMethodMetadata([HttpMethods.Post]));
                    }
                }
            }
        }
    }
}

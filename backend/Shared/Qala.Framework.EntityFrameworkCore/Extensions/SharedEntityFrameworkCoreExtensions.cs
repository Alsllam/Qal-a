using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qala.Framework.Domain.Events;
using Qala.Framework.Domain.Repositories;
using Qala.Framework.EntityFrameworkCore.Messaging;
using Qala.Framework.EntityFrameworkCore.Repositories;

namespace Qala.Framework.EntityFrameworkCore.Extensions;

public static class SharedEntityFrameworkCoreExtensions
{
    /// <summary>
    /// Generic repositories, the unit of work, and MassTransit with every consumer in <paramref name="consumersNamespace"/>
    /// (e.g. <c>Qala.Game.Players.Application.EventHandlers</c>). The module's own registration must alias its DbContext
    /// as <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.
    /// </summary>
    public static IServiceCollection AddSharedEntityFrameworkCoreModule(this IServiceCollection services, IConfiguration configuration, string? consumersNamespace = null)
    {
        services.AddSharedRepositories();
        var settings = configuration.GetSection(MessageBrokerSettings.SectionName).Get<MessageBrokerSettings>() ?? new MessageBrokerSettings();
        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();
            if (!string.IsNullOrWhiteSpace(consumersNamespace) && FindAssembly(consumersNamespace) is { } assembly)
            {
                bus.AddConsumers(type => type.Namespace == consumersNamespace, assembly);
            }

            if (string.Equals(settings.Transport, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                bus.UsingInMemory((context, cfg) =>
                {
                    cfg.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                bus.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(settings.Host, settings.VirtualHost, h =>
                    {
                        h.Username(settings.Username);
                        h.Password(settings.Password);
                    });
                    cfg.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                    cfg.ConfigureEndpoints(context);
                });
            }
        });
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();
        return services;
    }

    /// <summary>Generic repositories and the unit of work only (used by tests and the DbMigrator).</summary>
    public static IServiceCollection AddSharedRepositories(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.TryAddScoped(typeof(IReadOnlyRepository<,>), typeof(ReadOnlyRepository<,>));
        services.TryAddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    /// <summary>Finds the assembly that holds a namespace by trying ever shorter prefixes (assembly name = namespace prefix).</summary>
    private static Assembly? FindAssembly(string @namespace)
    {
        for (var name = @namespace; name.Length > 0; name = name.Contains('.', StringComparison.Ordinal) ? name[..name.LastIndexOf('.')] : string.Empty)
        {
            var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
            if (loaded is not null)
            {
                return loaded;
            }

            try
            {
                return Assembly.Load(new AssemblyName(name));
            }
            catch (FileNotFoundException)
            {
                // Try a shorter prefix.
            }
        }

        return null;
    }
}

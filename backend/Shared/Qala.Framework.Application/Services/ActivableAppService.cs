using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Qala.Framework.Application.Dtos;
using Qala.Framework.Application.Localization;
using Qala.Framework.Application.Routing;
using Qala.Framework.Domain.Entities;
using Qala.Framework.Domain.Exceptions;
using Qala.Framework.Domain.Repositories;
using Qala.Framework.Domain.Security;

namespace Qala.Framework.Application.Services;

/// <summary>
/// Adds <c>POST activate</c> and <c>POST deactivate</c> for an <see cref="IActivableEntity"/>.
/// Both require <see cref="ActivationPermission"/>, checked against the caller's permission claims.
/// </summary>
public abstract class ActivableAppService<TEntity, TKey> : ApplicationService
    where TEntity : class, IEntity<TKey>, IActivableEntity
{
    private readonly IRepository<TEntity, TKey> _repository;
    private readonly ICurrentUser _currentUser;
    private readonly IStringLocalizer _localizer;

    protected ActivableAppService(IRepository<TEntity, TKey> repository, ICurrentUser currentUser, IStringLocalizer localizer)
    {
        _repository = repository;
        _currentUser = currentUser;
        _localizer = localizer;
    }

    /// <summary>The permission needed to activate or deactivate.</summary>
    protected abstract string ActivationPermission { get; }

    /// <summary>Activates the entity.</summary>
    [HttpPost(RouteDefinitions.Activate)]
    public virtual Task ActivateAsync(EntityIdDto<TKey> input, CancellationToken cancellationToken = default) =>
        SetActiveAsync(input, true, cancellationToken);

    /// <summary>Deactivates the entity.</summary>
    [HttpPost(RouteDefinitions.Deactivate)]
    public virtual Task DeactivateAsync(EntityIdDto<TKey> input, CancellationToken cancellationToken = default) =>
        SetActiveAsync(input, false, cancellationToken);

    /// <summary>Hook after a successful change, e.g. to publish an event.</summary>
    protected virtual Task OnActiveChangedAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SetActiveAsync(EntityIdDto<TKey> input, bool isActive, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!_currentUser.HasPermission(ActivationPermission))
        {
            throw new ForbiddenException(_localizer[LocalizationKeys.Forbidden]);
        }

        var entity = await _repository.FindAsync(input.Id, cancellationToken)
            ?? throw new EntityNotFoundException(_localizer[LocalizationKeys.NotFound]);
        if (entity.IsActive == isActive)
        {
            return;
        }

        entity.IsActive = isActive;
        await _repository.UpdateAsync(entity, autoSave: true, cancellationToken);
        await OnActiveChangedAsync(entity, cancellationToken);
    }
}

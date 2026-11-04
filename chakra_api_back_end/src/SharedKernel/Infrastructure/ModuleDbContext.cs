using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SharedKernel.Application.Exceptions;
using SharedKernel.Domain;

namespace SharedKernel.Infrastructure;

/// <summary>
/// 模块数据库上下文基类，统一维护实体更新时间、版本号和乐观并发异常
/// </summary>
public abstract class ModuleDbContext(DbContextOptions options, IPublisher publisher) : DbContext(options)
{
    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var domainEvents = GetDomainEvents();
        IDbContextTransaction? transaction = null;

        try
        {
            if (domainEvents.Count > 0 && Database.CurrentTransaction == null)
            {
                transaction = Database.BeginTransaction();
            }

            PublishDomainEventsAsync(domainEvents, CancellationToken.None).GetAwaiter().GetResult();
            PrepareModifiedEntities();
            var result = base.SaveChanges(acceptAllChangesOnSuccess);
            transaction?.Commit();
            ClearDomainEvents(domainEvents);
            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            transaction?.Rollback();
            throw CreateConcurrencyException(ex);
        }
        catch
        {
            transaction?.Rollback();
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        var domainEvents = GetDomainEvents();
        IDbContextTransaction? transaction = null;

        try
        {
            if (domainEvents.Count > 0 && Database.CurrentTransaction == null)
            {
                transaction = await Database.BeginTransactionAsync(cancellationToken);
            }

            await PublishDomainEventsAsync(domainEvents, cancellationToken);
            PrepareModifiedEntities();
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            ClearDomainEvents(domainEvents);
            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw CreateConcurrencyException(ex);
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private List<(Entity Entity, IDomainEvent DomainEvent)> GetDomainEvents()
    {
        return ChangeTracker.Entries<Entity>()
            .SelectMany(
                entry => entry.Entity.GetDomainEvents(),
                (entry, domainEvent) => (entry.Entity, domainEvent))
            .ToList();
    }

    private async Task PublishDomainEventsAsync(
        IEnumerable<(Entity Entity, IDomainEvent DomainEvent)> domainEvents,
        CancellationToken cancellationToken)
    {
        foreach (var (_, domainEvent) in domainEvents)
        {
            await publisher.Publish(domainEvent, cancellationToken);
        }
    }

    private static void ClearDomainEvents(IEnumerable<(Entity Entity, IDomainEvent DomainEvent)> domainEvents)
    {
        foreach (var entity in domainEvents.Select(item => item.Entity).Distinct())
        {
            entity.ClearDomainEvents();
        }
    }

    private void PrepareModifiedEntities()
    {
        foreach (var entry in ChangeTracker.Entries<Entity>().Where(entry => entry.State == EntityState.Modified))
        {
            entry.Entity.SetUpdatedAt();
            entry.Entity.IncrementVersion();
        }
    }

    private static ConcurrencyException CreateConcurrencyException(DbUpdateConcurrencyException exception)
    {
        var entry = exception.Entries.FirstOrDefault();
        if (entry == null)
        {
            return new ConcurrencyException();
        }

        var key = entry.Metadata.FindPrimaryKey()?.Properties
            .Select(property => entry.Property(property.Name).CurrentValue)
            .FirstOrDefault() ?? "unknown";
        return new ConcurrencyException(entry.Metadata.ClrType.Name, key);
    }
}

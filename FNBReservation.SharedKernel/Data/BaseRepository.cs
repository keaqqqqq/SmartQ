using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FNBReservation.SharedKernel.Data
{
    /// <summary>
    /// Enhanced base repository implementation that handles all common EF Core operations
    /// and routes them to the appropriate database (primary or replica)
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TContext">The DbContext type</typeparam>
    public abstract class BaseRepository<TEntity, TContext>
        where TEntity : class
        where TContext : DbContext
    {
        protected readonly DbContextFactory<TContext> _contextFactory;
        protected readonly ILogger<BaseRepository<TEntity, TContext>> _logger; 

        protected BaseRepository(DbContextFactory<TContext> contextFactory, ILogger<BaseRepository<TEntity, TContext>> logger)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); 

        }

        #region Basic CRUD Operations

        // Read operations use the read replica
        public virtual async Task<TEntity> GetByIdAsync(object id)
        {
            using var context = _contextFactory.CreateReadContext();
            return await context.Set<TEntity>().FindAsync(id);
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            _logger.LogDebug("BaseRepository: Getting all entities of type {EntityType}", typeof(TEntity).Name);

            using var context = _contextFactory.CreateReadContext();
            return await context.Set<TEntity>().ToListAsync();
        }

        public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            using var context = _contextFactory.CreateReadContext();
            return await context.Set<TEntity>().Where(predicate).ToListAsync();
        }

        // Write operations use the primary database
        public virtual async Task<TEntity> AddAsync(TEntity entity)
        {
            using var context = _contextFactory.CreateWriteContext();
            var result = await context.Set<TEntity>().AddAsync(entity);
            await context.SaveChangesAsync();
            return result.Entity;
        }

        public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities)
        {
            using var context = _contextFactory.CreateWriteContext();
            await context.Set<TEntity>().AddRangeAsync(entities);
            await context.SaveChangesAsync();
            return entities;
        }

        public virtual async Task UpdateAsync(TEntity entity)
        {
            using var context = _contextFactory.CreateWriteContext();
            context.Set<TEntity>().Update(entity);
            await context.SaveChangesAsync();
        }

        public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities)
        {
            using var context = _contextFactory.CreateWriteContext();
            context.Set<TEntity>().UpdateRange(entities);
            await context.SaveChangesAsync();
        }

        public virtual async Task DeleteAsync(TEntity entity)
        {
            using var context = _contextFactory.CreateWriteContext();
            context.Set<TEntity>().Remove(entity);
            await context.SaveChangesAsync();
        }

        public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities)
        {
            using var context = _contextFactory.CreateWriteContext();
            context.Set<TEntity>().RemoveRange(entities);
            await context.SaveChangesAsync();
        }

        #endregion

        #region Enhanced Query Methods (Read Operations)

        // These methods use the read replica

        public virtual async Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            using var context = _contextFactory.CreateReadContext();
            return await context.Set<TEntity>().FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            using var context = _contextFactory.CreateReadContext();
            return await context.Set<TEntity>().SingleOrDefaultAsync(predicate);
        }

        public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
        {
            using var context = _contextFactory.CreateReadContext();
            return await context.Set<TEntity>().AnyAsync(predicate);
        }

        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            using var context = _contextFactory.CreateReadContext();
            return predicate == null
                ? await context.Set<TEntity>().CountAsync()
                : await context.Set<TEntity>().CountAsync(predicate);
        }

        public virtual async Task<List<TResult>> SelectAsync<TResult>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TResult>> selector)
        {
            using var context = _contextFactory.CreateReadContext();
            return await context.Set<TEntity>()
                .Where(predicate)
                .Select(selector)
                .ToListAsync();
        }

        public virtual async Task<List<TEntity>> ToListAsync(
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            params Expression<Func<TEntity, object>>[] includes)
        {
            using var context = _contextFactory.CreateReadContext();
            IQueryable<TEntity> query = context.Set<TEntity>();

            // Apply includes for eager loading
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            // Apply predicate if provided
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // Apply ordering if provided
            if (orderBy != null)
            {
                return await orderBy(query).ToListAsync();
            }

            return await query.ToListAsync();
        }

        public virtual async Task<(List<TEntity> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            params Expression<Func<TEntity, object>>[] includes)
        {
            using var context = _contextFactory.CreateReadContext();
            IQueryable<TEntity> query = context.Set<TEntity>();

            // Apply includes for eager loading
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            // Apply predicate if provided
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply ordering if provided
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            // Apply pagination
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        #endregion

        #region Advanced Operations

        // For complex read queries that need direct access to the read context
        public virtual async Task<TResult> ExecuteReadQueryAsync<TResult>(Func<DbSet<TEntity>, Task<TResult>> query)
        {
            using var context = _contextFactory.CreateReadContext();
            var dbSet = context.Set<TEntity>();
            return await query(dbSet);
        }

        // For complex read queries that need the full IQueryable interface
        public virtual async Task<TResult> ExecuteReadQueryableAsync<TResult>(
            Func<IQueryable<TEntity>, Task<TResult>> query)
        {
            using var context = _contextFactory.CreateReadContext();
            var queryable = context.Set<TEntity>().AsQueryable();
            return await query(queryable);
        }

        // For complex write operations that need direct access to the write context
        public virtual async Task<TResult> ExecuteWriteQueryAsync<TResult>(
            Func<DbSet<TEntity>, Task<TResult>> query)
        {
            using var context = _contextFactory.CreateWriteContext();
            var dbSet = context.Set<TEntity>();
            var result = await query(dbSet);
            await context.SaveChangesAsync();
            return result;
        }

        // For complex write operations that need to save changes manually
        public virtual async Task<TResult> ExecuteWriteTransactionAsync<TResult>(
            Func<TContext, Task<TResult>> operation)
        {
            using var context = _contextFactory.CreateWriteContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var result = await operation(context);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // For raw SQL queries (read operations)
        public virtual async Task<List<TEntity>> FromSqlRawAsync(string sql, params object[] parameters)
        {
            using var context = _contextFactory.CreateReadContext();
            return await context.Set<TEntity>().FromSqlRaw(sql, parameters).ToListAsync();
        }

        // For raw SQL commands (write operations)
        public virtual async Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
        {
            using var context = _contextFactory.CreateWriteContext();
            return await context.Database.ExecuteSqlRawAsync(sql, parameters);
        }

        #endregion
    }
}
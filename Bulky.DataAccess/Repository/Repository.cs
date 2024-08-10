using System.Linq.Expressions;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.Repository
{
    public class Repository<T> : IRepository<T>
        where T : class
    {
        internal DbSet<T> Dbset;

        public Repository(ApplicationDbContext context)
        {
            Dbset = context.Set<T>();
        }

        public IEnumerable<T> GetAll(
            Expression<Func<T, bool>>? filter = null,
            string? include = null
        )
        {
            IQueryable<T> query = Dbset;
            if (filter != null)
                query = query.Where(filter);
            if (!string.IsNullOrEmpty(include))
            {
                foreach (
                    var includeProp in include.Split(',', StringSplitOptions.RemoveEmptyEntries)
                )
                {
                    query = query.Include(includeProp);
                }
            }

            return query.ToList();
        }

        public T? GetOne(
            Expression<Func<T, bool>> filter,
            string? include = null,
            bool tracked = false
        )
        {
            IQueryable<T> query = tracked ? Dbset : Dbset.AsNoTracking();
            query = query.Where(filter);
            if (!string.IsNullOrEmpty(include))
            {
                foreach (
                    var includeProp in include.Split(',', StringSplitOptions.RemoveEmptyEntries)
                )
                {
                    query = query.Include(includeProp);
                }
            }
            return query.FirstOrDefault();
        }

        public void Add(T entity)
        {
            Dbset.Add(entity);
        }

        public void Remove(T entity)
        {
            Dbset.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            Dbset.RemoveRange(entities);
        }
    }
}

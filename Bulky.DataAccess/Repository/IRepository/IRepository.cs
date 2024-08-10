using System.Linq.Expressions;

namespace Bulky.DataAccess.Repository.IRepository;

public interface IRepository<T>
    where T : class
{
    IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter = null, string? include = null);
    T? GetOne(Expression<Func<T, bool>> filter, string? include = null, bool tracked = false); // parameter will be like(c => c.id == id)
    void Add(T entity);

    //void Update(T entity); // update may have different logic in different entities
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
}

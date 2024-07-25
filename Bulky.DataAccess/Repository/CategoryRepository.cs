using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    private readonly ApplicationDbContext context;

    public CategoryRepository(ApplicationDbContext _context)
        : base(_context)
    {
        this.context = _context;
    }

    public void Update(Category category)
    {
        context.Categories.Update(category);
    }

}

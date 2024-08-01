using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository;

public class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCartRepository
{
    private readonly ApplicationDbContext context;

    public ShoppingCartRepository(ApplicationDbContext context)
        : base(context)
    {
        this.context = context;
    }

    public void Update(ShoppingCart shoppingCart)
    {
        context.ShoppingCarts.Update(shoppingCart);
    }
}

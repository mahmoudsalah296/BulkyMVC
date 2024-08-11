using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository;

public class ProductImageRepository : Repository<ProductImage>, IProductImageRepository
{
    private readonly ApplicationDbContext context;

    public ProductImageRepository(ApplicationDbContext context)
        : base(context)
    {
        this.context = context;
    }

    public void Update(ProductImage productImage)
    {
        context.ProductImages.Update(productImage);
    }
}

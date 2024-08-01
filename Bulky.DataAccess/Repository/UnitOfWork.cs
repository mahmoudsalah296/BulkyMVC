﻿using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;

namespace Bulky.DataAccess.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    public ICategoryRepository CategoryRepository { get; private set; }
    public IProductRepository ProductRepository { get; private set; }
    public ICompanyRepository CompanyRepository { get; private set; }
    public IShoppingCartRepository ShoppingCartRepository { get; private set; }
    public IApplicationUserRepository ApplicationUserRepository { get; private set; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        CategoryRepository = new CategoryRepository(context);
        ProductRepository = new ProductRepository(context);
        CompanyRepository = new CompanyRepository(context);
        ShoppingCartRepository = new ShoppingCartRepository(context);
        ApplicationUserRepository = new ApplicationUserRepository(context);
    }

    public void Save()
    {
        _context.SaveChanges();
    }
}

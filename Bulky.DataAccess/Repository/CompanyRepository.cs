using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository;

internal class CompanyRepository : Repository<Company>, ICompanyRepository
{
    private readonly ApplicationDbContext context;

    public CompanyRepository(ApplicationDbContext context)
        : base(context)
    {
        this.context = context;
    }

    public void Update(Company company)
    {
        context.Companies.Update(company);
    }
}

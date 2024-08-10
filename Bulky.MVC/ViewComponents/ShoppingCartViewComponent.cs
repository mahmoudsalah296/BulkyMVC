using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Utility;
using Microsoft.AspNetCore.Mvc;

namespace Bulky.MVC.ViewComponents;

public class ShoppingCartViewComponent : ViewComponent
{
    private readonly IUnitOfWork _unitOfWork;

    public ShoppingCartViewComponent(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        ClaimsIdentity claimsIdentity = (ClaimsIdentity)User.Identity!;
        var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null)
        {
            if (HttpContext.Session.GetInt32(Constants.SessionCart) == null)
                HttpContext.Session.SetInt32(
                    Constants.SessionCart,
                    _unitOfWork
                        .ShoppingCartRepository.GetAll(u => u.ApplicationUserId == claim.Value)!
                        .Count()
                );
            return View(HttpContext.Session.GetInt32(Constants.SessionCart));
        }
        else
        {
            HttpContext.Session.Clear();
            return View(0);
        }
    }
}

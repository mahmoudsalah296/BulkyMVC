using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bulky.MVC.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class CartController : Controller
{
    private readonly IUnitOfWork unitOfWork;
    public ShoppingCartVM ShoppingCartVM { get; set; }

    public CartController(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        ShoppingCartVM = new()
        {
            ShoppingCartList = unitOfWork.ShoppingCartRepository.GetAll(
                u => u.ApplicationUserId == userId,
                include: "Product"
            )
        };

        foreach (var cart in ShoppingCartVM.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVM.OrderTotal += (cart.Price * cart.Count);
        }
        return View(ShoppingCartVM);
    }

    public IActionResult Plus(int cartId)
    {
        var cartFromDb = unitOfWork.ShoppingCartRepository.GetOne(u => u.Id == cartId);
        cartFromDb.Count += 1;
        unitOfWork.ShoppingCartRepository.Update(cartFromDb);
        unitOfWork.Save();

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Minus(int cartId)
    {
        var cartFromDb = unitOfWork.ShoppingCartRepository.GetOne(u => u.Id == cartId);
        if (cartFromDb.Count <= 1)
            unitOfWork.ShoppingCartRepository.Remove(cartFromDb);
        else
        {
            cartFromDb.Count -= 1;
            unitOfWork.ShoppingCartRepository.Update(cartFromDb);
        }
        unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Remove(int cartId)
    {
        var cartFromDb = unitOfWork.ShoppingCartRepository.GetOne(u => u.Id == cartId);
        unitOfWork.ShoppingCartRepository.Remove(cartFromDb);
        unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
    {
        if (shoppingCart.Count < 50)
            return shoppingCart.Product.Price;
        else if (shoppingCart.Count < 100)
            return shoppingCart.Product.Price50;
        else
            return shoppingCart.Product.Price100;
    }

    public IActionResult Summary()
    {
        return View();
    }
}

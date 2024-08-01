using System.Diagnostics;
using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bulky.MVC.Areas.Customer.Controllers;

[Area("Customer")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        IEnumerable<Product> products = _unitOfWork.ProductRepository.GetAll(include: "Category");
        return View(products);
    }

    public IActionResult Details(int productId)
    {
        var shoppingCart = new ShoppingCart();
        shoppingCart.Product = _unitOfWork.ProductRepository.GetOne(
            u => u.Id == productId,
            include: "Category"
        )!;
        shoppingCart.Count = 1;
        shoppingCart.ProductId = productId;

        return View(shoppingCart);
    }

    [HttpPost]
    [Authorize]
    public IActionResult Details(ShoppingCart cart)
    {
        // to get user id for logged in user
        ClaimsIdentity claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        cart.ApplicationUserId = userId;

        var cartFromDb = _unitOfWork.ShoppingCartRepository.GetOne(u =>
            u.ApplicationUserId == userId && u.ProductId == cart.ProductId
        );

        if (cartFromDb == null)
        {
            _unitOfWork.ShoppingCartRepository.Add(cart);
        }
        else
        {
            cartFromDb.Count += cart.Count;
            _unitOfWork.ShoppingCartRepository.Update(cartFromDb);
            // we don't have to use update because EF Core will track
            // the entity from when it was retrieved from the database
            // when we call save changes, it will update the entity automatically
            // if we want to disable this feature, we can use AsNoTracking() in the repository
        }
        _unitOfWork.Save();
        TempData["success"] = "Item add to the shopping cart";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }
        );
    }
}

using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Stripe;
using Stripe.Checkout;

namespace Bulky.MVC.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class CartController : Controller
{
    private readonly IUnitOfWork unitOfWork;

    [BindProperty]
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
            ),
            OrderHeader = new()
        };

        foreach (var cart in ShoppingCartVM.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
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
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        ShoppingCartVM = new()
        {
            ShoppingCartList = unitOfWork.ShoppingCartRepository.GetAll(
                u => u.ApplicationUserId == userId,
                include: "Product"
            ),
            OrderHeader = new()
        };
        ShoppingCartVM.OrderHeader.ApplicationUser = unitOfWork.ApplicationUserRepository.GetOne(
            u => u.Id == userId
        );
        var user = ShoppingCartVM.OrderHeader.ApplicationUser;
        ShoppingCartVM.OrderHeader.Name = user.Name;
        ShoppingCartVM.OrderHeader.PhoneNumber = user.PhoneNumber ?? "";
        ShoppingCartVM.OrderHeader.City = user.City ?? "";
        ShoppingCartVM.OrderHeader.State = user.State ?? "";
        ShoppingCartVM.OrderHeader.PostalCode = user.PostalCode ?? "";

        foreach (var cart in ShoppingCartVM.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }
        return View(ShoppingCartVM);
    }

    [HttpPost("Summary")]
    // because of bind property, we don't need to pass ShoppingCartVM as parameter
    public IActionResult SummaryPost()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        ShoppingCartVM.ShoppingCartList = unitOfWork.ShoppingCartRepository.GetAll(
            u => u.ApplicationUserId == userId,
            include: "Product"
        );

        ApplicationUser user = unitOfWork.ApplicationUserRepository.GetOne(u => u.Id == userId);

        ShoppingCartVM.OrderHeader.ApplicationUserId = userId;
        ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;

        foreach (var cart in ShoppingCartVM.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }
        if (user.CompanyId.GetValueOrDefault() == 0)
        {
            // regular customer
            ShoppingCartVM.OrderHeader.OrderStatus = Constants.StatusPending;
            ShoppingCartVM.OrderHeader.PaymentStatus = Constants.PaymentStatusPending;
        }
        else
        {
            // company account
            ShoppingCartVM.OrderHeader.OrderStatus = Constants.StatusApproved;
            ShoppingCartVM.OrderHeader.PaymentStatus = Constants.PaymentStatusDelayedPayment;
        }

        unitOfWork.OrderHeaderRepository.Add(ShoppingCartVM.OrderHeader);
        unitOfWork.Save();
        foreach (var cart in ShoppingCartVM.ShoppingCartList)
        {
            OrderDetail orderDetail =
                new()
                {
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    ProductId = cart.ProductId,
                    Count = cart.Count,
                    Price = cart.Price
                };
            unitOfWork.OrderDetailRepository.Add(orderDetail);
            unitOfWork.Save();
        }
        if (user.CompanyId.GetValueOrDefault() == 0)
        {
            //use stripe
            var domain = "https://localhost:7131/";
            var options = new SessionCreateOptions
            {
                SuccessUrl =
                    $"{domain}customer/cart/orderconfirmation?id={ShoppingCartVM.OrderHeader.Id}",
                CancelUrl = $"{domain}customer/cart/index",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in ShoppingCartVM.ShoppingCartList)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions()
                    {
                        UnitAmount = (long)(item.Price * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                };
                options.LineItems.Add(sessionLineItem);
            }
            var service = new SessionService();
            Session session = service.Create(options);
            unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(
                ShoppingCartVM.OrderHeader.Id,
                session.Id,
                session.PaymentIntentId
            );
            unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        return RedirectToAction(
            nameof(OrderConfirmation),
            new { id = ShoppingCartVM.OrderHeader.Id }
        );
    }

    public IActionResult OrderConfirmation(int id)
    {
        OrderHeader orderHeader = unitOfWork.OrderHeaderRepository.GetOne(
            u => u.Id == id,
            include: "ApplicationUser"
        );
        if (orderHeader.PaymentStatus != Constants.PaymentStatusDelayedPayment)
        {
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);
            if (session.PaymentStatus.ToLower() == "paid")
            {
                unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(
                    id,
                    session.Id,
                    session.PaymentIntentId
                );
                unitOfWork.OrderHeaderRepository.UpdateStatus(
                    id,
                    Constants.StatusApproved,
                    Constants.PaymentStatusApproved
                );
                unitOfWork.Save();
            }
        }

        List<ShoppingCart> shoppingCarts = unitOfWork
            .ShoppingCartRepository.GetAll(u =>
                u.ApplicationUserId == orderHeader.ApplicationUserId
            )
            .ToList();

        unitOfWork.ShoppingCartRepository.RemoveRange(shoppingCarts);
        unitOfWork.Save();

        return View(id);
    }
}

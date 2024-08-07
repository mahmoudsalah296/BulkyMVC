using System.Diagnostics;
using System.Security.Claims;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace Bulky.MVC.Areas.Admin.Controllers;

[Area(Constants.Role_Admin)]
[Authorize]
public class OrderController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    [BindProperty]
    public OrderVM OrderVM { get; set; }

    public OrderController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Details(int orderId)
    {
        OrderVM = new OrderVM()
        {
            OrderHeader = _unitOfWork.OrderHeaderRepository.GetOne(
                o => o.Id == orderId,
                include: "ApplicationUser"
            ),
            orderDetail = _unitOfWork.OrderDetailRepository.GetAll(
                u => u.OrderHeaderId == orderId,
                include: "Product"
            )
        };

        return View(OrderVM);
    }

    [Authorize(Roles = $"{Constants.Role_Admin},{Constants.Role_Employee}")]
    [HttpPost]
    public IActionResult UpdateOrderDetail()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeaderRepository.GetOne(u =>
            u.Id == OrderVM.OrderHeader.Id
        );
        orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
        orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
        orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
        orderHeaderFromDb.City = OrderVM.OrderHeader.City;
        orderHeaderFromDb.State = OrderVM.OrderHeader.State;
        orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;

        if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;

        if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;

        _unitOfWork.OrderHeaderRepository.Update(orderHeaderFromDb);
        _unitOfWork.Save();

        TempData["success"] = "Order Details updated successfully";
        return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
    }

    [Authorize(Roles = $"{Constants.Role_Admin},{Constants.Role_Employee}")]
    [HttpPost]
    public IActionResult StartProcessing()
    {
        _unitOfWork.OrderHeaderRepository.UpdateStatus(
            OrderVM.OrderHeader.Id,
            Constants.StatusProcessing
        );
        _unitOfWork.Save();
        TempData["success"] = "Order details updated successfully";

        return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
    }

    [Authorize(Roles = $"{Constants.Role_Admin},{Constants.Role_Employee}")]
    [HttpPost]
    public IActionResult ShipOrder()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeaderRepository.GetOne(u =>
            u.Id == OrderVM.OrderHeader.Id
        )!;
        orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
        orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
        orderHeaderFromDb.OrderStatus = Constants.StatusShipped;
        orderHeaderFromDb.ShippingDate = DateTime.Now;
        if (orderHeaderFromDb.PaymentStatus == Constants.PaymentStatusDelayedPayment)
        {
            orderHeaderFromDb.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
        }
        _unitOfWork.OrderHeaderRepository.Update(orderHeaderFromDb);

        _unitOfWork.OrderHeaderRepository.UpdateStatus(
            OrderVM.OrderHeader.Id,
            Constants.StatusShipped
        );

        _unitOfWork.Save();
        TempData["success"] = "Order shipped successfully";

        return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
    }

    public IActionResult CancelOrder()
    {
        var order = _unitOfWork.OrderHeaderRepository.GetOne(u => u.Id == OrderVM.OrderHeader.Id);
        if (order.PaymentStatus == Constants.PaymentStatusApproved)
        {
            var options = new RefundCreateOptions()
            {
                Reason = RefundReasons.RequestedByCustomer,
                PaymentIntent = order.PaymentIntentId
            };

            //var service =
            Refund refund = new RefundService().Create(options);

            _unitOfWork.OrderHeaderRepository.UpdateStatus(
                OrderVM.OrderHeader.Id,
                Constants.StatusCancelled,
                Constants.StatusRefunded
            );
        }
        else
        {
            _unitOfWork.OrderHeaderRepository.UpdateStatus(
                OrderVM.OrderHeader.Id,
                Constants.StatusCancelled,
                Constants.StatusCancelled
            );
        }
        _unitOfWork.Save();
        TempData["success"] = "Order cancelled successfully";

        return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
    }

    [HttpPost]
    [ActionName("Details")]
    public IActionResult PayNow()
    {
        OrderVM.OrderHeader = _unitOfWork.OrderHeaderRepository.GetOne(
            u => u.Id == OrderVM.OrderHeader.Id,
            include: "ApplicationUser"
        )!;
        OrderVM.orderDetail = _unitOfWork.OrderDetailRepository.GetAll(
            u => u.OrderHeaderId == OrderVM.OrderHeader.Id,
            include: "Product"
        );

        var domain = "https://localhost:7131/";
        var options = new SessionCreateOptions
        {
            SuccessUrl =
                $"{domain}admin/order/paymentconfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
            CancelUrl = $"{domain}admin/order/details?orderId={OrderVM.OrderHeader.Id}",
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "payment",
        };

        foreach (var item in OrderVM.orderDetail)
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
        _unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(
            OrderVM.OrderHeader.Id,
            session.Id,
            session.PaymentIntentId
        );
        _unitOfWork.Save();
        Response.Headers.Add("Location", session.Url);
        return new StatusCodeResult(303);
    }

    public IActionResult PaymentConfirmation(int orderHeaderId)
    {
        OrderHeader orderHeader = _unitOfWork.OrderHeaderRepository.GetOne(u =>
            u.Id == orderHeaderId
        )!;
        if (orderHeader.PaymentStatus == Constants.PaymentStatusDelayedPayment)
        {
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);
            if (session.PaymentStatus.ToLower() == "paid")
            {
                _unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(
                    orderHeaderId,
                    session.Id,
                    session.PaymentIntentId
                );
                _unitOfWork.OrderHeaderRepository.UpdateStatus(
                    orderHeaderId,
                    orderHeader.OrderStatus,
                    Constants.PaymentStatusApproved
                );
                _unitOfWork.Save();
            }
        }

        return View(orderHeaderId);
    }

    #region API CALLS
    [HttpGet]
    public IActionResult GetAll(string status)
    {
        IEnumerable<OrderHeader> orders;
        if (User.IsInRole(Constants.Role_Admin) || User.IsInRole(Constants.Role_Employee))
        {
            orders = _unitOfWork.OrderHeaderRepository.GetAll(include: "ApplicationUser");
            //.ToList();
        }
        else
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            orders = _unitOfWork.OrderHeaderRepository.GetAll(
                u => u.ApplicationUserId == userId,
                include: "ApplicationUser"
            );
        }
        switch (status)
        {
            case "pending":
                orders = orders.Where(o => o.PaymentStatus == Constants.PaymentStatusPending);
                break;
            case "inprocess":
                orders = orders.Where(o => o.PaymentStatus == Constants.StatusProcessing);
                break;
            case "completed":
                orders = orders.Where(o => o.PaymentStatus == Constants.StatusShipped);
                break;
            case "approved":
                orders = orders.Where(o => o.PaymentStatus == Constants.StatusApproved);
                break;
            default:
                break;
        }

        return Json(new { data = orders });
    }
    #endregion
}

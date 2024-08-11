using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Bulky.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.Role_Admin)]
public class UserController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> userManager;

    public UserController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        this.userManager = userManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult RoleManagement(string id)
    {
        var user = _context
            .ApplicationUsers.Include(u => u.Company)
            .FirstOrDefault(u => u.Id == id);
        var userVM = new UserVM()
        {
            User = user,
            RolesList = _context.Roles.Select(c => new SelectListItem()
            {
                Text = c.Name,
                Value = c.Name
            }),
            CompanyList = _context.Companies.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            })
        };
        var roleId = _context.UserRoles.FirstOrDefault(u => u.UserId == id)!.RoleId!;
        userVM.User!.Role = _context.Roles.FirstOrDefault(r => r.Id == roleId)!.Name!;
        return View(userVM);
    }

    [HttpPost]
    public IActionResult RoleManagement(UserVM userVM)
    {
        var roleId = _context.UserRoles.FirstOrDefault(u => u.UserId == userVM.User.Id)!.RoleId!;
        var oldRole = _context.Roles.FirstOrDefault(u => u.Id == roleId)!.Name;
        if (userVM.User.Role != oldRole)
        {
            var user = _context.ApplicationUsers.FirstOrDefault(u => u.Id == userVM.User.Id);

            if (userVM.User.Role == Constants.Role_Company)
                user.CompanyId = userVM.User.CompanyId;

            if (oldRole == Constants.Role_Company)
                user.CompanyId = null;

            _context.SaveChanges();

            userManager.RemoveFromRoleAsync(user, oldRole).GetAwaiter().GetResult();
            userManager.AddToRoleAsync(user, userVM.User.Role).GetAwaiter().GetResult();
        }
        TempData["success"] = "User updated successfully";
        return RedirectToAction("index");
    }

    #region API CALLS
    [HttpGet]
    public IActionResult GetAll()
    {
        var users = _context.ApplicationUsers.Include(u => u.Company).ToList();
        var userRole = _context.UserRoles.ToList();
        var roles = _context.Roles.ToList();
        foreach (var user in users)
        {
            if (user.Id == "7d7eecde-76cf-4d38-b6af-3a4eb7a4ff2b")
                continue;
            var roleId = userRole.FirstOrDefault(u => u.UserId == user.Id).RoleId;
            user.Role = roles.FirstOrDefault(u => u.Id == roleId).Name;

            if (user.Company == null)
                user.Company = new() { Name = "" };
        }
        return Json(new { data = users });
    }

    [HttpPost]
    public IActionResult LockUnlock([FromBody] string id)
    {
        var user = _context.ApplicationUsers.FirstOrDefault(u => u.Id == id);
        if (user == null)
            return Json(new { success = false, message = "Error while lock/unlock" });

        if (user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
            user.LockoutEnd = DateTime.Now;
        else
            user.LockoutEnd = DateTime.Now.AddDays(7);
        _context.SaveChanges();
        return Json(new { success = true, message = "Locked/Unlocked Successfully" });
    }
    #endregion
}

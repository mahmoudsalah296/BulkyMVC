﻿using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.DbInitializer;

public class DbInitializer : IDbInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DbInitializer(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
    )
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task Initialize()
    {
        // apply pending  migrations
        try
        {
            if (_context.Database.GetPendingMigrations().Count() > 0)
            {
                _context.Database.Migrate();
            }
        }
        catch (Exception) { }

        // create roles if they are not created
        if (!await _roleManager.RoleExistsAsync(Constants.Role_Admin))
        {
            await _roleManager.CreateAsync(new IdentityRole(Constants.Role_Admin));
            await _roleManager.CreateAsync(new IdentityRole(Constants.Role_Company));
            await _roleManager.CreateAsync(new IdentityRole(Constants.Role_Customer));
            await _roleManager.CreateAsync(new IdentityRole(Constants.Role_Employee));

            // if there is no admin user, create one
            await _userManager.CreateAsync(
                new ApplicationUser
                {
                    UserName = "admin@gmail.com",
                    Email = "admin@gmail.com",
                    Name = "Mahmoud Salah",
                    PhoneNumber = "01143859447",
                    StreetAddress = "test 123 avg",
                    State = "Giza",
                    PostalCode = "12898",
                    City = "Giza",
                },
                "Admin@123"
            );

            ApplicationUser user = _context.ApplicationUsers.FirstOrDefault(u =>
                u.Email == "admin@gmail.com"
            )!;
            _userManager.AddToRoleAsync(user, Constants.Role_Admin).GetAwaiter().GetResult();
        }

        return;
    }
}

using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bulky.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.Role_Admin)]
public class CompanyController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CompanyController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var companies = _unitOfWork.CompanyRepository.GetAll().ToList();
        return View(companies);
    }

    public IActionResult Upsert(int? id) // update and insert
    {
        Company? company = new();
        if (id is null or 0)
            return View(company);
        company = _unitOfWork.CompanyRepository.GetOne(p => p.Id == id);
        if (company == null)
            return NotFound();
        return View(company);
    }

    [HttpPost]
    public IActionResult Upsert(Company company)
    {
        if (!ModelState.IsValid)
            return View(company);

        if (company.Id == 0)
            _unitOfWork.CompanyRepository.Add(company);
        else
            _unitOfWork.CompanyRepository.Update(company);

        _unitOfWork.Save();
        TempData["success"] = "Company added successfully";
        return RedirectToAction("Index");
    }

    #region API CALLS
    [HttpGet]
    public IActionResult GetAll()
    {
        var companies = _unitOfWork.CompanyRepository.GetAll().ToList();
        return Json(new { data = companies });
    }

    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        var company = _unitOfWork.CompanyRepository.GetOne(c => c.Id == id);
        if (company is null)
            return NotFound("No company with this ID");

        _unitOfWork.CompanyRepository.Remove(company);
        _unitOfWork.Save();

        return Json(new { success = true, message = "Deleted Successfully" });
    }
    #endregion
}

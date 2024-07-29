using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bulky.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.Role_Admin)]
public class CategoryController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        List<Category> categories = _unitOfWork.CategoryRepository.GetAll().ToList();
        return View(categories);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Category category)
    {
        if (category.Name == category.DisplayOrder.ToString())
            ModelState.AddModelError("name", "Name and display order shouldn't be the same");

        if (!ModelState.IsValid)
            return View(category);

        _unitOfWork.CategoryRepository.Add(category);
        _unitOfWork.Save();
        TempData["success"] = "Category added successfully";
        return RedirectToAction("Index");
    }

    public IActionResult Edit(int? id)
    {
        if (id is null or 0)
            return BadRequest("invalid ID");

        var category = _unitOfWork.CategoryRepository.GetOne(c => c.Id == id);
        if (category is null)
            return NotFound("No category with this ID");
        return View(category);
    }

    [HttpPost]
    public IActionResult Edit(Category category)
    {
        if (!ModelState.IsValid)
            return View(category);

        _unitOfWork.CategoryRepository.Update(category);
        _unitOfWork.Save();
        TempData["success"] = "Category updated successfully";
        return RedirectToAction("Index");
    }

    public IActionResult Delete(int? id)
    {
        if (id is null or 0)
        {
            return NotFound("No category with this ID");
        }

        var category = _unitOfWork.CategoryRepository.GetOne(c => c.Id == id);
        if (category is null)
            return NotFound("No category with this ID");
        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    public IActionResult DeletePost(int? id)
    {
        var category = _unitOfWork.CategoryRepository.GetOne(c => c.Id == id);
        if (category is null)
            return NotFound("No category with this ID");

        _unitOfWork.CategoryRepository.Remove(category);
        _unitOfWork.Save();
        TempData["error"] = "Category deleted successfully";
        return RedirectToAction("Index");
    }
}

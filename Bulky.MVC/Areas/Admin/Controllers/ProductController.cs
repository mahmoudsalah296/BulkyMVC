using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bulky.MVC.Areas.Admin.Controllers;

[Area("Admin")]
public class ProductController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var products = _unitOfWork.ProductRepository.GetAll().ToList();
        return View(products);
    }

    public IActionResult Create()
    {
        // projection
        IEnumerable<SelectListItem> categories = _unitOfWork
            .CategoryRepository.GetAll()
            .ToList()
            .Select(c => new SelectListItem() { Text = c.Name, Value = c.Id.ToString() });

        //ViewBag.CategoryList = categories;
        ProductVM productVm = new() { CategoryList = categories, Product = new Product() };
        return View(productVm);
    }

    [HttpPost]
    public IActionResult Create(ProductVM productVm)
    {
        if (!ModelState.IsValid)
        {
            productVm.CategoryList = _unitOfWork
                .CategoryRepository.GetAll()
                .ToList()
                .Select(c => new SelectListItem() { Text = c.Name, Value = c.Id.ToString() });

            return View(productVm);
        }
        _unitOfWork.ProductRepository.Add(productVm.Product);
        _unitOfWork.Save();
        TempData["success"] = "Product added successfully";
        return RedirectToAction("Index");
    }

    public IActionResult Edit(int? id)
    {
        if (id is null or 0)
            return BadRequest("Invalid Id");
        var product = _unitOfWork.ProductRepository.GetOne(p => p.Id == id);
        if (product == null)
            return NotFound();
        return View(product);
    }

    [HttpPost]
    public IActionResult Edit(Product product)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        _unitOfWork.ProductRepository.Update(product);
        _unitOfWork.Save();
        TempData["success"] = "Product updated successfully";
        return RedirectToAction("Index");
    }

    public IActionResult Delete(int? id)
    {
        if (id is null or 0)
        {
            return NotFound("No product with this ID");
        }

        var product = _unitOfWork.ProductRepository.GetOne(c => c.Id == id);
        if (product is null)
            return NotFound("No product with this ID");
        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    public IActionResult DeletePost(int? id)
    {
        var product = _unitOfWork.ProductRepository.GetOne(c => c.Id == id);
        if (product is null)
            return NotFound("No product with this ID");

        _unitOfWork.ProductRepository.Remove(product);
        _unitOfWork.Save();
        TempData["error"] = "Category deleted successfully";
        return RedirectToAction("Index");
    }
}

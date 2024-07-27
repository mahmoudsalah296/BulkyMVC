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
    private readonly IWebHostEnvironment _webHostEnvironment; // to access wwwroot

    public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
    {
        _unitOfWork = unitOfWork;
        _webHostEnvironment = webHostEnvironment;
    }

    public IActionResult Index()
    {
        var products = _unitOfWork.ProductRepository.GetAll(include: "Category").ToList();
        return View(products);
    }

    public IActionResult Upsert(int? id) // update and insert
    {
        // projection
        IEnumerable<SelectListItem> categories = _unitOfWork
            .CategoryRepository.GetAll()
            .ToList()
            .Select(c => new SelectListItem() { Text = c.Name, Value = c.Id.ToString() });

        //ViewBag.CategoryList = categories;
        ProductVM productVm = new() { CategoryList = categories, Product = new Product() };
        if (id is null or 0)
            return View(productVm);
        productVm.Product = _unitOfWork.ProductRepository.GetOne(p => p.Id == id);
        if (productVm.Product == null)
            return NotFound();
        return View(productVm);
    }

    [HttpPost]
    public IActionResult Upsert(ProductVM productVm, IFormFile? file)
    {
        if (!ModelState.IsValid)
        {
            productVm.CategoryList = _unitOfWork
                .CategoryRepository.GetAll()
                .ToList()
                .Select(c => new SelectListItem() { Text = c.Name, Value = c.Id.ToString() });

            return View(productVm);
        }

        string wwwRootPath = _webHostEnvironment.WebRootPath;
        if (file is not null)
        {
            // change file name
            string filename = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
            // get the path in which file will be saved
            string productPath = Path.Combine(wwwRootPath, "images", "products");
            // when updating if user upload an image remove old image
            if (!string.IsNullOrEmpty(productVm.Product.ImageUrl))
            {
                var oldImagePath = Path.Combine(
                    wwwRootPath,
                    productVm.Product.ImageUrl.TrimStart('\\')
                );
                if (System.IO.File.Exists(oldImagePath))
                    System.IO.File.Delete(oldImagePath);
            }
            // saving the image
            using (
                var filestream = new FileStream(
                    Path.Combine(productPath, filename),
                    FileMode.Create
                )
            )
            {
                file.CopyTo(filestream);
            }

            productVm.Product.ImageUrl = @"\images\products\" + filename;
        }

        if (productVm.Product.Id == 0)
            _unitOfWork.ProductRepository.Add(productVm.Product);
        else
            _unitOfWork.ProductRepository.Update(productVm.Product);

        _unitOfWork.Save();
        TempData["success"] = "Product added successfully";
        return RedirectToAction("Index");
    }

    //public IActionResult Edit(int? id)
    //{
    //    if (id is null or 0)
    //        return BadRequest("Invalid Id");
    //    var product = _unitOfWork.ProductRepository.GetOne(p => p.Id == id);
    //    if (product == null)
    //        return NotFound();
    //    return View(product);
    //}

    //[HttpPost]
    //public IActionResult Edit(Product product)
    //{
    //    if (!ModelState.IsValid)
    //        return BadRequest(ModelState);
    //    _unitOfWork.ProductRepository.Update(product);
    //    _unitOfWork.Save();
    //    TempData["success"] = "Product updated successfully";
    //    return RedirectToAction("Index");
    //}

    //public IActionResult Delete(int? id)
    //{
    //    if (id is null or 0)
    //    {
    //        return NotFound("No product with this ID");
    //    }

    //    var product = _unitOfWork.ProductRepository.GetOne(c => c.Id == id);
    //    if (product is null)
    //        return NotFound("No product with this ID");
    //    return View(product);
    //}

    // moved to region api calls
    //[HttpPost, ActionName("Delete")]
    //public IActionResult DeletePost(int? id)
    //{
    //    var product = _unitOfWork.ProductRepository.GetOne(c => c.Id == id);
    //    if (product is null)
    //        return NotFound("No product with this ID");
    //    var oldImagePath = Path.Combine(
    //        _webHostEnvironment.WebRootPath,
    //        product.ImageUrl?.TrimStart('\\') ?? string.Empty
    //    );
    //    if (System.IO.File.Exists(oldImagePath))
    //        System.IO.File.Delete(oldImagePath);
    //    _unitOfWork.ProductRepository.Remove(product);
    //    _unitOfWork.Save();
    //    TempData["error"] = "Category deleted successfully";
    //    return RedirectToAction("Index");
    //}

    #region API CALLS
    [HttpGet]
    public IActionResult GetAll()
    {
        var products = _unitOfWork.ProductRepository.GetAll(include: "Category").ToList();
        return Json(new { data = products });
    }

    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        var product = _unitOfWork.ProductRepository.GetOne(c => c.Id == id);
        if (product is null)
            return NotFound("No product with this ID");
        var oldImagePath = Path.Combine(
            _webHostEnvironment.WebRootPath,
            product.ImageUrl?.TrimStart('\\') ?? string.Empty
        );
        if (System.IO.File.Exists(oldImagePath))
            System.IO.File.Delete(oldImagePath);
        _unitOfWork.ProductRepository.Remove(product);
        _unitOfWork.Save();

        return Json(new { success = true, message = "Deleted Successfully" });
    }
    #endregion
}

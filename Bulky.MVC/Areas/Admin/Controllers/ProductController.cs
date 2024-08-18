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
        productVm.Product = _unitOfWork.ProductRepository.GetOne(
            p => p.Id == id,
            include: "ProductImages"
        )!;
        if (productVm.Product == null)
            return NotFound();
        return View(productVm);
    }

    [HttpPost]
    public IActionResult Upsert(ProductVM productVm, List<IFormFile>? files)
    {
        if (!ModelState.IsValid)
        {
            productVm.CategoryList = _unitOfWork
                .CategoryRepository.GetAll()
                .ToList()
                .Select(c => new SelectListItem() { Text = c.Name, Value = c.Id.ToString() });

            return View(productVm);
        }

        if (productVm.Product.Id == 0)
            _unitOfWork.ProductRepository.Add(productVm.Product);
        else
            _unitOfWork.ProductRepository.Update(productVm.Product);

        _unitOfWork.Save();

        string wwwRootPath = _webHostEnvironment.WebRootPath;
        if (files is not null)
        {
            foreach (IFormFile file in files)
            {
                // change file name
                string filename = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);

                string productPath = @"images\products\product-" + productVm.Product.Id;
                // get the path in which file will be saved
                string finalPath = Path.Combine(wwwRootPath, productPath);

                if (!Directory.Exists(finalPath))
                {
                    Directory.CreateDirectory(finalPath);
                }

                using (
                    var filestream = new FileStream(
                        Path.Combine(finalPath, filename),
                        FileMode.Create
                    )
                )
                {
                    file.CopyTo(filestream);
                }

                ProductImage productImage =
                    new()
                    {
                        ImageUrl = $@"\{productPath}\{filename}",
                        ProductId = productVm.Product.Id
                    };

                if (productVm.Product.ProductImages == null)
                    productVm.Product.ProductImages = new List<ProductImage>();

                productVm.Product.ProductImages.Add(productImage);
            }

            _unitOfWork.ProductRepository.Update(productVm.Product);
            _unitOfWork.Save();

            // when updating if user upload an image remove old image
            //if (!string.IsNullOrEmpty(productVm.Product.ImageUrl))
            //{
            //    var oldImagePath = Path.Combine(
            //        wwwRootPath,
            //        productVm.Product.ImageUrl.TrimStart('\\')
            //    );
            //    if (System.IO.File.Exists(oldImagePath))
            //        System.IO.File.Delete(oldImagePath);
            //}
            //// saving the image
            //using (
            //    var filestream = new FileStream(
            //        Path.Combine(productPath, filename),
            //        FileMode.Create
            //    )
            //)
            //{
            //    file.CopyTo(filestream);
            //}

            //productVm.Product.ImageUrl = @"\images\products\" + filename;
        }

        TempData["success"] = "Product added/updated successfully";
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

    public IActionResult DeleteImage(int imageId)
    {
        var img = _unitOfWork.ProductImageRepository.GetOne(i => i.Id == imageId);

        if (!string.IsNullOrEmpty(img!.ImageUrl))
        {
            var oldImagePath = Path.Combine(
                _webHostEnvironment.WebRootPath,
                img.ImageUrl?.TrimStart('\\') ?? string.Empty
            );
            if (System.IO.File.Exists(oldImagePath))
                System.IO.File.Delete(oldImagePath);
        }
        _unitOfWork.ProductImageRepository.Remove(img);
        _unitOfWork.Save();
        TempData["success"] = "Deleted Successfully";

        return RedirectToAction(nameof(Upsert), new { Id = img.ProductId });
    }

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
        //var oldImagePath = Path.Combine(
        //    _webHostEnvironment.WebRootPath,
        //    product.ImageUrl?.TrimStart('\\') ?? string.Empty
        //);
        //if (System.IO.File.Exists(oldImagePath))
        //    System.IO.File.Delete(oldImagePath);

        string productPath = @"images\products\product-" + id;
        string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

        if (Directory.Exists(finalPath))
        {
            var files = Directory.GetFiles(finalPath);
            foreach (var file in files)
            {
                System.IO.File.Delete(file);
            }
            Directory.Delete(finalPath);
        }
        _unitOfWork.ProductRepository.Remove(product);
        _unitOfWork.Save();

        return Json(new { success = true, message = "Deleted Successfully" });
    }
    #endregion
}

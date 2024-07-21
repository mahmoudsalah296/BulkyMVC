using Bulky.MVC.Data;
using Bulky.MVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace Bulky.MVC.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext context;

        public CategoryController(ApplicationDbContext context)
        {
            this.context = context;
        }

        public IActionResult Index()
        {
            List<Category> categories = context.Categories.ToList();
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

            if (!ModelState.IsValid) return View(category);

            context.Categories.Add(category);
            context.SaveChanges();
            TempData["success"] = "Category added successfully";
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int? id)
        {
            if (id is null or 0)
            {
                return NotFound("No category with this ID");
            }

            var category = context.Categories.Find(id);
            if (category is null)
                return NotFound("No category with this ID");
            return View(category);
        }

        [HttpPost]
        public IActionResult Edit(Category category)
        {
            if (!ModelState.IsValid) return View(category);

            context.Categories.Update(category);
            context.SaveChanges();
            TempData["success"] = "Category updated successfully";
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int? id)
        {
            if (id is null or 0)
            {
                return NotFound("No category with this ID");
            }

            var category = context.Categories.Find(id);
            if (category is null)
                return NotFound("No category with this ID");
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePost(int? id)
        {
            var category = context.Categories.Find(id);
            if (category is null)
                return NotFound("No category with this ID");

            context.Categories.Remove(category);
            context.SaveChanges();
            TempData["success"] = "Category deleted successfully";
            return RedirectToAction("Index");
        }
    }
}

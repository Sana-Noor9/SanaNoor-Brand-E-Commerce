using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using SanaNoor_Brand.Data;
using SanaNoor_Brand.Models;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering; // 👈 Yeh lazmi add karein Regex ke liye

namespace SanaNoor_Brand.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CategoriesController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            // ParentCategory ko Include lazmi karna hai
            var categories = await _context.Categories
                .Include(c => c.ParentCategory)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return View(categories);
        }



        // GET: Categories/Create
        // Yeh method form ko "Display" karne ke liye zaroori hai
        public IActionResult Create()
        {
            // Dropdown ke liye data load karein
            ViewBag.ParentCategoryId = new SelectList(
                _context.Categories.Where(c => c.ParentCategoryId == null),
                "CategoryId",
                "CategoryName"
            );
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category, IFormFile? imageFile)
        {
            // Auto-generate slug
            category.Slug = await GenerateUniqueSlug(category.CategoryName);
            ModelState.Remove("Slug");

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/categories");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    category.ImagePath = "/uploads/categories/" + uniqueFileName;
                }

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            // Dropdown list ko load karna zaroori hai taake Parent Category nazar aaye
            ViewBag.ParentCategoryId = new SelectList(
                _context.Categories.Where(c => c.ParentCategoryId == null && c.CategoryId != id),
                "CategoryId",
                "CategoryName",
                category.ParentCategoryId
            );

            return View(category);
        }
        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category, IFormFile? imageFile)
        {
            if (id != category.CategoryId) return NotFound();

            var existing = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == id);

            // Agar naam badla hai toh naya slug banayein warna purana rakhein
            if (existing != null && existing.CategoryName != category.CategoryName)
                category.Slug = await GenerateUniqueSlug(category.CategoryName, id);
            else
                category.Slug = existing?.Slug;

            ModelState.Remove("Slug");

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/categories");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    category.ImagePath = "/uploads/categories/" + uniqueFileName;
                }
                else
                {
                    category.ImagePath = existing?.ImagePath;
                }

                _context.Update(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // 🔥 YEH METHOD MISSING THA, ISAY YAHA PASTE KAREIN
        private async Task<string> GenerateUniqueSlug(string name, int? excludeId = null)
        {
            if (string.IsNullOrEmpty(name)) return "category";

            // Slug ko URL friendly banayein
            string slug = name.ToLower().Trim().Replace(" ", "-");
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

            // Check karein ke database mein pehle se toh nahi hai
            var existing = await _context.Categories
                .Where(c => c.Slug == slug && (!excludeId.HasValue || c.CategoryId != excludeId.Value))
                .FirstOrDefaultAsync();

            if (existing == null) return slug;
            
            // Agar duplicate hai toh number add karein (slug-1, slug-2...)
            int counter = 1;
            string newSlug;
            do
            {
                newSlug = $"{slug}-{counter}";
                counter++;
                existing = await _context.Categories
                    .Where(c => c.Slug == newSlug && (!excludeId.HasValue || c.CategoryId != excludeId.Value))
                    .FirstOrDefaultAsync();
            } while (existing != null);

            return newSlug;
        }
        // GET: Categories/Delete/5
        // Yeh page sirf poochne ke liye hai ke "Kya aap waqai delete karna chahte hain?"
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(m => m.CategoryId == id);

            if (category == null) return NotFound();

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category != null)
            {
                // Agar image hai toh folder se bhi delete karein (Best Practice)
                if (!string.IsNullOrEmpty(category.ImagePath))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, category.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
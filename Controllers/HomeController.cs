using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanaNoor_Brand.Data;
using SanaNoor_Brand.Models;
using System.Threading.Tasks;
using System.Security.Claims;
using SanaNoor_Brand.Models.ViewModel;
using SanaNoor_Brand.Models;
namespace SanaNoor_Brand.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HomeController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        private async Task<HashSet<int>> GetWishlistedProductIdsAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return new HashSet<int>();

            return (await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Select(w => w.ProductId)
                .ToListAsync()).ToHashSet();
        }

        // ============ 1. HOME PAGE (INDEX) ============
        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel
            {
                Categories = await _context.Categories.ToListAsync(),
                Products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Discount)
                    .Include(p => p.ProductColors)
                        .ThenInclude(pc => pc.Images)
                    .ToListAsync(),
                WishlistedProductIds = await GetWishlistedProductIdsAsync()
            };
            return View(model);
        }

        // ============ 2. ALL PRODUCTS PAGE (with optional category filter) ============
public async Task<IActionResult> Allproduct(string category, string color)
{
    var query = _context.Products
        .Include(p => p.Category)
        .Include(p => p.Discount)
        .Include(p => p.ProductColors)
            .ThenInclude(pc => pc.Images)
        .Where(p => p.IsActive)
        .AsQueryable();

    if (!string.IsNullOrEmpty(category))
    {
        var selectedCategory = await _context.Categories
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Slug == category);

        if (selectedCategory != null)
        {
            var categoryIds = new List<int> { selectedCategory.CategoryId };
            if (selectedCategory.SubCategories != null)
                categoryIds.AddRange(selectedCategory.SubCategories.Select(sc => sc.CategoryId));

            query = query.Where(p => categoryIds.Contains(p.CategoryId));
            ViewBag.SelectedCategoryName = selectedCategory.CategoryName;
        }
    }

    if (!string.IsNullOrEmpty(color))
    {
        query = query.Where(p => p.ProductColors.Any(pc => pc.ColorName == color));
        ViewBag.SelectedColor = color;
    }

    var products = await query.ToListAsync();

    // Sidebar ke liye: in products (category filter ke baad) mein maujood saari unique colors nikalo
    ViewBag.AvailableColors = products
        .SelectMany(p => p.ProductColors)
        .GroupBy(c => c.ColorName)
        .Select(g => new { ColorName = g.Key, ColorCode = g.First().ColorCode })
        .OrderBy(c => c.ColorName)
        .ToList();

    ViewBag.WishlistedProductIds = await GetWishlistedProductIdsAsync();
    ViewBag.AllCategories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
    ViewBag.CurrentCategorySlug = category;

    return View(products);
}        // ============ 3. PRODUCT DETAILS PAGE ============
        public async Task<IActionResult> detailproduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Discount)
                .Include(p => p.ProductColors)
                    .ThenInclude(pc => pc.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            var wishlistedIds = await GetWishlistedProductIdsAsync();
            ViewBag.IsWishlisted = wishlistedIds.Contains(product.ProductId);

            return View(product);
        }

        // ============ 4. STATIC PAGES ============
        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(ContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                var contact = new Contact
                {
                    Name = model.Name,
                    Email = model.Email,
                    Phone = model.Phone,
                    Subject = model.Subject,
                    Message = model.Message,
                    CreatedAt = DateTime.Now
                };

                _context.Contacts.Add(contact);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Thank you! Your message has been sent successfully.";
                return RedirectToAction("Contact");
            }

            return View(model);
        }
        public IActionResult Blog()
        {
            return View();
        }

        public IActionResult FAQs()
        {
            return View();
        }
    }
}
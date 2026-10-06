using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanaNoor_Brand.Data;
using SanaNoor_Brand.Models;
using System.Security.Claims;

namespace SanaNoor_Brand.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // ============ 1. VIEW MY WISHLIST ============
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "Please login to view your wishlist";
                return RedirectToAction("Login", "Account");
            }

            var wishlistItems = await _context.Wishlists
                .Include(w => w.Product)
                    .ThenInclude(p => p!.ProductColors)
                        .ThenInclude(pc => pc.Images)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            return View(wishlistItems);
        }

        // ============ 2. ADD TO WISHLIST ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int? productColorId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Please login to add items to your wishlist." });

            var productExists = await _context.Products.AnyAsync(p => p.ProductId == productId && p.IsActive);
            if (!productExists)
                return Json(new { success = false, message = "Product not found." });

            var alreadyExists = await _context.Wishlists
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);

            if (alreadyExists)
                return Json(new { success = false, message = "This item is already in your wishlist.", alreadyAdded = true });

            var wishlistItem = new Wishlist
            {
                UserId = userId,
                ProductId = productId,
                ProductColorId = productColorId,
                CreatedAt = DateTime.Now
            };

            _context.Wishlists.Add(wishlistItem);
            await _context.SaveChangesAsync();

            var count = await _context.Wishlists.CountAsync(w => w.UserId == userId);

            return Json(new { success = true, message = "Added to wishlist.", wishlistCount = count });
        }

        // ============ 3. REMOVE FROM WISHLIST ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int wishlistId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Please login first." });

            var item = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.WishlistId == wishlistId && w.UserId == userId);

            if (item == null)
                return Json(new { success = false, message = "Wishlist item not found." });

            _context.Wishlists.Remove(item);
            await _context.SaveChangesAsync();

            var count = await _context.Wishlists.CountAsync(w => w.UserId == userId);

            return Json(new { success = true, message = "Removed from wishlist.", wishlistCount = count });
        }

        // ============ 4. TOGGLE (heart icon click - add if not present, remove if present) ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int productId, int? productColorId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Please login to use wishlist.", requiresLogin = true });

            var existing = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            bool isNowInWishlist;

            if (existing != null)
            {
                _context.Wishlists.Remove(existing);
                isNowInWishlist = false;
            }
            else
            {
                var productExists = await _context.Products.AnyAsync(p => p.ProductId == productId && p.IsActive);
                if (!productExists)
                    return Json(new { success = false, message = "Product not found." });

                _context.Wishlists.Add(new Wishlist
                {
                    UserId = userId,
                    ProductId = productId,
                    ProductColorId = productColorId,
                    CreatedAt = DateTime.Now
                });
                isNowInWishlist = true;
            }

            await _context.SaveChangesAsync();
            var count = await _context.Wishlists.CountAsync(w => w.UserId == userId);

            return Json(new { success = true, isInWishlist = isNowInWishlist, wishlistCount = count });
        }

        // ============ 5. WISHLIST COUNT (for navbar badge, called on page load) ============
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Json(new { count = 0 });

            var count = await _context.Wishlists.CountAsync(w => w.UserId == userId);
            return Json(new { count });
        }
    }
}
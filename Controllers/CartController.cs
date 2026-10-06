using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanaNoor_Brand.Data;
using SanaNoor_Brand.Models;
using System.Security.Claims;

namespace SanaNoor_Brand.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============ 1. CART PAGE ============
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "Please login to view your cart";
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await _context.Carts
                .Include(c => c.Product)
                    .ThenInclude(p => p.Discount)
                .Include(c => c.ProductColor)
                    .ThenInclude(pc => pc.Images)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(cartItems);
        }

        // ============ 2. ADD TO CART ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int productColorId, string size, int quantity)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                if (isAjax)
                    return Json(new { success = false, message = "Please login first!", needLogin = true });

                TempData["error"] = "Please login first";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // ✅ Check product exists
                var product = await _context.Products
                    .Include(p => p.Discount)
                    .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // ✅ FIX: Get colors for this product
                var colors = await _context.ProductColors
                    .Where(pc => pc.ProductId == productId)
                    .ToListAsync();

                if (colors == null || !colors.Any())
                {
                    return Json(new { success = false, message = "No colors found for this product" });
                }

                // ✅ FIX: Agar productColorId 0 hai to pehla color le lo
                if (productColorId <= 0)
                {
                    productColorId = colors.First().ProductColorId;
                }

                // ✅ Check if color exists in this product's colors
                var productColor = colors.FirstOrDefault(pc => pc.ProductColorId == productColorId);

                // ✅ Agar color nahi mila to pehla color le lo
                if (productColor == null)
                {
                    productColor = colors.First();
                    productColorId = productColor.ProductColorId;
                }

                // ✅ Calculate price
                decimal unitPrice = product.HasDiscount ? product.GetDiscountedPrice() : product.Price;
                unitPrice += (productColor.ExtraPrice ?? 0);

                // ✅ Check if already in cart
                var existingItem = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == userId &&
                                             c.ProductId == productId &&
                                             c.ProductColorId == productColorId &&
                                             c.Size == size);

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    existingItem.UnitPrice = unitPrice;
                    existingItem.UpdatedAt = DateTime.Now;
                }
                else
                {
                    var cartItem = new Cart
                    {
                        UserId = userId,
                        ProductId = productId,
                        ProductColorId = productColorId,
                        Size = size ?? "Free",
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _context.Carts.Add(cartItem);
                }

                await _context.SaveChangesAsync();

                int cartCount = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .SumAsync(c => c.Quantity);

                if (isAjax)
                {
                    return Json(new { success = true, message = "Added to cart!", cartCount = cartCount });
                }

                TempData["success"] = "Product added to cart!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        // ============ 3. REMOVE ITEM ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int cartId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Login required" });

            var item = await _context.Carts.FirstOrDefaultAsync(c => c.CartId == cartId && c.UserId == userId);
            if (item != null)
            {
                _context.Carts.Remove(item);
                await _context.SaveChangesAsync();
            }

            int cartCount = await _context.Carts.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);
            return Json(new { success = true, message = "Item removed", cartCount = cartCount });
        }

        // ============ 4. UPDATE QUANTITY ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartId, int quantity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Login required" });

            if (quantity < 1)
                return await RemoveItem(cartId);

            if (quantity > 10)
                quantity = 10;

            var item = await _context.Carts.FirstOrDefaultAsync(c => c.CartId == cartId && c.UserId == userId);
            if (item != null)
            {
                item.Quantity = quantity;
                item.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            int cartCount = await _context.Carts.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);
            return Json(new
            {
                success = true,
                cartCount = cartCount,
                itemTotal = (item?.UnitPrice * item?.Quantity ?? 0).ToString("F0")
            });
        }

        // ============ 5. GET CART COUNT ============
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int count = 0;

            if (!string.IsNullOrEmpty(userId))
            {
                count = await _context.Carts.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);
            }

            return Json(new { count = count });
        }

        // ============ 6. CLEAR CART ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false });

            var items = await _context.Carts.Where(c => c.UserId == userId).ToListAsync();
            _context.Carts.RemoveRange(items);
            await _context.SaveChangesAsync();

            return Json(new { success = true, cartCount = 0 });
        }
    }
}
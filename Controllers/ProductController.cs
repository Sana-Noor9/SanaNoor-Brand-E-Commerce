using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using SanaNoor_Brand.Data;
using SanaNoor_Brand.Models;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace SanaNoor_Brand.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ============ 1. ALL PRODUCTS LIST ============
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductColors)
                    .ThenInclude(pc => pc.Images)
                .Include(p => p.Discount)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // ============ 2. PRODUCT DETAILS ============
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Discount)
                .Include(p => p.ProductColors)
                    .ThenInclude(pc => pc.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // ============ 3. VIEW PRODUCT (CUSTOMER/FRONTEND) ============
        public async Task<IActionResult> View(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Discount)
                .Include(p => p.ProductColors)
                    .ThenInclude(pc => pc.Images)
                .Include(p => p.Videos)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // ============ 4. CREATE PRODUCT PAGE (GET) ============
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(new Product());
        }

        // ============ 5. SAVE PRODUCT (POST) ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, string ColorVariantsJson)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // 🔥 FIX: Availability model field [Required] hai, isliye isko custom control karne ke liye validation remove karni hogi
            ModelState.Remove("Availability");

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                if (isAjax) return Json(new { success = false, message = "Validation failed: " + errors });

                TempData["error"] = "Please fill all required fields properly.";
                return View(product);
            }

            if (await _context.Products.AnyAsync(p => p.SKU == product.SKU))
            {
                if (isAjax) return Json(new { success = false, message = "SKU already exists" });
                ModelState.AddModelError("SKU", "SKU already exists");
                return View(product);
            }

            if (string.IsNullOrEmpty(ColorVariantsJson))
            {
                if (isAjax) return Json(new { success = false, message = "Please add at least one color variant" });
                ModelState.AddModelError("", "Please add at least one color variant");
                return View(product);
            }

            var colorVariants = JsonConvert.DeserializeObject<Dictionary<string, ColorVariantDto>>(ColorVariantsJson);

            if (colorVariants == null || colorVariants.Count == 0)
            {
                if (isAjax) return Json(new { success = false, message = "Please add at least one color variant" });
                ModelState.AddModelError("", "Please add at least one color variant");
                return View(product);
            }

            foreach (var variant in colorVariants)
            {
                if (variant.Value.Images == null || variant.Value.Images.Count == 0)
                {
                    if (isAjax) return Json(new { success = false, message = $"Please select at least 1 image for color '{variant.Key}'" });
                    ModelState.AddModelError("", $"Please select at least 1 image for color '{variant.Key}'");
                    return View(product);
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                product.ProductCode = string.IsNullOrEmpty(product.ProductCode)
                    ? $"SN{DateTime.Now:yyyyMMdd}{new Random().Next(1000, 9999)}"
                    : product.ProductCode;

                product.CreatedAt = DateTime.Now;
                product.UpdatedAt = DateTime.Now;
                product.IsActive = true;
                product.UpdateAvailability(); // Model method toggles 'in-stock' / 'out-of-stock'

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                foreach (var color in colorVariants)
                {
                    var productColor = new ProductColor
                    {
                        ProductId = product.ProductId,
                        ColorName = color.Key,
                        ColorCode = color.Value.ColorCode ?? "#000000",
                        Stock = color.Value.Stock,
                        ExtraPrice = color.Value.ExtraPrice,
                        Sizes = string.Join(", ", color.Value.Sizes),
                        IsDefault = false
                    };

                    _context.ProductColors.Add(productColor);
                    await _context.SaveChangesAsync();

                    int order = 0;
                    foreach (var base64Image in color.Value.Images)
                    {
                        if (string.IsNullOrEmpty(base64Image)) continue;

                        string imageName = SaveBase64Image(base64Image);
                        if (!string.IsNullOrEmpty(imageName))
                        {
                            var productImage = new ProductImage
                            {
                                ProductColorId = productColor.ProductColorId,
                                ImagePath = "/uploads/products/" + imageName,
                                IsPrimary = order == 0,
                                DisplayOrder = order++,
                                UploadedAt = DateTime.Now
                            };
                            _context.ProductImages.Add(productImage);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (isAjax) return Json(new { success = true, message = "Product added successfully!", redirectUrl = Url.Action(nameof(Index)) });

                TempData["success"] = "Product added successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                if (isAjax) return Json(new { success = false, message = "Server Error: " + ex.Message });

                TempData["error"] = "Error: " + ex.Message;
                return View(product);
            }
        }

        // ============ 6. EDIT PRODUCT PAGE (GET) ============
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductColors)
                    .ThenInclude(pc => pc.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            ViewBag.Categories = await _context.Categories.ToListAsync();

            var productColors = product.ProductColors.Select(c => new
            {
                ColorName = c.ColorName,
                Stock = c.Stock,
                ExtraPrice = c.ExtraPrice ?? 0,
                ColorCode = c.ColorCode ?? "#000000",
                Sizes = string.IsNullOrEmpty(c.Sizes)
                    ? new string[0]
                    : c.Sizes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray(),
                Images = c.Images.Select(i => i.ImagePath).ToList()
            }).ToList();

            ViewBag.ProductColors = productColors;
            return View(product);
        }

        // ============ 7. EDIT PRODUCT PAGE (POST) ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, string ColorVariantsJson)
        {
            if (id != product.ProductId) return Json(new { success = false, message = "Product ID mismatch" });

            var existing = await _context.Products
                .Include(p => p.ProductColors).ThenInclude(pc => pc.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (existing == null) return Json(new { success = false, message = "Product not found" });

            ModelState.Remove("Availability");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 🔥 ALL MODEL PROPERTIES SYNC (Jo purane code me miss horhi thin)
                existing.Name = product.Name;
                existing.Price = product.Price;
                existing.ComparePrice = product.ComparePrice;
                existing.SKU = product.SKU;
                existing.Barcode = product.Barcode;
                existing.CategoryId = product.CategoryId;
                existing.Brand = product.Brand;
                existing.Fabric = product.Fabric;
                existing.ProductType = product.ProductType;
                existing.Occasion = product.Occasion;
                existing.DesignCode = product.DesignCode;
                existing.Pieces = product.Pieces;
                existing.Color = product.Color;

                // Descriptions & Status
                existing.ShortDescription = product.ShortDescription;
                existing.FullDescription = product.FullDescription;
                existing.CareInstruction = product.CareInstruction;
                existing.Disclaimer = product.Disclaimer;
                existing.StockQuantity = product.StockQuantity;
                existing.IsActive = product.IsActive;

                // 3‑piece structural attributes
                existing.ShirtLength = product.ShirtLength;
                existing.TrouserLength = product.TrouserLength;
                existing.DupattaLength = product.DupattaLength;
                existing.Weight = product.Weight;
                existing.ShirtFabric = product.ShirtFabric;
                existing.TrouserFabric = product.TrouserFabric;
                existing.DupattaFabric = product.DupattaFabric;
                existing.ShirtQuantity = product.ShirtQuantity;
                existing.TrouserQuantity = product.TrouserQuantity;
                existing.DupattaQuantity = product.DupattaQuantity;

                existing.UpdatedAt = DateTime.Now;
                existing.UpdateAvailability();

                // Only touch colors/images if new variants data is sent explicitly
                if (!string.IsNullOrEmpty(ColorVariantsJson))
                {
                    var newColors = JsonConvert.DeserializeObject<Dictionary<string, ColorVariantDto>>(ColorVariantsJson);
                    if (newColors != null && newColors.Count > 0)
                    {
                        // Clean up tracking references safely
                        foreach (var color in existing.ProductColors.ToList())
                        {
                            foreach (var img in color.Images.ToList())
                            {
                                // Pehle physically delete tabhi hoga agar purani/naya path overlap na ho
                                if (!img.ImagePath.StartsWith("data:image"))
                                {
                                    DeletePhysicalImage(img.ImagePath);
                                }
                                _context.ProductImages.Remove(img);
                            }
                            _context.ProductColors.Remove(color);
                        }
                        await _context.SaveChangesAsync();

                        foreach (var item in newColors)
                        {
                            var pc = new ProductColor
                            {
                                ProductId = existing.ProductId,
                                ColorName = item.Key,
                                ColorCode = item.Value.ColorCode ?? "#000000",
                                Stock = item.Value.Stock,
                                ExtraPrice = item.Value.ExtraPrice,
                                Sizes = string.Join(", ", item.Value.Sizes),
                                IsDefault = false
                            };
                            _context.ProductColors.Add(pc);
                            await _context.SaveChangesAsync();

                            int order = 0;
                            foreach (var base64 in item.Value.Images)
                            {
                                if (string.IsNullOrEmpty(base64)) continue;

                                bool isExistingUrl = base64.StartsWith("/uploads/");
                                var fileName = isExistingUrl ? Path.GetFileName(base64) : SaveBase64Image(base64);

                                if (fileName != null)
                                {
                                    _context.ProductImages.Add(new ProductImage
                                    {
                                        ProductColorId = pc.ProductColorId,
                                        ImagePath = isExistingUrl ? base64 : "/uploads/products/" + fileName,
                                        IsPrimary = order == 0,
                                        DisplayOrder = order++,
                                        UploadedAt = DateTime.Now
                                    });
                                }
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Json(new { success = true, message = "Product updated successfully!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error updating product: " + ex.Message });
            }
        }

        // ============ 8. DELETE PRODUCT ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductColors)
                        .ThenInclude(pc => pc.Images)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    TempData["error"] = "Product not found";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var color in product.ProductColors)
                {
                    foreach (var image in color.Images)
                    {
                        DeletePhysicalImage(image.ImagePath);
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                TempData["success"] = "Product deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============ 9. DISCOUNT MANAGEMENT ============
        public async Task<IActionResult> Discounts()
        {
            var products = await _context.Products
                .Include(p => p.ProductColors)
                    .ThenInclude(pc => pc.Images)
                .Include(p => p.Discount)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // ============ 10. APPLY SINGLE DISCOUNT ============
        [HttpPost]
        public async Task<IActionResult> ApplyDiscount(int productId, string discountType, decimal discountValue)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Discount)
                    .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product == null) return Json(new { success = false, message = "Product not found" });

                DiscountType type = discountType.Equals("percentage", StringComparison.OrdinalIgnoreCase)
                    ? DiscountType.Percentage
                    : DiscountType.FixedAmount;

                if (type == DiscountType.Percentage && (discountValue > 100 || discountValue <= 0))
                    return Json(new { success = false, message = "Percentage must be between 1-100" });

                if (type == DiscountType.FixedAmount && discountValue >= product.Price)
                    return Json(new { success = false, message = "Discount must be less than product price" });

                if (product.Discount != null)
                {
                    product.Discount.Type = type;
                    product.Discount.Value = discountValue;
                    product.Discount.Name = $"{discountValue}{(type == DiscountType.Percentage ? "%" : "₹")} Off";
                    product.Discount.UpdatedAt = DateTime.Now;
                }
                else
                {
                    var discount = new Discount
                    {
                        ProductId = productId,
                        Name = $"{discountValue}{(type == DiscountType.Percentage ? "%" : "₹")} Off",
                        Type = type,
                        Value = discountValue,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddMonths(1),
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.Discounts.Add(discount);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Discount applied successfully!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error applying discount" });
            }
        }

        // ============ 11. APPLY BULK DISCOUNT ============
        [HttpPost]
        public async Task<IActionResult> ApplyBulkDiscount(List<int> productIds, string discountType, decimal discountValue)
        {
            if (productIds == null || !productIds.Any())
                return Json(new { success = false, message = "No products selected" });

            try
            {
                DiscountType type = discountType.Equals("percentage", StringComparison.OrdinalIgnoreCase)
                    ? DiscountType.Percentage
                    : DiscountType.FixedAmount;

                var products = await _context.Products
                    .Include(p => p.Discount)
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToListAsync();

                foreach (var product in products)
                {
                    if (type == DiscountType.Percentage && discountValue > 100) continue;
                    if (type == DiscountType.FixedAmount && discountValue >= product.Price) continue;

                    if (product.Discount != null)
                    {
                        product.Discount.Type = type;
                        product.Discount.Value = discountValue;
                        product.Discount.Name = $"{discountValue}{(type == DiscountType.Percentage ? "%" : "₹")} Off";
                        product.Discount.UpdatedAt = DateTime.Now;
                    }
                    else
                    {
                        var discount = new Discount
                        {
                            ProductId = product.ProductId,
                            Name = $"{discountValue}{(type == DiscountType.Percentage ? "%" : "₹")} Off",
                            Type = type,
                            Value = discountValue,
                            StartDate = DateTime.Now,
                            EndDate = DateTime.Now.AddMonths(1),
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };
                        _context.Discounts.Add(discount);
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Bulk discount applied successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ============ HELPER UTILITIES ============
        private string SaveBase64Image(string base64String)
        {
            try
            {
                if (base64String.Contains(","))
                {
                    base64String = base64String.Split(',')[1];
                }

                byte[] imageBytes = Convert.FromBase64String(base64String);
                string uniqueFileName = Guid.NewGuid().ToString() + ".jpg";

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "products");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                System.IO.File.WriteAllBytes(filePath, imageBytes);

                return uniqueFileName;
            }
            catch
            {
                return null;
            }
        }

        private void DeletePhysicalImage(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath)) return;
                string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch {/* Log failures quietly */}
        }
    }
}
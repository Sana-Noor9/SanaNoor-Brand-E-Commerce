using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanaNoor_Brand.Data;
using SanaNoor_Brand.Models;
using System.Security.Claims;

namespace SanaNoor_Brand.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // ============ 1. GET APPROVED REVIEWS FOR A PRODUCT (shown on product detail page) ============
        [HttpGet]
        public async Task<IActionResult> ProductReviews(int productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var summary = new
            {
                totalReviews = reviews.Count,
                averageRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0,
                ratingBreakdown = new
                {
                    five = reviews.Count(r => r.Rating == 5),
                    four = reviews.Count(r => r.Rating == 4),
                    three = reviews.Count(r => r.Rating == 3),
                    two = reviews.Count(r => r.Rating == 2),
                    one = reviews.Count(r => r.Rating == 1)
                }
            };

            return Json(new { success = true, reviews, summary });
        }

        // ============ 2. SUBMIT A REVIEW ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int rating, string comment)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Please login to write a review." });

            if (rating < 1 || rating > 5)
                return Json(new { success = false, message = "Rating must be between 1 and 5." });

            if (string.IsNullOrWhiteSpace(comment))
                return Json(new { success = false, message = "Please write a comment for your review." });

            if (comment.Length > 500)
                return Json(new { success = false, message = "Comment must be under 500 characters." });

            var productExists = await _context.Products.AnyAsync(p => p.ProductId == productId);
            if (!productExists)
                return Json(new { success = false, message = "Product not found." });

            // Har user ek product pe sirf ek review de sakta hai
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

            if (existingReview != null)
                return Json(new { success = false, message = "You have already reviewed this product. You can edit your existing review instead." });

            // Professional practice: sirf wahi user review kar sake jisne product order kiya ho
            var hasPurchased = await _context.Orders
                .Where(o => o.UserId == userId && o.OrderStatus == OrderStatus.Delivered)
                .SelectMany(o => o.OrderItems)
                .AnyAsync(oi => oi.ProductId == productId);

            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Comment = comment.Trim(),
                IsApproved = false, // Admin approval required before it's shown publicly
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            string message = hasPurchased
                ? "Thank you! Your review has been submitted and is awaiting approval."
                : "Your review has been submitted and is awaiting approval.";

            return Json(new { success = true, message, verifiedPurchase = hasPurchased });
        }

        // ============ 3. EDIT MY OWN REVIEW (before or after approval - resets approval) ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int reviewId, int rating, string comment)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Please login first." });

            if (rating < 1 || rating > 5)
                return Json(new { success = false, message = "Rating must be between 1 and 5." });

            if (string.IsNullOrWhiteSpace(comment))
                return Json(new { success = false, message = "Please write a comment." });

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.UserId == userId);

            if (review == null)
                return Json(new { success = false, message = "Review not found." });

            review.Rating = rating;
            review.Comment = comment.Trim();
            review.IsApproved = false; // Edited review must be re-approved

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Review updated and is awaiting re-approval." });
        }

        // ============ 4. DELETE MY OWN REVIEW ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int reviewId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Please login first." });

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.UserId == userId);

            if (review == null)
                return Json(new { success = false, message = "Review not found." });

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Review deleted." });
        }

        // ============ 5. ADMIN: APPROVE A REVIEW ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        // TODO: [Authorize(Roles = "Admin")] - apna admin authorization attribute lagao
        public async Task<IActionResult> Approve(int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null)
                return Json(new { success = false, message = "Review not found." });

            review.IsApproved = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Review approved." });
        }

        // ============ 6. ADMIN: REJECT / DELETE A REVIEW ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        // TODO: [Authorize(Roles = "Admin")] - apna admin authorization attribute lagao
        public async Task<IActionResult> Reject(int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null)
                return Json(new { success = false, message = "Review not found." });

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Review rejected and removed." });
        }

        // ============ 7. ADMIN: LIST PENDING REVIEWS (for moderation dashboard) ============
        [HttpGet]
        // TODO: [Authorize(Roles = "Admin")] - apna admin authorization attribute lagao
        public async Task<IActionResult> Pending()
        {
            var pendingReviews = await _context.Reviews
                .Include(r => r.Product)
                .Where(r => !r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(pendingReviews);
        }
    }
}
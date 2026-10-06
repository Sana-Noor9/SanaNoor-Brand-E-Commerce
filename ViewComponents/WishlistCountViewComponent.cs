using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanaNoor_Brand.Data;
using System.Security.Claims;

namespace SanaNoor_Brand.ViewComponents
{
    public class WishlistCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public WishlistCountViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

            int count = 0;
            if (!string.IsNullOrEmpty(userId))
            {
                count = await _context.Wishlists.CountAsync(w => w.UserId == userId);
            }

            return View(count);
        }
    }
}
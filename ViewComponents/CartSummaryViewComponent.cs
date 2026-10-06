using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanaNoor_Brand.Data;
using System.Security.Claims;

namespace SanaNoor_Brand.ViewComponents
{
    public class CartSummaryViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CartSummaryViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

            var viewModel = new CartSummaryVm();

            if (!string.IsNullOrEmpty(userId))
            {
                var cartItems = await _context.Carts
                    .Include(c => c.Product)
                        .ThenInclude(p => p!.ProductColors)
                            .ThenInclude(pc => pc.Images)
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                viewModel.Items = cartItems;
                viewModel.TotalQuantity = cartItems.Sum(c => c.Quantity);
                viewModel.GrandTotal = cartItems.Sum(c => c.TotalPrice);
            }

            return View(viewModel);
        }
    }

    public class CartSummaryVm
    {
        public List<SanaNoor_Brand.Models.Cart> Items { get; set; } = new();
        public int TotalQuantity { get; set; } = 0;
        public decimal GrandTotal { get; set; } = 0;
    }
}
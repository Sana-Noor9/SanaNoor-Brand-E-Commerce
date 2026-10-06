using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Safepay;
using SanaNoor_Brand.Data;
using SanaNoor_Brand.Models;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SanaNoor_Brand.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        private const string SafepayBaseUrl = "https://sandbox.api.getsafepay.com";
        private const string SafepayPublicKey = "sec_31ab7b87-e4a7-4ee3-b7b9-4041691b4d41";
        private const string SafepaySecretKey = "7e29809139232730bff63892b05ecbfc363140fe8b1abb399dd48a5392108c8b";

        public CheckoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============ 1. CHECKOUT PAGE (GET) ============
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "Please login to checkout";
                return RedirectToAction("Login", "Account");
            }

            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            ViewBag.UserEmail = userEmail;

            var cartItems = await _context.Carts
                .Include(c => c.Product)
                .Include(c => c.ProductColor)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["error"] = "Your cart is empty";
                return RedirectToAction("Index", "Cart");
            }

            return View(cartItems);
        }

        // ============ 2. INITIATE SAFEPAY CHECKOUT (Card / Wallet) ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiateSafepayCheckout(
            string customerName, string customerEmail, string customerPhone,
            string shippingAddress, string city, string postalCode)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Session expired. Please login again." });

            var cartItems = await _context.Carts.Where(c => c.UserId == userId).ToListAsync();
            if (!cartItems.Any())
                return Json(new { success = false, message = "Cart is empty" });

            decimal subTotal = cartItems.Sum(c => c.UnitPrice * c.Quantity);
            decimal shipping = subTotal >= 3000 ? 0 : 200;
            decimal grandTotal = subTotal + shipping;
            int amountInPaisa = (int)(grandTotal * 100);

            TempData["customerName"] = customerName;
            TempData["customerEmail"] = customerEmail;
            TempData["customerPhone"] = customerPhone;
            TempData["shippingAddress"] = shippingAddress;
            TempData["city"] = city;
            TempData["postalCode"] = postalCode;

            try
            {
                using var http = new HttpClient();

                var trackerBody = new
                {
                    merchant_api_key = SafepayPublicKey,
                    intent = "CYBERSOURCE",
                    mode = "payment",
                    currency = "PKR",
                    amount = amountInPaisa
                };
                var trackerContent = new StringContent(JsonSerializer.Serialize(trackerBody), Encoding.UTF8, "application/json");
                var trackerResponse = await http.PostAsync($"{SafepayBaseUrl}/order/payments/v3/", trackerContent);
                var trackerBodyText = await trackerResponse.Content.ReadAsStringAsync();

                if (!trackerResponse.IsSuccessStatusCode)
                    return Json(new { success = false, message = "Tracker creation failed: " + trackerBodyText });

                var trackerJson = JsonDocument.Parse(trackerBodyText);
                string trackerToken = trackerJson.RootElement.GetProperty("data").GetProperty("tracker").GetProperty("token").GetString();

                SafepayConfiguration.ApiKey = SafepayPublicKey;
                SafepayConfiguration.Environment = "sandbox";
                SafepayClient.InitializeApiClient(false);

                string redirectUrl = Url.Action("Success", "Checkout", null, Request.Scheme);
                string cancelUrl = Url.Action("Index", "Checkout", null, Request.Scheme);
                string orderId = "SN" + DateTime.UtcNow.Ticks;

                var checkoutUrl = Checkout.CreateSession(trackerToken, orderId, cancelUrl, redirectUrl, usingWebhookVerification: false);

                return Json(new { success = true, checkoutUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ============ 3. PLACE COD ORDER (Cash on Delivery) ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceCodOrder(
            string customerName, string customerEmail, string customerPhone,
            string shippingAddress, string city, string postalCode)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Session expired. Please login again." });

            var cartItems = await _context.Carts
                .Include(c => c.Product)
                .Include(c => c.ProductColor)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
                return Json(new { success = false, message = "Cart is empty" });

            decimal subTotal = cartItems.Sum(c => c.UnitPrice * c.Quantity);
            decimal shipping = subTotal >= 3000 ? 0 : 200;
            decimal grandTotal = subTotal + shipping;

            var order = new Models.Order
            {
                OrderNumber = "SN" + DateTime.UtcNow.Ticks,
                UserId = userId,
                CustomerName = customerName,
                CustomerEmail = customerEmail,
                CustomerPhone = customerPhone,
                ShippingAddress = shippingAddress,
                City = city,
                PostalCode = postalCode,
                SubTotal = subTotal,
                ShippingCharges = shipping,
                GrandTotal = grandTotal,
                PaymentMethod = "Cash on Delivery",
                PaymentStatus = PaymentStatus.Pending,
                TransactionId = "COD-" + DateTime.UtcNow.Ticks,
                OrderStatus = OrderStatus.Processing,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var item in cartItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductColorId = item.ProductColorId,
                    Size = item.Size,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = item.UnitPrice * item.Quantity,
                    ProductName = item.Product?.Name ?? "Item",
                    ColorName = item.ProductColor?.ColorName ?? "Standard"
                });
            }

            _context.Orders.Add(order);
            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return Json(new { success = true, redirectUrl = Url.Action("Success", "Checkout", new { tracker = order.TransactionId }) });
        }

        // ============ 4. SAVE ORDER (Safepay Success page pe aane ke baad) ============
        private async Task<Models.Order> SaveOrderToDatabase(string transactionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = await _context.Carts
                .Include(c => c.Product)
                .Include(c => c.ProductColor)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any()) return null;

            decimal subTotal = cartItems.Sum(c => c.UnitPrice * c.Quantity);
            decimal shipping = subTotal >= 3000 ? 0 : 200;
            decimal grandTotal = subTotal + shipping;

            var order = new Models.Order
            {
                OrderNumber = "SN" + DateTime.UtcNow.Ticks,
                UserId = userId,
                CustomerName = TempData["customerName"]?.ToString() ?? "",
                CustomerEmail = TempData["customerEmail"]?.ToString() ?? "",
                CustomerPhone = TempData["customerPhone"]?.ToString() ?? "",
                ShippingAddress = TempData["shippingAddress"]?.ToString() ?? "",
                City = TempData["city"]?.ToString() ?? "",
                PostalCode = TempData["postalCode"]?.ToString() ?? "",
                SubTotal = subTotal,
                ShippingCharges = shipping,
                GrandTotal = grandTotal,
                PaymentMethod = "Safepay",
                PaymentStatus = PaymentStatus.Paid,
                TransactionId = transactionId,
                OrderStatus = OrderStatus.Processing,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var item in cartItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductColorId = item.ProductColorId,
                    Size = item.Size,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = item.UnitPrice * item.Quantity,
                    ProductName = item.Product?.Name ?? "Item",
                    ColorName = item.ProductColor?.ColorName ?? "Standard"
                });
            }

            _context.Orders.Add(order);
            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
            return order;
        }

        // ============ 5. SUCCESS PAGE (Safepay aur COD dono handle karta hai) ============
        public async Task<IActionResult> Success(string tracker)
        {
            if (string.IsNullOrEmpty(tracker))
                return RedirectToAction("Index", "Home");

            // Pehle check karo ke ye order pehle se ban chuka hai (COD ka case)
            var existingOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.TransactionId == tracker);

            if (existingOrder != null)
                return View(existingOrder);

            // Nahi mila toh Safepay flow hai - naya order banao
            var order = await SaveOrderToDatabase(tracker);
            if (order == null) return RedirectToAction("Index", "Home");

            return View(order);
        }
    }
}
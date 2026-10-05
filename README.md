# SanaNoor Brand — E-Commerce Platform

**SanaNoor** is a full-featured women's fashion e-commerce platform built with **ASP.NET Core MVC**, offering unstitched and stitched clothing collections (including Chikankari, embroidered, and printed fabric suits) for women, men, and boys — with a complete product-to-checkout shopping experience.

## ✨ Features

### 🛒 Product Catalog
- Hierarchical **categories** with parent/sub-category support, active/inactive status, and custom display ordering
- Dynamic **category-based filtering** on the shop page (e.g. browsing "Men's Wear" shows only men's products)
- Rich **product details**: SKU, barcode, brand, fabric, occasion, product type, and 3-piece suit specifics (shirt/trouser/dupatta length, fabric, and quantity — all shown on the product page)
- **Color variants per product** — each color has its own stock, extra price, available sizes, images, and videos
- **Interactive color swatches** on the product detail page — clicking a color updates the main image and selected variant
- **Color filtering** on the shop page, scoped to the currently browsed category
- Size-level stock tracking (`ProductSize`) with per-size extra pricing
- Multiple **product images** per color (with primary image and display ordering) and **product videos**
- Auto-calculated availability (`in-stock` / `out-of-stock`) based on stock levels
- Price range display (`MinPrice`–`MaxPrice`) based on color/size variant pricing

### 💰 Discounts
- Percentage or fixed-amount discounts, scoped per product
- Time-bound (start/end date) and active/inactive toggle
- Auto-calculated discounted price, saved amount, and discount percentage shown throughout the storefront

### 🛍️ Shopping Cart
- Per-user cart with product, color, and size selection
- Real-time subtotal calculation per item and across the cart
- Dynamic cart summary in the site header (live item count and total, no page reload needed)

### ❤️ Wishlist
- One-click add/remove (toggle) from product cards and the product detail page
- Dedicated wishlist page listing all saved items
- Live wishlist count badge in the site header

### ⭐ Reviews
- Star ratings (1–5) with written comments
- One review per user per product, with edit/delete support
- Verified-purchase detection (checks delivered orders)
- Admin approval workflow before a review is publicly shown

### 📬 Contact Form
- Validated contact submissions (name, email, phone, subject, message) stored for follow-up

### 💳 Checkout & Payments
- Billing details form (name, email, phone, shipping address, city, postal code)
- Free shipping on orders above Rs. 3,000 (Rs. 200 flat rate otherwise)
- **Multiple payment methods:**
  - 💳 Credit / Debit Card — via **Safepay** (Pakistan payment gateway)
  - 📱 Easypaisa / JazzCash mobile wallets — via **Safepay**
  - 💵 Cash on Delivery (COD)
- Order and payment status tracking (`OrderStatus`: Pending/Processing/Shipped/Delivered/Cancelled; `PaymentStatus`: Pending/Paid/Failed/Refunded)
- Coupon/discount code support on orders
- Order tracking number field for shipment tracking
- Snapshot fields on order items (product name, color name) so historical orders stay accurate even if a product is later edited or removed

### 📦 Order Management
- Full order history per user with itemized breakdown (product, color, size, quantity, unit price, total)
- Transaction ID tracking (Safepay reference or COD identifier)
- Order confirmation page showing order number, payment method, status, shipping address, and total

### 🔐 Authentication
- Email/password registration and login via ASP.NET Core Identity
- **Google Sign-In** — customers can register and log in with their Google account

### 🌐 Multi-Language & Localization
- **Multi-Language Support:** Full storefront localization supporting English (en), French (fr), and Spanish (es).

## 🏗️ Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core MVC (C#) |
| Database | Entity Framework Core (Code-First, SQL Server) |
| Authentication | ASP.NET Core Identity + Google OAuth |
| Frontend | Razor Views (.cshtml), Bootstrap, custom CSS |
| Localization | ASP.NET Core Localization (en, fr, es) |
| Payment Gateway | Safepay (Pakistan) |

## 🗂️ Data Model Overview

| Model | Purpose |
|---|---|
| `Category` | Hierarchical product categories with sub-category support |
| `Product` | Core product info, pricing, stock, and computed display properties |
| `ProductColor` | Color variant of a product (stock, extra price, sizes, images, videos) |
| `ProductSize` | Size-level stock and pricing within a color variant |
| `ProductImage` / `ProductVideo` | Media attached to a product/color |
| `Discount` | Product-level promotional pricing |
| `Cart` | User's active cart items |
| `Wishlist` | User's saved products |
| `Review` | Product ratings and comments (with approval flow) |
| `Order` / `OrderItem` | Finalized orders with itemized snapshot data |
| `Contact` | Customer contact form submissions |

## 💳 Payment Integration Details

The checkout integrates **Safepay**, chosen because PayPal does not support receiving payments in Pakistan.

**Flow:**
1. Customer fills billing details and selects a payment method on the checkout page
2. **Card/Wallet:** A payment Tracker is created via Safepay's API and the customer is redirected to Safepay's hosted checkout page to complete payment
3. **Cash on Delivery:** Order is saved directly with `PaymentStatus.Pending`, no gateway involved
4. On completion, the customer lands on an order confirmation page with full order details

## 📂 Key Files

- `Controllers/HomeController.cs` — Storefront, product listing (with category/color filters), product details
- `Controllers/CheckoutController.cs` — Checkout page, Safepay tracker creation, COD order placement, order confirmation
- `Controllers/WishlistController.cs` — Wishlist add/remove/toggle
- `Controllers/ReviewController.cs` — Review submission, editing, and admin moderation
- `Controllers/ProductController.cs` — Admin-side product and discount management
- `ViewComponents/WishlistCountViewComponent.cs`, `ViewComponents/CartSummaryViewComponent.cs` — Live header badges
- `Models/` — Full data model (Product, Category, Cart, Order, Discount, Review, Wishlist, Contact, etc.)

## 🚀 Getting Started

1. Clone the repository
2. Update the database connection string in `appsettings.json`
3. Apply EF Core migrations: `dotnet ef database update`
4. Add your Safepay API keys and Google OAuth credentials to configuration (see `appsettings.json` / user secrets — do not commit real keys)
5. Run the project: `dotnet run`

## 📌 Status

Project complete. Full shopping experience — product browsing with category/color filtering, cart, wishlist, reviews, Cash on Delivery and Safepay card/wallet checkout, Google/email authentication, and multi-language support (English, French, Spanish) — is implemented and working.

---
*Built for SanaNoor — bringing quality Pakistani fashion online.*

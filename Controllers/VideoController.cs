using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanaNoor_Brand.Data;
using SanaNoor_Brand.Models;

namespace SanaNoor_Brand.Controllers
{
    public class VideoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public VideoController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Sab videos ki list dekhne ke liye
        public async Task<IActionResult> Index()
        {
            var videos = await _context.ProductVideos.Include(v => v.Product).ToListAsync();
            return View(videos);
        }

        // Video upload karne ka logic
        [HttpPost]
        public async Task<IActionResult> Upload(int productId, IFormFile videoFile, string? videoTitle)
        {
            if (videoFile != null)
            {
                string folder = Path.Combine(_env.WebRootPath, "uploads/videos");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(videoFile.FileName);
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await videoFile.CopyToAsync(stream);
                }

                var video = new ProductVideo
                {
                    ProductId = productId,
                    VideoPath = "/uploads/videos/" + fileName,
                    VideoType = "Upload", // Ab model mein hai, error nahi ayega
                    Title = videoTitle,    // Ab model mein 'Title' hai, error nahi ayega
                    IsActive = true
                };

                _context.ProductVideos.Add(video);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
        // Video delete karne ke liye
        public async Task<IActionResult> Delete(int id)
        {
            var video = await _context.ProductVideos.FindAsync(id);
            if (video != null)
            {
                _context.ProductVideos.Remove(video);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}
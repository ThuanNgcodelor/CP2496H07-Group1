using Microsoft.AspNetCore.Mvc;
using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CP2496H07Group1.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SliderController : Controller
    {
        private readonly AppDataContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<SliderController> _logger;

        public SliderController(AppDataContext appDataContext, IWebHostEnvironment webHostEnvironment, ILogger<SliderController> logger)
        {
            _context = appDataContext;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: Admin/Slider
        public async Task<IActionResult> Index(string keyword = "", int page = 1)
        {
            int pageSize = 5; // Số mục mỗi trang
            var sliders = _context.Sliders.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                sliders = sliders.Where(s => s.Name.Contains(keyword) || s.Detail.Contains(keyword));
            }

            var totalItems = await sliders.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            sliders = sliders.Skip((page - 1) * pageSize).Take(pageSize);

            ViewBag.Keyword = keyword;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(await sliders.ToListAsync());
        }

        // GET: Admin/Slider/Add
        public IActionResult Add()
        {
            return View();
        }

        // POST: Admin/Slider/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Slider slider, IFormFile? ImageFile)
        {
            // Kiểm tra ImageFile trước
            if (ImageFile == null || ImageFile.Length == 0)
            {
                ModelState.AddModelError("ImageFile", "Vui lòng chọn một ảnh hợp lệ.");
            }
            else
            {
                // Kiểm tra loại và kích thước tệp
                if (!ImageFile.ContentType.StartsWith("image/"))
                {
                    ModelState.AddModelError("ImageFile", "Vui lòng chọn tệp định dạng ảnh (jpg, png, ...).");
                }
                if (ImageFile.Length > 5 * 1024 * 1024) // Giới hạn 5MB
                {
                    ModelState.AddModelError("ImageFile", "Ảnh không được lớn hơn 5MB.");
                }
            }

            // Xóa lỗi liên quan đến slider.Image vì chúng ta sẽ gán giá trị sau
            ModelState.Remove("Image");

            if (ModelState.IsValid)
            {
                // Tạo tên tệp duy nhất
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile!.FileName);
                var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "Admin", "img");
                var filePath = Path.Combine(uploadPath, fileName);

                // Đảm bảo thư mục tồn tại
                Directory.CreateDirectory(uploadPath);

                // Lưu tệp
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile!.CopyToAsync(stream);
                }

                // Gán đường dẫn ảnh cho slider.Image
                slider.Image = $"/Admin/img/{fileName}";
                slider.Date = DateTime.Now;

                _context.Add(slider);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Thêm slider thành công: {SliderName}", slider.Name);
                return RedirectToAction(nameof(Index));
            }

            // Debug: In lỗi ModelState
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("ModelState Error: {Error}", error.ErrorMessage);
            }

            return View(slider);
        }

        // GET: Admin/Slider/Update/5
        public async Task<IActionResult> Update(long id)
        {
            var slider = await _context.Sliders.FindAsync(id);
            if (slider == null)
            {
                _logger.LogWarning("Không tìm thấy slider với ID: {Id}", id);
                return NotFound();
            }
            return View(slider);
        }

        // POST: Admin/Slider/Update/5
        // POST: Admin/Slider/Update/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(long id, Slider slider, IFormFile? ImageFile)
        {
            if (id != slider.Id)
            {
                _logger.LogWarning("ID không khớp: {Id} != {SliderId}", id, slider.Id);
                return BadRequest();
            }

            var existingSlider = await _context.Sliders.FindAsync(id);
            if (existingSlider == null)
            {
                _logger.LogWarning("Không tìm thấy slider với ID: {Id}", id);
                return NotFound();
            }

            // Xóa lỗi Image khỏi ModelState vì nó sẽ được xử lý riêng
            ModelState.Remove("Image");

            // Kiểm tra ImageFile nếu được cung cấp
            if (ImageFile != null && ImageFile.Length > 0)
            {
                if (!ImageFile.ContentType.StartsWith("image/"))
                {
                    ModelState.AddModelError("ImageFile", "Vui lòng chọn tệp định dạng ảnh (jpg, png, ...).");
                }
                if (ImageFile.Length > 5 * 1024 * 1024) // Giới hạn 5MB
                {
                    ModelState.AddModelError("ImageFile", "Ảnh không được lớn hơn 5MB.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Cập nhật các trường
                    existingSlider.Name = slider.Name;
                    existingSlider.Detail = slider.Detail;
                    existingSlider.Status = slider.Status;

                    // Xử lý ảnh mới nếu có
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        // Xóa ảnh cũ nếu tồn tại
                        if (!string.IsNullOrEmpty(existingSlider.Image))
                        {
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingSlider.Image.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                                _logger.LogInformation("Đã xóa ảnh cũ: {ImagePath}", oldImagePath);
                            }
                        }

                        // Lưu ảnh mới
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                        var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "Admin", "img");
                        var filePath = Path.Combine(uploadPath, fileName);

                        Directory.CreateDirectory(uploadPath);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }

                        existingSlider.Image = $"/Admin/img/{fileName}";
                        _logger.LogInformation("Đã lưu ảnh mới: {ImagePath}", existingSlider.Image);
                    }

                    _context.Update(existingSlider);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cập nhật slider thành công: {SliderName}", slider.Name);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi cập nhật slider: {SliderName}", slider.Name);
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật slider. Vui lòng thử lại.");
                }
            }

            // Debug: In lỗi ModelState
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("ModelState Error: {Error}", error.ErrorMessage);
            }

            return View(slider);
        }
        // POST: Admin/Slider/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var slider = await _context.Sliders.FindAsync(id);
                if (slider != null)
                {
                    // Xóa ảnh nếu tồn tại
                    if (!string.IsNullOrEmpty(slider.Image))
                    {
                        var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, slider.Image.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                            _logger.LogInformation("Đã xóa ảnh: {ImagePath}", imagePath);
                        }
                    }

                    _context.Sliders.Remove(slider);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Đã xóa slider với ID: {Id}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa slider với ID: {Id}", id);
            }
            return RedirectToAction(nameof(Index));
        }

        private bool SliderExists(long id)
        {
            return _context.Sliders.Any(e => e.Id == id);
        }
    }
}
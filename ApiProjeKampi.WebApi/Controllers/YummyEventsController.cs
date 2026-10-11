
using ApiProjeKampi.WebApi.Context;
using ApiProjeKampi.WebApi.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApiProjeKampi.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class YummyEventsController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly IWebHostEnvironment _environment;

        public YummyEventsController(
            ApiContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult YummyEventList()
        {
            return Ok(_context.YummyEvents.ToList());
        }

        [HttpPost]
        public IActionResult CreateYummyEvent(
            [FromBody] YummyEvent yummyEvent)
        {
            _context.YummyEvents.Add(yummyEvent);
            _context.SaveChanges();

            return Ok("Etkinlik ekleme işlemi başarılı");
        }

        [HttpDelete]
        public IActionResult DeleteYummyEvent(int id)
        {
            var value = _context.YummyEvents.Find(id);

            if (value == null)
                return NotFound();

            _context.YummyEvents.Remove(value);
            _context.SaveChanges();

            return Ok("Etkinlik silme işlemi başarılı");
        }

        [HttpGet("GetYummyEvent")]
        public IActionResult GetYummyEvent(int id)
        {
            var value = _context.YummyEvents.Find(id);

            if (value == null)
                return NotFound();

            return Ok(value);
        }

        [HttpPut]
        public IActionResult UpdateYummyEvent(
            [FromBody] YummyEvent yummyEvent)
        {
            var value = _context.YummyEvents.Find(
                yummyEvent.YummyEventId);

            if (value == null)
                return NotFound();

            value.Title = yummyEvent.Title;
            value.Description = yummyEvent.Description;
            value.Price = yummyEvent.Price;
            value.ImageUrl = yummyEvent.ImageUrl;

            _context.SaveChanges();

            return Ok("Etkinlik güncelleme işlemi başarılı");
        }

        // Bilgisayardan veya telefondan fotoğraf yükleme
        [HttpPost("upload-image")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(
            [FromForm] IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return BadRequest("Fotoğraf seçiniz.");

            if (imageFile.Length > 5 * 1024 * 1024)
                return BadRequest("Fotoğraf en fazla 5 MB olabilir.");

            var extensions = new[] {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            var extension = Path.GetExtension(
                imageFile.FileName).ToLowerInvariant();

            if (!extensions.Contains(extension))
                return BadRequest("Geçersiz dosya uzantısı.");

            var signatures = new Dictionary<string, byte[][]>
            {
                [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
                [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
                [".png"] = new[] {
                    new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
                },
                [".webp"] = new[] {
                    System.Text.Encoding.ASCII.GetBytes("RIFF")
                }
            };

            byte[] header = new byte[12];

            using (var stream = imageFile.OpenReadStream())
            {
                int bytesRead = await stream.ReadAsync(
                    header, 0, header.Length);

                if (bytesRead < 12 ||
                    !signatures[extension].Any(s =>
                        header.Take(s.Length).SequenceEqual(s)) ||
                    (extension == ".webp" &&
                     System.Text.Encoding.ASCII.GetString(
                         header, 8, 4) != "WEBP"))
                {
                    return BadRequest("Geçersiz fotoğraf dosyası.");
                }
            }

            var webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath, "wwwroot");

            var folder = Path.Combine(
                webRoot, "uploads", "yummyevents");

            Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid().ToString("N")
                + extension;

            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(
                filePath, FileMode.CreateNew))
            {
                await imageFile.CopyToAsync(stream);
            }

            var imageUrl = "/uploads/yummyevents/" + fileName;

            return Ok(new { imageUrl });
        }
    }
}

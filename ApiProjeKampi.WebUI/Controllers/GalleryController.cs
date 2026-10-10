
using ApiProjeKampi.WebUI.Dtos.ImageDtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace ApiProjeKampi.WebUI.Controllers
{
    public class GalleryController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;

        private const string ApiUrl =
            "https://localhost:7020/api/Images";

        public GalleryController(
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment environment)
        {
            _httpClientFactory = httpClientFactory;
            _environment = environment;
        }

        // GALERİ LİSTESİ
        [HttpGet]
        public async Task<IActionResult> ImageList()
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(ApiUrl);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                var values = JsonConvert.DeserializeObject<
                    List<ResultImageDto>>(json);

                return View(values ?? new List<ResultImageDto>());
            }

            return View(new List<ResultImageDto>());
        }

        // GALERİ DÜZENLEME LİSTESİ
        [HttpGet]
        public async Task<IActionResult> ImageListWithEdit()
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(ApiUrl);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                var values = JsonConvert.DeserializeObject<
                    List<ResultImageDto>>(json);

                return View(values ?? new List<ResultImageDto>());
            }

            TempData["GalleryError"] =
                "Galeri listesi yüklenemedi.";

            return View(new List<ResultImageDto>());
        }

        // FOTOĞRAF EKLE - GET
        [HttpGet]
        public IActionResult CreateImage()
        {
            return View();
        }

        // FOTOĞRAF EKLE - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> CreateImage(
            IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Lütfen bir fotoğraf seçiniz.");

                return View();
            }

            if (imageFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(
                    "",
                    "Fotoğraf en fazla 5 MB olabilir.");

                return View();
            }

            var extension = Path.GetExtension(
                imageFile.FileName).ToLowerInvariant();

            var allowedExtensions = new[]
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    "",
                    "Yalnızca JPG, PNG ve WEBP fotoğrafları yüklenebilir.");

                return View();
            }

            // Dosyanın başlangıç imzasını kontrol et.
            var signature = new byte[12];

            using (var inputStream = imageFile.OpenReadStream())
            {
                var read = await inputStream.ReadAsync(
                    signature, 0, signature.Length);

                bool jpeg = read >= 3 &&
                    signature[0] == 0xFF &&
                    signature[1] == 0xD8 &&
                    signature[2] == 0xFF;

                bool png = read >= 8 &&
                    signature[0] == 0x89 &&
                    signature[1] == 0x50 &&
                    signature[2] == 0x4E &&
                    signature[3] == 0x47 &&
                    signature[4] == 0x0D &&
                    signature[5] == 0x0A &&
                    signature[6] == 0x1A &&
                    signature[7] == 0x0A;

                bool webp = read >= 12 &&
                    Encoding.ASCII.GetString(signature, 0, 4) == "RIFF" &&
                    Encoding.ASCII.GetString(signature, 8, 4) == "WEBP";

                bool isValid =
                    (jpeg && (extension == ".jpg" || extension == ".jpeg"))
                    || (png && extension == ".png")
                    || (webp && extension == ".webp");

                if (!isValid)
                {
                    ModelState.AddModelError(
                        "",
                        "Seçilen dosya geçerli bir görsel değil.");

                    return View();
                }
            }

            var webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            var folderPath = Path.Combine(
                webRoot,
                "uploads",
                "gallery");

            Directory.CreateDirectory(folderPath);

            var fileName =
                Guid.NewGuid().ToString("N") + extension;

            var filePath =
                Path.Combine(folderPath, fileName);

            try
            {
                // 1. Fotoğrafı wwwroot klasörüne kaydet.
                using (var stream = new FileStream(
                    filePath, FileMode.CreateNew))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // 2. Fotoğrafın web adresini oluştur.
                var imageUrl =
                    "/uploads/gallery/" + fileName;

                // 3. WebApi'ye gönderilecek DTO.
                var dto = new CreateImageDto
                {
                    ImageUrl = imageUrl
                };

                var client = _httpClientFactory.CreateClient();

                var json = JsonConvert.SerializeObject(dto);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                // 4. Veritabanına kaydet.
                using var response = await client.PostAsync(
                    ApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["GallerySuccess"] =
                        "Fotoğraf başarıyla galeriye eklendi.";

                    return RedirectToAction(
                        nameof(ImageListWithEdit));
                }

                var error =
                    await response.Content.ReadAsStringAsync();

                Console.Error.WriteLine(
                    "Galeri API hatası: " + error);

                ModelState.AddModelError(
                    "",
                    "Fotoğraf veritabanına kaydedilemedi. " +
                    "API hata kodu: " +
                    (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    "Galeri yükleme hatası: " + ex);

                ModelState.AddModelError(
                    "",
                    "Fotoğraf yüklenirken hata oluştu.");
            }

            // Kayıt başarısızsa yüklenen dosyayı temizle.
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            return View();
        }

        // FOTOĞRAF SİL
        public async Task<IActionResult> DeleteImage(int id)
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.DeleteAsync(
                ApiUrl + "?id=" + id);

            if (response.IsSuccessStatusCode)
            {
                TempData["GallerySuccess"] =
                    "Fotoğraf kaydı başarıyla silindi.";
            }
            else
            {
                TempData["GalleryError"] =
                    "Fotoğraf silinemedi.";
            }

            return RedirectToAction(nameof(ImageListWithEdit));
        }

        // FOTOĞRAF GÜNCELLE - GET
        [HttpGet]
        public async Task<IActionResult> UpdateImage(int id)
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync(
                ApiUrl + "/GetImage?id=" + id);

            if (!response.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var json = await response.Content.ReadAsStringAsync();

            var value = JsonConvert.DeserializeObject<
                GetImageByIdDto>(json);

            return View(value);
        }

        // FOTOĞRAF GÜNCELLE - POST
        [HttpPost]
        public async Task<IActionResult> UpdateImage(
            UpdateImageDto updateImageDto)
        {
            var client = _httpClientFactory.CreateClient();

            var json =
                JsonConvert.SerializeObject(updateImageDto);

            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync(
                ApiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                TempData["GallerySuccess"] =
                    "Galeri kaydı güncellendi.";

                return RedirectToAction(nameof(ImageListWithEdit));
            }

            ModelState.AddModelError(
                "",
                "Galeri kaydı güncellenemedi.");

            return View(updateImageDto);
        }
    }
}

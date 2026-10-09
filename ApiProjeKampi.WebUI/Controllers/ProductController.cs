
using ApiProjeKampi.WebUI.Dtos.CategoryDtos;
using ApiProjeKampi.WebUI.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Text;

namespace ApiProjeKampi.WebUI.Controllers
{
    public class ProductController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;

        private const string ApiUrl =
            "https://localhost:7020/api/";

        public ProductController(
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment environment)
        {
            _httpClientFactory = httpClientFactory;
            _environment = environment;
        }

        // KATEGORİLERİ GETİR
        private async Task LoadCategoriesAsync(
            int? selectedId = null)
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync(
                ApiUrl + "Categories");

            var categories = new List<ResultCategoryDto>();

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                categories =
                    JsonConvert.DeserializeObject<List<ResultCategoryDto>>(
                        json) ?? new List<ResultCategoryDto>();
            }

            ViewBag.v = categories.Select(x => new SelectListItem
            {
                Text = x.CategoryName,
                Value = x.CategoryId.ToString(),
                Selected = selectedId == x.CategoryId
            }).ToList();
        }

        // FOTOĞRAF YÜKLEME
        private async Task<string> SaveProductImageAsync(
            IFormFile file)
        {
            var allowedExtensions = new[]
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            var extension =
                Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Yalnızca JPG, PNG veya WEBP yükleyebilirsiniz.");
            }

            if (file.Length == 0 ||
                file.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException(
                    "Fotoğraf boş olamaz ve 5 MB boyutunu geçemez.");
            }

            // Dosyanın temel imzasını kontrol et
            byte[] header = new byte[12];

            using (var stream = file.OpenReadStream())
            {
                int read = await stream.ReadAsync(
                    header, 0, header.Length);

                bool jpeg = read >= 3 &&
                    header[0] == 0xFF &&
                    header[1] == 0xD8 &&
                    header[2] == 0xFF;

                bool png = read >= 8 &&
                    header.Take(8).SequenceEqual(
                        new byte[]
                        {
                            137, 80, 78, 71, 13, 10, 26, 10
                        });

                bool webp = read >= 12 &&
                    Encoding.ASCII.GetString(header, 0, 4) == "RIFF" &&
                    Encoding.ASCII.GetString(header, 8, 4) == "WEBP";

                bool valid = extension switch
                {
                    ".jpg" or ".jpeg" => jpeg,
                    ".png" => png,
                    ".webp" => webp,
                    _ => false
                };

                if (!valid)
                {
                    throw new InvalidOperationException(
                        "Geçersiz görsel dosyası.");
                }
            }

            var webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            var folder = Path.Combine(
                webRoot,
                "uploads",
                "products");

            Directory.CreateDirectory(folder);

            var fileName =
                Guid.NewGuid().ToString("N") + extension;

            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(
                filePath, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            return $"{Request.Scheme}://{Request.Host}" +
                   $"/uploads/products/{fileName}";
        }

        // ÜRÜN LİSTESİ
        [HttpGet]
        public async Task<IActionResult> ProductList()
        {
            var client = _httpClientFactory.CreateClient();

            try
            {
                var response = await client.GetAsync(
                    ApiUrl + "Products/ProductListWithCategory");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] =
                        "Ürünler API'den alınamadı.";

                    return View(new List<ResultProductDto>());
                }

                var json = await response.Content.ReadAsStringAsync();

                var products =
                    JsonConvert.DeserializeObject<List<ResultProductDto>>(
                        json) ?? new List<ResultProductDto>();

                return View(products);
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "WebApi bağlantısı kurulamadı.";

                return View(new List<ResultProductDto>());
            }
        }

        // YENİ ÜRÜN EKLEME - GET
        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            await LoadCategoriesAsync();

            return View(new CreateProductDto());
        }

        // YENİ ÜRÜN EKLEME - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(
            CreateProductDto createProductDto,
            IFormFile? imageFile)
        {
            // ImageUrl formdan gelmez, dosya yüklendikten
            // sonra sunucuda oluşturulur.
            ModelState.Remove(nameof(CreateProductDto.ImageUrl));

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(createProductDto.CategoryId);
                return View(createProductDto);
            }

            try
            {
                // Fotoğraf eklemek isteğe bağlı
                createProductDto.ImageUrl = "";

                if (imageFile != null && imageFile.Length > 0)
                {
                    createProductDto.ImageUrl =
                        await SaveProductImageAsync(imageFile);
                }

                var client = _httpClientFactory.CreateClient();

                var json =
                    JsonConvert.SerializeObject(createProductDto);

                using var content = new StringContent(
                    json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(
                    ApiUrl + "Products/CreateProductWithCategory",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Ürün başarıyla eklendi.";

                    return RedirectToAction(nameof(ProductList));
                }

                ModelState.AddModelError(
                    "",
                    "Ürün eklenemedi: " +
                    await response.Content.ReadAsStringAsync());
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("imageFile", ex.Message);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "WebApi bağlantısı kurulamadı.");
            }
            catch (IOException)
            {
                ModelState.AddModelError(
                    "",
                    "Fotoğraf sunucuya kaydedilemedi.");
            }

            await LoadCategoriesAsync(createProductDto.CategoryId);

            return View(createProductDto);
        }

        // ÜRÜN SİLME
        [HttpGet]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var client = _httpClientFactory.CreateClient();

            try
            {
                var response = await client.DeleteAsync(
                    ApiUrl + "Products?id=" + id);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Ürün başarıyla silindi.";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "Ürün silinemedi: " +
                        await response.Content.ReadAsStringAsync();
                }
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "WebApi bağlantısı kurulamadı.";
            }

            return RedirectToAction(nameof(ProductList));
        }

        // ÜRÜN GÜNCELLEME - GET
        [HttpGet]
        public async Task<IActionResult> UpdateProduct(int id)
        {
            if (id <= 0)
                return BadRequest("Geçersiz ürün numarası.");

            var client = _httpClientFactory.CreateClient();

            try
            {
                var response = await client.GetAsync(
                    ApiUrl + "Products/GetProduct?id=" + id);

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound("Ürün bulunamadı.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(
                        502, "Ürün bilgileri alınamadı.");
                }

                var json = await response.Content.ReadAsStringAsync();

                var product =
                    JsonConvert.DeserializeObject<UpdateProductDto>(json);

                if (product == null)
                    return NotFound("Ürün bulunamadı.");

                await LoadCategoriesAsync(product.CategoryId);

                return View("UpdateProduct", product);
            }
            catch (HttpRequestException)
            {
                return StatusCode(
                    503, "WebApi bağlantısı kurulamadı.");
            }
        }

        // ÜRÜN GÜNCELLEME - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProduct(
            UpdateProductDto updateProductDto,
            IFormFile? imageFile,
            bool removeImage)
        {
            ModelState.Remove(nameof(UpdateProductDto.ImageUrl));

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(
                    updateProductDto.CategoryId);

                return View("UpdateProduct", updateProductDto);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                // Veritabanındaki mevcut ürünü API'den getir
                var currentResponse = await client.GetAsync(
                    ApiUrl + "Products/GetProduct?id=" +
                    updateProductDto.ProductId);

                if (currentResponse.StatusCode ==
                    System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound("Ürün bulunamadı.");
                }

                if (!currentResponse.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        "", "Mevcut ürün bilgileri alınamadı.");

                    await LoadCategoriesAsync(
                        updateProductDto.CategoryId);

                    return View("UpdateProduct", updateProductDto);
                }

                var currentJson =
                    await currentResponse.Content.ReadAsStringAsync();

                var currentProduct =
                    JsonConvert.DeserializeObject<UpdateProductDto>(
                        currentJson);

                if (currentProduct == null)
                    return NotFound("Ürün bulunamadı.");

                // Önce mevcut fotoğrafı koru
                updateProductDto.ImageUrl =
                    currentProduct.ImageUrl;

                if (removeImage)
                {
                    // Kullanıcı fotoğrafı silmek istiyor
                    updateProductDto.ImageUrl = "";
                }
                else if (imageFile != null &&
                         imageFile.Length > 0)
                {
                    // Kullanıcı başka fotoğraf seçti
                    updateProductDto.ImageUrl =
                        await SaveProductImageAsync(imageFile);
                }

                var json =
                    JsonConvert.SerializeObject(updateProductDto);

                using var content = new StringContent(
                    json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(
                    ApiUrl + "Products",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Ürün başarıyla güncellendi.";

                    return RedirectToAction(nameof(ProductList));
                }

                ModelState.AddModelError(
                    "",
                    "Ürün güncellenemedi: " +
                    await response.Content.ReadAsStringAsync());
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("imageFile", ex.Message);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "WebApi bağlantısı kurulamadı.");
            }
            catch (IOException)
            {
                ModelState.AddModelError(
                    "", "Fotoğraf sunucuya kaydedilemedi.");
            }

            await LoadCategoriesAsync(
                updateProductDto.CategoryId);

            return View("UpdateProduct", updateProductDto);
        }

        // ÜRÜN GÖRÜNTÜLEME
        [HttpGet]
        public async Task<IActionResult> ViewProduct(int id)
        {
            var client = _httpClientFactory.CreateClient();

            try
            {
                var response = await client.GetAsync(
                    ApiUrl + "Products/GetProduct?id=" + id);

                if (!response.IsSuccessStatusCode)
                    return NotFound("Ürün bulunamadı.");

                var json = await response.Content.ReadAsStringAsync();

                var product =
                    JsonConvert.DeserializeObject<GetProductByIdDto>(
                        json);

                if (product == null)
                    return NotFound();

                return View(product);
            }
            catch (HttpRequestException)
            {
                return StatusCode(
                    503, "WebApi bağlantısı kurulamadı.");
            }
        }

        // SÜRÜKLE-BIRAK ÜRÜN SIRALAMA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProductOrder(
            [FromBody] List<int> productIds)
        {
            if (productIds == null ||
                productIds.Count == 0 ||
                productIds.Distinct().Count() != productIds.Count)
            {
                return BadRequest("Geçersiz ürün sıralaması.");
            }

            var orders = productIds
                .Select((id, index) => new
                {
                    ProductId = id,
                    DisplayOrder = index + 1
                })
                .ToList();

            try
            {
                var client = _httpClientFactory.CreateClient();

                var json = JsonConvert.SerializeObject(orders);

                using var content = new StringContent(
                    json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(
                    ApiUrl + "Products/UpdateOrder",
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content.ReadAsStringAsync();

                    return StatusCode(
                        (int)response.StatusCode, error);
                }

                return Ok(new
                {
                    success = true,
                    message = "Ürün sıralaması kaydedildi."
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(
                    503,
                    "WebApi bağlantısı kurulamadı: " + ex.Message);
            }
        }
    }
}

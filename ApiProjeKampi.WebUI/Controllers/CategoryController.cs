using ApiProjeKampi.WebUI.Dtos.CategoryDtos;
using ApiProjeKampi.WebUI.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace ApiProjeKampi.WebUI.Controllers
{
    public class CategoryController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ApiUrl = "https://localhost:7020/api/";

        public CategoryController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // KATEGORİ LİSTESİ
        [HttpGet]
        public async Task<IActionResult> CategoryList()
        {
            var client = _httpClientFactory.CreateClient();

            try
            {
                // 1. Kategorileri API'den getir
                var categoryResponse = await client.GetAsync(ApiUrl + "Categories");

                if (!categoryResponse.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Kategoriler API'den alınamadı.";
                    return View(new List<ResultCategoryDto>());
                }

                var categoryJson = await categoryResponse.Content.ReadAsStringAsync();
                var categories = JsonConvert.DeserializeObject<List<ResultCategoryDto>>(categoryJson)
                                 ?? new List<ResultCategoryDto>();

                // 2. Ürün sayılarını hesaplamak için ürünleri getir
                var productCounts = new Dictionary<int, int>();
                var productResponse = await client.GetAsync(ApiUrl + "Products");

                if (productResponse.IsSuccessStatusCode)
                {
                    var productJson = await productResponse.Content.ReadAsStringAsync();
                    var products = JsonConvert.DeserializeObject<List<ResultProductDto>>(productJson)
                                   ?? new List<ResultProductDto>();

                    productCounts = products
                        .GroupBy(x => x.CategoryId)
                        .ToDictionary(x => x.Key, x => x.Count());
                }

                ViewBag.ProductCounts = productCounts;

                return View(categories);
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "WebApi projesine bağlanılamadı. API'nin çalıştığından emin olun.";
                return View(new List<ResultCategoryDto>());
            }
        }

        // KATEGORİ EKLEME - GET
        [HttpGet]
        public IActionResult CreateCategory()
        {
            return View();
        }

        // KATEGORİ EKLEME - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CreateCategoryDto createCategoryDto)
        {
            if (!ModelState.IsValid)
                return View(createCategoryDto);

            var client = _httpClientFactory.CreateClient();
            var json = JsonConvert.SerializeObject(createCategoryDto);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(ApiUrl + "Categories", content);

            if (response.IsSuccessStatusCode)
                return RedirectToAction(nameof(CategoryList));

            ModelState.AddModelError("", await response.Content.ReadAsStringAsync());
            return View(createCategoryDto);
        }

        // KATEGORİ SİLME
        [HttpGet]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var client = _httpClientFactory.CreateClient();

            try
            {
                var response = await client.DeleteAsync(ApiUrl + $"Categories?id={id}");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = await response.Content.ReadAsStringAsync();
                }

                return RedirectToAction(nameof(CategoryList));
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "WebApi bağlantısı kurulamadı.";
                return RedirectToAction(nameof(CategoryList));
            }
        }

        // KATEGORİ GÜNCELLEME - GET
        [HttpGet]
        public async Task<IActionResult> UpdateCategory(int id)
        {
            var client = _httpClientFactory.CreateClient();

            try
            {
                var response = await client.GetAsync(ApiUrl + $"Categories/GetCategory?id={id}");

                if (!response.IsSuccessStatusCode)
                    return NotFound("Kategori bulunamadı.");

                var json = await response.Content.ReadAsStringAsync();
                var category = JsonConvert.DeserializeObject<GetCategoryByIdDto>(json);

                if (category == null)
                    return NotFound("Kategori bulunamadı.");

                return View(category);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "WebApi bağlantısı kurulamadı.");
            }
        }

        // KATEGORİ GÜNCELLEME - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory(GetCategoryByIdDto categoryDto)
        {
            if (!ModelState.IsValid)
                return View(categoryDto);

            var client = _httpClientFactory.CreateClient();
            var json = JsonConvert.SerializeObject(categoryDto);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PutAsync(ApiUrl + "Categories", content);

                if (response.IsSuccessStatusCode)
                    return RedirectToAction(nameof(CategoryList));

                ModelState.AddModelError("", await response.Content.ReadAsStringAsync());
                return View(categoryDto);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("", "WebApi bağlantısı kurulamadı.");
                return View(categoryDto);
            }
        }

        // KATEGORİ GÖRÜNTÜLEME
        [HttpGet]
        public async Task<IActionResult> ViewCategory(int id)
        {
            var client = _httpClientFactory.CreateClient();

            try
            {
                var response = await client.GetAsync(ApiUrl + "Products");

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, "Ürünler getirilemedi.");

                var json = await response.Content.ReadAsStringAsync();
                var products = JsonConvert.DeserializeObject<List<ResultProductDto>>(json)
                               ?? new List<ResultProductDto>();

                var filteredProducts = products.Where(x => x.CategoryId == id).ToList();
                return View(filteredProducts);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "WebApi bağlantısı kurulamadı.");
            }
        }

        // SÜRÜKLE - BIRAK SIRALAMA GÜNCELLEME
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategoryOrder([FromBody] List<int> categoryIds)
        {
            if (categoryIds == null || categoryIds.Count == 0)
            {
                return BadRequest("Kategori sıralaması boş.");
            }

            var orders = categoryIds
                .Select((id, index) => new
                {
                    CategoryId = id,
                    DisplayOrder = index + 1
                })
                .ToList();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var json = JsonConvert.SerializeObject(orders);

                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(ApiUrl + "Categories/UpdateOrder", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, error);
                }

                return Ok(new
                {
                    success = true,
                    message = "Sıralama başarıyla kaydedildi."
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(503, "WebApi bağlantısı kurulamadı: " + ex.Message);
            }
        }
    }
}
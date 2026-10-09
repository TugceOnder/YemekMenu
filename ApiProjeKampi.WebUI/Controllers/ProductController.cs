
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

        private const string ApiUrl =
            "https://localhost:7020/api/";

        public ProductController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // KATEGORİLERİ YÜKLE
        private async Task LoadCategoriesAsync(int? selectedId = null)
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync(
                ApiUrl + "Categories");

            var categoryValues = new List<SelectListItem>();

            if (response.IsSuccessStatusCode)
            {
                var jsonData =
                    await response.Content.ReadAsStringAsync();

                var categories =
                    JsonConvert.DeserializeObject<List<ResultCategoryDto>>(
                        jsonData) ?? new List<ResultCategoryDto>();

                categoryValues = categories.Select(x =>
                    new SelectListItem
                    {
                        Text = x.CategoryName,
                        Value = x.CategoryId.ToString(),
                        Selected = selectedId == x.CategoryId
                    }).ToList();
            }

            // CreateProduct sayfası için
            ViewBag.v = categoryValues;

            // UpdateProduct sayfası için
            ViewData["Categories"] = categoryValues;
        }

        // ÜRÜN LİSTESİ
        [HttpGet]
        public async Task<IActionResult> ProductList()
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync(
                ApiUrl + "Products/ProductListWithCategory");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Ürünler yüklenemedi.";
                return View(new List<ResultProductDto>());
            }

            var jsonData =
                await response.Content.ReadAsStringAsync();

            var products =
                JsonConvert.DeserializeObject<List<ResultProductDto>>(
                    jsonData) ?? new List<ResultProductDto>();

            return View(products);
        }

        // ÜRÜN EKLEME - GET
        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            await LoadCategoriesAsync();

            return View();
        }

        // ÜRÜN EKLEME - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(
            CreateProductDto createProductDto)
        {
            var client = _httpClientFactory.CreateClient();

            var jsonData =
                JsonConvert.SerializeObject(createProductDto);

            using var content = new StringContent(
                jsonData,
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(
                ApiUrl + "Products/CreateProductWithCategory",
                content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] =
                    "Ürün başarıyla eklendi.";

                return RedirectToAction("ProductList");
            }

            ModelState.AddModelError(
                "",
                "Ürün eklenemedi.");

            await LoadCategoriesAsync(createProductDto.CategoryId);

            return View(createProductDto);
        }

        // ÜRÜN SİLME
        [HttpGet]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var client = _httpClientFactory.CreateClient();

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
                    "Ürün silinemedi.";
            }

            return RedirectToAction("ProductList");
        }

        // ÜRÜN GÜNCELLEME - GET
        [HttpGet]
        public async Task<IActionResult> UpdateProduct(int id)
        {
            if (id <= 0)
                return BadRequest("Geçersiz ürün numarası.");

            var client = _httpClientFactory.CreateClient();

            // DOĞRU ENDPOINT: GetProduct
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

            var jsonData =
                await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(jsonData) ||
                jsonData.Trim() == "null")
            {
                return NotFound("Ürün bulunamadı.");
            }

            UpdateProductDto? product;

            try
            {
                product =
                    JsonConvert.DeserializeObject<UpdateProductDto>(
                        jsonData);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return StatusCode(
                    502, "Ürün JSON verisi okunamadı.");
            }

            if (product == null)
            {
                return NotFound("Ürün bulunamadı.");
            }

            // Seçili kategoriyle birlikte kategorileri getir
            await LoadCategoriesAsync(product.CategoryId);

            // Güncelleme ekranına dolu model gönder
            return View("UpdateProduct", product);
        }

        // ÜRÜN GÜNCELLEME - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProduct(
            UpdateProductDto updateProductDto)
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(
                    updateProductDto.CategoryId);

                return View("UpdateProduct", updateProductDto);
            }

            var client = _httpClientFactory.CreateClient();

            var jsonData =
                JsonConvert.SerializeObject(updateProductDto);

            using var content = new StringContent(
                jsonData,
                Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync(
                ApiUrl + "Products",
                content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] =
                    "Ürün başarıyla güncellendi.";

                return RedirectToAction("ProductList");
            }

            var errorMessage =
                await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(
                "",
                "Ürün güncellenemedi: " + errorMessage);

            // Hata varsa kategorileri yeniden yükle
            await LoadCategoriesAsync(
                updateProductDto.CategoryId);

            return View("UpdateProduct", updateProductDto);
        }

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

            var client = _httpClientFactory.CreateClient();

            var json = JsonConvert.SerializeObject(orders);

            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            try
            {
                var response = await client.PutAsync(
                    "https://localhost:7020/api/Products/UpdateOrder",
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();

                    return StatusCode(
                        (int)response.StatusCode,
                        error);
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
                    "WebApi bağlantı hatası: " + ex.Message);
            }
        }

    }
}

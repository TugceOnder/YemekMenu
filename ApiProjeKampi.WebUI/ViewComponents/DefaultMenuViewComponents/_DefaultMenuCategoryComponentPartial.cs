using ApiProjeKampi.WebUI.Dtos.CategoryDtos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ApiProjeKampi.WebUI.ViewComponents.DefaultViewComponents
{
    public class _DefaultMenuCategoryComponentPartial : ViewComponent
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public _DefaultMenuCategoryComponentPartial(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var client = _httpClientFactory.CreateClient();

            try
            {
                // Port numaranızı (7020) kontrol edin
                var responseMessage = await client.GetAsync("https://localhost:7020/api/Categories");

                if (responseMessage.IsSuccessStatusCode)
                {
                    var jsonData = await responseMessage.Content.ReadAsStringAsync();
                    var values = JsonConvert.DeserializeObject<List<ResultCategoryDto>>(jsonData);
                    return View(values ?? new List<ResultCategoryDto>());
                }
            }
            catch (Exception)
            {
                // API kapalıysa veya hata verirse uygulamanın çökmesini engeller
            }

            // Hata durumunda null yerine boş liste gönderilir
            return View(new List<ResultCategoryDto>());
        }
    }
}
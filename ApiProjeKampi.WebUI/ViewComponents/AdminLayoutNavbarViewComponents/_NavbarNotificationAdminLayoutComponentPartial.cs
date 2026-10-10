
using ApiProjeKampi.WebUI.Dtos.NotificationDtos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ApiProjeKampi.WebUI.ViewComponents.AdminLayoutNavbarViewComponents
{
    public class _NavbarNotificationAdminLayoutComponentPartial : ViewComponent
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public _NavbarNotificationAdminLayoutComponentPartial(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var notifications = new List<ResultNotificationDto>();

            try
            {
                var client = _httpClientFactory.CreateClient();

                var responseMessage = await client.GetAsync(
                    "https://localhost:7020/api/Notifications");

                if (responseMessage.IsSuccessStatusCode)
                {
                    var jsonData =
                        await responseMessage.Content.ReadAsStringAsync();

                    notifications =
                        JsonConvert.DeserializeObject<List<ResultNotificationDto>>(
                            jsonData)
                        ?? new List<ResultNotificationDto>();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    "Bildirimler yüklenirken hata: " + ex.Message);
            }

            return View("Default", notifications);
        }
    }
}

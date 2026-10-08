using ApiProjeKampi.WebUI.Dtos.GroupReservationDtos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ApiProjeKampi.WebUI.ViewComponents.DashboardViewComponents
{
    public class _DashboardGroupReservationComponentPartial : ViewComponent
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public _DashboardGroupReservationComponentPartial(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var client = _httpClientFactory.CreateClient();

            var responseMessage = await client.GetAsync(
                "https://localhost:7020/api/GroupReservations/"
            );

            if (responseMessage.IsSuccessStatusCode)
            {
                var jsonData = await responseMessage.Content.ReadAsStringAsync();

                var values =
                    JsonConvert.DeserializeObject<List<ResultGroupReservationDto>>(jsonData);

                return View(values ?? new List<ResultGroupReservationDto>());
            }

            return View(new List<ResultGroupReservationDto>());
        }
    }
}
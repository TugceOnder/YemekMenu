using ApiProjeKampi.WebApi.Context;
using ApiProjeKampi.WebApi.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiProjeKampi.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class YummyEventsController : ControllerBase
    {
        private readonly ApiContext _context;

        public YummyEventsController(ApiContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult YummyEventList()
        {
            return Ok(_context.YummyEvents.ToList());
        }

        [HttpPost]
        public IActionResult CreateYummyEvent(YummyEvent yummyEvent)
        {
            _context.YummyEvents.Add(yummyEvent);
            _context.SaveChanges();
            return Ok("Etkinlik ekleme işlemi başarılı");
        }

        [HttpDelete]
        public IActionResult DeleteYummyEvent(int id)
        {
            var value = _context.YummyEvents.Find(id);
            if (value == null) return NotFound();

            _context.YummyEvents.Remove(value);
            _context.SaveChanges();
            return Ok("Etkinlik silme işlemi başarılı");
        }

        [HttpGet("GetYummyEvent")]
        public IActionResult GetYummyEvent(int id)
        {
            var value = _context.YummyEvents.Find(id);
            if (value == null) return NotFound();

            return Ok(value);
        }

        [HttpPut]
        public IActionResult UpdateYummyEvent(YummyEvent yummyEvent)
        {
            var value = _context.YummyEvents.Find(yummyEvent.YummyEventId);
            if (value == null) return NotFound();

            value.Title = yummyEvent.Title;
            value.Description = yummyEvent.Description;
            value.Price = yummyEvent.Price;
            value.ImageUrl = yummyEvent.ImageUrl;

            _context.SaveChanges();
            return Ok("Etkinlik güncelleme işlemi başarılı");
        }
    }
}
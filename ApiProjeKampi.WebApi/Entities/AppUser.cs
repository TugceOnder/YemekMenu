using Microsoft.AspNetCore.Identity;

namespace ApiProjeKampi.WebApi.Entities
{
    public class AppUser : IdentityUser<int>
    {
        public string NameSurname { get; set; }
    }
}
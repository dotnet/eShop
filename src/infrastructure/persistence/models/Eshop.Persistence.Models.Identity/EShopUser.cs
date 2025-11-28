using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eshop.Persistence.Models.Identity
{
    [Table("AspNetUsers")]
    public class EShopUser : IdentityUser<int>
    {

    }
}

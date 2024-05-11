using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheWebApiServer.Model
{
    public class Payments
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }
        public string Status { get; set; }
        public DateTime CreationTime { get; set; }
        public int Amonut { get; set; }
    }
}

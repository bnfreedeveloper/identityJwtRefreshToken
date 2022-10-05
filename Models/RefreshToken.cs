using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace todaapp.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        //foreign key for the identityUser
        public string UserId { get; set; }
        public string Token { get; set; }
        //id of the token, this refreshtoken belongs to(so foreign key of the token)
        public string JwId { get; set; }
        public bool IsUsed { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime AddedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        [ForeignKey(nameof(UserId))]
        public IdentityUser User { get; set; }
    }
}
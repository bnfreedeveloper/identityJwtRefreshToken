using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace todaapp.Configuration
{
    public class AuthResult
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public bool Success { get; set; }
        public List<string> Errors { get; set; }

        //public bool Role { get; set; }
    }
}
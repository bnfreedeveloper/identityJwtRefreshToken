using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace todaapp.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool Done { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using todaapp.Models;

namespace todaapp.Data
{
    public class TodoDbContext : IdentityDbContext
    {
        public virtual DbSet<Item> Items { get; set; }
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

        public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options)
        {
        }
    }
}
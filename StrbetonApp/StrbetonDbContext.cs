using Microsoft.EntityFrameworkCore;
using StrbetonApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrbetonApp
{
    public class StrbetonDbContext : DbContext
    {
        public DbSet<Role> Roles { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<OrderStatus> OrderStatuses { get; set; }
        public DbSet<Auth> Auths { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }

        public StrbetonDbContext(DbContextOptions<StrbetonDbContext> options)
            : base(options)
        {
        }
    }
}

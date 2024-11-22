using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrbetonApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int? WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }
        public string ImagePath { get; set; }
    }
}
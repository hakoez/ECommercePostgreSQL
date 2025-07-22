using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagment
{
    public class Order
    {
    
        public int Id { get; set; }

        // Sipariş veren müşteri
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        // Sipariş tarihi
        public DateTime OrderDate { get; set; } = DateTime.Now;

        // Siparişteki ürünler
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        // Sipariş durumu
        public OrderStat Status { get; set; } = OrderStat.Pending;
    }

}

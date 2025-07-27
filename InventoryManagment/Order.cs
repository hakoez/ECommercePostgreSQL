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
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public OrderStat Status { get; set; } = OrderStat.Pending;
    }

}

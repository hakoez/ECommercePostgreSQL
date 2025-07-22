using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagment
{
    public class Cart
    {
        public int Id { get; set; }

        // Sepet sahibi müşteri
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        // Sepetteki ürünler
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        // Toplam tutarı hesaplayan bir property (sadece okuma)
        public decimal TotalAmount
        {
            get
            {
                return Items.Sum(item => item.Product.Price * item.Quantity);
            }
        }
    }
}

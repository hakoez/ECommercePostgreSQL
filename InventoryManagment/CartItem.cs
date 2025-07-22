using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagment
{
    public class CartItem
    {
        public int Id { get; set; }

        // Sepetteki ürün
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // Ürün miktarı
        public int Quantity { get; set; }

        // Hangi sepetin parçası
        public int CartId { get; set; }
        public Cart Cart { get; set; } = null!;
    }
}

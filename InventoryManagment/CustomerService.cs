using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagment
{
    public class CustomerService
    {
        private readonly string _connectionString;

        public CustomerService(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Sepete ürün ekle
        public void AddToCart(int customerId, int productId, int quantity)
        {
            // Burada, önce stok kontrolü yapacağız, sonra sepete ekleyeceğiz
            // SQL işlemleri olacak (INSERT veya UPDATE)
        }

        // Sipariş oluştur
        public void CreateOrder(int customerId)
        {
            // Sepetteki ürünleri al, yeni bir Order oluştur, stoktan düş, sipariş tablosuna yaz
        }

        // Sipariş geçmişini getir
        public List<Order> GetOrderHistory(int customerId)
        {
            // Müşterinin geçmiş siparişlerini veritabanından çek
            return new List<Order>();
        }
    }
}

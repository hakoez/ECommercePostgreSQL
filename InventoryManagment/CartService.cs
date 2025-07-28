using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace InventoryManagment
{
    public class CartService
    {
        private readonly string _connectionString;

        public CartService(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Sepet oluştur
        public void CreateCart(int customerId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
             
            var cmd = new NpgsqlCommand("INSERT INTO Carts (CustomerId) VALUES (@CustomerId)", conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            cmd.ExecuteNonQuery();

            Console.WriteLine("Sepet oluşturuldu.");
        }

      
        

        // Sepetten ürün çıkar
        public void RemoveFromCart(int cartItemId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand("DELETE FROM CartItems WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", cartItemId);
            cmd.ExecuteNonQuery();

            Console.WriteLine("Ürün sepetten çıkarıldı.");
        }
        public void ViewCartByCustomerId(int customerId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            // Önce sepete ait ID'yi bul
            string findCartSql = "SELECT Id FROM Carts WHERE CustomerId = @customerId ORDER BY Id DESC LIMIT 1";
            using var findCartCmd = new NpgsqlCommand(findCartSql, conn);
            findCartCmd.Parameters.AddWithValue("customerId", customerId);

            object? result = findCartCmd.ExecuteScalar();
            if (result == null)
            {
                Console.WriteLine("Bu kullanıcıya ait bir sepet bulunamadı.");
                return;
            }

            int cartId = Convert.ToInt32(result);

            // sepet icerigi
            string sql = @"SELECT ci.Id, p.Name, ci.Quantity, p.Price 
                   FROM CartItems ci 
                   JOIN Products p ON ci.ProductId = p.Id 
                   WHERE ci.CartId = @cartId";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("cartId", cartId);

            using var reader = cmd.ExecuteReader();
            Console.WriteLine("\n--- Sepet İçeriği ---");

            decimal total = 0; //fiyat bilgisi

            while (reader.Read())
            {
                int cartItemId = reader.GetInt32(0);
                string productName = reader.GetString(1);
                int quantity = reader.GetInt32(2);
                decimal price = reader.GetDecimal(3);

                decimal itemTotal = price * quantity;
                total += itemTotal;

                Console.WriteLine($"Ürün: {productName} | Miktar: {quantity} | Fiyat: {price}TL | Tutar: {itemTotal}TL | SepetÜrünId: {cartItemId}");
            }

            Console.WriteLine($"\n🧾 Sepet Toplamı: {total}TL");
        }


       


    }
}

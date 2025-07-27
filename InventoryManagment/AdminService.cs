using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace InventoryManagment
{
    public class AdminService
    {
        private readonly string _connectionString;

        public AdminService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void ShowAdminMenu()
        {
            while (true)
            {
                Console.WriteLine("\n--- Admin Menüsü ---");
                Console.WriteLine("1. Ürün Ekle");
                Console.WriteLine("2. Ürünleri Listele");
                Console.WriteLine("3. Ürün Sil");
                Console.WriteLine("4. Siparişleri Görüntüle");
                Console.WriteLine("5. Çıkış");

                Console.Write("Seçiminiz: ");
                string choice = Console.ReadLine()!;

                switch (choice)
                {
                    case "1":
                        AddProduct();
                        break;
                    case "2":
                        ListProducts();
                        break;
                    case "3":
                        DeleteProduct();
                        break;
                    case "4":
                        Console.WriteLine("Sipariş listesi özelliği henüz eklenmedi.");
                        break;
                    case "5":
                        Console.WriteLine("Çıkış yapılıyor...");
                        return;
                    default:
                        Console.WriteLine("Geçersiz seçim.");
                        break;
                }
            }
        }

        private void AddProduct()
        {
            Console.Write("Ürün Adı: ");
            string name = Console.ReadLine()!;
            Console.Write("Açıklama: ");
            string description = Console.ReadLine()!;
            Console.Write("Fiyat: ");
            decimal price = decimal.Parse(Console.ReadLine()!);
            Console.Write("Stok Adedi: ");
            int stock = int.Parse(Console.ReadLine()!);

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand("INSERT INTO Products (Name, Description, Price, StockQuantity) VALUES (@name, @desc, @price, @stock)", conn);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@desc", description);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@stock", stock);

            cmd.ExecuteNonQuery();

            Console.WriteLine("Ürün başarıyla eklendi.");
        }

        private void ListProducts()
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand("SELECT Id, Name, Price, StockQuantity FROM Products", conn);

            using var reader = cmd.ExecuteReader();
            Console.WriteLine("\n--- Ürün Listesi ---");

            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string name = reader.GetString(1);
                decimal price = reader.GetDecimal(2);
                int stock = reader.GetInt32(3);

                Console.WriteLine($"ID: {id} | {name} | {price}₺ | Stok: {stock}");
            }
        }

        private void DeleteProduct()
        {
            Console.Write("Silinecek ürün ID: ");
            int id = int.Parse(Console.ReadLine()!);

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand("DELETE FROM Products WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            int affected = cmd.ExecuteNonQuery();

            if (affected > 0)
                Console.WriteLine("Ürün silindi.");
            else
                Console.WriteLine("Ürün bulunamadı.");
        }
    }
}

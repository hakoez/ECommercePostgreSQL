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
            var categoryService = new CategoryService(_connectionString);
            while (true)
            {
                Console.WriteLine("\n--- Admin Menüsü ---");
                Console.WriteLine("1. Ürün Ekle");
                Console.WriteLine("2. Ürünleri Listele");
                Console.WriteLine("3. Ürün Sil");
                Console.WriteLine("4. Siparişleri Görüntüle");
                Console.WriteLine("5. Kategori Ekle");
                Console.WriteLine("6. Kategori Listele ve Sil");
                Console.WriteLine("7. Çıkış");

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
                        var adminService = new AdminService(_connectionString);
                        var orders = adminService.GetAllOrders();

                        if (orders.Count == 0)
                        {
                            Console.WriteLine("Henüz sipariş yok.");
                        }
                        else
                        {
                            foreach (var order in orders)
                            {
                                Console.WriteLine($"\nSipariş ID: {order.Id}, Müşteri: {order.CustomerName}, Tarih: {order.OrderDate}, Durum: {order.Status}");
                                foreach (var item in order.CartItems)
                                {
                                    Console.WriteLine($" - {item.Product.Name} x {item.Quantity}");
                                }
                            }
                        }
                        break;
                    case "5":
                        Console.Write("Yeni kategori adı: ");
                        string categoryName = Console.ReadLine()!;
                        categoryService.AddCategory(categoryName);
                        Console.WriteLine("Kategori eklendi.");
                        break;

                    case "6":
                        var categories = categoryService.GetAllCategories();
                        foreach (var cat in categories)
                            Console.WriteLine($"ID: {cat.Id} - {cat.Name}");

                        Console.Write("Silinecek kategori ID: ");
                        int catId = int.Parse(Console.ReadLine()!);
                        categoryService.DeleteCategory(catId);
                        Console.WriteLine("Kategori silindi.");
                        break;
                    case "7":
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
            var categoryService = new CategoryService(_connectionString);
            var categories = categoryService.GetAllCategories();

            if (categories.Count == 0)
            {
                Console.WriteLine("Öncelikle kategori eklemeniz gerekiyor.");
                return;
            }

            Console.WriteLine("Kategori Listesi:");
            foreach (var cat in categories)
            {
                Console.WriteLine($"{cat.Id}. {cat.Name}");
            }

            Console.Write("Kategori ID seçin: ");
            int categoryId;
            while (!int.TryParse(Console.ReadLine(), out categoryId) || !categories.Any(c => c.Id == categoryId))
            {
                Console.Write("Geçersiz kategori ID. Lütfen tekrar girin: ");
            }

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

            var cmd = new NpgsqlCommand("INSERT INTO Products (Name, Description, Price, StockQuantity," +
                " CategoryId) VALUES (@name, @desc, @price, @stock, @categoryId)", conn);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@desc", description);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@stock", stock);
            cmd.Parameters.AddWithValue("@categoryId", categoryId);

            cmd.ExecuteNonQuery();//return olmadigi icin 

            Console.WriteLine("Ürün başarıyla eklendi.");
        }

        private void ListProducts()
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            string sql = @"
            SELECT p.Id, p.Name, p.Price, p.StockQuantity, c.Name AS CategoryName
            FROM Products p
            LEFT JOIN Categories c ON p.CategoryId = c.Id";

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();//sql veri okuma metodu

            Console.WriteLine("\n--- Ürün Listesi ---");

            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string name = reader.GetString(1);
                decimal price = reader.GetDecimal(2);
                int stock = reader.GetInt32(3);
                string categoryName = reader.IsDBNull(4) ? "Kategori Yok" : reader.GetString(4);

                Console.WriteLine($"ID: {id} | {name} | {price}TL | Stok: {stock} | Kategori: {categoryName}");
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
        public List<Order> GetAllOrders()
        {
            var orders = new List<Order>();

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            string sql = @"
            SELECT o.Id, o.OrderDate, o.Status, u.Username,
                   oi.Id AS OrderItemId, p.Name, p.Price, oi.Quantity
            FROM Orders o
            JOIN Users u ON o.CustomerId = u.Id
            JOIN OrderItems oi ON oi.OrderId = o.Id
            JOIN Products p ON oi.ProductId = p.Id
            ORDER BY o.OrderDate DESC;";

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                int orderId = reader.GetInt32(0);
                var existingOrder = orders.FirstOrDefault(o => o.Id == orderId);

                if (existingOrder == null)
                {
                    existingOrder = new Order
                    {
                        Id = orderId,
                        OrderDate = reader.GetDateTime(1),
                        Status = Enum.Parse<OrderStat>(reader.GetString(2)),
                        CustomerName = reader.GetString(3), 
                        CartItems = new List<CartItem>()
                    };
                    orders.Add(existingOrder);
                }

                var cartItem = new CartItem
                {
                    Id = reader.GetInt32(4),
                    Product = new Product
                    {
                        Name = reader.GetString(5),
                        Price = reader.GetDecimal(6)
                    },
                    Quantity = reader.GetInt32(7)
                };

                existingOrder.CartItems.Add(cartItem);
            }

            return orders;
        }

    }
}

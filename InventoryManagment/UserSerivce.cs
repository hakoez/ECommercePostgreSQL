using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace InventoryManagment
{
    public class UserService
    {
        private readonly string _connectionString;

        public UserService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void RegisterUser(User user)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            string sql = "INSERT INTO Users (Username, Email, Password, UserType) VALUES (@username, @email, @password, @usertype)";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("username", user.Username);
            cmd.Parameters.AddWithValue("email", user.Email);
            cmd.Parameters.AddWithValue("password", user.Password);
            cmd.Parameters.AddWithValue("usertype", user.UserType);

            cmd.ExecuteNonQuery();
        }
       

        public void ShowCustomerMenu(Customer customer)
        {
            var customerService = new CustomerService(_connectionString);
            var cartService = new CartService(_connectionString);
            var productService = new ProductService(_connectionString);
            while (true)
            {
                Console.WriteLine("\n--- Müsteri Menüsü ---");
                Console.WriteLine("1. Sepete Ürün Ekle");
                Console.WriteLine("2. Sepetten Ürün Cikar");
                Console.WriteLine("3. Sepeti Görüntüle");
                Console.WriteLine("4. Cikis");
                Console.WriteLine("5. Siparis Gecmisini Görüntüle");
                Console.WriteLine("6. Ürünleri Listele");
                Console.WriteLine("7. Ürün Ara");
                Console.WriteLine("8. Siparis Olustur");

                Console.Write("Seciminiz: ");
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        Console.Write("Ürün ID: ");
                        int productId = int.Parse(Console.ReadLine()!);
                        Console.Write("Miktar: ");
                        int quantity = int.Parse(Console.ReadLine()!);
                        customerService.AddToCart(customer.Id, productId, quantity);
                        break;

                    case "2":
                        Console.Write("Silinecek Sepet Ürün ID: ");
                        int cartItemId = int.Parse(Console.ReadLine()!);
                        cartService.RemoveFromCart(cartItemId);
                        break;

                    case "3":
                        cartService.ViewCartByCustomerId(customer.Id);
                        break;

                    case "4":
                        Console.WriteLine("Cikis yapiliyor");
                        return;

                    case "5":
                        var orders = customerService.GetOrderHistory(customer.Id);
                        if (orders.Count == 0)
                        {
                            Console.WriteLine("Hic siparis bulunamadi.");
                        }
                        else
                        {
                            foreach (var order in orders)
                            {
                                Console.WriteLine($"\nSipariş ID: {order.Id}, Tarih: {order.OrderDate}, Durum: {order.Status}");
                                foreach (var item in order.CartItems)
                                {
                                    Console.WriteLine($" - {item.Product.Name} x {item.Quantity}");
                                }
                            }
                        }
                        break;
                    case "6":
                        var products = productService.GetAllProducts();
                        foreach (var p in products)
                        {
                            Console.WriteLine($"ID: {p.Id}, Ad: {p.Name}, Fiyat: {p.Price}, Stok: {p.StockQuantity}");
                        
                        }
                        break;

                    case "7":
                        Console.Write("Ürün adi girin: ");
                        string searchTerm = Console.ReadLine()!;
                        var found = productService.SearchProductsByName(searchTerm);
                        if (found.Count == 0)
                        {
                            Console.WriteLine("Ürün Bulunamadi");
                        }
                        else
                        {
                            foreach (var p in found)
                            {
                                Console.WriteLine($"ID: {p.Id}, Ad: {p.Name}, Fiyat: {p.Price}, Stok: {p.StockQuantity}");
                            }
                        }
                        break;
                    case "8":
                        customerService.CreateOrder(customer.Id);
                        break;


                    default:
                        Console.WriteLine("Gecersiz Secim!");
                        break;
                }
            }
        }
        public void CreateOrder(int customerId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            // 1. Müşterinin sepeti var mı kontrol et
            var getCartCmd = new NpgsqlCommand("SELECT Id FROM Carts WHERE CustomerId = @customerId", conn);
            getCartCmd.Parameters.AddWithValue("customerId", customerId);
            var cartIdObj = getCartCmd.ExecuteScalar();

            if (cartIdObj == null)
            {
                Console.WriteLine("Sepet bulunamadı.");
                return;
            }

            int cartId = Convert.ToInt32(cartIdObj);

            // 2. Sepette ürün var mı kontrol et
            var getItemsCmd = new NpgsqlCommand(@"
            SELECT ci.ProductId, ci.Quantity, p.StockQuantity 
            FROM CartItems ci 
            JOIN Products p ON ci.ProductId = p.Id 
            WHERE ci.CartId = @cartId", conn);
            getItemsCmd.Parameters.AddWithValue("cartId", cartId);

            var items = new List<(int ProductId, int Quantity, int Stock)>();
            using (var reader = getItemsCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2)));
                }
            }

            if (items.Count == 0)
            {
                Console.WriteLine("Sepetiniz boş.");
                return;
            }

            // 3. Stok kontrolü
            foreach (var item in items)
            {
                if (item.Quantity > item.Stock)
                {
                    Console.WriteLine($"Yetersiz stok: Ürün ID {item.ProductId}");
                    return;
                }
            }

            // 4. Sipariş oluştur
            var insertOrderCmd = new NpgsqlCommand(@"
            INSERT INTO Orders (CustomerId, OrderDate, Status) 
            VALUES (@customerId, @orderDate, @status) 
            RETURNING Id", conn);
            insertOrderCmd.Parameters.AddWithValue("customerId", customerId);
            insertOrderCmd.Parameters.AddWithValue("orderDate", DateTime.Now);
            insertOrderCmd.Parameters.AddWithValue("status", "Pending");
            int orderId = Convert.ToInt32(insertOrderCmd.ExecuteScalar());

            // 5. OrderItems'a ekle ve stok güncelle
            foreach (var item in items)
            {
                var insertItemCmd = new NpgsqlCommand(@"
                INSERT INTO OrderItems (OrderId, ProductId, Quantity) 
                VALUES (@orderId, @productId, @quantity)", conn);
                insertItemCmd.Parameters.AddWithValue("orderId", orderId);
                insertItemCmd.Parameters.AddWithValue("productId", item.ProductId);
                insertItemCmd.Parameters.AddWithValue("quantity", item.Quantity);
                insertItemCmd.ExecuteNonQuery();

                var updateStockCmd = new NpgsqlCommand(@"
                UPDATE Products SET StockQuantity = StockQuantity - @qty 
                WHERE Id = @productId", conn);
                updateStockCmd.Parameters.AddWithValue("qty", item.Quantity);
                updateStockCmd.Parameters.AddWithValue("productId", item.ProductId);
                updateStockCmd.ExecuteNonQuery();
            }

            // 6. Sepeti temizle
            var deleteItemsCmd = new NpgsqlCommand("DELETE FROM CartItems WHERE CartId = @cartId", conn);
            deleteItemsCmd.Parameters.AddWithValue("cartId", cartId);
            deleteItemsCmd.ExecuteNonQuery();

            Console.WriteLine($"Sipariş oluşturuldu! Sipariş ID: {orderId}");
        }

        public Customer? Login(string username, string password)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand("SELECT Id, Username, Email, Password, UserType FROM Users WHERE Username = @username AND Password = @password", conn);
            cmd.Parameters.AddWithValue("username", username);
            cmd.Parameters.AddWithValue("password", password);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Customer
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    Password = reader.GetString(3),
                    UserType = reader.GetString(4)
                };
            }
            return null;
        }


    }
}

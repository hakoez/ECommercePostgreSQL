using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

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
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            // Ürünün stok miktarını al
            var stockCmd = new NpgsqlCommand("SELECT StockQuantity FROM Products WHERE Id = @productId", conn);
            stockCmd.Parameters.AddWithValue("productId", productId);
            var stockObj = stockCmd.ExecuteScalar();

            if (stockObj == null)
            {
                Console.WriteLine("Ürün bulunamadı.");
                return;
            }

            int stockQuantity = Convert.ToInt32(stockObj);

            if (stockQuantity < quantity)
            {
                Console.WriteLine("Yeterli stok yok.");
                return;
            }

            // Sepet yoksa oluştur
            var cartIdCmd = new NpgsqlCommand("SELECT Id FROM Carts WHERE CustomerId = @customerId", conn);
            cartIdCmd.Parameters.AddWithValue("customerId", customerId);
            var cartIdObj = cartIdCmd.ExecuteScalar();

            int cartId;

            if (cartIdObj == null)
            {
                var createCartCmd = new NpgsqlCommand("INSERT INTO Carts (CustomerId) VALUES (@customerId) RETURNING Id", conn);
                createCartCmd.Parameters.AddWithValue("customerId", customerId);
                cartId = (int)createCartCmd.ExecuteScalar();
            }
            else
            {
                cartId = (int)cartIdObj;
            }

            // Ürün sepette var mı kontrol et
            var cartItemCmd = new NpgsqlCommand("SELECT Id FROM CartItems WHERE CartId = @cartId AND ProductId = @productId", conn);
            cartItemCmd.Parameters.AddWithValue("cartId", cartId);
            cartItemCmd.Parameters.AddWithValue("productId", productId);
            var cartItemObj = cartItemCmd.ExecuteScalar();

            if (cartItemObj == null)
            {
                // Yeni ürün ekle
                var insertCmd = new NpgsqlCommand("INSERT INTO CartItems (CartId, ProductId, Quantity) VALUES (@cartId, @productId, @quantity)", conn);
                insertCmd.Parameters.AddWithValue("cartId", cartId);
                insertCmd.Parameters.AddWithValue("productId", productId);
                insertCmd.Parameters.AddWithValue("quantity", quantity);
                insertCmd.ExecuteNonQuery();
                Console.WriteLine("Ürün sepete eklendi.");
            }
            else
            {
                // Ürün varsa miktarı güncelle
                var updateCmd = new NpgsqlCommand("UPDATE CartItems SET Quantity = Quantity + @quantity WHERE Id = @id", conn);
                updateCmd.Parameters.AddWithValue("quantity", quantity);
                updateCmd.Parameters.AddWithValue("id", (int)cartItemObj);
                updateCmd.ExecuteNonQuery();
                Console.WriteLine("Sepetteki ürün miktarı güncellendi.");
            }
        }


        // Sipariş oluştur
        public void CreateOrder(int customerId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            //Sepeti getir
            var cartIdCmd = new NpgsqlCommand("SELECT Id FROM Carts WHERE CustomerId = @customerId", conn);
            cartIdCmd.Parameters.AddWithValue("customerId", customerId);
            var cartIdObj = cartIdCmd.ExecuteScalar();

            if (cartIdObj == null)
            {
                Console.WriteLine("Sepet bulunamadı.");
                return;
            }

            int cartId = (int)cartIdObj;

            //Sepetteki ürünleri çek
            var itemsCmd = new NpgsqlCommand(@"
            SELECT ProductId, Quantity 
            FROM CartItems 
            WHERE CartId = @cartId", conn);
            itemsCmd.Parameters.AddWithValue("cartId", cartId);

            var items = new List<(int ProductId, int Quantity)>();

            using (var reader = itemsCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add((reader.GetInt32(0), reader.GetInt32(1)));
                }
            }

            if (items.Count == 0)
            {
                Console.WriteLine("Sepet boş.");
                return;
            }

            using var transaction = conn.BeginTransaction();

            try
            {
                // 3. Yeni sipariş oluştur
                var insertOrderCmd = new NpgsqlCommand("INSERT INTO Orders (CustomerId, OrderDate, Status) VALUES (@customerId, @orderDate, @status) RETURNING Id", conn, transaction);
                insertOrderCmd.Parameters.AddWithValue("customerId", customerId);
                insertOrderCmd.Parameters.AddWithValue("orderDate", DateTime.Now);
                insertOrderCmd.Parameters.AddWithValue("status", "Pending");
                int orderId = (int)insertOrderCmd.ExecuteScalar();

                foreach (var item in items)
                {
                    // 4. Stok kontrolü ve güncelleme
                    var stockCmd = new NpgsqlCommand("SELECT StockQuantity FROM Products WHERE Id = @productId", conn, transaction);
                    stockCmd.Parameters.AddWithValue("productId", item.ProductId);
                    int stock = (int)stockCmd.ExecuteScalar();

                    if (stock < item.Quantity)
                    {
                        Console.WriteLine($"Ürün (ID: {item.ProductId}) için yeterli stok yok.");
                        transaction.Rollback();
                        return;
                    }

                    var updateStockCmd = new NpgsqlCommand("UPDATE Products SET StockQuantity = StockQuantity - @qty WHERE Id = @productId", conn, transaction);
                    updateStockCmd.Parameters.AddWithValue("qty", item.Quantity);
                    updateStockCmd.Parameters.AddWithValue("productId", item.ProductId);
                    updateStockCmd.ExecuteNonQuery();

                    // 5. Sipariş ürünlerini ekle 
                    var insertOrderItemCmd = new NpgsqlCommand("INSERT INTO OrderItems (OrderId, ProductId, Quantity) VALUES (@orderId, @productId, @quantity)", conn, transaction);
                    insertOrderItemCmd.Parameters.AddWithValue("orderId", orderId);
                    insertOrderItemCmd.Parameters.AddWithValue("productId", item.ProductId);
                    insertOrderItemCmd.Parameters.AddWithValue("quantity", item.Quantity);
                    insertOrderItemCmd.ExecuteNonQuery();
                }

                // 6. Sepeti temizle
                var clearCartCmd = new NpgsqlCommand("DELETE FROM CartItems WHERE CartId = @cartId", conn, transaction);
                clearCartCmd.Parameters.AddWithValue("cartId", cartId);
                clearCartCmd.ExecuteNonQuery();

                transaction.Commit();

                Console.WriteLine("Sipariş başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine("Sipariş oluşturulurken hata oluştu: " + ex.Message);
            }
        }

        public List<Order> GetOrderHistory(int customerId)
        {
            var orders = new List<Order>();

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            string sql = @"
            SELECT o.Id, o.OrderDate, o.Status, 
                   oi.Id AS OrderItemId, p.Name, p.Price, oi.Quantity
            FROM Orders o
            JOIN OrderItems oi ON o.Id = oi.OrderId
            JOIN Products p ON oi.ProductId = p.Id
            WHERE o.CustomerId = @customerId
            ORDER BY o.OrderDate DESC";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("customerId", customerId);

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
                        CartItems = new List<CartItem>()
                    };
                    orders.Add(existingOrder);
                }

                var cartItem = new CartItem
                {
                    Id = reader.GetInt32(3),
                    Product = new Product
                    {
                        Name = reader.GetString(4),
                        Price = reader.GetDecimal(5)
                    },
                    Quantity = reader.GetInt32(6)
                };

                existingOrder.CartItems.Add(cartItem);
            }

            return orders;
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

            // sepet içeriğini getir
            string sql = @"SELECT ci.Id, p.Name, ci.Quantity, p.Price 
                   FROM CartItems ci 
                   JOIN Products p ON ci.ProductId = p.Id 
                   WHERE ci.CartId = @cartId";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("cartId", cartId);

            using var reader = cmd.ExecuteReader();
            Console.WriteLine("\n--- Sepet İçeriği ---");
            while (reader.Read())
            {
                int cartItemId = reader.GetInt32(0);
                string productName = reader.GetString(1);
                int quantity = reader.GetInt32(2);
                decimal price = reader.GetDecimal(3);

                Console.WriteLine($"Ürün: {productName} | Miktar: {quantity} | Fiyat: {price:C} | SepetÜrünId: {cartItemId}");
            }
        }

    }
}

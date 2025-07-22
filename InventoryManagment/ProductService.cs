using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace InventoryManagment
{
    public class ProductService
    {
        private readonly string _connectionString;

        public ProductService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void AddProduct(Product product)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            string sql = @"INSERT INTO Products (Name, Description, Price, StockQuantity, CategoryId) 
                       VALUES (@name, @description, @price, @stockQuantity, @categoryId)";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("name", product.Name);
            cmd.Parameters.AddWithValue("description", product.Description);
            cmd.Parameters.AddWithValue("price", product.Price);
            cmd.Parameters.AddWithValue("stockQuantity", product.StockQuantity);
            cmd.Parameters.AddWithValue("categoryId", product.CategoryId);

            cmd.ExecuteNonQuery();
        }

        // Diğer CRUD işlemleri eklenecek.
    }

}

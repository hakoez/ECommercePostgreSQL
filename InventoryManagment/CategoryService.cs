using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace InventoryManagment
{
    public class CategoryService
    {
        private readonly string _connectionString;
        public CategoryService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void AddCategory(string name)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            var cmd = new NpgsqlCommand("INSERT INTO Categories (Name) VALUES (@name)", conn);
            cmd.Parameters.AddWithValue("name", name);
            cmd.ExecuteNonQuery();
        }

        public void DeleteCategory(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            var cmd = new NpgsqlCommand("DELETE FROM Categories WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);
            cmd.ExecuteNonQuery();
        }

        public List<Category> GetAllCategories()
        {
            var categories = new List<Category>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand("SELECT Id, Name FROM Categories", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(new Category
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }

            return categories;
        }
    }

}

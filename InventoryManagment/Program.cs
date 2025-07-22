using InventoryManagment;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        string connectionString = configuration.GetConnectionString("DefaultConnection");

        var userService = new UserService(connectionString);

        var customer = new Customer()
        {
            Username = "hakan123",
            Email = "hakan@example.com",
            Password = "12345",
            UserType = "Customer"
        };

        userService.RegisterUser(customer);

        Console.WriteLine("Kullanıcı kaydı başarılı!");
    }
}

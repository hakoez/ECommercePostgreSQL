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
        var adminService = new AdminService(connectionString);
        var cartService = new CartService(connectionString);


        while (true)
        {
            Console.WriteLine("\n--- E-Ticaret Uygulaması ---");
            Console.WriteLine("1. Giriş Yap");
            Console.WriteLine("2. Kayıt Ol");
            Console.WriteLine("3. Çıkış");

            Console.Write("Seçiminiz: ");
            string choice = Console.ReadLine()!;

            switch (choice)
            {
                case "1":
                    Console.Write("Kullanıcı Adı: ");
                    string username = Console.ReadLine()!;
                    Console.Write("Şifre: ");
                    string password = Console.ReadLine()!;

                    if (username == "admin" && password == "admin")
                    {
                        Console.WriteLine("Admin olarak giriş yapıldı.");
                        adminService.ShowAdminMenu();
                    }
                    else
                    {
                        var customer = userService.Login(username, password);
                        if (customer != null)
                        {
                            Console.WriteLine("Giriş başarılı.");
                            userService.ShowCustomerMenu(customer);
                        }
                        else
                        {
                            Console.WriteLine("Hatalı kullanıcı adı veya şifre.");
                        }
                    }
                    break;

                case "2":
                    Console.WriteLine("Yeni kullanıcı kaydı için bilgileri giriniz:");
                    Console.Write("Kullanıcı Adı: ");
                    string newUsername = Console.ReadLine()!;
                    Console.Write("Email: ");
                    string newEmail = Console.ReadLine()!;
                    Console.Write("Şifre: ");
                    string newPassword = Console.ReadLine()!;

                    var newCustomer = new Customer()
                    {
                        Username = newUsername,
                        Email = newEmail,
                        Password = newPassword,
                        UserType = "Customer"
                    };

                    userService.RegisterUser(newCustomer);
                    Console.WriteLine("Kullanıcı kaydı başarılı!");
                    break;

                case "3":
                    Console.WriteLine("Çıkış yapılıyor...");
                    return;

                default:
                    Console.WriteLine("Geçersiz seçim.");
                    break;
            }
            
        }
        
    }
 
}

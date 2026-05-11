using System.Diagnostics;
using FF.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace FF.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        public IActionResult Index(string search, string category, double? minPrice, string sort)
        {
            List<Product> products = new List<Product>();

            using (var connection = new SqliteConnection("Data Source=SatisDB.db"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                string query = "SELECT Id, Name, Price, Stock, Category FROM Products WHERE 1=1";

                if (!string.IsNullOrEmpty(search))
                    query += " AND Name LIKE @search";
                if (!string.IsNullOrEmpty(category))
                    query += " AND Category = @category";
                if (minPrice.HasValue && minPrice.Value > 0)
                    query += " AND CAST(Price AS DOUBLE) >= @minPrice";

                if (sort == "price_asc") query += " ORDER BY Price ASC";
                else if (sort == "price_desc") query += " ORDER BY Price DESC";

                command.CommandText = query;

                if (!string.IsNullOrEmpty(search))
                    command.Parameters.AddWithValue("@search", "%" + search + "%");
                if (!string.IsNullOrEmpty(category))
                    command.Parameters.AddWithValue("@category", category);
                if (minPrice.HasValue)
                    command.Parameters.AddWithValue("@minPrice", minPrice.Value);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new Product
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Price = reader.GetDouble(2),
                            Stock = reader.GetInt32(3),
                            Category = reader.IsDBNull(4) ? "" : reader.GetString(4)
                        });
                    }
                }
            }
            return View(products);
        }
        public IActionResult ElaveEt()
        {
            return View();
        }
        [HttpPost]
        public IActionResult ElaveEt(Product product)
        {
            using (var connection = new SqliteConnection("Data Source=SatisDB.db"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"SELECT Id FROM Products 
                                WHERE LOWER(TRIM(Name)) = LOWER(TRIM(@name)) 
                                AND ABS(Price - @price) < 0.01";

                command.Parameters.AddWithValue("@name", product.Name);
                command.Parameters.AddWithValue("@price", product.Price);

                var existingId = command.ExecuteScalar();

                if (existingId != null)
                {
                    command.Parameters.Clear();
                    command.CommandText = "UPDATE Products SET Stock = Stock + @stock WHERE Id = @id";
                    command.Parameters.AddWithValue("@stock", product.Stock);
                    command.Parameters.AddWithValue("@id", existingId);
                    command.ExecuteNonQuery();
                }
                else
                {
                    command.Parameters.Clear();
                    command.CommandText = "INSERT INTO Products (Name, Price, Stock, Category) VALUES (@name, @price, @stock, @category)";
                    command.Parameters.AddWithValue("@name", product.Name.Trim());
                    command.Parameters.AddWithValue("@price", product.Price);
                    command.Parameters.AddWithValue("@stock", product.Stock);
                    command.Parameters.AddWithValue("@category", product.Category ?? "");
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult SatisEt(int productId, int quantity)
        {
            using (var connection = new SqliteConnection("Data Source=SatisDB.db"))
            {
                connection.Open();
                string prodName = "";
                double price = 0;

                using (var cmd = new SqliteCommand("SELECT Name, Price FROM Products WHERE Id = @id", connection))
                {
                    cmd.Parameters.AddWithValue("@id", productId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            prodName = reader.GetString(0);
                            price = reader.GetDouble(1);
                        }
                    }
                }

                string insertQuery = "INSERT INTO Sales (ProductName, Quantity, Amount, SaleDate) VALUES (@pname, @qty, @total, @date)";
                using (var cmd = new SqliteCommand(insertQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@pname", prodName);
                    cmd.Parameters.AddWithValue("@qty", quantity);
                    cmd.Parameters.AddWithValue("@total", price * quantity);
                    cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }

                string updateQuery = "UPDATE Products SET Stock = Stock - @qty WHERE Id = @id";
                using (var cmd = new SqliteCommand(updateQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@qty", quantity);
                    cmd.Parameters.AddWithValue("@id", productId);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }
        public IActionResult History()
        {
            List<Sale> sales = new List<Sale>();
            using (var connection = new SqliteConnection("Data Source=SatisDB.db"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, ProductName, Quantity, Amount, SaleDate FROM Sales ORDER BY Id DESC";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        sales.Add(new Sale
                        {
                            Id = reader.GetInt32(0),
                            ProductName = reader.GetString(1),
                            Quantity = reader.GetInt32(2),
                            Amount = reader.GetDouble(3),
                            SaleDate = reader.GetString(4)
                        });
                    }
                }
            }
            return View(sales);
        }
        public IActionResult RedakteEt(int id)
        {
            Product product = null;
            using (var connection = new SqliteConnection("Data Source=SatisDB.db"))
            {
                connection.Open();
                var cmd = new SqliteCommand("SELECT Id, Name, Price, Stock, Category FROM Products WHERE Id = @id", connection);
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        product = new Product
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Price = reader.GetDouble(2),
                            Stock = reader.GetInt32(3),
                            Category = reader.IsDBNull(4) ? "" : reader.GetString(4)
                        };
                    }
                }
            }
            return View(product);
        }
        [HttpPost]
        public IActionResult RedakteEt(Product product)
        {
            using (var connection = new SqliteConnection("Data Source=SatisDB.db"))
            {
                connection.Open();
                var cmd = new SqliteCommand("UPDATE Products SET Name=@n, Price=@p, Stock=@s, Category=@c WHERE Id=@id", connection);
                cmd.Parameters.AddWithValue("@n", product.Name);
                cmd.Parameters.AddWithValue("@p", product.Price);
                cmd.Parameters.AddWithValue("@s", product.Stock);
                cmd.Parameters.AddWithValue("@c", product.Category);
                cmd.Parameters.AddWithValue("@id", product.Id);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult Sil(int id)
        {
            using (var connection = new SqliteConnection("Data Source=SatisDB.db"))
            {
                connection.Open();
                var cmd = new SqliteCommand("DELETE FROM Products WHERE Id = @id", connection);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
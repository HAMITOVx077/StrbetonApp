using Microsoft.Data.Sqlite;
using StrbetonApp.Models;
using System.Security.Cryptography;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using SixLabors.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace StrbetonApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string UserLogin;
        public MainWindow(string login)
        {
            this.UserLogin = login;

            InitializeComponent();

            LoadUserData();

            LoadWarehouses();
            LoadProducts();

            LoadUserOrdersByLogin(UserLogin);
        }
        private void LoadWarehouses()
        {
            List<Warehouse> warehouses = new List<Warehouse>();

            using (var connection = Database.GetConnection())
            {
                connection.Open();

                var query = "SELECT id, name FROM Warehouse";
                using (var command = new SqliteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            warehouses.Add(new Warehouse
                            {
                                Id = reader.GetInt32(0),
                                WarehouseName = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            WarehouseFilterComboBox.ItemsSource = warehouses;
            WarehouseFilterComboBox.DisplayMemberPath = "WarehouseName";
            WarehouseFilterComboBox.SelectedValuePath = "Id";
        }
        private void WarehouseFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedWarehouseId = WarehouseFilterComboBox.SelectedValue as int?;

            if (selectedWarehouseId.HasValue)
            {
                LoadProducts(selectedWarehouseId.Value);
            }
            else
            {
                LoadProducts();
            }
        }
        private void LoadProducts(int? warehouseId = null)
        {
            List<Product> products = new List<Product>();

            using (var connection = Database.GetConnection())
            {
                connection.Open();

                string query = @"
            SELECT 
                Product.id, 
                Product.name, 
                Product.price, 
                Warehouse.name AS WarehouseName,
                Product.image_path
            FROM Product
            LEFT JOIN Warehouse ON Product.warehouse_id = Warehouse.id";

                if (warehouseId.HasValue)
                {
                    query += " WHERE Product.warehouse_id = @WarehouseId";
                }

                using (var command = new SqliteCommand(query, connection))
                {
                    if (warehouseId.HasValue)
                    {
                        command.Parameters.AddWithValue("@WarehouseId", warehouseId.Value);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var product = new Product
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Price = reader.GetDecimal(2),
                                Warehouse = new Warehouse
                                {
                                    WarehouseName = reader.GetString(3)
                                },
                                ImagePath = reader.IsDBNull(4) ? null : reader.GetString(4)
                            };
                            products.Add(product);
                        }
                    }
                }
            }

            ProductCatalogDataGrid.ItemsSource = products;
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWind = new LoginWindow();
            loginWind.Show();
            this.Close();
        }
        private void LoadUserData()
        {
            using (var connection = Database.GetConnection())
            {
                connection.Open();
                var command = new SqliteCommand(
                    "SELECT u.first_name, u.last_name, u.phone_number, r.name AS role " +
                    "FROM User u " +
                    "JOIN Role r ON u.role_id = r.id " +
                    "JOIN Auth a ON u.auth_id = a.id " +
                    "WHERE a.login = @login", connection);

                command.Parameters.AddWithValue("@login", UserLogin);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        FirstNameText.Text = reader["first_name"].ToString();
                        LastNameText.Text = reader["last_name"].ToString();
                        PhoneNumberText.Text = reader["phone_number"].ToString();
                        RoleText.Text = reader["role"].ToString();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось загрузить данные пользователя.");
                    }
                }
            }
        }

        private void ChangeUserButton_Click(object sender, RoutedEventArgs e)
        {
            var editProfileWindow = new EditUserProfile(UserLogin, FirstNameText.Text, LastNameText.Text, PhoneNumberText.Text);
            editProfileWindow.ShowDialog();
            LoadUserData();
        }

        private void ProductAddButton_Click(object sender, RoutedEventArgs e)
        {
            var addProductWindow = new AddProductWindow();
            addProductWindow.ShowDialog();
            LoadProducts();
        }

        private void UserAddButton_Click(object sender, RoutedEventArgs e)
        {
            var addUserWindow = new AddUserWindow();
            addUserWindow.ShowDialog();
        }
        private void PlaceOrderButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text;
            string productNumber = ProductNumberTextBox.Text;
            string quantityText = QuantityTextBox.Text;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(productNumber) || string.IsNullOrWhiteSpace(quantityText))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(quantityText, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите правильное количество товара.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var connection = Database.GetConnection())
            {
                connection.Open();

                var userQuery = "SELECT id FROM User WHERE auth_id = (SELECT id FROM Auth WHERE login = @login)";
                var userCommand = new SqliteCommand(userQuery, connection);
                userCommand.Parameters.AddWithValue("@login", login);
                var userId = userCommand.ExecuteScalar();

                if (userId == null)
                {
                    MessageBox.Show("Пользователь не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var productQuery = "SELECT id FROM Product WHERE name = @productName";
                var productCommand = new SqliteCommand(productQuery, connection);
                productCommand.Parameters.AddWithValue("@productName", productNumber);
                var productId = productCommand.ExecuteScalar();

                if (productId == null)
                {
                    MessageBox.Show("Продукт не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var orderQuery = @"
            INSERT INTO 'Order' (quantity, status_id, product_id, user_id) 
            VALUES (@quantity, @status_id, @productId, @userId)";

                var orderCommand = new SqliteCommand(orderQuery, connection);
                orderCommand.Parameters.AddWithValue("@quantity", quantity);
                orderCommand.Parameters.AddWithValue("@status_id", 3);
                orderCommand.Parameters.AddWithValue("@productId", productId);
                orderCommand.Parameters.AddWithValue("@userId", userId);

                orderCommand.ExecuteNonQuery();

                MessageBox.Show("Заказ успешно оформлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearFields();
            }
        }

        private void ClearFields()
        {
            LoginTextBox.Clear();
            ProductNumberTextBox.Clear();
            QuantityTextBox.Clear();
        }
        private void LoadUserOrdersByLogin(string login)
        {
            List<Order> orders = new List<Order>();
            int userId = -1;

            using (var connection = Database.GetConnection())
            {
                connection.Open();

                var userQuery = "SELECT id FROM \"User\" WHERE auth_id = (SELECT id FROM Auth WHERE login = @login)";
                using (var command = new SqliteCommand(userQuery, connection))
                {
                    command.Parameters.AddWithValue("@login", login);

                    object result = command.ExecuteScalar();
                    if (result != null)
                    {
                        userId = Convert.ToInt32(result);
                    }
                    else
                    {
                        MessageBox.Show("Пользователь с таким логином не найден.");
                        return;
                    }
                }

                var ordersQuery = @"
            SELECT 
                o.id, 
                o.quantity, 
                o.status_id, 
                os.name AS StatusName, 
                p.id AS ProductId, 
                p.name AS ProductName, 
                p.price AS ProductPrice,
                p.warehouse_id,
                u.id AS UserId,
                u.first_name, 
                u.last_name, 
                u.phone_number
            FROM ""Order"" o
                    JOIN Product p ON o.product_id = p.id
                    JOIN ""OrderStatus"" os ON o.status_id = os.id
                    JOIN ""User"" u ON o.user_id = u.id
                    WHERE o.user_id = @userId";

                using (var command = new SqliteCommand(ordersQuery, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var order = new Order
                            {
                                Id = reader.GetInt32(0),
                                Quantity = reader.GetInt32(1),
                                StatusId = reader.GetInt32(2),
                                Status = new OrderStatus
                                {
                                    Name = reader.GetString(3)
                                },
                                ProductId = reader.GetInt32(4),
                                Product = new Product
                                {
                                    Id = reader.GetInt32(4),
                                    Name = reader.GetString(5),
                                    Price = reader.GetDecimal(6),
                                    WarehouseId = reader.GetInt32(7)
                                },
                                UserId = reader.GetInt32(8),
                                User = new User
                                {
                                    Id = reader.GetInt32(8),
                                    FirstName = reader.GetString(9),
                                    LastName = reader.GetString(10),
                                    PhoneNumber = reader.GetString(11)
                                }
                            };
                            orders.Add(order);
                        }
                    }
                }
            }

            OrdersDataGrid.ItemsSource = orders;
        }
        private void ExportToExcelButton_Click(object sender, RoutedEventArgs e)
        {
            List<UserAuthData> users = new List<UserAuthData>();

            using (var connection = Database.GetConnection())
            {
                connection.Open();

                var query = @"
            SELECT u.first_name, u.last_name, u.phone_number, r.name AS role, a.login
            FROM User u
            JOIN Auth a ON u.auth_id = a.id
            JOIN Role r ON u.role_id = r.id";

                using (var command = new SqliteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new UserAuthData
                            {
                                FirstName = reader.GetString(0),
                                LastName = reader.GetString(1),
                                PhoneNumber = reader.GetString(2),
                                Role = reader.GetString(3),
                                Login = reader.GetString(4)
                            });
                        }
                    }
                }
            }

            var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("Users");

            worksheet.Cell(1, 1).Value = "Имя";
            worksheet.Cell(1, 2).Value = "Фамилия";
            worksheet.Cell(1, 3).Value = "Телефон";
            worksheet.Cell(1, 4).Value = "Роль";
            worksheet.Cell(1, 5).Value = "Логин";

            for (int i = 0; i < users.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = users[i].FirstName;
                worksheet.Cell(i + 2, 2).Value = users[i].LastName;
                worksheet.Cell(i + 2, 3).Value = users[i].PhoneNumber;
                worksheet.Cell(i + 2, 4).Value = users[i].Role;
                worksheet.Cell(i + 2, 5).Value = users[i].Login;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog();
            saveDialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
            if (saveDialog.ShowDialog() == true)
            {
                workbook.SaveAs(saveDialog.FileName);
                MessageBox.Show("Экспорт завершен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public class UserAuthData
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string PhoneNumber { get; set; }
            public string Role { get; set; }
            public string Login { get; set; }
        }
        private void ExportToPdfButton_Click(object sender, RoutedEventArgs e)
        {
            List<UserAuthData> users = new List<UserAuthData>();

            using (var connection = Database.GetConnection())
            {
                connection.Open();

                var query = @"
            SELECT u.first_name, u.last_name, u.phone_number, r.name AS role, a.login
            FROM User u
            JOIN Auth a ON u.auth_id = a.id
            JOIN Role r ON u.role_id = r.id";

                using (var command = new SqliteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new UserAuthData
                            {
                                FirstName = reader.GetString(0),
                                LastName = reader.GetString(1),
                                PhoneNumber = reader.GetString(2),
                                Role = reader.GetString(3),
                                Login = reader.GetString(4)
                            });
                        }
                    }
                }
            }

            PdfDocument document = new PdfDocument();
            document.Info.Title = "Exported Users";

            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XFont font = new XFont("Arial", 12);

            gfx.DrawString("Имя", font, XBrushes.Black, new XPoint(40, 50));
            gfx.DrawString("Фамилия", font, XBrushes.Black, new XPoint(120, 50));
            gfx.DrawString("Телефон", font, XBrushes.Black, new XPoint(200, 50));
            gfx.DrawString("Роль", font, XBrushes.Black, new XPoint(280, 50));
            gfx.DrawString("Логин", font, XBrushes.Black, new XPoint(360, 50));

            int yPosition = 80;
            foreach (var user in users)
            {
                gfx.DrawString(user.FirstName, font, XBrushes.Black, new XPoint(40, yPosition));
                gfx.DrawString(user.LastName, font, XBrushes.Black, new XPoint(120, yPosition));
                gfx.DrawString(user.PhoneNumber, font, XBrushes.Black, new XPoint(200, yPosition));
                gfx.DrawString(user.Role, font, XBrushes.Black, new XPoint(280, yPosition));
                gfx.DrawString(user.Login, font, XBrushes.Black, new XPoint(360, yPosition));

                yPosition += 20;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog();
            saveDialog.Filter = "PDF Files (*.pdf)|*.pdf";
            if (saveDialog.ShowDialog() == true)
            {
                document.Save(saveDialog.FileName);
                MessageBox.Show("Экспорт завершен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
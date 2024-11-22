using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace StrbetonApp
{
    /// <summary>
    /// Логика взаимодействия для AddProductWindow.xaml
    /// </summary>
    public partial class AddProductWindow : Window
    {
        private string imagePath;

        public AddProductWindow()
        {
            InitializeComponent();
            LoadWarehouses();
        }

        private void LoadWarehouses()
        {
            using (var connection = Database.GetConnection())
            {
                connection.Open();
                var query = "SELECT id, name FROM Warehouse";
                var command = new SqliteCommand(query, connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        WarehouseComboBox.Items.Add(new { Id = reader.GetInt32(0), Name = reader.GetString(1) });
                    }
                }
            }
            WarehouseComboBox.DisplayMemberPath = "Name";
            WarehouseComboBox.SelectedValuePath = "Id";
        }
        private void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;

                string fileName = System.IO.Path.GetFileName(selectedFilePath);

                string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string imagesDirectory = Path.Combine(projectDirectory, "Images");

                if (!Directory.Exists(imagesDirectory))
                {
                    Directory.CreateDirectory(imagesDirectory);
                }

                imagePath = Path.Combine(imagesDirectory, fileName);

                File.Copy(selectedFilePath, imagePath, true);

                SelectedImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
            }
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            string name = ProductNameTextBox.Text;
            if (decimal.TryParse(ProductPriceTextBox.Text, out decimal price) && WarehouseComboBox.SelectedValue != null)
            {
                int warehouseId = (int)WarehouseComboBox.SelectedValue;

                string relativeImagePath = "Images/" + Path.GetFileName(imagePath);

                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    var query = "INSERT INTO Product (name, price, warehouse_id, image_path) VALUES (@name, @price, @warehouseId, @imagePath)";
                    var command = new SqliteCommand(query, connection);
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@price", price);
                    command.Parameters.AddWithValue("@warehouseId", warehouseId);
                    command.Parameters.AddWithValue("@imagePath", relativeImagePath); 
                    command.ExecuteNonQuery();
                }

                MessageBox.Show("Товар успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            else
            {
                MessageBox.Show("Проверьте правильность заполнения полей.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
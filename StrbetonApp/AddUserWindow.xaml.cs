using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
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

namespace StrbetonApp
{
    /// <summary>
    /// Логика взаимодействия для AddUserWindow.xaml
    /// </summary>
    public partial class AddUserWindow : Window
    {
        public AddUserWindow()
        {
            InitializeComponent();
            LoadRoles();
        }

        private void LoadRoles()
        {
            RoleComboBox.Items.Add("Сотрудник");
            RoleComboBox.Items.Add("Пользователь");
            RoleComboBox.Items.Add("Админ");
            RoleComboBox.SelectedIndex = 0;
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            string firstName = FirstNameTextBox.Text;
            string lastName = LastNameTextBox.Text;
            string phoneNumber = PhoneNumberTextBox.Text;
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;
            string role = RoleComboBox.SelectedItem.ToString();

            if (!string.IsNullOrWhiteSpace(firstName) &&
                !string.IsNullOrWhiteSpace(lastName) &&
                !string.IsNullOrWhiteSpace(phoneNumber) &&
                !string.IsNullOrWhiteSpace(login) &&
                !string.IsNullOrWhiteSpace(password))
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();

                    var authQuery = @"
                INSERT INTO Auth (login, password) 
                VALUES (@login, @password); 
                SELECT last_insert_rowid()";

                    var authCommand = new SqliteCommand(authQuery, connection);
                    authCommand.Parameters.AddWithValue("@login", login);
                    authCommand.Parameters.AddWithValue("@password", password);
                    long authId = (long)authCommand.ExecuteScalar();

                    int roleId = role switch
                    {
                        "Сотрудник" => 1,
                        "Пользователь" => 2,
                        "Админ" => 3,
                        _ => throw new InvalidOperationException("Неверная роль")
                    };

                    var userQuery = @"
                INSERT INTO User (first_name, last_name, phone_number, role_id, auth_id) 
                VALUES (@first_name, @last_name, @phone_number, @role_id, @auth_id)";

                    var userCommand = new SqliteCommand(userQuery, connection);

                    userCommand.Parameters.AddWithValue("@first_name", firstName);
                    userCommand.Parameters.AddWithValue("@last_name", lastName);
                    userCommand.Parameters.AddWithValue("@phone_number", phoneNumber);
                    userCommand.Parameters.AddWithValue("@role_id", roleId);
                    userCommand.Parameters.AddWithValue("@auth_id", authId);

                    userCommand.ExecuteNonQuery();

                    MessageBox.Show("Пользователь успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                }
            }
            else
            {
                MessageBox.Show("Заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

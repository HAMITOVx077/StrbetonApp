using Microsoft.Data.Sqlite;
using StrbetonApp.Models;
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
    /// Логика взаимодействия для EditUserProfile.xaml
    /// </summary>
    public partial class EditUserProfile : Window
    {
        private string UserLogin;

        public EditUserProfile(string login, string firstName, string lastName, string phoneNumber)
        {
            InitializeComponent();

            FirstNameTextBox.Text = firstName;
            LastNameTextBox.Text = lastName;
            PhoneNumberTextBox.Text = phoneNumber;
            UserLogin = login;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string newFirstName = FirstNameTextBox.Text.Trim();
            string newLastName = LastNameTextBox.Text.Trim();
            string newPhoneNumber = PhoneNumberTextBox.Text.Trim();
            string newPassword = PasswordTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(newFirstName) ||
                string.IsNullOrWhiteSpace(newLastName) ||
                string.IsNullOrWhiteSpace(newPhoneNumber) ||
                string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("Все поля должны быть заполнены.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var connection = Database.GetConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var updateUserCommand = new SqliteCommand(
                            @"UPDATE User
                              SET first_name = @firstName, 
                                  last_name = @lastName, 
                                  phone_number = @phoneNumber
                              WHERE auth_id = (SELECT id FROM Auth WHERE login = @originalLogin)", connection, transaction);

                        updateUserCommand.Parameters.AddWithValue("@firstName", newFirstName);
                        updateUserCommand.Parameters.AddWithValue("@lastName", newLastName);
                        updateUserCommand.Parameters.AddWithValue("@phoneNumber", newPhoneNumber);
                        updateUserCommand.Parameters.AddWithValue("@originalLogin", UserLogin);

                        int rowsAffectedUser = updateUserCommand.ExecuteNonQuery();

                        var updateAuthCommand = new SqliteCommand(
                            @"UPDATE Auth
                              SET password = @newPassword
                              WHERE login = @originalLogin", connection, transaction);

                        updateAuthCommand.Parameters.AddWithValue("@newPassword", newPassword);
                        updateAuthCommand.Parameters.AddWithValue("@originalLogin", UserLogin);

                        int rowsAffectedAuth = updateAuthCommand.ExecuteNonQuery();

                        if (rowsAffectedUser > 0 && rowsAffectedAuth > 0)
                        {
                            transaction.Commit();
                            MessageBox.Show("Данные успешно сохранены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            throw new Exception("Не удалось обновить данные. Проверьте корректность логина.");
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            this.Close();
        }
    }
}
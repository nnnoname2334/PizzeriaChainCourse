using PizzeriaChainCourse;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace PizzeriaChainCourse
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        // В БД пароли хешировались через HASHBYTES('SHA2_256', N'...'),
        // а N'...' хешируется как UTF-16, поэтому тут Encoding.Unicode
        private static byte[] Hash(string password)
        {
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.Unicode.GetBytes(password));
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordInput.Password;

            if (login == "" || password == "")
            {
                ErrorText.Text = "Введите логин и пароль";
                return;
            }

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    var employee = db.Employees.FirstOrDefault(x => x.Login == login);

                    if (employee == null || !employee.PasswordHash.SequenceEqual(Hash(password)))
                    {
                        ErrorText.Text = "Неверный логин или пароль";
                        return;
                    }

                    Session.EmployeeId = employee.EmployeeId;
                    Session.PizzeriaId = employee.PizzeriaId;
                    Session.FullName = employee.LastName + " " + employee.FirstName;
                    Session.Role = employee.Role;
                }

                new MainWindow().Show();
                Close();
            }
            catch (Exception ex)
            {
                ErrorText.Text = "Ошибка подключения к базе данных: " + ex.Message;
            }
        }
    }
}
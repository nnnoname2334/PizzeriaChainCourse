using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace PizzeriaChainCourse
{
    public partial class CustomerWindow : Window
    {
        private readonly int? _id;

        public CustomerWindow(int? id)
        {
            InitializeComponent();
            _id = id;

            if (id == null)
            {
                Title = "Новый клиент";
            }
            else
            {
                Title = "Изменение клиента";
                using (var db = new PizzeriaChainDBEntities())
                {
                    var customer = db.Customers.Find(id.Value);
                    NameBox.Text = customer.FirstName;
                    PhoneBox.Text = customer.Phone;
                }
            }
        }

        // Приводит телефон к виду +7XXXXXXXXXX, возвращает null, если формат неверный
        private static string NormalizePhone(string input)
        {
            string digits = new string(input.Where(char.IsDigit).ToArray());
            if (digits.Length == 11 && (digits[0] == '7' || digits[0] == '8'))
                return "+7" + digits.Substring(1);
            if (digits.Length == 10)
                return "+7" + digits;
            return null;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();

            if (name == "" || !Regex.IsMatch(name, @"^[\p{L}\- ]+$"))
            {
                MessageBox.Show("Введите имя: только буквы, пробел или дефис.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string phone = NormalizePhone(PhoneBox.Text);
            if (phone == null)
            {
                MessageBox.Show("Введите телефон в формате +7 900 123-45-67 (10 цифр после +7).", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    int currentId = _id ?? 0;
                    if (db.Customers.Any(x => x.Phone == phone && x.CustomerId != currentId))
                    {
                        MessageBox.Show("Клиент с таким телефоном уже зарегистрирован.", "Проверка данных",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    Customers customer;
                    if (_id == null)
                    {
                        customer = db.Customers.Create();
                        customer.RegisteredAt = DateTime.Today;
                        db.Customers.Add(customer);
                    }
                    else
                    {
                        customer = db.Customers.Find(_id.Value);
                    }

                    customer.FirstName = name;
                    customer.Phone = phone;
                    db.SaveChanges();
                }
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить данные: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
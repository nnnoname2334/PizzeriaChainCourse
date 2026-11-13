using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace PizzeriaChainCourse
{
    public partial class EmployeeWindow : Window
    {
        private class RoleOption
        {
            public string Code { get; set; }
            public string Name { get; set; }
        }

        private readonly int? _id;
        private readonly bool _hadAccess;

        public EmployeeWindow(int? id)
        {
            InitializeComponent();
            _id = id;

            RoleBox.ItemsSource = new List<RoleOption>
            {
                new RoleOption { Code = "admin", Name = "Администратор сети" },
                new RoleOption { Code = "manager", Name = "Менеджер пиццерии" },
                new RoleOption { Code = "cashier", Name = "Кассир" }
            };

            using (var db = new PizzeriaChainDBEntities())
            {
                PizzeriaBox.ItemsSource = db.Pizzerias.OrderBy(x => x.Name).ToList();
                PositionBox.ItemsSource = db.Positions.OrderBy(x => x.Name).ToList();

                if (id == null)
                {
                    Title = "Новый сотрудник";
                    HireDatePicker.SelectedDate = DateTime.Today;
                }
                else
                {
                    Title = "Изменение сотрудника";
                    var employee = db.Employees.Find(id.Value);
                    PizzeriaBox.SelectedValue = employee.PizzeriaId;
                    PositionBox.SelectedValue = employee.PositionId;
                    LastNameBox.Text = employee.LastName;
                    FirstNameBox.Text = employee.FirstName;
                    MiddleNameBox.Text = employee.MiddleName;
                    PhoneBox.Text = employee.Phone;
                    HireDatePicker.SelectedDate = employee.HireDate;

                    _hadAccess = employee.Login != null;
                    if (_hadAccess)
                    {
                        AccessBox.IsChecked = true;
                        LoginBox.Text = employee.Login;
                        RoleBox.SelectedValue = employee.Role;
                        PasswordLabel.Text = "Новый пароль (оставьте пустым, чтобы не менять)";
                    }
                }
            }

            // Свою учётную запись нельзя отключить и нельзя лишить роли администратора
            if (id != null && id.Value == Session.EmployeeId)
            {
                AccessBox.IsEnabled = false;
                RoleBox.IsEnabled = false;
            }
        }

        private void Access_Changed(object sender, RoutedEventArgs e)
        {
            AccessPanel.Visibility = AccessBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void Warn(string text)
        {
            MessageBox.Show(text, "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string lastName = LastNameBox.Text.Trim();
            string firstName = FirstNameBox.Text.Trim();
            string middleName = MiddleNameBox.Text.Trim();

            if (PizzeriaBox.SelectedValue == null || PositionBox.SelectedValue == null)
            {
                Warn("Выберите пиццерию и должность.");
                return;
            }
            if (!ValidationHelper.IsName(lastName))
            {
                Warn("Введите фамилию: только буквы, пробел или дефис.");
                return;
            }
            if (!ValidationHelper.IsName(firstName))
            {
                Warn("Введите имя: только буквы, пробел или дефис.");
                return;
            }
            if (middleName != "" && !ValidationHelper.IsName(middleName))
            {
                Warn("Отчество может содержать только буквы, пробел или дефис.");
                return;
            }

            string phone = ValidationHelper.NormalizePhone(PhoneBox.Text);
            if (phone == null)
            {
                Warn("Введите телефон в формате +7 900 123-45-67 (10 цифр после +7).");
                return;
            }

            DateTime? hireDate = HireDatePicker.SelectedDate;
            if (hireDate == null || hireDate.Value.Date > DateTime.Today)
            {
                Warn("Укажите дату приёма: она не может быть в будущем.");
                return;
            }

            bool access = AccessBox.IsChecked == true;
            string login = LoginBox.Text.Trim();
            string password = PasswordInput.Password;
            string role = RoleBox.SelectedValue as string;

            if (access)
            {
                if (!Regex.IsMatch(login, @"^[A-Za-z0-9_]{3,50}$"))
                {
                    Warn("Логин: от 3 до 50 символов, только латинские буквы, цифры и знак подчёркивания.");
                    return;
                }
                if (role == null)
                {
                    Warn("Выберите роль для входа в систему.");
                    return;
                }
                bool passwordRequired = !_hadAccess;
                if (passwordRequired && password == "")
                {
                    Warn("Задайте пароль для нового пользователя.");
                    return;
                }
                if (password != "" && password.Length < 6)
                {
                    Warn("Пароль должен быть не короче 6 символов.");
                    return;
                }
            }
            else if (_hadAccess)
            {
                if (MessageBox.Show("У сотрудника будет отключён вход в систему. Продолжить?", "Подтверждение",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
            }

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    int currentId = _id ?? 0;
                    if (access && db.Employees.Any(x => x.Login == login && x.EmployeeId != currentId))
                    {
                        Warn("Пользователь с таким логином уже существует.");
                        return;
                    }

                    Employees employee;
                    if (_id == null)
                    {
                        employee = db.Employees.Create();
                        db.Employees.Add(employee);
                    }
                    else
                    {
                        employee = db.Employees.Find(_id.Value);
                    }

                    employee.PizzeriaId = (int)PizzeriaBox.SelectedValue;
                    employee.PositionId = (int)PositionBox.SelectedValue;
                    employee.LastName = lastName;
                    employee.FirstName = firstName;
                    employee.MiddleName = middleName == "" ? null : middleName;
                    employee.Phone = phone;
                    employee.HireDate = hireDate.Value.Date;

                    if (access)
                    {
                        employee.Login = login;
                        employee.Role = role;
                        if (password != "")
                            employee.PasswordHash = PasswordHelper.Hash(password);
                    }
                    else
                    {
                        employee.Login = null;
                        employee.Role = null;
                        employee.PasswordHash = null;
                    }

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
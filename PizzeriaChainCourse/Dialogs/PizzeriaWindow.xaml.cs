using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace PizzeriaChainCourse
{
    public partial class PizzeriaWindow : Window
    {
        private static readonly string[] TimeFormats = { @"h\:mm", @"hh\:mm" };
        private readonly int? _id;

        public PizzeriaWindow(int? id)
        {
            InitializeComponent();
            _id = id;

            if (id == null)
            {
                Title = "Новая пиццерия";
                OpenBox.Text = "10:00";
                CloseBox.Text = "23:00";
            }
            else
            {
                Title = "Изменение пиццерии";
                using (var db = new PizzeriaChainDBEntities())
                {
                    var pizzeria = db.Pizzerias.Find(id.Value);
                    NameBox.Text = pizzeria.Name;
                    AddressBox.Text = pizzeria.Address;
                    PhoneBox.Text = pizzeria.Phone;
                    OpenBox.Text = pizzeria.OpenTime.ToString(@"hh\:mm");
                    CloseBox.Text = pizzeria.CloseTime.ToString(@"hh\:mm");
                }
            }
        }

        private static void Warn(string text)
        {
            MessageBox.Show(text, "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string address = AddressBox.Text.Trim();

            if (name == "")
            {
                Warn("Введите название пиццерии.");
                return;
            }
            if (address == "")
            {
                Warn("Введите адрес.");
                return;
            }

            string phone = ValidationHelper.NormalizePhone(PhoneBox.Text);
            if (phone == null)
            {
                Warn("Введите телефон в формате +7 495 111-00-01 (10 цифр после +7).");
                return;
            }

            TimeSpan open, close;
            if (!TimeSpan.TryParseExact(OpenBox.Text.Trim(), TimeFormats, CultureInfo.InvariantCulture, out open)
                || !TimeSpan.TryParseExact(CloseBox.Text.Trim(), TimeFormats, CultureInfo.InvariantCulture, out close))
            {
                Warn("Время нужно вводить в формате чч:мм, например 10:00.");
                return;
            }
            if (open == close)
            {
                Warn("Время открытия и закрытия не должно совпадать.");
                return;
            }

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    int currentId = _id ?? 0;
                    if (db.Pizzerias.Any(x => x.Name == name && x.PizzeriaId != currentId))
                    {
                        Warn("Пиццерия с таким названием уже есть.");
                        return;
                    }

                    var pizzeria = _id == null ? db.Pizzerias.Create() : db.Pizzerias.Find(_id.Value);
                    if (_id == null)
                        db.Pizzerias.Add(pizzeria);

                    pizzeria.Name = name;
                    pizzeria.Address = address;
                    pizzeria.Phone = phone;
                    pizzeria.OpenTime = open;
                    pizzeria.CloseTime = close;

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
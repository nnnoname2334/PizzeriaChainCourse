using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace PizzeriaChainCourse
{
    public partial class OrderWindow : Window
    {
        private class PriceOption
        {
            public int MenuItemPriceId { get; set; }
            public string Name { get; set; }
            public string SizeName { get; set; }
            public decimal Price { get; set; }

            public string Display
            {
                get
                {
                    return Name + (SizeName == "Стандарт" ? "" : ", " + SizeName)
                           + " — " + Price.ToString("0.##") + " ₽";
                }
            }
        }

        private class LineRow
        {
            public int MenuItemPriceId { get; set; }
            public string Name { get; set; }
            public string SizeName { get; set; }
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; }
            public decimal Sum { get { return UnitPrice * Quantity; } }
        }

        private readonly int? _id;
        private readonly ObservableCollection<LineRow> _lines = new ObservableCollection<LineRow>();

        public OrderWindow(int? id)
        {
            InitializeComponent();
            _id = id;
            LinesGrid.ItemsSource = _lines;
            OrderTypeBox.ItemsSource = new[] { "В зале", "На вынос", "Доставка" };

            using (var db = new PizzeriaChainDBEntities())
            {
                PizzeriaBox.ItemsSource = db.Pizzerias.OrderBy(x => x.Name).ToList();
                StatusBox.ItemsSource = db.OrderStatuses.OrderBy(x => x.StatusId).ToList();

                var customers = new List<object> { new { CustomerId = 0, Display = "Без клиента" } };
                customers.AddRange(db.Customers.OrderBy(x => x.FirstName).ToList()
                    .Select(c => (object)new { CustomerId = c.CustomerId, Display = c.FirstName + " — " + c.Phone }));
                CustomerBox.ItemsSource = customers;

                MenuPriceBox.ItemsSource = db.MenuItemPrices
                    .Where(p => p.MenuItems.IsAvailable)
                    .OrderBy(p => p.MenuItems.Name).ThenBy(p => p.SizeId)
                    .Select(p => new PriceOption
                    {
                        MenuItemPriceId = p.MenuItemPriceId,
                        Name = p.MenuItems.Name,
                        SizeName = p.Sizes.Name,
                        Price = p.Price
                    })
                    .ToList();

                if (id == null)
                {
                    Title = "Новый заказ";
                    PizzeriaBox.SelectedValue = Session.PizzeriaId;
                    CustomerBox.SelectedIndex = 0;
                    OrderTypeBox.SelectedIndex = 0;
                    StatusBox.SelectedIndex = 0; // первый статус в справочнике — «Принят»
                }
                else
                {
                    int orderId = id.Value;
                    Title = "Заказ № " + orderId;
                    var order = db.Orders.Find(orderId);
                    PizzeriaBox.SelectedValue = order.PizzeriaId;
                    CustomerBox.SelectedValue = order.CustomerId ?? 0;
                    OrderTypeBox.SelectedItem = order.OrderType;
                    StatusBox.SelectedValue = order.StatusId;

                    var lines = db.OrderItems
                        .Where(i => i.OrderId == orderId)
                        .Select(i => new LineRow
                        {
                            MenuItemPriceId = i.MenuItemPriceId,
                            Name = i.MenuItemPrices.MenuItems.Name,
                            SizeName = i.MenuItemPrices.Sizes.Name,
                            UnitPrice = i.UnitPrice,
                            Quantity = i.Quantity
                        })
                        .ToList();
                    foreach (var line in lines)
                        _lines.Add(line);
                }
            }

            // Выбирать пиццерию может только администратор
            PizzeriaBox.IsEnabled = Session.Role == "admin";
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            TotalText.Text = "Итого: " + _lines.Sum(l => l.Sum).ToString("N2") + " ₽";
        }

        private void AddLine_Click(object sender, RoutedEventArgs e)
        {
            var option = MenuPriceBox.SelectedItem as PriceOption;
            if (option == null)
            {
                MessageBox.Show("Выберите позицию меню.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int quantity;
            if (!int.TryParse(QtyBox.Text, out quantity) || quantity < 1 || quantity > 99)
            {
                MessageBox.Show("Количество должно быть целым числом от 1 до 99.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existing = _lines.FirstOrDefault(l => l.MenuItemPriceId == option.MenuItemPriceId);
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                _lines.Add(new LineRow
                {
                    MenuItemPriceId = option.MenuItemPriceId,
                    Name = option.Name,
                    SizeName = option.SizeName,
                    UnitPrice = option.Price,
                    Quantity = quantity
                });
            }

            LinesGrid.Items.Refresh();
            QtyBox.Text = "1";
            UpdateTotal();
        }

        private void RemoveLine_Click(object sender, RoutedEventArgs e)
        {
            var line = LinesGrid.SelectedItem as LineRow;
            if (line == null)
            {
                MessageBox.Show("Выберите позицию в таблице.", "Удаление позиции", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _lines.Remove(line);
            UpdateTotal();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (PizzeriaBox.SelectedValue == null || OrderTypeBox.SelectedItem == null || StatusBox.SelectedValue == null)
            {
                MessageBox.Show("Заполните пиццерию, тип заказа и статус.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_lines.Count == 0)
            {
                MessageBox.Show("Добавьте в заказ хотя бы одну позицию.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    Orders order;
                    if (_id == null)
                    {
                        order = db.Orders.Create();
                        order.CreatedAt = DateTime.Now;
                        order.EmployeeId = Session.EmployeeId;
                        db.Orders.Add(order);
                    }
                    else
                    {
                        int orderId = _id.Value;
                        order = db.Orders.Find(orderId);
                        // Старые позиции заменяются новым составом
                        db.OrderItems.RemoveRange(db.OrderItems.Where(i => i.OrderId == orderId).ToList());
                    }

                    int customerId = (int)CustomerBox.SelectedValue;
                    order.PizzeriaId = (int)PizzeriaBox.SelectedValue;
                    order.CustomerId = customerId == 0 ? (int?)null : customerId;
                    order.OrderType = (string)OrderTypeBox.SelectedItem;
                    order.StatusId = (int)StatusBox.SelectedValue;
                    order.TotalAmount = _lines.Sum(l => l.Sum);

                    foreach (var line in _lines)
                    {
                        order.OrderItems.Add(new OrderItems
                        {
                            MenuItemPriceId = line.MenuItemPriceId,
                            Quantity = line.Quantity,
                            UnitPrice = line.UnitPrice
                        });
                    }

                    db.SaveChanges();
                }
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить заказ: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
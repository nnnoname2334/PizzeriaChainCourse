using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PizzeriaChainCourse
{
    public partial class OrdersPage : Page
    {
        private class OrderRow
        {
            public int OrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public string PizzeriaName { get; set; }
            public string CustomerName { get; set; }
            public string OrderType { get; set; }
            public string StatusName { get; set; }
            public decimal TotalAmount { get; set; }
        }

        private bool _ready;

        public OrdersPage()
        {
            InitializeComponent();
            // Удалять заказы может только администратор
            DeleteButton.Visibility = Session.Role == "admin" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            using (var db = new PizzeriaChainDBEntities())
            {
                var items = new List<object> { new { StatusId = 0, Name = "Все статусы" } };
                items.AddRange(db.OrderStatuses.OrderBy(x => x.StatusId).ToList()
                    .Select(s => (object)new { StatusId = s.StatusId, Name = s.Name }));
                StatusFilter.ItemsSource = items;
                StatusFilter.SelectedIndex = 0;
            }
            _ready = true;
            LoadData();
        }

        private void LoadData()
        {
            if (!_ready) return;

            string search = SearchBox.Text.Trim();
            int statusId = StatusFilter.SelectedValue is int ? (int)StatusFilter.SelectedValue : 0;
            DateTime? from = FromDate.SelectedDate;
            DateTime? to = ToDate.SelectedDate;

            using (var db = new PizzeriaChainDBEntities())
            {
                var query = db.Orders.AsQueryable();

                // Менеджер и кассир видят только заказы своей пиццерии
                if (Session.Role != "admin")
                {
                    int pizzeriaId = Session.PizzeriaId;
                    query = query.Where(x => x.PizzeriaId == pizzeriaId);
                }
                if (statusId != 0)
                    query = query.Where(x => x.StatusId == statusId);
                if (from != null)
                {
                    DateTime fromValue = from.Value.Date;
                    query = query.Where(x => x.CreatedAt >= fromValue);
                }
                if (to != null)
                {
                    DateTime toValue = to.Value.Date.AddDays(1);
                    query = query.Where(x => x.CreatedAt < toValue);
                }
                if (search != "")
                    query = query.Where(x => x.Customers.FirstName.Contains(search) || x.Customers.Phone.Contains(search));

                OrdersGrid.ItemsSource = query
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new OrderRow
                    {
                        OrderId = x.OrderId,
                        CreatedAt = x.CreatedAt,
                        PizzeriaName = x.Pizzerias.Name,
                        CustomerName = x.CustomerId == null ? "—" : x.Customers.FirstName + " (" + x.Customers.Phone + ")",
                        OrderType = x.OrderType,
                        StatusName = x.OrderStatuses.Name,
                        TotalAmount = x.TotalAmount
                    })
                    .ToList();
            }
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            FromDate.SelectedDate = null;
            ToDate.SelectedDate = null;
            StatusFilter.SelectedIndex = 0;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var window = new OrderWindow(null) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                LoadData();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var row = OrdersGrid.SelectedItem as OrderRow;
            if (row == null)
            {
                MessageBox.Show("Выберите заказ в таблице.", "Открытие заказа", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var window = new OrderWindow(row.OrderId) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                LoadData();
        }

        private void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (OrdersGrid.SelectedItem != null)
                Edit_Click(sender, e);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var row = OrdersGrid.SelectedItem as OrderRow;
            if (row == null)
            {
                MessageBox.Show("Выберите заказ в таблице.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Удалить заказ № " + row.OrderId + " вместе с его позициями?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    var order = db.Orders.Find(row.OrderId);
                    db.Orders.Remove(order);
                    db.SaveChanges();
                }
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось удалить заказ: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
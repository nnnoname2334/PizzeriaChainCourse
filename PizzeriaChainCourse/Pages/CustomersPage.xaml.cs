using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PizzeriaChainCourse
{
    public partial class CustomersPage : Page
    {
        private class CustomerRow
        {
            public int CustomerId { get; set; }
            public string FirstName { get; set; }
            public string Phone { get; set; }
            public DateTime RegisteredAt { get; set; }
            public int OrdersCount { get; set; }
        }

        public CustomersPage()
        {
            InitializeComponent();
            // Удалять клиентов могут администратор и менеджер
            DeleteButton.Visibility = Session.Role == "cashier" ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            if (CustomersGrid == null) return;

            string search = SearchBox.Text.Trim();

            using (var db = new PizzeriaChainDBEntities())
            {
                var query = db.Customers.AsQueryable();

                if (search != "")
                    query = query.Where(x => x.FirstName.Contains(search) || x.Phone.Contains(search));

                CustomersGrid.ItemsSource = query
                    .OrderBy(x => x.FirstName)
                    .Select(x => new CustomerRow
                    {
                        CustomerId = x.CustomerId,
                        FirstName = x.FirstName,
                        Phone = x.Phone,
                        RegisteredAt = x.RegisteredAt,
                        OrdersCount = db.Orders.Count(o => o.CustomerId == x.CustomerId)
                    })
                    .ToList();
            }
        }

        private void Search_Changed(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomerWindow(null) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                LoadData();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var row = CustomersGrid.SelectedItem as CustomerRow;
            if (row == null)
            {
                MessageBox.Show("Выберите клиента в таблице.", "Изменение", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var window = new CustomerWindow(row.CustomerId) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                LoadData();
        }

        private void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CustomersGrid.SelectedItem != null)
                Edit_Click(sender, e);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var row = CustomersGrid.SelectedItem as CustomerRow;
            if (row == null)
            {
                MessageBox.Show("Выберите клиента в таблице.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    int customerId = row.CustomerId;
                    if (db.Orders.Any(o => o.CustomerId == customerId))
                    {
                        MessageBox.Show("Нельзя удалить клиента «" + row.FirstName + "»: у него есть заказы.",
                            "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (MessageBox.Show("Удалить клиента «" + row.FirstName + "»?", "Подтверждение",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                        return;

                    var customer = db.Customers.Find(customerId);
                    db.Customers.Remove(customer);
                    db.SaveChanges();
                }
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось удалить клиента: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
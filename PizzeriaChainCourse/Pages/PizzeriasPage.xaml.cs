using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PizzeriaChainCourse
{
    public partial class PizzeriasPage : Page
    {
        private class PizzeriaRow
        {
            public int PizzeriaId { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public string Phone { get; set; }
            public TimeSpan OpenTime { get; set; }
            public TimeSpan CloseTime { get; set; }
            public int EmployeesCount { get; set; }

            public string Hours
            {
                get { return OpenTime.ToString(@"hh\:mm") + " – " + CloseTime.ToString(@"hh\:mm"); }
            }
        }

        public PizzeriasPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            if (PizzeriasGrid == null) return;

            string search = SearchBox.Text.Trim();

            using (var db = new PizzeriaChainDBEntities())
            {
                var query = db.Pizzerias.AsQueryable();

                if (search != "")
                    query = query.Where(x => x.Name.Contains(search) || x.Address.Contains(search));

                PizzeriasGrid.ItemsSource = query
                    .OrderBy(x => x.Name)
                    .Select(x => new PizzeriaRow
                    {
                        PizzeriaId = x.PizzeriaId,
                        Name = x.Name,
                        Address = x.Address,
                        Phone = x.Phone,
                        OpenTime = x.OpenTime,
                        CloseTime = x.CloseTime,
                        EmployeesCount = db.Employees.Count(emp => emp.PizzeriaId == x.PizzeriaId)
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
            var window = new PizzeriaWindow(null) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                LoadData();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var row = PizzeriasGrid.SelectedItem as PizzeriaRow;
            if (row == null)
            {
                MessageBox.Show("Выберите пиццерию в таблице.", "Изменение", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var window = new PizzeriaWindow(row.PizzeriaId) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                LoadData();
        }

        private void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PizzeriasGrid.SelectedItem != null)
                Edit_Click(sender, e);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var row = PizzeriasGrid.SelectedItem as PizzeriaRow;
            if (row == null)
            {
                MessageBox.Show("Выберите пиццерию в таблице.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    int id = row.PizzeriaId;
                    if (db.Employees.Any(x => x.PizzeriaId == id) || db.Orders.Any(x => x.PizzeriaId == id)
                        || db.Stock.Any(x => x.PizzeriaId == id))
                    {
                        MessageBox.Show("Нельзя удалить пиццерию «" + row.Name + "»: к ней привязаны сотрудники, заказы или остатки.",
                            "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (MessageBox.Show("Удалить пиццерию «" + row.Name + "»?", "Подтверждение",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                        return;

                    var pizzeria = db.Pizzerias.Find(id);
                    db.Pizzerias.Remove(pizzeria);
                    db.SaveChanges();
                }
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось удалить пиццерию: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
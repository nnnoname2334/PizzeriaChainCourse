using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PizzeriaChainCourse
{
    public partial class EmployeesPage : Page
    {
        private class EmployeeRow
        {
            public int EmployeeId { get; set; }
            public string LastName { get; set; }
            public string FirstName { get; set; }
            public string MiddleName { get; set; }
            public string PizzeriaName { get; set; }
            public string PositionName { get; set; }
            public string LoginValue { get; set; }
            public string Role { get; set; }

            public string FullName
            {
                get { return LastName + " " + FirstName + (string.IsNullOrEmpty(MiddleName) ? "" : " " + MiddleName); }
            }

            public string Login { get { return LoginValue ?? "—"; } }

            public string RoleName
            {
                get
                {
                    switch (Role)
                    {
                        case "admin": return "Администратор";
                        case "manager": return "Менеджер";
                        case "cashier": return "Кассир";
                        default: return "—";
                    }
                }
            }
        }

        private bool _ready;

        public EmployeesPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            using (var db = new PizzeriaChainDBEntities())
            {
                var pizzerias = new List<object> { new { PizzeriaId = 0, Name = "Все пиццерии" } };
                pizzerias.AddRange(db.Pizzerias.OrderBy(x => x.Name).ToList()
                    .Select(p => (object)new { PizzeriaId = p.PizzeriaId, Name = p.Name }));
                PizzeriaFilter.ItemsSource = pizzerias;
                PizzeriaFilter.SelectedIndex = 0;

                var positions = new List<object> { new { PositionId = 0, Name = "Все должности" } };
                positions.AddRange(db.Positions.OrderBy(x => x.Name).ToList()
                    .Select(p => (object)new { PositionId = p.PositionId, Name = p.Name }));
                PositionFilter.ItemsSource = positions;
                PositionFilter.SelectedIndex = 0;
            }
            _ready = true;
            LoadData();
        }

        private void LoadData()
        {
            if (!_ready) return;

            string search = SearchBox.Text.Trim();
            int pizzeriaId = PizzeriaFilter.SelectedValue is int ? (int)PizzeriaFilter.SelectedValue : 0;
            int positionId = PositionFilter.SelectedValue is int ? (int)PositionFilter.SelectedValue : 0;

            using (var db = new PizzeriaChainDBEntities())
            {
                var query = db.Employees.AsQueryable();

                if (pizzeriaId != 0)
                    query = query.Where(x => x.PizzeriaId == pizzeriaId);
                if (positionId != 0)
                    query = query.Where(x => x.PositionId == positionId);
                if (search != "")
                    query = query.Where(x => x.LastName.Contains(search) || x.FirstName.Contains(search)
                                             || x.Phone.Contains(search) || x.Login.Contains(search));

                EmployeesGrid.ItemsSource = query
                    .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
                    .Select(x => new EmployeeRow
                    {
                        EmployeeId = x.EmployeeId,
                        LastName = x.LastName,
                        FirstName = x.FirstName,
                        MiddleName = x.MiddleName,
                        PizzeriaName = x.Pizzerias.Name,
                        PositionName = x.Positions.Name,
                        LoginValue = x.Login,
                        Role = x.Role
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
            PizzeriaFilter.SelectedIndex = 0;
            PositionFilter.SelectedIndex = 0;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var window = new EmployeeWindow(null) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                LoadData();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var row = EmployeesGrid.SelectedItem as EmployeeRow;
            if (row == null)
            {
                MessageBox.Show("Выберите сотрудника в таблице.", "Изменение", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var window = new EmployeeWindow(row.EmployeeId) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                LoadData();
        }

        private void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EmployeesGrid.SelectedItem != null)
                Edit_Click(sender, e);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var row = EmployeesGrid.SelectedItem as EmployeeRow;
            if (row == null)
            {
                MessageBox.Show("Выберите сотрудника в таблице.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (row.EmployeeId == Session.EmployeeId)
            {
                MessageBox.Show("Нельзя удалить собственную учётную запись.", "Удаление невозможно",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    int employeeId = row.EmployeeId;
                    if (db.Orders.Any(o => o.EmployeeId == employeeId))
                    {
                        MessageBox.Show("Нельзя удалить сотрудника «" + row.FullName + "»: он принимал заказы.",
                            "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (MessageBox.Show("Удалить сотрудника «" + row.FullName + "»?", "Подтверждение",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                        return;

                    var employee = db.Employees.Find(employeeId);
                    db.Employees.Remove(employee);
                    db.SaveChanges();
                }
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось удалить сотрудника: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
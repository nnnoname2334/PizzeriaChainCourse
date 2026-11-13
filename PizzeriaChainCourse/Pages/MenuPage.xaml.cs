using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PizzeriaChainCourse
{
    public partial class MenuPage : Page
    {
        private class MenuRow
        {
            public int MenuItemId { get; set; }
            public string Name { get; set; }
            public string CategoryName { get; set; }
            public string Description { get; set; }
            public bool IsAvailable { get; set; }
        }

        private bool _ready;

        public MenuPage()
        {
            InitializeComponent();
            AdminButtons.Visibility = Session.Role == "admin" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            using (var db = new PizzeriaChainDBEntities())
            {
                var categories = db.Categories.OrderBy(x => x.Name).ToList();
                var items = new List<object> { new { CategoryId = 0, Name = "Все категории" } };
                items.AddRange(categories.Select(c => (object)new { c.CategoryId, c.Name }));
                CategoryFilter.ItemsSource = items;
                CategoryFilter.SelectedIndex = 0;
            }
            _ready = true;
            LoadData();
        }

        private void LoadData()
        {
            if (!_ready) return;

            string search = SearchBox.Text.Trim();
            int categoryId = CategoryFilter.SelectedValue is int ? (int)CategoryFilter.SelectedValue : 0;

            using (var db = new PizzeriaChainDBEntities())
            {
                var query = db.MenuItems.AsQueryable();

                if (search != "")
                    query = query.Where(x => x.Name.Contains(search));
                if (categoryId != 0)
                    query = query.Where(x => x.CategoryId == categoryId);

                MenuGrid.ItemsSource = query
                    .OrderBy(x => x.Name)
                    .Select(x => new MenuRow
                    {
                        MenuItemId = x.MenuItemId,
                        Name = x.Name,
                        CategoryName = x.Categories.Name,
                        Description = x.Description,
                        IsAvailable = x.IsAvailable
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
            CategoryFilter.SelectedIndex = 0;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var window = new MenuItemWindow(null) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                LoadData();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuGrid.SelectedItem as MenuRow;
            if (row == null)
            {
                MessageBox.Show("Выберите позицию в таблице.", "Изменение", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var window = new MenuItemWindow(row.MenuItemId) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                LoadData();
        }
        private void Prices_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuGrid.SelectedItem as MenuRow;
            if (row == null)
            {
                MessageBox.Show("Выберите позицию в таблице.", "Цены", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            new MenuItemPricesWindow(row.MenuItemId, row.Name) { Owner = Window.GetWindow(this) }.ShowDialog();
        }

        private void Composition_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuGrid.SelectedItem as MenuRow;
            if (row == null)
            {
                MessageBox.Show("Выберите позицию в таблице.", "Состав", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            new MenuItemIngredientsWindow(row.MenuItemId, row.Name) { Owner = Window.GetWindow(this) }.ShowDialog();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuGrid.SelectedItem as MenuRow;
            if (row == null)
            {
                MessageBox.Show("Выберите позицию в таблице.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Удалить позицию «" + row.Name + "»?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    var item = db.MenuItems.Find(row.MenuItemId);
                    db.MenuItems.Remove(item);
                    db.SaveChanges();
                }
                LoadData();
            }
            catch (DbUpdateException)
            {
                MessageBox.Show("Нельзя удалить позицию: она используется в ценах, составе или заказах.\n" +
                                "Вместо удаления можно снять отметку «Доступна» через «Изменить».",
                                "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
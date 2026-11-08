using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PizzeriaChainCourse
{
    public partial class StockPage : Page
    {
        private class StockRow
        {
            public int PizzeriaId { get; set; }
            public int IngredientId { get; set; }
            public string PizzeriaName { get; set; }
            public string IngredientName { get; set; }
            public string Unit { get; set; }
            public decimal Quantity { get; set; }
            public decimal MinQuantity { get; set; }
            public bool NeedsPurchase { get { return Quantity < MinQuantity; } }
            public string StatusText { get { return NeedsPurchase ? "Нужна закупка" : "В норме"; } }
        }

        private bool _ready;

        public StockPage()
        {
            InitializeComponent();
            // Выбирать пиццерию в фильтре может только администратор
            PizzeriaPanel.Visibility = Session.Role == "admin" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (Session.Role == "admin")
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    var items = new List<object> { new { PizzeriaId = 0, Name = "Все пиццерии" } };
                    items.AddRange(db.Pizzerias.OrderBy(x => x.Name).ToList()
                        .Select(p => (object)new { PizzeriaId = p.PizzeriaId, Name = p.Name }));
                    PizzeriaFilter.ItemsSource = items;
                    PizzeriaFilter.SelectedIndex = 0;
                }
            }
            _ready = true;
            LoadData();
        }

        private void LoadData()
        {
            if (!_ready) return;

            string search = SearchBox.Text.Trim();
            bool onlyLow = OnlyLowBox.IsChecked == true;
            int pizzeriaId = PizzeriaFilter.SelectedValue is int ? (int)PizzeriaFilter.SelectedValue : 0;

            using (var db = new PizzeriaChainDBEntities())
            {
                var query = db.Stock.AsQueryable();

                if (Session.Role != "admin")
                {
                    int own = Session.PizzeriaId;
                    query = query.Where(x => x.PizzeriaId == own);
                }
                else if (pizzeriaId != 0)
                {
                    query = query.Where(x => x.PizzeriaId == pizzeriaId);
                }

                if (search != "")
                    query = query.Where(x => x.Ingredients.Name.Contains(search));
                if (onlyLow)
                    query = query.Where(x => x.Quantity < x.MinQuantity);

                StockGrid.ItemsSource = query
                    .OrderBy(x => x.Pizzerias.Name).ThenBy(x => x.Ingredients.Name)
                    .Select(x => new StockRow
                    {
                        PizzeriaId = x.PizzeriaId,
                        IngredientId = x.IngredientId,
                        PizzeriaName = x.Pizzerias.Name,
                        IngredientName = x.Ingredients.Name,
                        Unit = x.Ingredients.Unit,
                        Quantity = x.Quantity,
                        MinQuantity = x.MinQuantity
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
            OnlyLowBox.IsChecked = false;
            if (Session.Role == "admin")
                PizzeriaFilter.SelectedIndex = 0;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var window = new StockWindow(null, null) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                LoadData();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var row = StockGrid.SelectedItem as StockRow;
            if (row == null)
            {
                MessageBox.Show("Выберите запись в таблице.", "Изменение", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var window = new StockWindow(row.PizzeriaId, row.IngredientId) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                LoadData();
        }

        private void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (StockGrid.SelectedItem != null)
                Edit_Click(sender, e);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var row = StockGrid.SelectedItem as StockRow;
            if (row == null)
            {
                MessageBox.Show("Выберите запись в таблице.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Удалить запись «" + row.IngredientName + "» для пиццерии «" + row.PizzeriaName + "»?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    var item = db.Stock.Find(row.PizzeriaId, row.IngredientId);
                    db.Stock.Remove(item);
                    db.SaveChanges();
                }
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось удалить запись: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
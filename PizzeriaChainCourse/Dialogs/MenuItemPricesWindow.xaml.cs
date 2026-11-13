using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PizzeriaChainCourse
{
    public partial class MenuItemPricesWindow : Window
    {
        private class PriceRow
        {
            public int SizeId { get; set; }
            public string SizeName { get; set; }
            public decimal Price { get; set; }
        }

        private readonly int _menuItemId;

        public MenuItemPricesWindow(int menuItemId, string menuItemName)
        {
            InitializeComponent();
            _menuItemId = menuItemId;
            Title = "Цены позиции";
            TitleText.Text = menuItemName;

            using (var db = new PizzeriaChainDBEntities())
            {
                SizeBox.ItemsSource = db.Sizes.OrderBy(x => x.SizeId).ToList();
            }
            LoadData();
        }

        private void LoadData()
        {
            using (var db = new PizzeriaChainDBEntities())
            {
                PricesGrid.ItemsSource = db.MenuItemPrices
                    .Where(p => p.MenuItemId == _menuItemId)
                    .OrderBy(p => p.SizeId)
                    .Select(p => new PriceRow
                    {
                        SizeId = p.SizeId,
                        SizeName = p.Sizes.Name,
                        Price = p.Price
                    })
                    .ToList();
            }
        }

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var row = PricesGrid.SelectedItem as PriceRow;
            if (row == null) return;
            SizeBox.SelectedValue = row.SizeId;
            PriceBox.Text = row.Price.ToString("0.##");
        }

        private static void Warn(string text)
        {
            MessageBox.Show(text, "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (SizeBox.SelectedValue == null)
            {
                Warn("Выберите размер.");
                return;
            }

            decimal price;
            if (!ValidationHelper.TryParseAmount(PriceBox.Text, out price) || price <= 0 || price > 99999999.99m)
            {
                Warn("Цена должна быть числом больше нуля.");
                return;
            }
            price = Math.Round(price, 2);
            int sizeId = (int)SizeBox.SelectedValue;

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    var row = db.MenuItemPrices.FirstOrDefault(p => p.MenuItemId == _menuItemId && p.SizeId == sizeId);
                    if (row == null)
                    {
                        row = db.MenuItemPrices.Create();
                        row.MenuItemId = _menuItemId;
                        row.SizeId = sizeId;
                        db.MenuItemPrices.Add(row);
                    }
                    row.Price = price;
                    db.SaveChanges();
                }
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить цену: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var selected = PricesGrid.SelectedItem as PriceRow;
            if (selected == null)
            {
                MessageBox.Show("Выберите цену в таблице.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Удалить цену для размера «" + selected.SizeName + "»?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    int sizeId = selected.SizeId;
                    var row = db.MenuItemPrices.First(p => p.MenuItemId == _menuItemId && p.SizeId == sizeId);
                    db.MenuItemPrices.Remove(row);
                    db.SaveChanges();
                }
                PriceBox.Text = "";
                LoadData();
            }
            catch (DbUpdateException)
            {
                MessageBox.Show("Нельзя удалить цену: этот размер уже заказывали.", "Удаление невозможно",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
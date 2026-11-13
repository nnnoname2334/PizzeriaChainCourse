using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PizzeriaChainCourse
{
    public partial class MenuItemIngredientsWindow : Window
    {
        private class CompRow
        {
            public int IngredientId { get; set; }
            public string Name { get; set; }
            public string Unit { get; set; }
            public decimal Quantity { get; set; }
        }

        private readonly int _menuItemId;

        public MenuItemIngredientsWindow(int menuItemId, string menuItemName)
        {
            InitializeComponent();
            _menuItemId = menuItemId;
            Title = "Состав позиции";
            TitleText.Text = menuItemName;

            using (var db = new PizzeriaChainDBEntities())
            {
                IngredientBox.ItemsSource = db.Ingredients.OrderBy(x => x.Name).ToList();
            }
            LoadData();
        }

        private void LoadData()
        {
            using (var db = new PizzeriaChainDBEntities())
            {
                CompGrid.ItemsSource = db.MenuItemIngredients
                    .Where(i => i.MenuItemId == _menuItemId)
                    .OrderBy(i => i.Ingredients.Name)
                    .Select(i => new CompRow
                    {
                        IngredientId = i.IngredientId,
                        Name = i.Ingredients.Name,
                        Unit = i.Ingredients.Unit,
                        Quantity = i.Quantity
                    })
                    .ToList();
            }
        }

        private void IngredientBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var ingredient = IngredientBox.SelectedItem as Ingredients;
            UnitText.Text = ingredient == null ? "" : ingredient.Unit;
        }

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var row = CompGrid.SelectedItem as CompRow;
            if (row == null) return;
            IngredientBox.SelectedValue = row.IngredientId;
            QuantityBox.Text = row.Quantity.ToString("0.##");
        }

        private static void Warn(string text)
        {
            MessageBox.Show(text, "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (IngredientBox.SelectedValue == null)
            {
                Warn("Выберите ингредиент.");
                return;
            }

            decimal quantity;
            if (!ValidationHelper.TryParseAmount(QuantityBox.Text, out quantity) || quantity <= 0 || quantity > 99999999.99m)
            {
                Warn("Количество должно быть числом больше нуля.");
                return;
            }
            quantity = Math.Round(quantity, 2);
            int ingredientId = (int)IngredientBox.SelectedValue;

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    var row = db.MenuItemIngredients.Find(_menuItemId, ingredientId);
                    if (row == null)
                    {
                        row = db.MenuItemIngredients.Create();
                        row.MenuItemId = _menuItemId;
                        row.IngredientId = ingredientId;
                        db.MenuItemIngredients.Add(row);
                    }
                    row.Quantity = quantity;
                    db.SaveChanges();
                }
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить данные: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var selected = CompGrid.SelectedItem as CompRow;
            if (selected == null)
            {
                MessageBox.Show("Выберите ингредиент в таблице.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Убрать «" + selected.Name + "» из состава?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    var row = db.MenuItemIngredients.Find(_menuItemId, selected.IngredientId);
                    db.MenuItemIngredients.Remove(row);
                    db.SaveChanges();
                }
                QuantityBox.Text = "";
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось удалить запись: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
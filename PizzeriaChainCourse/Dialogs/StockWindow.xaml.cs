using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PizzeriaChainCourse
{
    public partial class StockWindow : Window
    {
        private readonly bool _isEdit;

        public StockWindow(int? pizzeriaId, int? ingredientId)
        {
            InitializeComponent();
            _isEdit = pizzeriaId != null && ingredientId != null;

            using (var db = new PizzeriaChainDBEntities())
            {
                PizzeriaBox.ItemsSource = db.Pizzerias.OrderBy(x => x.Name).ToList();
                IngredientBox.ItemsSource = db.Ingredients.OrderBy(x => x.Name).ToList();

                if (_isEdit)
                {
                    Title = "Изменение остатка";
                    var row = db.Stock.Find(pizzeriaId.Value, ingredientId.Value);
                    PizzeriaBox.SelectedValue = row.PizzeriaId;
                    IngredientBox.SelectedValue = row.IngredientId;
                    QuantityBox.Text = row.Quantity.ToString("0.##");
                    MinBox.Text = row.MinQuantity.ToString("0.##");
                    PizzeriaBox.IsEnabled = false;
                    IngredientBox.IsEnabled = false;
                }
                else
                {
                    Title = "Новая запись остатка";
                    PizzeriaBox.SelectedValue = Session.PizzeriaId;
                    PizzeriaBox.IsEnabled = Session.Role == "admin";
                    QuantityBox.Text = "0";
                    MinBox.Text = "0";
                }
            }
        }

        private void IngredientBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var ingredient = IngredientBox.SelectedItem as Ingredients;
            UnitText.Text = ingredient == null ? "" : "Единица измерения: " + ingredient.Unit;
        }

        private static bool TryParseAmount(string text, out decimal value)
        {
            string sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            text = text.Trim().Replace(".", sep).Replace(",", sep);
            return decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out value);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (PizzeriaBox.SelectedValue == null || IngredientBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите пиццерию и ингредиент.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal quantity, min;
            if (!TryParseAmount(QuantityBox.Text, out quantity) || quantity < 0 || quantity > 99999999.99m)
            {
                MessageBox.Show("Остаток должен быть числом от 0 до 99 999 999,99.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!TryParseAmount(MinBox.Text, out min) || min < 0 || min > 99999999.99m)
            {
                MessageBox.Show("Минимальный остаток должен быть числом от 0 до 99 999 999,99.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            quantity = Math.Round(quantity, 2);
            min = Math.Round(min, 2);
            int pizzeriaId = (int)PizzeriaBox.SelectedValue;
            int ingredientId = (int)IngredientBox.SelectedValue;

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    Stock row;
                    if (_isEdit)
                    {
                        row = db.Stock.Find(pizzeriaId, ingredientId);
                    }
                    else
                    {
                        if (db.Stock.Any(x => x.PizzeriaId == pizzeriaId && x.IngredientId == ingredientId))
                        {
                            MessageBox.Show("Для этой пиццерии такой ингредиент уже есть. Измените остаток в таблице.",
                                "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        row = db.Stock.Create();
                        row.PizzeriaId = pizzeriaId;
                        row.IngredientId = ingredientId;
                        db.Stock.Add(row);
                    }

                    row.Quantity = quantity;
                    row.MinQuantity = min;
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
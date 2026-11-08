using System;
using System.Linq;
using System.Windows;

namespace PizzeriaChainCourse
{
    public partial class MenuItemWindow : Window
    {
        private readonly int? _id;

        public MenuItemWindow(int? id)
        {
            InitializeComponent();
            _id = id;

            using (var db = new PizzeriaChainDBEntities())
            {
                CategoryBox.ItemsSource = db.Categories.OrderBy(x => x.Name).ToList();

                if (id == null)
                {
                    Title = "Новая позиция меню";
                    AvailableBox.IsChecked = true;
                }
                else
                {
                    Title = "Изменение позиции меню";
                    var item = db.MenuItems.Find(id.Value);
                    NameBox.Text = item.Name;
                    CategoryBox.SelectedValue = item.CategoryId;
                    DescriptionBox.Text = item.Description;
                    AvailableBox.IsChecked = item.IsAvailable;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string description = DescriptionBox.Text.Trim();

            if (name == "")
            {
                MessageBox.Show("Введите название позиции.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (CategoryBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите категорию.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    int currentId = _id ?? 0;
                    if (db.MenuItems.Any(x => x.Name == name && x.MenuItemId != currentId))
                    {
                        MessageBox.Show("Позиция с таким названием уже есть.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var item = _id == null ? db.MenuItems.Create() : db.MenuItems.Find(_id.Value);
                    if (_id == null)
                        db.MenuItems.Add(item);

                    item.Name = name;
                    item.CategoryId = (int)CategoryBox.SelectedValue;
                    item.Description = description == "" ? null : description;
                    item.IsAvailable = AvailableBox.IsChecked == true;

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
using System;
using System.Linq;
using System.Windows;

namespace PizzeriaChainCourse
{
    public partial class DictionaryWindow : Window
    {
        private readonly string _kind;
        private readonly int? _id;

        public DictionaryWindow(string kind, string title, string extraLabel, int? id)
        {
            InitializeComponent();
            _kind = kind;
            _id = id;
            Title = (id == null ? "Новая запись: " : "Изменение: ") + title;

            switch (kind)
            {
                case "positions":
                case "categories": NameBox.MaxLength = 50; break;
                case "sizes":
                case "statuses": NameBox.MaxLength = 30; break;
                case "ingredients": NameBox.MaxLength = 100; break;
            }

            if (extraLabel != null)
            {
                ExtraPanel.Visibility = Visibility.Visible;
                ExtraLabel.Text = kind == "sizes" ? "Диаметр, см (необязательно)" : "Единица измерения (г, мл, шт…)";
                ExtraBox.MaxLength = kind == "sizes" ? 3 : 10;
            }

            if (id != null)
                LoadValues(id.Value);
        }

        private void LoadValues(int id)
        {
            using (var db = new PizzeriaChainDBEntities())
            {
                switch (_kind)
                {
                    case "positions":
                        NameBox.Text = db.Positions.Find(id).Name;
                        break;
                    case "categories":
                        NameBox.Text = db.Categories.Find(id).Name;
                        break;
                    case "sizes":
                        {
                            var size = db.Sizes.Find(id);
                            NameBox.Text = size.Name;
                            ExtraBox.Text = size.DiameterCm == null ? "" : size.DiameterCm.ToString();
                            break;
                        }
                    case "ingredients":
                        {
                            var ingredient = db.Ingredients.Find(id);
                            NameBox.Text = ingredient.Name;
                            ExtraBox.Text = ingredient.Unit;
                            break;
                        }
                    default:
                        NameBox.Text = db.OrderStatuses.Find(id).Name;
                        break;
                }
            }
        }

        private bool NameExists(PizzeriaChainDBEntities db, string name, int currentId)
        {
            switch (_kind)
            {
                case "positions": return db.Positions.Any(x => x.Name == name && x.PositionId != currentId);
                case "categories": return db.Categories.Any(x => x.Name == name && x.CategoryId != currentId);
                case "sizes": return db.Sizes.Any(x => x.Name == name && x.SizeId != currentId);
                case "ingredients": return db.Ingredients.Any(x => x.Name == name && x.IngredientId != currentId);
                default: return db.OrderStatuses.Any(x => x.Name == name && x.StatusId != currentId);
            }
        }

        private static void Warn(string text)
        {
            MessageBox.Show(text, "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string extra = ExtraBox.Text.Trim();

            if (name == "")
            {
                Warn("Введите название.");
                return;
            }

            int? diameter = null;
            if (_kind == "sizes" && extra != "")
            {
                int d;
                if (!int.TryParse(extra, out d) || d < 1 || d > 100)
                {
                    Warn("Диаметр должен быть целым числом от 1 до 100.");
                    return;
                }
                diameter = d;
            }
            if (_kind == "ingredients" && extra == "")
            {
                Warn("Укажите единицу измерения.");
                return;
            }

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    if (NameExists(db, name, _id ?? 0))
                    {
                        Warn("Запись с таким названием уже существует.");
                        return;
                    }

                    switch (_kind)
                    {
                        case "positions":
                            {
                                var entity = _id == null ? db.Positions.Create() : db.Positions.Find(_id.Value);
                                if (_id == null) db.Positions.Add(entity);
                                entity.Name = name;
                                break;
                            }
                        case "categories":
                            {
                                var entity = _id == null ? db.Categories.Create() : db.Categories.Find(_id.Value);
                                if (_id == null) db.Categories.Add(entity);
                                entity.Name = name;
                                break;
                            }
                        case "sizes":
                            {
                                var entity = _id == null ? db.Sizes.Create() : db.Sizes.Find(_id.Value);
                                if (_id == null) db.Sizes.Add(entity);
                                entity.Name = name;
                                entity.DiameterCm = diameter;
                                break;
                            }
                        case "ingredients":
                            {
                                var entity = _id == null ? db.Ingredients.Create() : db.Ingredients.Find(_id.Value);
                                if (_id == null) db.Ingredients.Add(entity);
                                entity.Name = name;
                                entity.Unit = extra;
                                break;
                            }
                        default:
                            {
                                var entity = _id == null ? db.OrderStatuses.Create() : db.OrderStatuses.Find(_id.Value);
                                if (_id == null) db.OrderStatuses.Add(entity);
                                entity.Name = name;
                                break;
                            }
                    }

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
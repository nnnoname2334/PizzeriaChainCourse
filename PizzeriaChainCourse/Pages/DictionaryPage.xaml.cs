using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PizzeriaChainCourse
{
    public partial class DictionaryPage : Page
    {
        private class DictKind
        {
            public string Code { get; set; }
            public string Title { get; set; }
            public string ExtraHeader { get; set; }
        }

        private class DictRow
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Extra { get; set; }
        }

        private readonly List<DictKind> _kinds = new List<DictKind>
        {
            new DictKind { Code = "positions", Title = "Должности" },
            new DictKind { Code = "categories", Title = "Категории меню" },
            new DictKind { Code = "sizes", Title = "Размеры", ExtraHeader = "Диаметр" },
            new DictKind { Code = "ingredients", Title = "Ингредиенты", ExtraHeader = "Единица измерения" },
            new DictKind { Code = "statuses", Title = "Статусы заказов" }
        };

        private bool _ready;

        public DictionaryPage()
        {
            InitializeComponent();
        }

        private DictKind CurrentKind
        {
            get { return KindBox.SelectedItem as DictKind; }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            KindBox.ItemsSource = _kinds;
            _ready = true;
            KindBox.SelectedIndex = 0;
        }

        private void Kind_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || CurrentKind == null) return;

            var extraColumn = DictGrid.Columns[1];
            extraColumn.Visibility = CurrentKind.ExtraHeader == null ? Visibility.Collapsed : Visibility.Visible;
            extraColumn.Header = CurrentKind.ExtraHeader;
            LoadData();
        }

        private void LoadData()
        {
            if (!_ready || CurrentKind == null) return;

            using (var db = new PizzeriaChainDBEntities())
            {
                List<DictRow> rows;
                switch (CurrentKind.Code)
                {
                    case "positions":
                        rows = db.Positions.OrderBy(x => x.Name).ToList()
                            .Select(x => new DictRow { Id = x.PositionId, Name = x.Name }).ToList();
                        break;
                    case "categories":
                        rows = db.Categories.OrderBy(x => x.Name).ToList()
                            .Select(x => new DictRow { Id = x.CategoryId, Name = x.Name }).ToList();
                        break;
                    case "sizes":
                        rows = db.Sizes.OrderBy(x => x.SizeId).ToList()
                            .Select(x => new DictRow
                            {
                                Id = x.SizeId,
                                Name = x.Name,
                                Extra = x.DiameterCm == null ? "—" : x.DiameterCm + " см"
                            }).ToList();
                        break;
                    case "ingredients":
                        rows = db.Ingredients.OrderBy(x => x.Name).ToList()
                            .Select(x => new DictRow { Id = x.IngredientId, Name = x.Name, Extra = x.Unit }).ToList();
                        break;
                    default:
                        rows = db.OrderStatuses.OrderBy(x => x.StatusId).ToList()
                            .Select(x => new DictRow { Id = x.StatusId, Name = x.Name }).ToList();
                        break;
                }
                DictGrid.ItemsSource = rows;
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var window = new DictionaryWindow(CurrentKind.Code, CurrentKind.Title, CurrentKind.ExtraHeader, null)
            { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                LoadData();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var row = DictGrid.SelectedItem as DictRow;
            if (row == null)
            {
                MessageBox.Show("Выберите запись в таблице.", "Изменение", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var window = new DictionaryWindow(CurrentKind.Code, CurrentKind.Title, CurrentKind.ExtraHeader, row.Id)
            { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                LoadData();
        }

        private void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DictGrid.SelectedItem != null)
                Edit_Click(sender, e);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var row = DictGrid.SelectedItem as DictRow;
            if (row == null)
            {
                MessageBox.Show("Выберите запись в таблице.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Удалить запись «" + row.Name + "»?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    switch (CurrentKind.Code)
                    {
                        case "positions": db.Positions.Remove(db.Positions.Find(row.Id)); break;
                        case "categories": db.Categories.Remove(db.Categories.Find(row.Id)); break;
                        case "sizes": db.Sizes.Remove(db.Sizes.Find(row.Id)); break;
                        case "ingredients": db.Ingredients.Remove(db.Ingredients.Find(row.Id)); break;
                        default: db.OrderStatuses.Remove(db.OrderStatuses.Find(row.Id)); break;
                    }
                    db.SaveChanges();
                }
                LoadData();
            }
            catch (DbUpdateException)
            {
                MessageBox.Show("Нельзя удалить запись «" + row.Name + "»: она используется в других таблицах.",
                    "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
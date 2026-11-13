using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace PizzeriaChainCourse
{
    public partial class ReportsPage : Page
    {
        private class ReportKind
        {
            public string Code { get; set; }
            public string Title { get; set; }
        }

        // Название статуса из справочника OrderStatuses: такие заказы в выручку не входят
        private const string Cancelled = "Отменён";

        private readonly List<ReportKind> _kinds = new List<ReportKind>
        {
            new ReportKind { Code = "revenue", Title = "Выручка по пиццериям" },
            new ReportKind { Code = "popular", Title = "Популярные позиции меню" },
            new ReportKind { Code = "stock", Title = "Остатки ингредиентов" },
            new ReportKind { Code = "client", Title = "Заказы клиента" }
        };

        private bool _ready;
        private DataTable _table;
        private string _reportTitle;

        public ReportsPage()
        {
            InitializeComponent();
        }

        private string CurrentCode
        {
            get { return ReportBox.SelectedValue as string; }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ReportBox.ItemsSource = _kinds;

            using (var db = new PizzeriaChainDBEntities())
            {
                if (Session.Role == "admin")
                {
                    var pizzerias = new List<object> { new { PizzeriaId = 0, Name = "Вся сеть" } };
                    pizzerias.AddRange(db.Pizzerias.OrderBy(x => x.Name).ToList()
                        .Select(p => (object)new { PizzeriaId = p.PizzeriaId, Name = p.Name }));
                    PizzeriaBox.ItemsSource = pizzerias;
                    PizzeriaBox.SelectedIndex = 0;
                }
                else
                {
                    // Менеджер видит только свою пиццерию
                    PizzeriaPanel.Visibility = Visibility.Collapsed;
                }

                ClientBox.ItemsSource = db.Customers.OrderBy(x => x.FirstName).ToList()
                    .Select(c => (object)new { CustomerId = c.CustomerId, Display = c.FirstName + " — " + c.Phone })
                    .ToList();
            }

            FromDate.SelectedDate = DateTime.Today.AddDays(-30);
            ToDate.SelectedDate = DateTime.Today;

            _ready = true;
            ReportBox.SelectedIndex = 0;
        }

        private void Report_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || CurrentCode == null) return;

            PeriodPanel.Visibility = CurrentCode == "stock" ? Visibility.Collapsed : Visibility.Visible;
            ClientPanel.Visibility = CurrentCode == "client" ? Visibility.Visible : Visibility.Collapsed;

            _table = null;
            ResultGrid.ItemsSource = null;
            SubtitleText.Text = "";
        }

        private int SelectedPizzeriaId()
        {
            if (Session.Role != "admin") return Session.PizzeriaId;
            return PizzeriaBox.SelectedValue is int ? (int)PizzeriaBox.SelectedValue : 0;
        }

        private static string Money(decimal value)
        {
            return value.ToString("N2");
        }

        private static void Warn(string text)
        {
            MessageBox.Show(text, "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Build_Click(object sender, RoutedEventArgs e)
        {
            string code = CurrentCode;
            if (code == null) return;

            DateTime start = DateTime.Today, end = DateTime.Today;
            if (code != "stock")
            {
                if (FromDate.SelectedDate == null || ToDate.SelectedDate == null)
                {
                    Warn("Укажите период отчёта.");
                    return;
                }
                start = FromDate.SelectedDate.Value.Date;
                end = ToDate.SelectedDate.Value.Date.AddDays(1);
                if (end <= start)
                {
                    Warn("Дата начала не может быть позже даты окончания.");
                    return;
                }
            }
            if (code == "client" && ClientBox.SelectedValue == null)
            {
                Warn("Выберите клиента.");
                return;
            }

            int pizzeriaId = SelectedPizzeriaId();

            try
            {
                using (var db = new PizzeriaChainDBEntities())
                {
                    switch (code)
                    {
                        case "revenue": _table = BuildRevenue(db, start, end, pizzeriaId); break;
                        case "popular": _table = BuildPopular(db, start, end, pizzeriaId); break;
                        case "stock": _table = BuildStock(db, pizzeriaId); break;
                        default: _table = BuildClient(db, start, end, pizzeriaId, (int)ClientBox.SelectedValue); break;
                    }

                    string scope = pizzeriaId == 0 ? "вся сеть" : db.Pizzerias.Find(pizzeriaId).Name;
                    string when = code == "stock"
                        ? "На момент: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                        : "Период: " + start.ToString("dd.MM.yyyy") + " – " + end.AddDays(-1).ToString("dd.MM.yyyy");
                    SubtitleText.Text = when + "  ·  " + scope;
                }

                _reportTitle = _kinds.First(k => k.Code == code).Title;
                ResultGrid.ItemsSource = _table.DefaultView;

                if (_table.Rows.Count == 0)
                    SubtitleText.Text += "  ·  данных за выбранные условия нет";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сформировать отчёт: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Выручка по пиццериям за период, отменённые заказы не учитываются
        private DataTable BuildRevenue(PizzeriaChainDBEntities db, DateTime start, DateTime end, int pizzeriaId)
        {
            var query = db.Orders.Where(o => o.CreatedAt >= start && o.CreatedAt < end
                                             && o.OrderStatuses.Name != Cancelled);
            if (pizzeriaId != 0)
                query = query.Where(o => o.PizzeriaId == pizzeriaId);

            var rows = query
                .GroupBy(o => o.Pizzerias.Name)
                .Select(g => new { Name = g.Key, Count = g.Count(), Sum = g.Sum(o => o.TotalAmount) })
                .OrderByDescending(r => r.Sum)
                .ToList();

            var table = new DataTable();
            table.Columns.Add("Пиццерия");
            table.Columns.Add("Количество заказов");
            table.Columns.Add("Выручка, ₽");

            foreach (var r in rows)
                table.Rows.Add(r.Name, r.Count.ToString(), Money(r.Sum));
            if (rows.Count > 0)
                table.Rows.Add("ИТОГО", rows.Sum(r => r.Count).ToString(), Money(rows.Sum(r => r.Sum)));
            return table;
        }

        // Популярные позиции меню за период по количеству проданных штук
        private DataTable BuildPopular(PizzeriaChainDBEntities db, DateTime start, DateTime end, int pizzeriaId)
        {
            var query = db.OrderItems.Where(i => i.Orders.CreatedAt >= start && i.Orders.CreatedAt < end
                                                 && i.Orders.OrderStatuses.Name != Cancelled);
            if (pizzeriaId != 0)
                query = query.Where(i => i.Orders.PizzeriaId == pizzeriaId);

            var rows = query
                .GroupBy(i => new
                {
                    Name = i.MenuItemPrices.MenuItems.Name,
                    Category = i.MenuItemPrices.MenuItems.Categories.Name
                })
                .Select(g => new
                {
                    g.Key.Name,
                    g.Key.Category,
                    Qty = g.Sum(i => i.Quantity),
                    Sum = g.Sum(i => i.Quantity * i.UnitPrice)
                })
                .OrderByDescending(r => r.Qty).ThenBy(r => r.Name)
                .ToList();

            var table = new DataTable();
            table.Columns.Add("Позиция");
            table.Columns.Add("Категория");
            table.Columns.Add("Продано, шт.");
            table.Columns.Add("Сумма, ₽");

            foreach (var r in rows)
                table.Rows.Add(r.Name, r.Category, r.Qty.ToString(), Money(r.Sum));
            if (rows.Count > 0)
                table.Rows.Add("ИТОГО", "", rows.Sum(r => r.Qty).ToString(), Money(rows.Sum(r => r.Sum)));
            return table;
        }

        // Остатки ингредиентов на текущий момент, сначала те, что требуют закупки
        private DataTable BuildStock(PizzeriaChainDBEntities db, int pizzeriaId)
        {
            var query = db.Stock.AsQueryable();
            if (pizzeriaId != 0)
                query = query.Where(s => s.PizzeriaId == pizzeriaId);

            var rows = query
                .Select(s => new
                {
                    Pizzeria = s.Pizzerias.Name,
                    Ingredient = s.Ingredients.Name,
                    Unit = s.Ingredients.Unit,
                    s.Quantity,
                    s.MinQuantity
                })
                .ToList()
                .OrderByDescending(r => r.Quantity < r.MinQuantity)
                .ThenBy(r => r.Pizzeria).ThenBy(r => r.Ingredient)
                .ToList();

            var table = new DataTable();
            table.Columns.Add("Пиццерия");
            table.Columns.Add("Ингредиент");
            table.Columns.Add("Остаток");
            table.Columns.Add("Ед.");
            table.Columns.Add("Минимальный остаток");
            table.Columns.Add("Требуется закупка");

            foreach (var r in rows)
                table.Rows.Add(r.Pizzeria, r.Ingredient, Money(r.Quantity), r.Unit, Money(r.MinQuantity),
                               r.Quantity < r.MinQuantity ? "Да" : "Нет");
            return table;
        }

        // Заказы выбранного клиента за период
        private DataTable BuildClient(PizzeriaChainDBEntities db, DateTime start, DateTime end, int pizzeriaId, int customerId)
        {
            var query = db.Orders.Where(o => o.CustomerId == customerId && o.CreatedAt >= start && o.CreatedAt < end);
            if (pizzeriaId != 0)
                query = query.Where(o => o.PizzeriaId == pizzeriaId);

            var rows = query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.OrderId,
                    o.CreatedAt,
                    Pizzeria = o.Pizzerias.Name,
                    o.OrderType,
                    Status = o.OrderStatuses.Name,
                    o.TotalAmount
                })
                .ToList();

            var table = new DataTable();
            table.Columns.Add("№ заказа");
            table.Columns.Add("Дата");
            table.Columns.Add("Пиццерия");
            table.Columns.Add("Тип заказа");
            table.Columns.Add("Статус");
            table.Columns.Add("Сумма, ₽");

            foreach (var r in rows)
                table.Rows.Add(r.OrderId.ToString(), r.CreatedAt.ToString("dd.MM.yyyy HH:mm"), r.Pizzeria,
                               r.OrderType, r.Status, Money(r.TotalAmount));
            if (rows.Count > 0)
                table.Rows.Add("ИТОГО (без отменённых)", "", "", "", "",
                               Money(rows.Where(r => r.Status != Cancelled).Sum(r => r.TotalAmount)));
            return table;
        }

        private static string Escape(string value)
        {
            value = value ?? "";
            if (value.Contains(";") || value.Contains("\"") || value.Contains("\n"))
                value = "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (_table == null || _table.Rows.Count == 0)
            {
                MessageBox.Show("Сначала сформируйте отчёт, в котором есть данные.", "Сохранение отчёта",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Файл CSV (*.csv)|*.csv",
                FileName = _reportTitle + " " + DateTime.Now.ToString("yyyy-MM-dd") + ".csv"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine(string.Join(";", _table.Columns.Cast<DataColumn>().Select(c => Escape(c.ColumnName))));
                foreach (DataRow row in _table.Rows)
                    sb.AppendLine(string.Join(";", row.ItemArray.Select(v => Escape(Convert.ToString(v)))));

                File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(true));
                MessageBox.Show("Отчёт сохранён.", "Сохранение отчёта", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (IOException ex)
            {
                MessageBox.Show("Не удалось сохранить файл: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
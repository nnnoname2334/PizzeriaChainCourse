using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PizzeriaChainCourse
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            UserNameText.Text = Session.FullName;
            UserRoleText.Text = RoleName(Session.Role);
            Navigate(MenuButton, new MenuPage());
        }

        private static string RoleName(string role)
        {
            switch (role)
            {
                case "admin": return "Администратор сети";
                case "manager": return "Менеджер пиццерии";
                case "cashier": return "Кассир";
                default: return role;
            }
        }

        // Открывает страницу и подсвечивает активную кнопку меню
        private void Navigate(Button button, Page page)
        {
            foreach (var b in NavPanel.Children.OfType<Button>())
                b.Background = Brushes.Transparent;
            button.Background = (Brush)FindResource("AccentBrush");
            MainFrame.Navigate(page);
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            Navigate(MenuButton, new MenuPage());
        }
        private void OrdersButton_Click(object sender, RoutedEventArgs e)
        {
            Navigate(OrdersButton, new OrdersPage());
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Session.EmployeeId = 0;
            Session.PizzeriaId = 0;
            Session.FullName = null;
            Session.Role = null;
            new LoginWindow().Show();
            Close();
        }
    }
}
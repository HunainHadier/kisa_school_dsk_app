using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using KisaSchoolMangement.Views.User;

namespace KisaSchoolMangement
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // ensure current user id is available (default to Owner = 1)
        private int currentUserId = 1;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuPermissions_Click(object sender, RoutedEventArgs e)
        {
            // open RolePermissionWindow
            var win = new RolePermissionWindow(currentUserId);
            win.Owner = this;
            win.ShowDialog();
        }

        private void MenuRoles_Click(object sender, RoutedEventArgs e)
        {
            // RolesWindow not present in project; open RolePermissionWindow which shows roles management
            var win = new RolePermissionWindow(currentUserId);
            win.Owner = this;
            win.ShowDialog();
        }
    }
}
using System.Windows;
using System.Windows.Controls;

namespace Zombie.Controls
{
    /// <summary>
    /// Interaction logic for GitHubView.xaml
    /// </summary>
    public partial class GitHubView
    {
        public GitHubView()
        {
            InitializeComponent();
        }

        private void AccessTokenTextBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null)
            {
                ((dynamic)DataContext).Settings.AccessToken = ((PasswordBox)sender).Password;
            }
        }

        private void GitHubView_OnLoaded(object sender, RoutedEventArgs e)
        {
            AccessTokenTextBox.Password = ((GitHubViewModel) DataContext).Settings.AccessToken;
        }
    }
}

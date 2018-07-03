using System.Windows;
using System.Windows.Input;

namespace Zombie.Controls
{
    /// <summary>
    /// Interaction logic for MappingsView.xaml
    /// </summary>
    public partial class MappingsView
    {
        public MappingsView()
        {
            InitializeComponent();
        }

        private void UIElement_OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            Mouse.SetCursor(Cursors.Hand);
            e.Handled = true;
        }
    }
}

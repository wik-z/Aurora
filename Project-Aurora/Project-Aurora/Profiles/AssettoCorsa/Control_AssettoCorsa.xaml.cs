using Aurora.Settings;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Aurora.Profiles.AssettoCorsa
{
    /// <summary>
    /// Interaction logic for Control_AssettoCorsa.xaml
    /// </summary>
    public partial class Control_AssettoCorsa : UserControl
    {
        private Application profile_manager;

        public Control_AssettoCorsa(Application profile)
        {
            InitializeComponent();

            profile_manager = profile;

            SetSettings();
        }

        private void SetSettings()
        {
            this.game_enabled.IsChecked = profile_manager.Settings.IsEnabled;
        }

        private void patch_button_Click(object sender, RoutedEventArgs e)
        {
            App.InstallLogitech();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private void game_enabled_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                profile_manager.Settings.IsEnabled = (this.game_enabled.IsChecked.HasValue) ? this.game_enabled.IsChecked.Value : false;
                profile_manager.SaveProfiles();
            }
        }
    }
}

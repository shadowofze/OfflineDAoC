using System.Windows;
using System.Windows.Input;
using WPFLauncher.Properties;

namespace WPFLauncher
{
    public partial class OptionsWindow : Window
    {
        public OptionsWindow()
        {
            InitializeComponent();
            LoadSavedOptions();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void LoadSavedOptions()
        {
            PTRButton.IsChecked = Settings.Default.PTR;
            KeepOpenButton.IsChecked = Settings.Default.KeepOpen;
            TrayButton.IsChecked = Settings.Default.MinimizeToTray;
            SaveAccountButton.IsChecked = Settings.Default.SaveAccount;
        }

        private void OptionSaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (PTRButton.IsChecked.HasValue) Settings.Default.PTR = PTRButton.IsChecked.Value;
            if (KeepOpenButton.IsChecked.HasValue) Settings.Default.KeepOpen = KeepOpenButton.IsChecked.Value;
            if (SaveAccountButton.IsChecked.HasValue) Settings.Default.SaveAccount = SaveAccountButton.IsChecked.Value;
            if (TrayButton.IsChecked.HasValue) Settings.Default.MinimizeToTray = TrayButton.IsChecked.Value;
            Settings.Default.Save();
            Close();
        }

        private void ForcePatchButton_click(object sender, MouseButtonEventArgs e)
        {
            Settings.Default.localVersion = 0;
            Settings.Default.Save();
            Close();
        }
    }
}
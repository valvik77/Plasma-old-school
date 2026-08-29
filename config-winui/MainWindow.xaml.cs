using System;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace PlasmaOldSchool.Config
{
    public sealed partial class MainWindow : Window
    {
        private readonly SettingsViewModel _settings;
        private bool _initializing;
        public MainWindow()
        {
            _settings = new SettingsViewModel(global::PlasmaOldSchool.PlasmaSettings.Load());
            InitializeComponent(); _initializing = true;
            AppWindow.Resize(new SizeInt32(1180, 860));
            Language.SelectedIndex = _settings.Language == "en" ? 1 : 0;
            _initializing = false; ApplyText(); ShowPage("general");
        }
        private string T(string key) { return Localizer.Get(_settings.Language, key); }
        private void ApplyText()
        {
            Subtitle.Text=T("Subtitle"); LanguageLabel.Text=T("Language"); GeneralItem.Content=T("General"); DisplayItem.Content=T("Display"); EngineItem.Content=T("Engine");
            ((ComboBoxItem)Language.Items[0]).Content=T("Spanish"); ((ComboBoxItem)Language.Items[1]).Content=T("English"); TestButton.Content=T("Test"); CancelButton.Content=T("Cancel"); SaveButton.Content=T("Save");
        }
        private void NavigationChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args) { NavigationViewItem item=args.SelectedItem as NavigationViewItem; if(item!=null) ShowPage(item.Tag as string); }
        private void ShowPage(string page) { if(page=="display") ContentFrame.Content=new DisplayPage(_settings); else if(page=="engine") ContentFrame.Content=new EnginePage(_settings); else ContentFrame.Content=new GeneralPage(_settings); }
        private void LanguageChanged(object sender, SelectionChangedEventArgs e) { if(_initializing)return; _settings.Language=Language.SelectedIndex==1?"en":"es"; ApplyText(); NavigationViewItem item=Navigation.SelectedItem as NavigationViewItem; ShowPage(item==null?"general":item.Tag as string); }
        private void SaveClicked(object sender, RoutedEventArgs e) { _settings.Save(); Close(); }
        private void CancelClicked(object sender, RoutedEventArgs e) { Close(); }
        private void TestClicked(object sender, RoutedEventArgs e)
        {
            _settings.Save();
            string scr=Path.Combine(Environment.SystemDirectory, "PlasmaOldSchool.scr");
            if(!File.Exists(scr)) scr=Path.Combine(AppContext.BaseDirectory, "PlasmaOldSchool.scr");
            if(File.Exists(scr)) Process.Start(new ProcessStartInfo(scr, "/s") { UseShellExecute=true, WorkingDirectory=Path.GetDirectoryName(scr) });
        }
    }
}

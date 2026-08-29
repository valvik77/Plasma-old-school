using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace PlasmaOldSchool.Config
{
    public sealed partial class GeneralPage : UserControl
    {
        private readonly SettingsViewModel s;
        public GeneralPage(SettingsViewModel settings)
        {
            s=settings; InitializeComponent(); DataContext=s;
            Title.Text=T("General"); PaletteLabel.Text=T("Palette"); ColorsLabel.Text=T("CustomColors"); PixelLabel.Text=T("Pixelation"); SpeedLabel.Text=T("Speed"); ZoomLabel.Text=T("Zoom"); WarpLabel.Text=T("Warp"); CycleLabel.Header=T("ColorCycle"); CycleSpeedLabel.Text=T("ColorCycleSpeed");
            RgbPaletteCycleLabel.Header = s.Language == "en" ? "RGB palette animation" : "Animación RGB de paleta";
            s.PropertyChanged += SettingsChanged; UpdateColorButtons();
        }
        private string T(string k) { return Localizer.Get(s.Language,k); }
        private void SettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(s.Color1) || e.PropertyName == nameof(s.Color2) || e.PropertyName == nameof(s.Color3) || e.PropertyName == nameof(s.Color4) || e.PropertyName == nameof(s.PaletteKey)) UpdateColorButtons();
        }
        private void ColorButtonClicked(object sender, RoutedEventArgs e)
        {
            Button button=(Button)sender; int index=Convert.ToInt32(button.Tag); ColorPicker picker=new ColorPicker { Color=GetColor(index), IsAlphaEnabled=false, IsAlphaSliderVisible=false, IsAlphaTextInputVisible=false };
            picker.ColorChanged += (source,args) => { SetColor(index,picker.Color); UpdateColorButtons(); };
            new Flyout { Content=picker }.ShowAt(button);
        }
        private Color GetColor(int index) { return index==0?s.Color1:index==1?s.Color2:index==2?s.Color3:s.Color4; }
        private void SetColor(int index, Color color) { if(index==0)s.Color1=color; else if(index==1)s.Color2=color; else if(index==2)s.Color3=color; else s.Color4=color; }
        private void UpdateColorButtons() { SetButton(Color1Button,s.Color1); SetButton(Color2Button,s.Color2); SetButton(Color3Button,s.Color3); SetButton(Color4Button,s.Color4); }
        private static void SetButton(Button button, Color color) { button.Background=new SolidColorBrush(color); button.Content="#"+color.R.ToString("X2")+color.G.ToString("X2")+color.B.ToString("X2"); }
    }
}

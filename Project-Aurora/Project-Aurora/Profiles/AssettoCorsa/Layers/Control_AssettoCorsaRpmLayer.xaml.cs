using Aurora.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aurora.Profiles.AssettoCorsa.Layers
{
    /// <summary>
    /// Interaction logic for Control_AssettoCorsaRpmLayer.xaml
    /// </summary>
    public partial class Control_AssettoCorsaRpmLayer : UserControl
    {
        private bool settingsset = false;
        private bool profileset = false;

        public Control_AssettoCorsaRpmLayer()
        {
            InitializeComponent();
        }

        public Control_AssettoCorsaRpmLayer(AssettoCorsaRpmLayerHandler datacontext)
        {
            InitializeComponent();

            this.DataContext = datacontext;
        }

        public void SetSettings()
        {
            if (this.DataContext is AssettoCorsaRpmLayerHandler && !settingsset)
            {
                // this.ColorPicker_Rpm.SelectedColor = Utils.ColorUtils.DrawingColorToMediaColor((this.DataContext as AssettoCorsaRpmLayerHandler).Properties._RpmColor ?? System.Drawing.Color.Empty);

                settingsset = true;
            }
        }

        internal void SetProfile(Application profile)
        {
            if (profile != null && !profileset)
            {
                var var_types_numerical = profile.ParameterLookup?.Where(kvp => Utils.TypeUtils.IsNumericType(kvp.Value.Item1));

                profileset = true;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetSettings();

            this.Loaded -= UserControl_Loaded;
        }

        private void ColorPicker_Rpm_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            // if (IsLoaded && settingsset && this.DataContext is AssettoCorsaRpmLayerHandler && sender is Xceed.Wpf.Toolkit.ColorPicker && (sender as Xceed.Wpf.Toolkit.ColorPicker).SelectedColor.HasValue)
            //     (this.DataContext as AssettoCorsaRpmLayerHandler).Properties._RpmColor = Utils.ColorUtils.MediaColorToDrawingColor((sender as Xceed.Wpf.Toolkit.ColorPicker).SelectedColor.Value);
        }
    }
}

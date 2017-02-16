using Aurora.Settings.Layers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Aurora.Controls
{
    /// <summary>
    /// Interaction logic for LogicCheckEdit.xaml
    /// </summary>
    public partial class LogicCheckEdit : UserControl
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public static readonly DependencyProperty LayerProperty = DependencyProperty.Register("Layer", typeof(Layer), typeof(LogicCheckEdit), new PropertyMetadata(LayerPropertyChanged));

        public Layer Layer
        {
            get
            {
                return (Layer)GetValue(LayerProperty);
            }
            set
            {
                SetValue(LayerProperty, value);
            }
        }

        private static void LayerPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            LogicCheckEdit instance = source as LogicCheckEdit;

            instance.cmbParameter.ItemsSource = (e.NewValue as Layer).AssociatedProfile.ParameterLookup
                    .Where(s => (s.Value.Item1.IsPrimitive || s.Value.Item1 == typeof(string)) && s.Value.Item2 == null).ToDictionary(s => s) //Remove methods and non-primitives for right now
                    .Keys;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public static readonly DependencyProperty LogicProperty = DependencyProperty.Register("Check", typeof(KeyValuePair<string, Tuple<LogicOperator, object>>), typeof(LogicCheckEdit), new PropertyMetadata(LogicPropertyChanged));

        public KeyValuePair<string, Tuple<LogicOperator, object>> Check
        {
            get
            {
                return (KeyValuePair<string, Tuple<LogicOperator, object>>)GetValue(LogicProperty);
            }
            set
            {
                SetValue(LogicProperty, value);
            }
        }

        private static void LogicPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            LogicCheckEdit instance = source as LogicCheckEdit;

            instance.cmbParameter.SelectedItem = ((KeyValuePair<string, Tuple<LogicOperator, object>>)e.NewValue).Key;
            instance.cmbCheck.SelectedItem = instance.Check.Value.Item1;
        }

        public LogicCheckEdit()
        {
            InitializeComponent();
        }

        private void cmbParameter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            string str = (e.AddedItems.Count > 0 ? ((KeyValuePair<string, Tuple<Type, Type>>)e.AddedItems[0]).Key as string : null);
            string old_str = (e.RemovedItems.Count > 0 ? ((KeyValuePair<string, Tuple<Type, Type>>)e.RemovedItems[0]).Key as string : null);
            Tuple<Type, Type> typ = Layer.AssociatedProfile.ParameterLookup[str];
            Tuple<Type, Type> old_typ = old_str != null ? Layer.AssociatedProfile.ParameterLookup[str] : null;
            if (old_typ == null || old_typ.Item1 != typ.Item1)
            {
                List<LogicOperator> operators = new List<LogicOperator>();
                switch (Type.GetTypeCode(typ.Item1))
                {
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Single:
                        operators = ((LogicOperator[])Enum.GetValues(typeof(LogicOperator))).ToList();
                        break;
                    //case TypeCode.Object:
                    case TypeCode.Boolean:
                    case TypeCode.String:
                        operators.Add(LogicOperator.Equal);
                        operators.Add(LogicOperator.NotEqual);
                        break;
                }
                this.cmbCheck.ItemsSource = operators;

                this.grdValue.Children.Clear();
            }
        }
    }
}

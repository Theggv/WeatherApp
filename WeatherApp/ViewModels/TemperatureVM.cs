using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WeatherApp
{
    public class TemperatureVM : BindableBase
    {
        public double? Temp { get; set; }

        public Visibility Visibility => Temp == null ? Visibility.Collapsed : Visibility.Visible;

        public TemperatureVM()
        {
            Temp = null;
            RaisePropertyChanged(nameof(Visibility));
        }

        public TemperatureVM(double temp)
        {
            Temp = temp;

            RaisePropertyChanged(nameof(Visibility));
        }
    }
}

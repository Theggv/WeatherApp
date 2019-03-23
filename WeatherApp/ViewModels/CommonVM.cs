using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.Commands;
using System.Windows.Media.Imaging;
using System.Windows;

namespace WeatherApp
{
    using static LoadingStatusVM;

    public class CommonVM: BindableBase
    {
        private MainVM mainVM;
        private BrieflyVM brieflyVM;


        public TemperatureVM TemperatureVM { get; set; }


        public City City { get; set; }

        public Location Location { get; set; }
        

        public BitmapImage Icon { get; set; }


        public CommonVM(MainVM main, BrieflyVM vm)
        {
            mainVM = main;
            brieflyVM = vm;

            Location = brieflyVM.Location;

            TemperatureVM = brieflyVM.ForecastModel?.GetTemperature();

            City = brieflyVM.City;

            try
            {
                Icon = new BitmapImage(brieflyVM.Icon?.UriSource);
                Icon.Freeze();
            }
            catch { };
        }
    }
}

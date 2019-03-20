using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Prism.Mvvm;
using Prism.Commands;

namespace WeatherApp
{
    public class DetailVM : BindableBase
    {
        private ForecastVM mainVM;

        public List<HourVM> HourForecasts { get; set; }

        public string DayName => HourForecasts != null ? HourForecasts[0].Hour.From.DayOfWeek.ToString() : "";

        public DelegateCommand<Forecast> GetData { get; set; }

        public Visibility DetailVis
        {
            get
            {
                if (HourForecasts != null && HourForecasts.Count > 0)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public DetailVM(ForecastVM vm)
        {
            mainVM = vm;

            GetData = new DelegateCommand<Forecast>((f) =>
            {
                HourForecasts = mainVM.GetDetailForecast(f);

                if(HourForecasts != null && HourForecasts.Count > 0)
                {
                    RaisePropertyChanged("HourForecasts");
                    RaisePropertyChanged("DayName");
                    RaisePropertyChanged("DetailVis");
                }
            });

            // GetData.Execute(mainVM.CurrentForecast);
        }
    }
}

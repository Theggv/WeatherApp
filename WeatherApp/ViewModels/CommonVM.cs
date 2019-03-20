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

        private ForecastModel forecastModel;
        private HourModel hourModel;

        public LoadingStatusVM LoadingStatusVM { get; set; }

        public TemperatureVM TemperatureVM => forecastModel.GetTemperature();


        public Visibility Visibility => LoadingStatusVM.Status == eStatus.Invisible ? 
            Visibility.Visible : Visibility.Collapsed;


        public City Location => forecastModel.GetCity();
        

        public BitmapImage BackgroundImage => forecastModel.GetImage();

        public BitmapImage Icon => hourModel.GetImage();


        public DelegateCommand Update { get; set; }


        public CommonVM(MainVM main, City city)
        {
            mainVM = main;

            forecastModel = new ForecastModel(city);

            hourModel = new HourModel();

            LoadingStatusVM = new LoadingStatusVM();

            Update = new DelegateCommand(() =>
            {
                if(LoadingStatusVM.Status == eStatus.Error)
                    forecastModel.LoadData();
            });

            forecastModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "WeatherName")
                {
                    hourModel = new HourModel(forecastModel.CurrentForecast);

                    RaisePropertyChanged(nameof(Location));
                    RaisePropertyChanged(nameof(Icon));
                    RaisePropertyChanged(nameof(TemperatureVM));
                    RaisePropertyChanged(nameof(BackgroundImage));
                }

                if (e.PropertyName == "Loading")
                    LoadingStatusVM.LoadingStatus.Execute();
                if (e.PropertyName == "LoadingCompleted")
                    LoadingStatusVM.ResetStatus.Execute();
                if (e.PropertyName == "LoadingError")
                    LoadingStatusVM.ErrorStatus.Execute();

                RaisePropertyChanged("Visibility");
            };

            forecastModel.LoadData();
        }

        public CommonVM(MainVM main, Location location)
        {
            mainVM = main;

            forecastModel = new ForecastModel(location);

            hourModel = new HourModel();

            LoadingStatusVM = new LoadingStatusVM();

            Update = new DelegateCommand(() =>
            {
                if (LoadingStatusVM.Status == eStatus.Error)
                    forecastModel.LoadData();
            });

            forecastModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "WeatherName")
                {
                    hourModel = new HourModel(forecastModel.CurrentForecast);

                    RaisePropertyChanged(nameof(Location));
                    RaisePropertyChanged(nameof(Icon));
                    RaisePropertyChanged(nameof(TemperatureVM));
                    RaisePropertyChanged(nameof(BackgroundImage));
                }

                if (e.PropertyName == "Loading")
                    LoadingStatusVM.LoadingStatus.Execute();
                if (e.PropertyName == "LoadingCompleted")
                    LoadingStatusVM.ResetStatus.Execute();
                if (e.PropertyName == "LoadingError")
                    LoadingStatusVM.ErrorStatus.Execute();

                RaisePropertyChanged("Visibility");
            };

            forecastModel.LoadData();
        }
    }
}

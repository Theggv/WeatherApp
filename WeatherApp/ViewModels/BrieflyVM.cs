using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.Commands;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WeatherApp
{
    using static LoadingStatusVM;

    public class BrieflyVM: BindableBase
    {
        private MainVM mainVM;
        private HourModel hourModel;

        private int numAttemps;
        private bool isFavorite;

        public ForecastModel ForecastModel { get; set; }


        public bool IsContextMenuEnable
        {
            get => !isFavorite;
            set
            {
                isFavorite = !value;

                RaisePropertyChanged(nameof(IsContextMenuEnable));
            }
        }

        public LoadingStatusVM LoadingStatusVM { get; set; }


        public string Temperature => GetTemp();


        public Visibility Visibility => LoadingStatusVM?.Status == eStatus.Invisible ?
            Visibility.Visible : Visibility.Collapsed;

        public Visibility NotDraw { get; set; } = Visibility.Visible;

        public Visibility ContextMenuVis => isFavorite ? Visibility.Collapsed : Visibility.Visible;



        public City City => ForecastModel?.GetCity();

        public Location Location { get; set; }


        public BitmapImage BackgroundImage => ForecastModel?.GetImage();

        public BitmapImage Icon => hourModel?.GetImage();


        public DelegateCommand Update { get; set; }

        public DelegateCommand Close { get; set; }

        public DelegateCommand AddToFavorites { get; set; }


        public BrieflyVM()
        {
            NotDraw = Visibility.Collapsed;
            RaisePropertyChanged(nameof(NotDraw));
        }

        public BrieflyVM(MainVM main, Location location)
        {
            mainVM = main;

            Location = location;

            ForecastModel = new ForecastModel(location);

            hourModel = new HourModel();

            LoadingStatusVM = new LoadingStatusVM();

            Update = new DelegateCommand(() =>
            {
                if (LoadingStatusVM.Status == eStatus.Error)
                {
                    numAttemps = 0;
                    ForecastModel.LoadData();
                }
            });

            Close = new DelegateCommand(() =>
            {
                mainVM.DeleteFromFavorites.Execute(this);

                isFavorite = false;
                RaisePropertyChanged(nameof(IsContextMenuEnable));
            });

            AddToFavorites = new DelegateCommand(() =>
            {
                mainVM.AddToFavorites.Execute(this);

                isFavorite = true;
                RaisePropertyChanged(nameof(IsContextMenuEnable));
            });

            ForecastModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Loading")
                    LoadingStatusVM.LoadingStatus.Execute();
                if (e.PropertyName == "LoadingCompleted")
                {
                    hourModel = new HourModel(ForecastModel.CurrentForecast);

                    RaisePropertyChanged(nameof(City));
                    RaisePropertyChanged(nameof(Temperature));
                    RaisePropertyChanged(nameof(Icon));
                    RaisePropertyChanged(nameof(BackgroundImage));

                    LoadingStatusVM.ResetStatus.Execute();

                    RaisePropertyChanged("LoadingCompleted");
                }
                if (e.PropertyName == "LoadingError")
                {
                    if (numAttemps < 3)
                        ForecastModel.LoadData();
                    else
                        LoadingStatusVM.ErrorStatus.Execute();

                    numAttemps++;
                }

                RaisePropertyChanged("Visibility");
            };

            ForecastModel.LoadData();
        }

        public string GetTemp()
        {
            var temp = ForecastModel?.GetTemperature()?.Temp;
            if (temp != null)
                return $"{Math.Round(temp.Value)} C";

            return null;
        }
    }
}

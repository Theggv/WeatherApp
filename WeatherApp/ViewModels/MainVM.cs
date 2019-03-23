using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Prism.Mvvm;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Device.Location;

namespace WeatherApp
{
    public class MainVM : BindableBase
    {
        private BrieflyVM currentBriefly;
        private CommonVM currentCommon;

        private MainModel model;

        private bool isShowDetail;


        public BitmapImage BackgroundImage => CurrentBriefly?.ForecastModel?.GetImage();


        public BrieflyVM CurrentBriefly
        {
            get => currentBriefly;
            set
            {
                if (value != currentBriefly && value != null)
                {
                    currentBriefly = value;

                    RaisePropertyChanged(nameof(CurrentBriefly));

                    if (value.Location != null)
                    {
                        CurrentCommon = new CommonVM(this, value);

                        RaisePropertyChanged(nameof(BackgroundImage));

                        if (isShowDetail)
                            DetailVM.ChangeLocation.Execute(value.Location);
                    }

                    CurrentBriefly.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == "LoadingCompleted")
                        {
                            CurrentCommon = new CommonVM(this, value);

                            RaisePropertyChanged(nameof(BackgroundImage));

                            if (isShowDetail)
                                DetailVM.ChangeLocation.Execute(value.Location);
                        }
                    };
                }
            }
        }

        public CommonVM CurrentCommon
        {
            get => currentCommon;
            set
            {
                currentCommon = value;

                RaisePropertyChanged(nameof(CurrentCommon));
                RaisePropertyChanged(nameof(IsShowCommon));
            }
        }

        public ObservableCollection<BrieflyVM> FavoriteList { get; set; }

        public ForecastVM DetailVM { get; set; }

        public SearchVM SearchVM { get; set; }

        public AdvancedSearchVM AdvancedSearchVM => SearchVM?.AdvancedSearchVM;

        public GetResourceVM GetResourceVM { get; set; }


        public Visibility IsShowCommon => (!isShowDetail && CurrentCommon != null) ?
            Visibility.Visible : Visibility.Collapsed;

        public Visibility IsShowDetail => isShowDetail ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IsShowSearch => SearchVM.IsShowDetailSearch ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IsShowDownload { get; set; } = Visibility.Collapsed;



        public DelegateCommand Close { get; set; }

        public DelegateCommand Back { get; set; }

        public DelegateCommand HideDetail { get; set; }

        public DelegateCommand DownloadInfo { get; set; }

        public DelegateCommand ShowDetail { get; set; }

        public DelegateCommand<BrieflyVM> AddToFavorites { get; set; }

        public DelegateCommand<BrieflyVM> DeleteFromFavorites { get; set; }

        public DelegateCommand GetUserLocation { get; set; }


        public MainVM()
        {
            model = new MainModel();

            SearchVM = new SearchVM(this);
            GetResourceVM = new GetResourceVM();
            CurrentBriefly = new BrieflyVM();

            FavoriteList = new ObservableCollection<BrieflyVM>();

            SearchVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsShowDetailSearch")
                {
                    if (IsShowDownload == Visibility.Visible)
                        DownloadInfo.Execute();

                    RaisePropertyChanged(nameof(IsShowSearch));
                }

                if (e.PropertyName == "CitySelected")
                {
                    CurrentBriefly = new BrieflyVM(this, SearchVM.AdvancedSearchVM.SelectedCity);

                    RaisePropertyChanged("CurrentBriefly");
                }
            };

            Close = new DelegateCommand(() =>
            {
                var settings = new UserSettings();
                settings.SaveSettings(FavoriteList, FavoriteList?.First());

                Environment.Exit(0);
            });

            Back = new DelegateCommand(() =>
            {
                DetailVM?.Back.Execute();
            });

            ShowDetail = new DelegateCommand(() =>
            {
                if (CurrentBriefly.LoadingStatusVM.Status == LoadingStatusVM.eStatus.Error)
                    return;

                isShowDetail = true;

                DetailVM = new ForecastVM(CurrentCommon.Location);
                DetailVM.SetRootVM(this);

                RaisePropertyChanged(nameof(IsShowCommon));
                RaisePropertyChanged(nameof(IsShowDetail));
                RaisePropertyChanged(nameof(DetailVM));
            });

            HideDetail = new DelegateCommand(() =>
            {
                isShowDetail = false;

                DetailVM = null;

                RaisePropertyChanged(nameof(IsShowCommon));
                RaisePropertyChanged(nameof(IsShowDetail));
                RaisePropertyChanged(nameof(DetailVM));
            });

            DownloadInfo = new DelegateCommand(() =>
            {
                if (IsShowDownload == Visibility.Collapsed)
                {
                    if (SearchVM.IsShowDetailSearch)
                        SearchVM.Menu.Execute();

                    IsShowDownload = Visibility.Visible;
                }
                else
                    IsShowDownload = Visibility.Collapsed;

                RaisePropertyChanged(nameof(IsShowDownload));
            });

            GetResourceVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "UnavailableCountries")
                    SearchVM.AdvancedSearchVM.UpdateCountries.Execute();
            };

            AddToFavorites = new DelegateCommand<BrieflyVM>((item) =>
            {
                if (FavoriteList.Count(vm => vm.City?.Id == item.City?.Id) > 0)
                    return;

                FavoriteList.Add(item);
            });

            DeleteFromFavorites = new DelegateCommand<BrieflyVM>((item) =>
            {
                if (FavoriteList.Count(vm => vm.City?.Id == item.City?.Id) == 0)
                    return;

                FavoriteList.Remove(FavoriteList
                    .First(vm => vm.City?.Id == item.City?.Id));
            });

            GetUserLocation = new DelegateCommand(() =>
            {
                model.GetLocation();
            });

            model.PropertyChanged += (s, e) =>
            {
                if(e.PropertyName == "UserLocation")
                {
                    CurrentBriefly = new BrieflyVM(this, new Location
                    {
                        Latitude = model.UserLocation.Latitude,
                        Longtitude = model.UserLocation.Longitude,
                        IsSearchByCoords = true
                    });
                }
                if (e.PropertyName == "SettingsLoaded")
                {
                    CurrentBriefly = new BrieflyVM(this, model.DefaultLocation);

                    FavoriteList = new ObservableCollection<BrieflyVM>(model.FavoriteList
                        .Select(item => new BrieflyVM(this, item)));
                }
            };
        }
    }

    public class MainModel : BindableBase
    {
        public GeoCoordinate UserLocation { get; set; }

        public List<Location> FavoriteList { get; set; }

        public Location DefaultLocation { get; set; }


        public MainModel()
        {
            Task.Run(() =>
            {
                var settings = UserSettings.LoadSettings();

                FavoriteList = settings.Locations.Select(item => item.ToLocation()).ToList();
                DefaultLocation = settings.DefaultLocation.ToLocation();

                RaisePropertyChanged("SettingsLoaded");
            });
        }

        public void GetLocation()
        {
            GeoCoordinateWatcher watcher = new GeoCoordinateWatcher();

            watcher.StatusChanged += (s, e) =>
            {
                if (e.Status == GeoPositionStatus.Ready)
                {
                    UserLocation = watcher.Position.Location;

                    RaisePropertyChanged(nameof(UserLocation));
                }
            };
            watcher.Start();
        }
    }
}

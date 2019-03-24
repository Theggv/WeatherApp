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
using System.Data.SqlClient;
using System.IO;
using Microsoft.Win32;
using Microsoft.Office.Interop.Excel;

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
                            RaisePropertyChanged("SearchLoadingCompleted");

                            if (isShowDetail)
                                DetailVM.ChangeLocation.Execute(value.Location);
                        }
                        if(e.PropertyName == "LoadingError")
                        {
                            RaisePropertyChanged("SearchLoadingCompleted");
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

        public DelegateCommand SaveReport { get; set; }


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

                if (e.PropertyName == "AdvancedSearchSelected")
                {
                    CurrentBriefly = new BrieflyVM(this, SearchVM.AdvancedSearchVM.SelectedCity);

                    RaisePropertyChanged("CurrentBriefly");
                }
                if (e.PropertyName == "QueryValid")
                {
                    CurrentBriefly = new BrieflyVM(this, new Location
                    {
                        Name = SearchVM.Query.Trim()
                    });

                    RaisePropertyChanged("CurrentBriefly");
                }
            };

            Close = new DelegateCommand(() =>
            {
                var settings = new UserSettings();
                settings.SaveSettings(FavoriteList, FavoriteList?.FirstOrDefault());

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

            SaveReport = new DelegateCommand(() =>
            {
                model.SaveReport();
            });

            model.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "UserLocation")
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
                    if(model.DefaultLocation != null)
                        CurrentBriefly = new BrieflyVM(this, model.DefaultLocation);

                    FavoriteList = new ObservableCollection<BrieflyVM>(model.FavoriteList
                        .Select(item => new BrieflyVM(this, item)).AsParallel());
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

                if (settings == null)
                    return;

                FavoriteList = settings.Locations.Select(item => item.ToLocation()).ToList();
                DefaultLocation = settings.DefaultLocation?.ToLocation();

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

        public void SaveReport()
        {
            using (SqlConnection conn = new SqlConnection(ConnectionInfo.ConnString))
            {
                conn.Open();

                SqlCommand command = new SqlCommand
                {
                    Connection = conn,
                    CommandText = @"
SELECT name, alternatename, MIN([from]), MAX([to]), AVG([temperature].value),
MIN([temperature].value), MAX([temperature].value)
  FROM [forecast]
  INNER JOIN [temperature] ON [forecast].id = [temperature].forecast_id
  INNER JOIN [location] ON [forecast].city_id = [location].forecast_id
  WHERE [forecast].city_id <> 0
  GROUP BY name, alternatename
ORDER BY [name] DESC;"
                };

                using(SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        MessageBox.Show("В базе данных нет информации для формирования отчёта.\n" +
                            "Попробуйте повторить попытку.");
                        return;
                    }

                    var app = new Microsoft.Office.Interop.Excel.Application
                    {
                        Visible = true
                    };

                    app.Workbooks.Add();

                    Worksheet worksheet = (Worksheet)app.ActiveSheet;
                    worksheet.Cells[1, 1] = "Название города";
                    worksheet.Cells[1, 2] = "Альтернативное название города";
                    worksheet.Cells[1, 3] = "От";
                    worksheet.Cells[1, 4] = "До";
                    worksheet.Cells[1, 5] = "Средняя температура";
                    worksheet.Cells[1, 6] = "Минимальная температура";
                    worksheet.Cells[1, 7] = "Максимальная температура";

                    worksheet.StandardWidth = 20;

                    int curRow = 2;

                    object[] values = new object[reader.FieldCount];
                    
                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        worksheet.Cells[curRow, 1] = values[0].ToString();
                        worksheet.Cells[curRow, 2] = values[1].ToString();
                        worksheet.Cells[curRow, 3] = DateTime.Parse(values[2].ToString()).ToLongDateString();
                        worksheet.Cells[curRow, 4] = DateTime.Parse(values[3].ToString()).ToLongDateString();
                        worksheet.Cells[curRow, 5] = Math.Round(FixDouble.ToDouble(values[4].ToString()), 1);
                        worksheet.Cells[curRow, 6] = Math.Round(FixDouble.ToDouble(values[5].ToString()), 1);
                        worksheet.Cells[curRow, 7] = Math.Round(FixDouble.ToDouble(values[6].ToString()), 1);

                        curRow++;
                    }
                }
            }
        }
    }
}

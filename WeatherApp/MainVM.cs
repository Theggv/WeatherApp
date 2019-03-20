using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Prism.Mvvm;
using Prism.Commands;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace WeatherApp
{
    public class MainVM: BindableBase
    {
        private MainModel model;
        private Country selectedCountry;
        private City selectedCity;
        private BitmapImage bgImage;

        public BitmapImage BackgroundImage
        {
            get => bgImage;
            set
            {
                if(value != null)
                {
                    bgImage = value;

                    RaisePropertyChanged("BackgroundImage");
                }
            }
        }

        public ObservableCollection<CommonVM> Common { get; set; }

        public ForecastVM DetailVM { get; set; }

        public SearchVM SearchVM { get; set; }

        public AdvancedSearchVM AdvancedSearchVM => SearchVM?.AdvancedSearchVM;

        public GetResourceVM GetResourceVM { get; set; }

        public CommonVM SelectedCommon
        {
            set
            {
                if (value != null && value.Visibility != Visibility.Collapsed)
                {
                    SelectDetail.Execute(value.Location);
                    BackgroundImage = value.BackgroundImage;
                }
            }
        }


        public List<Country> Countries => model.Countries;

        public Country SelectedCountry
        {
            get => selectedCountry;
            set
            {
                if(selectedCountry != value)
                {
                    selectedCountry = value;

                    model.LoadCities(selectedCountry);
                }
            }
        }

        public List<City> Cities => model.Cities;

        public City SelectedCity
        {
            get => selectedCity;
            set
            {
                if (selectedCity != value && value != null)
                {
                    selectedCity = value;

                    Common.RemoveAt(0);
                    Common.Add(new CommonVM(this, selectedCity));

                    RaisePropertyChanged("Common");
                }
            }
        }
        

        public Visibility IsDetailSelected => DetailVM != null ? Visibility.Collapsed : Visibility.Visible;

        public Visibility IsShowDetail => DetailVM == null ? Visibility.Collapsed : Visibility.Visible;

        public Visibility IsShowSearch => SearchVM.IsShowDetailSearch ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IsShowDownload { get; set; } = Visibility.Collapsed;



        public DelegateCommand Close { get; set; }

        public DelegateCommand Back { get; set; }

        public DelegateCommand HideDetail { get; set; }

        public DelegateCommand DownloadInfo { get; set; }

        public DelegateCommand<City> SelectDetail { get; set; }


        public MainVM()
        {
            model = new MainModel();
            SearchVM = new SearchVM(this);
            GetResourceVM = new GetResourceVM();

            SearchVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsShowDetailSearch")
                {
                    if (IsShowDownload == Visibility.Visible)
                        DownloadInfo.Execute();

                    RaisePropertyChanged(nameof(IsShowSearch));
                }

                if(e.PropertyName == "CitySelected")
                {
                    Common.RemoveAt(0);
                    Common.Add(new CommonVM(this, SearchVM.AdvancedSearchVM.SelectedCity));

                    RaisePropertyChanged("Common");
                }
            };

            model.PropertyChanged += (s, e) =>
            {
                if(e.PropertyName == "Countries")
                {
                    RaisePropertyChanged("Countries");
                }
                if (e.PropertyName == "Cities")
                {
                    RaisePropertyChanged("Cities");
                }
            };

            model.LoadCountries();

            Common = new ObservableCollection<CommonVM>
            {
                new CommonVM(this, new City(2643743)),
                new CommonVM(this, new City(4317656)),
                new CommonVM(this, new City(511196)),
                new CommonVM(this, new City(524901))
            };

            BackgroundImage = Common[0].BackgroundImage;

            Close = new DelegateCommand(() =>
            {
                Environment.Exit(0);
            });

            Back = new DelegateCommand(() =>
            {
                DetailVM?.Back.Execute();
            });

            SelectDetail = new DelegateCommand<City>((city) =>
            {
                DetailVM = new ForecastVM(city);
                DetailVM.SetRootVM(this);

                RaisePropertyChanged(nameof(IsDetailSelected));
                RaisePropertyChanged(nameof(IsShowDetail));
                RaisePropertyChanged(nameof(DetailVM));
            });

            HideDetail = new DelegateCommand(() =>
            {
                DetailVM = null;
                SelectedCommon = null;

                RaisePropertyChanged(nameof(IsDetailSelected));
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
        }
    }

    public class MainModel : BindableBase
    {
        public List<Country> Countries { get; private set; }

        public List<City> Cities { get; private set; }

        public MainModel() { }

        public void LoadCountries()
        {
            using (SqlConnection conn = new SqlConnection(ConnectionInfo.ConnString))
            {
                conn.Open();

                SqlCommand command = new SqlCommand
                {
                    Connection = conn
                };

                command.CommandText =   "SELECT [code], [name] " +
                                        "FROM [weatherdb].[dbo].[country];";

                SqlDataReader reader = command.ExecuteReader();

                if (!reader.HasRows)
                {
                    reader.Close();
                    return;
                }

                object[] values = new object[reader.FieldCount];

                Countries = new List<Country>();

                while (reader.Read())
                {
                    reader.GetValues(values);
                    Countries.Add(new Country()
                    {
                        Code = values[0].ToString(),
                        Name = values[1].ToString()
                    });
                }

                Countries.Sort((a, b) => a.Name.CompareTo(b.Name));
            }

            RaisePropertyChanged(nameof(Countries));
        }

        public void LoadCities(Country country)
        {
            Task.Run(() =>
            {
                using (SqlConnection conn = new SqlConnection(ConnectionInfo.ConnString))
                {
                    conn.Open();

                    SqlCommand command = new SqlCommand
                    {
                        Connection = conn
                    };

                    command.CommandText = "SELECT [external_id], [city].[name], [country].[name] " +
                                            "FROM[dbo].[city] " +
                                            "JOIN[dbo].[country] " +
                                            "ON country.code = country_id " +
                                            $"WHERE [city].[country_id] = '{country.Code}';";

                    SqlDataReader reader = command.ExecuteReader();

                    if (!reader.HasRows)
                    {
                        reader.Close();
                        return;
                    }

                    object[] values = new object[reader.FieldCount];

                    Cities = new List<City>();

                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        Cities.Add(new City()
                        {
                            Id = int.Parse(values[0].ToString()),
                            Name = values[1].ToString(),
                            Country = values[2].ToString(),
                        });
                    }

                    Cities.Sort((a, b) => a.Name.CompareTo(b.Name));
                }

                RaisePropertyChanged(nameof(Cities));
            });

            
        }
    }
}

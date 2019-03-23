using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Prism.Mvvm;
using Prism.Commands;
using System.Data.SqlClient;
using System.Windows.Controls;

namespace WeatherApp
{
    public class AdvancedSearchVM : BindableBase
    {
        private AdvancedSearchModel model;

        private Location selectedCountry;
        private Location selectedRegion;
        private Location selectedCity;


        public bool ByCoords { get; set; } = false;


        public Visibility LoadingContriesVis { get; set; } = Visibility.Collapsed;

        public Visibility LoadingRegionsVis { get; set; } = Visibility.Collapsed;

        public Visibility LoadingCitiesVis { get; set; } = Visibility.Collapsed;


        public List<Location> Countries => model.Countries;

        public List<Location> Regions => model.Regions;

        public List<Location> Cities => model.Cities.Take(1000).ToList();


        public Location SelectedCountry
        {
            get => selectedCountry;
            set
            {
                if (value != null)
                    selectedCountry = value;

                Task.Run(() =>
                {
                    model.GetRegions(selectedCountry);
                    model.GetCities(selectedCountry);
                });
            }
        }

        public Location SelectedRegion
        {
            get => selectedRegion;
            set
            {
                if (value != null)
                    selectedRegion = value;

                Task.Run(() =>
                {
                    RaisePropertyChanged();
                    model.GetCities(selectedRegion);
                });
            }
        }

        public Location SelectedCity
        {
            get => selectedCity;
            set
            {
                if (value != null)
                    selectedCity = value;
            }
        }

        public ComboBoxItem SelectedItem
        {
            get;
            set;
        }

        public DelegateCommand Search { get; set; }

        public DelegateCommand UpdateCountries { get; set; }


        public AdvancedSearchVM()
        {
            model = new AdvancedSearchModel();

            model.PropertyChanged += (s, e) =>
            {
                if(e.PropertyName == "Countries")
                {
                    if (Countries.Count == 0)
                        LoadingContriesVis = Visibility.Visible;
                    else
                        LoadingContriesVis = Visibility.Collapsed;

                    RaisePropertyChanged(nameof(LoadingContriesVis));
                    RaisePropertyChanged(nameof(Countries));
                }
                if (e.PropertyName == "Regions")
                {
                    if (Regions.Count == 0)
                        LoadingRegionsVis = Visibility.Visible;
                    else
                        LoadingRegionsVis = Visibility.Collapsed;

                    RaisePropertyChanged(nameof(LoadingRegionsVis));
                    RaisePropertyChanged(nameof(Regions));
                }
                if (e.PropertyName == "Cities")
                {
                    if (Cities.Count == 0)
                        LoadingCitiesVis = Visibility.Visible;
                    else
                        LoadingCitiesVis = Visibility.Collapsed;

                    RaisePropertyChanged(nameof(LoadingCitiesVis));
                    RaisePropertyChanged(nameof(Cities));
                }
            };

            Search = new DelegateCommand(() =>
            {
                if (SelectedCity != null)
                {
                    SelectedCity.IsSearchByCoords = ByCoords;

                    RaisePropertyChanged("StartSearch");
                }
            });

            UpdateCountries = new DelegateCommand(() =>
            {
                Task.Run(() =>
                {
                    model.GetCountries();
                });
            });

            UpdateCountries.Execute();
        }
    }

    public class AdvancedSearchModel : BindableBase
    {
        public List<Location> Countries { get; set; }
        public List<Location> Regions { get; set; }
        public List<Location> Cities { get; set; }


        public AdvancedSearchModel()
        {
            Countries = new List<Location>();
            Regions = new List<Location>();
            Cities = new List<Location>();
        }


        public void GetCountries()
        {
            Countries.Clear();
            RaisePropertyChanged(nameof(Countries));

            var countries = new List<Location>();

            using (SqlConnection conn = new SqlConnection(ConnectionInfo.ConnString))
            {
                conn.Open();

                SqlCommand command = new SqlCommand
                {
                    Connection = conn
                };

                command.CommandText = @"
SELECT * FROM [location]
WHERE [id] in 
    (SELECT MIN(id) FROM [location]
    WHERE [feature_code] = 'PCLI' and [country_code] in
        (SELECT [country_code] FROM [location]
        GROUP BY country_code)
    GROUP BY [name])
ORDER BY [name]";

                using (SqlDataReader reader = command.ExecuteReaderAsync().Result)
                {
                    if (!reader.HasRows)
                        return;

                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        countries.Add(new Location(values));
                    }
                }
            }

            Countries = countries.ToList();
            RaisePropertyChanged(nameof(Countries));
        }

        public void GetRegions(Location country)
        {
            Regions.Clear();
            RaisePropertyChanged(nameof(Regions));

            if (country == null)
                return;

            var regions = new List<Location>();

            using (SqlConnection conn = new SqlConnection(ConnectionInfo.ConnString))
            {
                conn.Open();

                SqlCommand command = new SqlCommand
                {
                    Connection = conn
                };

                command.CommandText =
                    $"SELECT * FROM [location]" +
                    $"WHERE [id] in " +
                        $"(SELECT MIN(id)" +
                        $"FROM [location]" +
                        $"WHERE [feature_code] = 'ADM1' AND [country_code] = '{country.CountryCode}'" +
                        $"GROUP BY [name])" +
                    $"ORDER BY [population] DESC";

                using (SqlDataReader reader = command.ExecuteReaderAsync().Result)
                {
                    if (!reader.HasRows)
                        return;

                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        regions.Add(new Location(values));
                    }
                }
            }

            Regions = regions.ToList();
            RaisePropertyChanged(nameof(Regions));
        }

        public void GetCities(Location location)
        {
            Cities.Clear();
            RaisePropertyChanged(nameof(Cities));

            if (location == null)
                return;

            var cities = new List<Location>();

            using (SqlConnection conn = new SqlConnection(ConnectionInfo.ConnString))
            {
                conn.Open();

                SqlCommand command = new SqlCommand
                {
                    Connection = conn
                };

                // location is country
                if (location.FeatureCode == "PCLI")
                {
                    command.CommandText =
                        $"SELECT * FROM [location] " +
                        $"WHERE [id] in " +
                            $"(SELECT MIN(id) " +
                            $"FROM [location] " +
                            $"WHERE ([feature_code] = 'PPL' OR [feature_code] = 'PPLA' OR [feature_code] = 'PPLC') " +
                            $"AND [country_code] = '{location.CountryCode}' " +
                            $"GROUP BY [name]) " +
                        $"ORDER BY [population] DESC ";
                }
                // location is region
                else
                {
                    command.CommandText =
                        $"SELECT * FROM [location] " +
                        $"WHERE [id] in " +
                            $"(SELECT MIN(id) " +
                            $"FROM [location] " +
                            $"WHERE ([feature_code] = 'PPL' OR [feature_code] = 'PPLA' OR [feature_code] = 'PPLC') " +
                            $"AND [country_code] = '{location.CountryCode}' AND [admin1_code] = '{location.AdminCode1}' " +
                            $"GROUP BY [name]) " +
                        $"ORDER BY [population] DESC";
                }

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.HasRows)
                        return;

                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        cities.Add(new Location(values));
                    }
                }
            }

            Cities = cities.ToList();
            RaisePropertyChanged(nameof(Cities));
        }
    }
}

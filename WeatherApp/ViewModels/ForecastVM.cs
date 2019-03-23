using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.Commands;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;

namespace WeatherApp
{
    public class ForecastVM : BindableBase
    {
        private int numAttemps;

        private ForecastModel model;
        private MainVM rootVM;
        private DailyVM selectedForecast;

        public string WeatherName => model.WeatherName;

        public DetailVM DetailViewModel { get; set; }

        public City City => model.GetCity();

        public string DisplayLocation
        {
            get
            {
                string str = City?.Name;

                if (str == null)
                    return null;

                string region = model.GetRegion();
                
                if (region != null)
                    str += $", {region}";

                return str;
            }
        }

        public string DisplayCountry
        {
            get
            {
                string country = model.GetCountry();

                if (country != null)
                    return $"{country}";
                else
                    return $"{City?.Country}";
            }
        }

        public ObservableCollection<DailyVM> DailyForecasts => model.GetDailyForecasts();

        public Forecast CurrentForecast => model.CurrentForecast;

        public TemperatureVM Temperature => model.GetTemperature();

        public LoadingStatusVM LoadingStatusVM { get; set; }

        public DelegateCommand<Location> ChangeLocation { get; private set; }

        public DelegateCommand Back { get; set; }

        public DailyVM SelectedForecast
        {
            get => selectedForecast;
            set
            {
                selectedForecast = value;

                if (value != null)
                    DetailViewModel.GetData.Execute(value.Daily);
            }
        }

        public Visibility ElementVis => CurrentForecast != null ? Visibility.Visible : Visibility.Hidden;

        public DelegateCommand Update { get; set; }


        public ForecastVM(Location location)
        {
            DetailViewModel = new DetailVM(this);

            LoadingStatusVM = new LoadingStatusVM();

            Back = new DelegateCommand(() =>
            {
                rootVM.HideDetail.Execute();
            });

            Update = new DelegateCommand(() =>
            {
                if (LoadingStatusVM.Status == LoadingStatusVM.eStatus.Error)
                {
                    numAttemps = 0;
                    model.LoadData();
                }
            });

            ChangeLocation = new DelegateCommand<Location>((loc) =>
            {
                numAttemps = 0;

                model = new ForecastModel(loc);

                model.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "Visibility")
                        RaisePropertyChanged("ElementVis");
                    if (e.PropertyName == "Loading")
                        LoadingStatusVM.LoadingStatus.Execute();
                    if (e.PropertyName == "LoadingCompleted")
                    {
                        RaisePropertyChanged(nameof(WeatherName));
                        RaisePropertyChanged(nameof(DetailViewModel));
                        RaisePropertyChanged(nameof(DisplayLocation));
                        RaisePropertyChanged(nameof(DisplayCountry));
                        RaisePropertyChanged(nameof(DailyForecasts));
                        RaisePropertyChanged(nameof(CurrentForecast));
                        RaisePropertyChanged(nameof(Temperature));

                        LoadingStatusVM.ResetStatus.Execute();
                    }
                    if (e.PropertyName == "LoadingError")
                    {
                        if (numAttemps < 3)
                            model.LoadData();
                        else
                            LoadingStatusVM.ErrorStatus.Execute();

                        numAttemps++;
                    }
                };

                model.LoadData();
            });

            ChangeLocation.Execute(location);
        }

        public List<HourVM> GetDetailForecast(Forecast f) => model.GetDetailForecast(f);

        public void SetRootVM(MainVM vm)
        {
            rootVM = vm;
        }
    }

    public class ForecastModel : BindableBase
    {
        private City _city;
        private Location _location;
        private WeatherData _data;
        private Forecast _current;

        public string WeatherName => GetWeatherName();

        public Forecast CurrentForecast
        {
            get => _current;
            set
            {
                if(_current != GetCurrentForecast())
                {
                    _current = GetCurrentForecast();

                    RaisePropertyChanged("Visibility");
                }
            }
        }

        public ForecastModel(City city)
        {
            _city = city;
        }

        public ForecastModel(Location location)
        {
            _location = location;
        }

        public void LoadData()
        {
            string query;

            if(_location != null)
                query = ApiQuery.ToQuery(_location);
            else
                query = ApiQuery.ToQuery(_city);

            Task.Run(() =>
            {
                RaisePropertyChanged("Loading");

                try
                {
                    // Try get data from api
                    _data = new WeatherData(query);

                    _city = _data.Location;

                    UpdateForecast();
                }
                catch
                {
                    // Try to get data from database

                    if(_city != null)
                        ExtractDataFromDB(_city.Id);
                    else if(_location != null)
                        ExtractDataFromDB(_location.ForecastId);
                    else
                        RaisePropertyChanged("LoadingError");
                }
            });
        }


        public void UpdateForecast()
        {
            string delQ = $"DELETE FROM [forecast] WHERE city_id = {_data.Location.Id};";

            string insForecastQ = $"INSERT INTO [forecast] (city_id, [from], [to], humidity) VALUES";
            string insTempQ = $"INSERT INTO [temperature] ([forecast_id], [unit], [value], [min], [max]) VALUES";
            string insCloudsQ = $"INSERT INTO [clouds] ([forecast_id], [value], [all]) VALUES";
            string insPrecQ = $"INSERT INTO [precipitation] ([forecast_id], [unit], [value], [type]) VALUES";
            string insSymbolQ = $"INSERT INTO [symbol] ([forecast_id], [number], [name], [var]) VALUES";
            string insWindDirQ = $"INSERT INTO [wind_dir] ([forecast_id], [degrees], [code], [name]) VALUES";
            string insWindSpeedQ = $"INSERT INTO [wind_speed] ([forecast_id], [mps], [name]) VALUES";
            string insPressureQ = $"INSERT INTO [pressure] ([forecast_id], [unit], [value]) VALUES";

            using (SqlConnection conn = new SqlConnection(ConnectionInfo.ConnString))
            {
                conn.Open();

                SqlCommand command = new SqlCommand
                {
                    Connection = conn,
                    CommandText = delQ
                };

                command.ExecuteNonQuery();

                SqlDataReader reader;

                foreach (var f in _data.Forecasts)
                {
                    command.CommandText = insForecastQ + f.ToQuery(_data.Location) + ";SELECT @@IDENTITY as [id];";

                    reader = command.ExecuteReader();

                    if (!reader.HasRows)
                    {
                        reader.Close();
                        throw new Exception();
                    }

                    reader.Read();
                    f.Id = int.Parse(reader[0].ToString());

                    reader.Close();

                    insWindDirQ += f.Wind?.ToDirQuery(f) ?? "";
                    insWindSpeedQ += f.Wind?.ToSpeedQuery(f) ?? "";
                    insCloudsQ += f.Clouds?.ToQuery(f) ?? "";
                    insTempQ += f.Temperature?.ToQuery(f) ?? "";
                    insPrecQ += f.Precipitation?.ToQuery(f) ?? "";
                    insSymbolQ += f.Symbol?.ToQuery(f) ?? "";
                    insPressureQ += f.Pressure?.ToQuery(f) ?? "";
                }

                command.CommandText = insWindDirQ.Remove(insWindDirQ.Length - 1);
                command.ExecuteNonQuery();

                command.CommandText = insWindSpeedQ.Remove(insWindSpeedQ.Length - 1);
                command.ExecuteNonQuery();

                command.CommandText = insCloudsQ.Remove(insCloudsQ.Length - 1);
                command.ExecuteNonQuery();

                command.CommandText = insTempQ.Remove(insTempQ.Length - 1);
                command.ExecuteNonQuery();

                command.CommandText = insPrecQ.Remove(insPrecQ.Length - 1);
                command.ExecuteNonQuery();

                command.CommandText = insSymbolQ.Remove(insSymbolQ.Length - 1);
                command.ExecuteNonQuery();

                command.CommandText = insPressureQ.Remove(insPressureQ.Length - 1);
                command.ExecuteNonQuery();
            }

            UpdateLocationId();

            CurrentForecast = GetCurrentForecast();
            RaisePropertyChanged("LoadingCompleted");
        }
        

        public void ExtractDataFromDB(int cityId)
        {
            _data = new WeatherData();

            using (SqlConnection conn = new SqlConnection(ConnectionInfo.ConnString))
            {
                conn.Open();

                SqlCommand command = new SqlCommand
                {
                    Connection = conn
                };

                command.CommandText = "SELECT * FROM [dbo].[city] " +
                                        $"WHERE [external_id] = {cityId};";

                using(SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        RaisePropertyChanged("LoadingError");
                        return;
                    }


                    object[] values = new object[reader.FieldCount];

                    reader.Read();
                    reader.GetValues(values);

                    _data.Location = new City
                    {
                        Id = cityId,
                        Name = values[2].ToString(),
                        Country = values[3].ToString()
                    };
                }

                command.CommandText = "SELECT * FROM [dbo].[forecast] " +
                                        "[INNER] JOIN [dbo].[temperature] " +
                                        "ON id = [temperature].[forecast_id] " +
                                        "JOIN [dbo].precipitation " +
                                        "ON id = [precipitation].forecast_id " +
                                        "JOIN [dbo].pressure " +
                                        "ON id = [pressure].forecast_id " +
                                        "JOIN [dbo].symbol " +
                                        "ON id = symbol.forecast_id " +
                                        "JOIN [dbo].wind_dir " +
                                        "ON id = wind_dir.forecast_id " +
                                        "JOIN [dbo].wind_speed " +
                                        "ON id = wind_speed.forecast_id " +
                                        "JOIN [dbo].clouds " +
                                        "ON id = clouds.forecast_id " +
                                        $"WHERE [city_id] = {cityId};";

                try
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            RaisePropertyChanged("LoadingError");

                            return;
                        }

                        object[] values = new object[reader.FieldCount];

                        while (reader.Read())
                        {
                            reader.GetValues(values);
                            _data.Forecasts.Add(new Forecast(values));
                        }
                    }
                }
                catch (SqlException)
                {
                    ExtractDataFromDB(cityId);
                }
            }

            CurrentForecast = GetCurrentForecast();
            RaisePropertyChanged("LoadingCompleted");
        }

        public string GetRegion()
        {
            if (_location?.AdminCode1 == null || _location?.CountryCode == null)
                return null;

            using (SqlConnection conn = new SqlConnection(ConnectionInfo.ConnString))
            {
                conn.Open();

                SqlCommand command = new SqlCommand
                {
                    Connection = conn,
                    CommandText = $"SELECT MAX([name]), MAX([alternatename]) FROM[location] " +
                                    $"WHERE [admin1_code] = '{_location.AdminCode1}' and [feature_code] = 'ADM1' AND [country_code] = '{_location.CountryCode}'" +
                                    $"group by name;"
                };

                using(SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.HasRows)
                        return null;

                    object[] values = new object[reader.FieldCount];

                    reader.Read();
                    reader.GetValues(values);

                    return reader[1].ToString() != "" ? reader[1].ToString() : reader[0].ToString();
                }
            }
        }

        public string GetCountry()
        {
            if (_location?.CountryCode == null)
                return null;

            using (SqlConnection conn = new SqlConnection(ConnectionInfo.ConnString))
            {
                conn.Open();

                SqlCommand command = new SqlCommand
                {
                    Connection = conn,
                    CommandText = $"SELECT MIN([alternatename]) FROM[location] " +
                                    $"WHERE [feature_code] = 'PCLI' AND [country_code] = '{_location.CountryCode}'" +
                                    $"group by name;"
                };

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.HasRows)
                        return null;

                    object[] values = new object[reader.FieldCount];

                    reader.Read();
                    reader.GetValues(values);

                    return reader[0].ToString();
                }
            }
        }

        public City GetCity()
        {
            if (_data?.Location == null)
                return null;

            if (_location == null || _location.Name == null)
                return _data.Location;

            _data.Location.Name = _location.DisplayName;

            return _data.Location;
        }

        public TemperatureVM GetTemperature()
        {
            if (_current != null)
                return new TemperatureVM(_current.Temperature.Value);

            return new TemperatureVM();
        }

        public Forecast GetCurrentForecast()
        {
            if (_data == null)
                return null;

            foreach (var f in _data.Forecasts)
            {
                if (f.From < DateTime.Now && f.To > DateTime.Now)
                {
                    return f;
                }
            }
            
            return null;
        }

        public List<HourVM> GetDetailForecast(Forecast daily)
        {
            if (_data == null)
                return null;

            List<Forecast> detail = new List<Forecast>();

            foreach (var f in _data.Forecasts)
            {
                if (f.From.Date == daily.From.Date)
                    detail.Add(f);
            }

            detail.Sort((a, b) =>
            {
                if (a.From > b.From)
                    return 1;

                if (a.From < b.From)
                    return -1;

                return 0;
            });

            return detail.Select(f => new HourVM(f)).ToList();
        }

        public BitmapImage GetImage()
        {
            if (_current == null)
                return null;

            var basePath = "pack://application:,,,/Resources/Background";
            var code = _current.Symbol.Number;

            if (code >= 200 && code < 300) // Thunderstorm
                return new BitmapImage(new Uri($"{basePath}/thunderstorm.jpg", UriKind.Absolute));
            else if (code >= 300 && code < 400) // Drizzle
                return new BitmapImage(new Uri($"{basePath}/rain.jpg", UriKind.Absolute));
            else if (code >= 500 && code < 600) // Rain
                return new BitmapImage(new Uri($"{basePath}/rain.jpg", UriKind.Absolute));
            else if (code >= 600 && code < 700) // Snow
                return new BitmapImage(new Uri($"{basePath}/snow.jpg", UriKind.Absolute));
            else if (code >= 700 && code < 800) // Atmosphere
                return new BitmapImage(new Uri($"{basePath}/clear_sky.jpg", UriKind.Absolute));
            else if (code == 800) // Clear sky
                return new BitmapImage(new Uri($"{basePath}/clear_sky.jpg", UriKind.Absolute));
            else if (code == 801) // few clouds
                return new BitmapImage(new Uri($"{basePath}/few_clouds.jpg", UriKind.Absolute));
            else if (code == 802) // scattered clouds
                return new BitmapImage(new Uri($"{basePath}/scattered_clouds.jpg", UriKind.Absolute));
            else if (code == 803) // broken clouds
                return new BitmapImage(new Uri($"{basePath}/broken_clouds.jpg", UriKind.Absolute));
            else if (code == 804) // overcast clouds
                return new BitmapImage(new Uri($"{basePath}/overcast.jpg", UriKind.Absolute));

            return null;
        }

        private string GetWeatherName()
        {
            if (_current == null)
                return null;

            var code = _current.Symbol.Number;

            switch (code)
            {
                case 200:
                    return "Гроза с небольшим дождём";
                case 201:
                    return "Гроза с дождём";
                case 202:
                    return "Гроза с сильным дождём";
                case 210:
                    return "Лёгкая гроза";
                case 211:
                    return "Гроза";
                case 212:
                    return "Сильная гроза";
                case 221:
                    return "Оборванная гроза";
                case 230:
                    return "Гроза с небольшим моросящим дождём";
                case 231:
                    return "Гроза с моросящим дождём";
                case 232:
                    return "Гроза с сильным моросящим дождём";

                case 300:
                case 301:
                case 302:
                    return "Изморось";
                case 310:
                case 311:
                case 312:
                case 313:
                case 314:
                case 321:
                    return "Моросящий дождь";

                case 500:
                case 520:
                    return "Лёгкий дождь";
                case 501:
                case 521:
                    return "Умеренный дождь";
                case 502:
                case 522:
                    return "Сильный дождь";
                case 503:
                case 523:
                    return "Очень сильный дождь";
                case 504:
                    return "Экстремально сильный дождь";
                case 511:
                    return "Ледяной дождь";
                    
                case 600:
                case 620:
                    return "Небольшой снег";
                case 601:
                case 621:
                    return "Снег";
                case 602:
                case 622:
                    return "Сильный снег";
                case 611:
                case 612:
                case 615:
                case 616:
                    return "Дождь со снегом";

                case 701:
                    return "Дымка";
                case 721:
                    return "Мгла";
                case 731:
                case 751:
                case 761:
                    return "Пыльная буря";
                case 711:
                case 741:
                    return "Туман";
                case 762:
                    return "Вулканический пепел";
                case 771:
                    return "Шквал";
                case 781:
                    return "Торнадо";

                case 800:
                    return "Ясно";
                case 801:
                    return "Небольшая облачность";
                case 802:
                    return "Рассеянные облака";
                case 803:
                    return "Облачность";
                case 804:
                    return "Пасмурно";

                default:
                    return null;
            }
        }

        public ObservableCollection<DailyVM> GetDailyForecasts()
        {
            if (_data == null)
                return null;

            var daily = new List<Forecast>();

            _data.Forecasts.Sort((a, b) =>
            {
                if (a.From > b.From)
                    return 1;

                if (a.From < b.From)
                    return -1;

                return 0;
            });

            foreach (var f in _data.Forecasts)
            {
                if (daily.Count == 0)
                {
                    daily.Add(f);
                    continue;
                }

                if (daily.Last().From.Date == f.From.Date)
                {
                    // gets minimum temperature
                    if (daily.Last().Temperature.Min > f.Temperature.Min)
                        daily.Last().Temperature.Min = f.Temperature.Min;

                    // gets maximum temperature
                    if (daily.Last().Temperature.Max < f.Temperature.Max)
                        daily.Last().Temperature.Max = f.Temperature.Max;
                }
                else
                {
                    daily.Add(f);
                }
            }

            var obs = new ObservableCollection<DailyVM>();
            daily.ForEach(f => obs.Add(new DailyVM(f)));

            return obs;
        }

        private void UpdateLocationId()
        {
            if (_location == null || _location.Id == 0)
                return;

            _location.ForecastId = _city.Id;

            using(SqlConnection conn = new SqlConnection(ConnectionInfo.ConnString))
            {
                conn.Open();

                SqlCommand command = new SqlCommand
                {
                    Connection = conn,
                    CommandText = $"UPDATE [location] SET [forecast_id] = {_location.ForecastId} " +
                                    $"WHERE [id] = {_location.Id};"
                };

                command.ExecuteNonQuery();
            }
        }
    }
}

using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.IO;
using System.Data.SqlClient;
using Ionic.Zip;
using System.Collections;

namespace WeatherApp
{
    public class GetResourceVM : BindableBase
    {
        private ResourcesModel model;


        public List<Country> AllCountries => model.GetCountries();

        public List<Country> AvailableCountries => model.GetCountries(true);

        public List<Country> UnavailableCountries => Enumerable.Except(AllCountries, AvailableCountries).ToList();


        public Country SelectedCountry { get; set; }


        public double Progress => model.Progress;

        public string Status1 => model.TextStatus1;

        public string Status2 => model.TextStatus2;


        public DelegateCommand Download { get; set; }

        public DelegateCommand Cancel { get; set; }


        public GetResourceVM()
        {
            model = new ResourcesModel();

            model.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Progress")
                    RaisePropertyChanged(nameof(Progress));
                if (e.PropertyName == "TextStatus1")
                    RaisePropertyChanged(nameof(Status1));
                if (e.PropertyName == "TextStatus2")
                    RaisePropertyChanged(nameof(Status2));
                if (e.PropertyName == "Countries")
                {
                    RaisePropertyChanged(nameof(UnavailableCountries));
                    SelectedCountry = null;
                }
            };

            Download = new DelegateCommand(() =>
            {
                if (SelectedCountry != null && model.Status == ResourcesModel.ProgressStatus.Wait)
                    model.DownloadData(SelectedCountry);
            });

            Cancel = new DelegateCommand(() =>
            {
                if (model.Status != ResourcesModel.ProgressStatus.Wait)
                    model.CancelDownload();
            });
        }
    }

    public class ResourcesModel : BindableBase
    {
        private bool isCancel;

        public enum ProgressStatus
        {
            Wait,
            DownloadData,
            DownloadNames,
            Extract,
            Import
        }

        public double Progress { get; set; }

        public ProgressStatus Status { get; set; }

        public string TextStatus1 { get; set; }

        public string TextStatus2 { get; set; }


        public ResourcesModel() { }


        public List<Country> GetCountries(bool onlyAvailable = false)
        {
            var countries = new List<Country>();

            using (SqlConnection conn = new SqlConnection(ConnectionInfo.ConnString))
            {
                conn.Open();

                SqlCommand command = new SqlCommand
                {
                    Connection = conn
                };

                if (onlyAvailable)
                    command.CommandText = @"SELECT [country].[code], [country].[name] 
FROM[location]
INNER JOIN[country]
ON[location].[country_code] = [country].[code]
GROUP BY[country].[name], [country].[code]";
                else
                    command.CommandText = $"SELECT [code], [name] FROM [country];";

                using (SqlDataReader reader = command.ExecuteReaderAsync().Result)
                {
                    if (!reader.HasRows)
                        return countries;

                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        countries.Add(new Country
                        {
                            Code = values[0].ToString(),
                            Name = values[1].ToString()
                        });
                    }
                }
            }

            return countries.OrderBy(x => x.Name).ToList();
        }

        public void DownloadData(Country country)
        {
            isCancel = false;

            var dataPath = $"http://download.geonames.org/export/dump/{country.Code}.zip";
            var namePath = $"http://download.geonames.org/export/dump/alternatenames/{country.Code}.zip";

            if (!Directory.Exists("temp"))
            {
                Directory.CreateDirectory("temp");
                Directory.CreateDirectory("temp/data");
                Directory.CreateDirectory("temp/names");
            }

            using (WebClient webClient = new WebClient())
            {
                webClient.DownloadFileCompleted += (s, e) =>
                {
                    if (e.Cancelled)
                    {
                        DownloadCanceled();
                        return;
                    }

                    if (Status == ProgressStatus.DownloadData)
                    {
                        Status = ProgressStatus.DownloadNames;
                        RaisePropertyChanged(nameof(Status));

                        webClient.DownloadFileAsync(new Uri(namePath), $"temp/{country.Code}_names.zip");
                    }
                    else
                    {
                        Task.Run(() =>
                        {
                            TextStatus1 = $"Распаковка..";
                            TextStatus2 = "";
                            RaisePropertyChanged(nameof(TextStatus1));
                            RaisePropertyChanged(nameof(TextStatus2));

                            try
                            {
                                using (ZipFile zip = new ZipFile($"temp/{country.Code}.zip"))
                                {
                                    zip.IgnoreDuplicateFiles = true;

                                    zip.ExtractProgress += (source, args) =>
                                    {
                                        if (isCancel)
                                        {
                                            zip.ZipErrorAction = ZipErrorAction.InvokeErrorEvent;
                                            return;
                                        }
                                        Progress = args.BytesTransferred / (double)args.TotalBytesToTransfer * 100;
                                        RaisePropertyChanged(nameof(Progress));
                                    };

                                    zip.ZipError += (source, args) =>
                                    {
                                        DownloadCanceled();
                                    };

                                    zip.ExtractAll($"temp/data/{country.Code}");
                                }

                                using (ZipFile zip = new ZipFile($"temp/{country.Code}_names.zip"))
                                {
                                    zip.IgnoreDuplicateFiles = true;

                                    zip.ExtractProgress += (source, args) =>
                                    {
                                        if (isCancel)
                                        {
                                            zip.ZipErrorAction = ZipErrorAction.InvokeErrorEvent;
                                            return;
                                        }

                                        Progress = args.BytesTransferred / (double)args.TotalBytesToTransfer * 100;
                                        RaisePropertyChanged(nameof(Progress));
                                    };

                                    zip.ZipError += (source, args) =>
                                    {
                                        DownloadCanceled();
                                    };

                                    zip.ExtractAll($"temp/names/{country.Code}");
                                }
                            }
                            catch { };

                            ImportData(country);
                        });
                    }
                };

                webClient.DownloadProgressChanged += (s, e) =>
                {
                    if (isCancel)
                        webClient.CancelAsync();

                    Progress = e.ProgressPercentage;
                    RaisePropertyChanged(nameof(Progress));

                    if (Status == ProgressStatus.DownloadData)
                        TextStatus1 = $"Загрузка {country.Code}.zip";
                    else
                        TextStatus1 = $"Загрузка {country.Code}_names.zip";

                    TextStatus2 = $"{e.BytesReceived / 1024} kb / {e.TotalBytesToReceive / 1024} kb";

                    RaisePropertyChanged(nameof(TextStatus1));
                    RaisePropertyChanged(nameof(TextStatus2));
                };

                Status = ProgressStatus.DownloadData;
                RaisePropertyChanged(nameof(Status));

                webClient.DownloadFileAsync(new Uri(dataPath), $"temp/{country.Code}.zip");
            }
        }

        private void ImportData(Country country)
        {
            Status = ProgressStatus.Import;
            RaisePropertyChanged(nameof(Status));

            List<Location> locations = new List<Location>();

            // Extract data
            StreamReader sr = new StreamReader($"temp/data/{country.Code}/{country.Code}.txt");

            while (!sr.EndOfStream)
            {
                locations.Add(new Location(sr.ReadLine().Split('\t')));
            }

            sr.Close();

            // Extract names
            sr = new StreamReader($"temp/names/{country.Code}/{country.Code}.txt");

            List<AlternativeName> names = new List<AlternativeName>();

            while (!sr.EndOfStream)
            {
                names.Add(new AlternativeName(sr.ReadLine().Split('\t')));
            }

            sr.Close();

            var localizedNames = names
                .Where(name => name.Iso == "ru" && name.Name != "")
                .ToList();

            // Skip not important locations
            locations = locations
                .Where(item => item.FeatureClass == "A" || item.FeatureClass == "P")
                .ToList();

            var subset = locations
                .FullOuterJoin(localizedNames,
                loc => loc.Id,
                name => name.GeoNameId, (loc, name) => new { Location = loc, Name = name })
                .Where(item => item.Location != null).ToList();

            using (SqlConnection conn = new SqlConnection(ConnectionInfo.ConnString))
            {
                conn.Open();

                string baseQuery = @"INSERT INTO [dbo].[location]
([forecast_id]
,[name]
,[alternatename]
,[latitude]
,[longtitude]
,[feature_class]
,[feature_code]
,[country_code]
,[admin1_code]
,[admin2_code]
,[admin3_code]
,[admin4_code]
,[population])
VALUES ";

                SqlCommand command = new SqlCommand
                {
                    Connection = conn
                };

                string query = "";

                int numPerQuery = 50;

                Location loc;

                for (int i = 0; i < subset.Count; i++)
                {
                    if (isCancel)
                    {
                        DownloadCanceled();
                        return;
                    }

                    if (i % numPerQuery == 0)
                        query = baseQuery;

                    loc = subset[i].Location;
                    loc.AlternativeName = subset[i].Name?.Name;

                    query += loc.ToQuery();

                    if (i % numPerQuery == numPerQuery - 1 || i == subset.Count - 1)
                    {
                        command.CommandText = query.Remove(query.Length - 1);
                        try
                        {
                            command.ExecuteNonQuery();

                            Progress = i / (double)subset.Count * 100;
                            RaisePropertyChanged(nameof(Progress));

                            TextStatus1 = $"Добавление записей в базу";
                            TextStatus2 = $"{i} / {subset.Count}";
                            RaisePropertyChanged(nameof(TextStatus1));
                            RaisePropertyChanged(nameof(TextStatus2));
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message);
                        }
                    }
                }
            }

            Status = ProgressStatus.Wait;
            RaisePropertyChanged(nameof(Status));

            Progress = 0;
            RaisePropertyChanged(nameof(Progress));

            RemoveTempFiles();

            subset = null;
            locations = null;
            localizedNames = null;

            RaisePropertyChanged("Countries");
        }

        public void CancelDownload()
        {
            isCancel = true;
        }

        private void DownloadCanceled()
        {
            TextStatus1 = $"";
            TextStatus2 = $"";
            Status = ProgressStatus.Wait;
            Progress = 0;

            RaisePropertyChanged(nameof(TextStatus1));
            RaisePropertyChanged(nameof(TextStatus2));
            RaisePropertyChanged(nameof(Status));
            RaisePropertyChanged(nameof(Progress));
        }

        private void RemoveTempFiles()
        {
            TextStatus1 = $"Удаление временных файлов..";
            TextStatus2 = $"";
            RaisePropertyChanged(nameof(TextStatus1));
            RaisePropertyChanged(nameof(TextStatus2));

            var tempPath = new DirectoryInfo("temp");
            var dataPath = new DirectoryInfo("temp/data");
            var namesPath = new DirectoryInfo("temp/names");

            foreach (var f in tempPath.GetFiles())
                f.Delete();

            foreach (var dir in dataPath.GetDirectories())
            {
                foreach (var f in dir.GetFiles())
                    f.Delete();

                dir.Delete();
            }

            foreach (var dir in namesPath.GetDirectories())
            {
                foreach (var f in dir.GetFiles())
                    f.Delete();

                dir.Delete();
            }

            TextStatus1 = $"";
            TextStatus2 = $"";
            RaisePropertyChanged(nameof(TextStatus1));
            RaisePropertyChanged(nameof(TextStatus2));
        }
    }

    public static class Joins
    {
        public static IEnumerable<TResult> FullOuterJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer = null)
        {
            var outerLookup = outer.ToLookup(outerKeySelector, comparer);
            var innerLookup = inner.ToLookup(innerKeySelector, comparer);

            foreach (var innerGrouping in innerLookup)
                if (!outerLookup.Contains(innerGrouping.Key))
                    foreach (TInner innerItem in innerGrouping)
                        yield return resultSelector(default(TOuter), innerItem);

            foreach (var outerGrouping in outerLookup)
                foreach (var innerItem in innerLookup[outerGrouping.Key].DefaultIfEmpty())
                    foreach (var outerItem in outerGrouping)
                        yield return resultSelector(outerItem, innerItem);
        }
    }

    public class JoinedEnumerable<T> : IEnumerable<T>
    {
        public readonly IEnumerable<T> Source;
        public bool IsOuter;

        public JoinedEnumerable(IEnumerable<T> source) { Source = source; }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() { return Source.GetEnumerator(); }
        public IEnumerator GetEnumerator() { return Source.GetEnumerator(); }
    }
}

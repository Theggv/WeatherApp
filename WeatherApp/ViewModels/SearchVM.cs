using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Prism.Commands;
using Prism.Mvvm;

namespace WeatherApp
{
    public class SearchVM: BindableBase
    {
        private const string defaultText = "Введите название города";
        private bool isLoading = false;
        private ResourceDictionary resource;

        private MainVM mainVM;
        private SearchModel model;

        public bool IsShowDetailSearch { get; set; } = false;


        public string Query { get; set; } = "";


        public AdvancedSearchVM AdvancedSearchVM { get; set; }


        public Visibility LoadingVis => isLoading ? Visibility.Visible : Visibility.Collapsed;


        public BitmapImage SearchImage { get; set; }

        public BitmapImage MenuImage { get; set; }


        public string Background { get; set; }

        public string Foreground { get; set; }


        public DelegateCommand GotFocus { get; set; }

        public DelegateCommand LostFocus { get; set; }

        public DelegateCommand Search { get; set; }

        public DelegateCommand Menu { get; set; }

        public SearchVM(MainVM mainVM)
        {
            this.mainVM = mainVM;
            model = new SearchModel();

            resource = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/CustomColors.xaml")
            };

            AdvancedSearchVM = new AdvancedSearchVM();

            AdvancedSearchVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "StartSearch")
                    Search.Execute();
            };

            GotFocus = new DelegateCommand(() =>
            {
                var basePath = "pack://application:,,,/Resources/Icon";

                SearchImage = new BitmapImage(new Uri($"{basePath}/search_black.png", UriKind.Absolute));
                MenuImage = new BitmapImage(new Uri($"{basePath}/menu_black.png", UriKind.Absolute));
                Background = "#FFDDDDDD";
                Foreground = "#FF666666";

                RaisePropertyChanged(nameof(SearchImage));
                RaisePropertyChanged(nameof(MenuImage));
                RaisePropertyChanged(nameof(Background));
                RaisePropertyChanged(nameof(Foreground));

                if (Query == defaultText)
                {
                    Query = "";
                    RaisePropertyChanged(nameof(Query));
                }
            });

            LostFocus = new DelegateCommand(() =>
            {
                var basePath = "pack://application:,,,/Resources/Icon";

                SearchImage = new BitmapImage(new Uri($"{basePath}/search_white.png", UriKind.Absolute));
                MenuImage = new BitmapImage(new Uri($"{basePath}/menu_white.png", UriKind.Absolute));

                Background = resource["SearchBarColor"].ToString();
                Foreground = resource["DeactiveTextColor"].ToString();

                RaisePropertyChanged(nameof(SearchImage));
                RaisePropertyChanged(nameof(MenuImage));
                RaisePropertyChanged(nameof(Background));
                RaisePropertyChanged(nameof(Foreground));

                if (Query == "")
                {
                    Query = defaultText;
                    RaisePropertyChanged(nameof(Query));
                }
            });

            Search = new DelegateCommand(() =>
            {
                if (isLoading)
                    return;

                if (AdvancedSearchVM.SelectedCity != null)
                {
                    // city selected

                    RaisePropertyChanged("AdvancedSearchSelected");
                }
                else
                {
                    if (!IsValidQuery())
                        return;

                    // search by query

                    RaisePropertyChanged("QueryValid");
                }
            });

            Menu = new DelegateCommand(() =>
            {
                IsShowDetailSearch = !IsShowDetailSearch;

                RaisePropertyChanged(nameof(IsShowDetailSearch));
            });

            LostFocus.Execute();
        }

        private bool IsValidQuery()
        {
            if (Query == "" || Query == defaultText)
                return false;

            return true;
        }
    }

    public class SearchModel
    {
        public SearchModel()
        {

        }
    }
}

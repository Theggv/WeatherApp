using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Prism.Commands;
using Prism.Mvvm;

namespace WeatherApp
{
    public class LoadingStatusVM: BindableBase
    {
        public enum eStatus
        {
            Invisible = 0,
            Loading,
            Error
        }

        private const string loadingMsg = "Загрузка..";
        private const string errorMsg = "Возникла ошибка при заргузке данных";

        public eStatus Status { get; set; } = eStatus.Invisible;

        public string Message { get; set; }


        public Visibility Visibility => Message == null ? Visibility.Collapsed : Visibility.Visible;

        public Visibility ErrorVis => Message == errorMsg ? Visibility.Visible : Visibility.Collapsed;

        public Visibility LoadingVis => Message == loadingMsg ? Visibility.Visible : Visibility.Collapsed;


        public DelegateCommand LoadingStatus { get; set; }

        public DelegateCommand ResetStatus { get; set; }

        public DelegateCommand ErrorStatus { get; set; }


        public LoadingStatusVM()
        {
            ResetStatus =   new DelegateCommand(() => UpdateStatus(eStatus.Invisible));
            LoadingStatus = new DelegateCommand(() => UpdateStatus(eStatus.Loading));
            ErrorStatus =   new DelegateCommand(() => UpdateStatus(eStatus.Error));

            ResetStatus.Execute();
        }

        private void UpdateStatus(eStatus status)
        {
            Status = status;

            if (Status == eStatus.Error)
                Message = errorMsg;
            else if (Status == eStatus.Loading)
                Message = loadingMsg;
            else
                Message = null;

            RaisePropertyChanged(nameof(Message));
            RaisePropertyChanged(nameof(Visibility));
            RaisePropertyChanged(nameof(ErrorVis));
            RaisePropertyChanged(nameof(LoadingVis));
        }
    }
}

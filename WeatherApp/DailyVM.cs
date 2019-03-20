using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Prism.Mvvm;

namespace WeatherApp
{
    public class DailyVM: BindableBase
    {
        private HourModel model;

        public Forecast Daily { get; }

        public BitmapImage WeatherImage => model.GetImage();

        public string DayName => Daily?.From.DayOfWeek.ToString();

        public string DayDate
        {
            get
            {
                if (Daily == null)
                    return "";

                if (Daily.From.Date == DateTime.Now.Date)
                    return "Сегодня";
                else if (Daily.From.DayOfYear - 1 == DateTime.Now.DayOfYear)
                    return "Завтра";
                else
                    return $"{Daily.From.ToString("m")}";
            }
        }

        public Image Photo { get; set; }

        public DailyVM()
        {
            model = new HourModel();
        }

        public DailyVM(Forecast daily)
        {
            model = new HourModel(daily);

            Daily = daily;
        }
    }
}

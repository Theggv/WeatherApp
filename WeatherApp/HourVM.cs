using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Prism.Mvvm;

namespace WeatherApp
{
    public class HourVM: BindableBase
    {
        private HourModel model;

        public Forecast Hour { get; set; }

        public BitmapImage WeatherImage => model.GetImage();

        public string HourString => Hour != null ? $"{Hour.From.Hour}:00" : "";

        public HourVM()
        {
            model = new HourModel();
        }

        public HourVM(Forecast hour)
        {
            model = new HourModel(hour);

            Hour = hour;
        }
    }

    public class HourModel : BindableBase
    {
        private Forecast data;

        public HourModel() { }

        public HourModel(Forecast f)
        {
            data = f;
        }

        public BitmapImage GetImage()
        {
            if (data == null)
                return null;

            var basePath = "pack://application:,,,/Resources/Dynamic";
            var code = data.Symbol.Number;

            if (code >= 200 && code < 300) // Thunderstorm
                return new BitmapImage(new Uri($"{basePath}/storm.png", UriKind.Absolute));
            else if (code >= 300 && code < 400) // Drizzle
                return new BitmapImage(new Uri($"{basePath}/rain.png", UriKind.Absolute));
            else if (code >= 500 && code < 600) // Rain
                return new BitmapImage(new Uri($"{basePath}/rain.png", UriKind.Absolute));
            else if (code >= 600 && code < 700) // Snow
                return new BitmapImage(new Uri($"{basePath}/snow.png", UriKind.Absolute));
            else if (code >= 700 && code < 800) // Atmosphere
                return new BitmapImage(new Uri($"{basePath}/wind.png", UriKind.Absolute));
            else if (code == 800) // Clear sky
                return new BitmapImage(new Uri($"{basePath}/sun.png", UriKind.Absolute));
            else if (code == 801) // few clouds
                return new BitmapImage(new Uri($"{basePath}/cloud.png", UriKind.Absolute));
            else if (code == 802) // scattered clouds
                return new BitmapImage(new Uri($"{basePath}/cloud.png", UriKind.Absolute));
            else if (code == 803) // broken clouds
                return new BitmapImage(new Uri($"{basePath}/cloud.png", UriKind.Absolute));
            else if (code == 804) // overcast clouds
                return new BitmapImage(new Uri($"{basePath}/cloud.png", UriKind.Absolute));

            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.IO;
using System.Data.SqlClient;
using System.Collections;

namespace WeatherApp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            /*
            Task.Run(() => {
                string json = new StreamReader("city.list.json").ReadToEnd();

                var obj = JsonConvert.DeserializeObject<List<CityJson>>(json);

                string connectionString = $"Data Source=DESKTOP-QBSNIUJ;Initial Catalog=weatherdb;Integrated Security=True";

                var connection = new SqlConnection(connectionString);

                var command = new SqlCommand
                {
                    Connection = connection
                };

                int counter = 0;

                connection.Open();

                string query = "";

                command.CommandText = "DELETE FROM [dbo].city;";
                command.ExecuteNonQuery();

                foreach (var c in obj)
                {
                    if (counter % 100 == 0)
                        query = "INSERT INTO [city] ([external_id], [name], [country_id]) VALUES ";

                    query += $"({c.Id}, '{c.Name.Replace('\'', ' ')}', '{c.Code}'),";

                    counter++;

                    if (counter % 100 == 0 || counter == obj.Count)
                    {
                        command.CommandText = query.Remove(query.Length - 1);

                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message + "\n" + command.CommandText);
                        }
                    }
                }

                connection.Close();
            });
            */
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}

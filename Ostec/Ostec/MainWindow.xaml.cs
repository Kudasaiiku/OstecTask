using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Windows.Threading;
using System.Timers;

namespace Ostec
{

    public partial class MainWindow : Window
    {
        // Список с цветами кнопок.
        private readonly List<Color> _colors = new List<Color>
        {
            Colors.White, Colors.Black, Colors.Sienna, Colors.DodgerBlue, Colors.LightBlue,
            Colors.DarkRed, Colors.Gold, Colors.ForestGreen, Colors.DimGray, Colors.Pink
        };

        // Словарь для сопоставления цветов с их названиями.
        private readonly Dictionary<Color, string> colorNames = new Dictionary<Color, string>
        {
            { Colors.White, "Белый" },
            { Colors.Black, "Чёрный" },
            { Colors.Sienna, "Коричневый" },
            { Colors.DodgerBlue, "Синий" },
            { Colors.LightBlue, "Голубой" },
            { Colors.DarkRed, "Красный" },
            { Colors.Gold, "Жёлтый" },
            { Colors.ForestGreen, "Зелёный" },
            { Colors.DimGray, "Серый" },
            { Colors.Pink, "Розовый" }
        };

        private readonly Random random = new Random();
        private Color reserveColor;
        private int clickCount = 0;

        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly Timer TimeUpdateTimer = new Timer(60000);

        public MainWindow()
        {
            InitializeComponent();

            PaintButtons();

            UpdateClickCounter();

            GetTime();
            // Обновление времени (запроса) раз в минуту со старта программы.
            TimeUpdateTimer.Elapsed += OnTimeUpdate;
            TimeUpdateTimer.Start();

            GetTemperature();
        }

        // БЛОК С ЦВЕТНЫМИ КНОПКАМИ.
        // Метод случайного окрашивания кнопок.
        private void PaintButtons()
        {
            var mixedColors = _colors.OrderBy(color => random.Next()).ToList();
            var selectedColors = mixedColors.Take(9).ToList();
            reserveColor = mixedColors.Last();

            var buttons = ButtonsGrid.Children.OfType<Button>().ToList();

            for (int i = 0; i < buttons.Count; i++)
            {
                var color = selectedColors[i];

                // Вызов метода окрашивания кнопки.
                SetButtonColor(buttons[i], color);

                // Присваивание события нажатия кнопки.
                buttons[i].Click += Button_Click;
            }
        }

        // Метод окрашивания кнопки и назначения названия цвета.
        private void SetButtonColor(Button button, Color color)
        {
            button.Background = new SolidColorBrush(color);
            button.Content = colorNames[color];

            if (color == Colors.Black)
            {
                button.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                button.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        // Событие нажатия кнопки.
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var currentColor = ((SolidColorBrush)button.Background).Color;
                SetButtonColor(button, reserveColor);
                reserveColor = currentColor;

                // Счетчик.
                clickCount++;
                UpdateClickCounter();
            }
        }

        // Метод обновления кол-ва нажатий (счетчика).
        private void UpdateClickCounter()
        {
            ClickCounter.Text = $"{clickCount}";
        }

        // БЛОК API.
        // Получение текущего времени (Москва) (API worldtimeapi.org).
        private async void GetTime()
        {
            const string timeUrl = "http://worldtimeapi.org/api/timezone/Europe/Moscow";

            try
            {
                HttpResponseMessage response = await HttpClient.GetAsync(timeUrl);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                JObject timeData = JObject.Parse(responseBody);

                string currentTime = timeData["datetime"].ToString();
                DateTime localTime = DateTime.Parse(currentTime);

                Dispatcher.Invoke(() =>
                {
                    Clock.Text = $"{localTime.ToString("HH:mm")}";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    Clock.Text = $"Ошибка при получении данных: {ex.Message}";
                });
            }
        }

        // Метод обновления времени через таймер.
        private void OnTimeUpdate(object source, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                GetTime();
            });
        }

        // Получение текущей температуры воздуха (Москва) (API api.weather.yandex.ru).
        private async void GetTemperature()
        {
            const string accessKey = "42ddaf3b-4ba1-4508-8222-9697d93c2866";
            const string url = "https://api.weather.yandex.ru/v2/forecast?lat=55.7558&lon=37.6176";

            HttpClient.DefaultRequestHeaders.Add("X-Yandex-Weather-Key", accessKey);

            try
            {
                HttpResponseMessage response = await HttpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                JObject weatherData = JObject.Parse(responseBody);

                int temperature = weatherData["fact"]["temp"].ToObject<int>();

                Dispatcher.Invoke(() =>
                {
                    Temperature.Text = $"{temperature} °C";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    Temperature.Text = $"Ошибка при получении данных: {ex.Message}";
                });
            }
        }

        // Подтверждение для подтверждения завершения работы программы.
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            MessageBoxResult result = MessageBox.Show("Вы действительно хотите выйти из приложения?", "Подтверждение выхода", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
    }
}
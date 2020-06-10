//Copyright 2018 Samsung Electronics Co., Ltd
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Weather.Config;
using Weather.Models.Location;
using Weather.Models.Weather;
using Weather.Utils;
using Xamarin.Forms;

namespace Weather.ViewModels
{
    /// <summary>
    /// ViewModel class for CurrentWeatherPage.
    /// </summary>
    public class CurrentWeatherViewModel : ViewModelBase
    {
        #region fields

        /// <summary>
        /// Local storage of command that initializes weather data.
        /// </summary>
        private Command _initializeCommand;

        /// <summary>
        /// Local storage of current weather.
        /// </summary>
        private CurrentWeather _currentWeather;

        /// <summary>
        /// Local storage of city time zone.
        /// </summary>
        private Models.Location.TimeZone _cityTimeZone;

        /// <summary>
        /// Local storage of command that shows screen with forecast.
        /// </summary>
        private Command _checkForecastCommand;

        /// <summary>
        /// Local storage of forecast data.
        /// </summary>
        private Forecast _forecast;

        #endregion

        #region properties

        /// <summary>
        /// Bindable property that allows to set city data.
        /// </summary>
        public static readonly BindableProperty CityDataProperty =
            BindableProperty.Create(nameof(CityData), typeof(City), typeof(CurrentWeatherViewModel), default(City));

        /// <summary>
        /// Bindable property that allows to set navigation context.
        /// </summary>
        public static readonly BindableProperty NavigationProperty =
            BindableProperty.Create(nameof(Navigation), typeof(INavigation), typeof(MainPageViewModel), default(Type));

        /// <summary>
        /// Gets or sets city data.
        /// View model holds weather data for this city.
        /// </summary>
        public City CityData
        {
            get => (City)GetValue(CityDataProperty);
            set => SetValue(CityDataProperty, value);
        }

        /// <summary>
        /// Gets or sets the current weather.
        /// </summary>
        public CurrentWeather CurrentWeather
        {
            get => _currentWeather;
            set => SetProperty(ref _currentWeather, value);
        }

        /// <summary>
        /// Gets or sets city time zone.
        /// </summary>
        public Models.Location.TimeZone CityTimeZone
        {
            get => _cityTimeZone;
            set => SetProperty(ref _cityTimeZone, value);
        }

        /// <summary>
        /// Gets or sets command that initializes weather data.
        /// </summary>
        public Command InitializeCommand
        {
            get => _initializeCommand;
            set => SetProperty(ref _initializeCommand, value);
        }

        /// <summary>
        /// Gets or sets command that shows screen with forecast.
        /// </summary>
        public Command CheckForecastCommand
        {
            get => _checkForecastCommand;
            set => SetProperty(ref _checkForecastCommand, value);
        }

        /// <summary>
        /// Gets or sets navigation context of application.
        /// </summary>
        public INavigation Navigation
        {
            get => (INavigation)GetValue(NavigationProperty);
            set => SetValue(NavigationProperty, value);
        }

        /// <summary>
        /// Gets or sets forecast data.
        /// </summary>
        public Forecast Forecast
        {
            get => _forecast;
            set => SetProperty(ref _forecast, value);
        }

        /// <summary>
        /// Indicates if initialization was completed.
        /// </summary>
        public bool InitializationCompleted => ((App) Application.Current).IsInitialized =
            Forecast != null && CurrentWeather != null && CityTimeZone != null;

        public bool InitializationInProgress => !InitializationCompleted;

        #endregion

        #region methods

        /// <summary>
        /// Default class constructor.
        /// </summary>
        public CurrentWeatherViewModel()
        {
            InitializeCommand = new Command(async () =>
            {
                CurrentWeather = null;
                Forecast = null;
                OnPropertyChanged(nameof(InitializationCompleted));
                OnPropertyChanged(nameof(InitializationInProgress));

                try
                {
                    CityTimeZone = await InitializeTimeZone();
                    CurrentWeather = await InitializeWeather();
                    Forecast = await InitializeForecast();

                    foreach (var currentWeather in Forecast.WeatherList)
                    {
                        if (currentWeather != null)
                        {
                            currentWeather.CityName = CityData.Name;
                        }
                    }

                    OnPropertyChanged(nameof(InitializationCompleted));
                    OnPropertyChanged(nameof(InitializationInProgress));
                }
                catch (HttpException ex)
                {
                    await ErrorHandler.HandleException((int)ex.StatusCode, ex.StatusCode.ToString());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            });

            CheckForecastCommand = new Command(CheckForecast);
        }

        /// <summary>
        /// Pushes page with forecast data to navigation stack.
        /// </summary>
        /// <param name="param">Page to push to navigation stack.</param>
        private async void CheckForecast(object param)
        {
            if (param is Page page)
            {
                await Navigation.PushAsync(page);
            }
        }

        /// <summary>
        /// Initializes current weather class.
        /// Sends GET request to server.
        /// </summary>
        /// <returns>Async task with current weather.</returns>
        private async Task<CurrentWeather> InitializeWeather()
        {
            var request = new Request<CurrentWeather>(ApiConfig.WEATHER_URL);

            request.AddParameter("appid", ApiConfig.API_KEY);
            request.AddParameter("id", CityData.Id.ToString());
            request.AddParameter("units", RegionInfo.CurrentRegion.IsMetric ? "metric" : "imperial");

            return await request.Get();
        }

        /// <summary>
        /// Initializes forecast class.
        /// Sends GET request to server.
        /// </summary>
        /// <returns>Async task with forecast data.</returns>
        private async Task<Forecast> InitializeForecast()
        {
            var request = new Request<Forecast>(ApiConfig.FORECAST_URL);

            request.AddParameter("appid", ApiConfig.API_KEY);
            request.AddParameter("id", CityData.Id.ToString());
            request.AddParameter("units", RegionInfo.CurrentRegion.IsMetric ? "metric" : "imperial");

            return await request.Get();
        }

        /// <summary>
        /// Initializes time zone for city.
        /// </summary>
        /// <returns>Async task with time zone.</returns>
        private async Task<Models.Location.TimeZone> InitializeTimeZone()
        {
            var request = new Request<Models.Location.TimeZone>(ApiConfig.TIMEZONE_URL);

            request.AddParameter("location", $"{CityData.Coordinates.Latitude},{CityData.Coordinates.Longitude}");
            request.AddParameter("timestamp",
                DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString());
            request.AddParameter("sensor", "false");

            return await request.Get();
        }

        #endregion
    }
}
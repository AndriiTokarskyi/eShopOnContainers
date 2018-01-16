﻿using System.Globalization;
using eShopOnContainers.Services;
using eShopOnContainers.Core.ViewModels.Base;
using System.Threading.Tasks;
using eShopOnContainers.Core.Models.Location;
using eShopOnContainers.Core.Services.Location;
using Plugin.Geolocator;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using eShopOnContainers.Core.Services.Settings;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace eShopOnContainers
{
    public partial class App : Application
    {
        ISettingsService _settingsService;
        bool _useMockServices;

        public App()
        {
            InitializeComponent();

            InitApp();
            if (Device.RuntimePlatform == Device.UWP)
            {
                InitNavigation();
            }
        }

        private void InitApp()
        {
            _useMockServices = true;//_settingsService.UseMocks;
            ViewModelLocator.RegisterDependencies(_useMockServices);
            _settingsService = ViewModelLocator.Resolve<ISettingsService>();
        }

        private Task InitNavigation()
        {
            var navigationService = ViewModelLocator.Resolve<INavigationService>();
            return navigationService.InitializeAsync();
        }

        protected override async void OnStart()
        {
            base.OnStart();

            if (Device.RuntimePlatform != Device.UWP)
            {
                await InitNavigation();
            }

            if (_settingsService.AllowGpsLocation && !_settingsService.UseFakeLocation)
            {
                await GetGpsLocation();
            }

            if (!_settingsService.UseMocks && !string.IsNullOrEmpty(_settingsService.AuthAccessToken))
            {
                await SendCurrentLocation();
            }

            base.OnResume();
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        private async Task GetGpsLocation()
        {
            var locator = CrossGeolocator.Current;

            if (locator.IsGeolocationEnabled && locator.IsGeolocationAvailable)
            {
                locator.AllowsBackgroundUpdates = true;
                locator.DesiredAccuracy = 50;

                var position = await locator.GetPositionAsync();

                _settingsService.Latitude = position.Latitude.ToString();
                _settingsService.Longitude = position.Longitude.ToString();
            }
            else
            {
                _settingsService.AllowGpsLocation = false;
            }
        }

        private async Task SendCurrentLocation()
        {
            var location = new Location
            {
                Latitude = double.Parse(_settingsService.Latitude, CultureInfo.InvariantCulture),
                Longitude = double.Parse(_settingsService.Longitude, CultureInfo.InvariantCulture)
            };

            var locationService = ViewModelLocator.Resolve<ILocationService>();
            await locationService.UpdateUserLocation(location, _settingsService.AuthAccessToken);
        }
    }
}
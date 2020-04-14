using System;
using Prism.Navigation;
using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;
using Slipways.Mobile.Helpers;
using Xamarin.Forms.Maps;

namespace Slipways.Mobile.ViewModels
{
    public class MarinaDetailsViewModel : ViewModelBase
    {
        private Marina _marina;
        public Marina Marina
        {
            get => _marina;
            set => SetProperty(ref _marina, value);
        }

        private Map _map;
        public Map Map
        {
            get => _map;
            set => SetProperty(ref _map, value);
        }

        public string Name
        {
            get => _marina?.Name;
        }
        public string Address
        {
            get => $"{_marina?.Street} - {_marina?.Postalcode} {_marina?.City}";
        }

        public MarinaDetailsViewModel(
            IRepositoryWrapper repository,
            INavigationService navigationService) : base(repository, navigationService)
        {
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            Marina = parameters.GetValue<Marina>(DataT.Marina);
            Title = Marina.Name;

            Position position = new Position(Marina.Latitude, Marina.Longitude);
            var pin = new Pin
            {
                Label = Marina.Name,
                Position = position,
                Type = PinType.Place,
                Address = Marina.Street
            };
            MapSpan mapSpan = new MapSpan(position, 0.01, 0.01);
            Map = new Map(mapSpan)
            {
                MapType = MapType.Satellite
            };
            Map.Pins.Add(pin);
        }
    }
}

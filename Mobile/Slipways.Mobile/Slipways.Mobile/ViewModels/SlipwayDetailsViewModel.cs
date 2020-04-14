using Prism.Events;
using Prism.Navigation;
using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;
using Xamarin.Forms.Maps;

namespace Slipways.Mobile.ViewModels
{
    public class SlipwayDetailsViewModel : ViewModelBase
    {
        private Slipway _slipway;
        public Slipway Slipway
        {
            get => _slipway;
            set => SetProperty(ref _slipway, value);
        }

        private Map _map;
        public Map Map
        {
            get => _map;
            set => SetProperty(ref _map, value);
        }

        public string Name
        {
            get => _slipway?.Name;
        }
        public string Address
        {
            get => $"{_slipway?.Street} - {_slipway?.Postalcode} {_slipway?.City}";
        }

        public SlipwayDetailsViewModel(
            IEventAggregator eventAggregator,
            IRepositoryWrapper repository,
            INavigationService navigationService) : base(repository, navigationService)
        {
        }

        public override void OnNavigatedFrom(
            INavigationParameters parameters)
        {
        }

        public override void OnNavigatedTo(
            INavigationParameters parameters)
        {
            Slipway = parameters.GetValue<Slipway>("slipway");
            Title = Slipway.Name;

            Position position = new Position(Slipway.Latitude, Slipway.Longitude);
            var pin = new Pin
            {
                Label = Slipway.Name,
                Position = position,
                Type = PinType.Place,
                Address = Slipway.Street
            };
            MapSpan mapSpan = new MapSpan(position, 0.01, 0.01);
            Map = new Map(mapSpan);
            Map.MapType = MapType.Satellite;
            Map.Pins.Add(pin);
        }
    }
}

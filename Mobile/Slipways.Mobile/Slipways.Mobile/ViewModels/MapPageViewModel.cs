using Prism.Events;
using Prism.Navigation;
using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;
using Xamarin.Forms.Maps;

namespace Slipways.Mobile.ViewModels
{
    public class MapPageViewModel : ViewModelBase
    {
        private Map _map;

        public Map Map
        {
            get => _map;
            set => SetProperty(ref _map, value);
        }

        public MapPageViewModel(
            IDataStore dataStore,
            IRepositoryWrapper repository,
            IEventAggregator eventAggregator,
            INavigationService navigationService) : base(repository, navigationService)
        {
            Title = "Übersicht";
        }
        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
        }

        public override async void OnNavigatedTo(INavigationParameters parameters)
        {
            var slipways = await Repository.Slipways.GetAllAsync();
            var position = new Position(51.312801, 9.481544);
            var mapSpan = new MapSpan(position, 10, 10);
            Map = new Map(mapSpan);
            foreach (var slipway in slipways)
            {
                var pos = new Position(slipway.Latitude, slipway.Longitude);
                var pin = new Pin
                {
                    Label = slipway.Name,
                    Position = pos,
                    Type = PinType.Place,
                    Address = slipway.Street
                };
                Map.Pins.Add(pin);
            }
            Map.MapType = MapType.Satellite;
        }
    }
}
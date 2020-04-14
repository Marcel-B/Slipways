using Prism.Navigation;
using Slipways.Mobile.Contracts;

namespace Slipways.Mobile.ViewModels
{
    public class InfoPageViewModel : ViewModelBase
    {
        private int _slipwaysCount;
        private int _watersCount;
        private int _marinasCount;
        private int _stationsCount;

        public int SlipwaysCount
        {
            get => _slipwaysCount;
            set => SetProperty(ref _slipwaysCount, value);
        }

        public int WatersCount
        {
            get => _watersCount;
            set => SetProperty(ref _watersCount, value);
        }

        public int MarinasCount
        {
            get => _marinasCount;
            set => SetProperty(ref _marinasCount, value);
        }

        public int StationsCount
        {
            get => _stationsCount;
            set => SetProperty(ref _stationsCount, value);
        }

        public InfoPageViewModel(
            IRepositoryWrapper repository,
            INavigationService navigationService)
            : base(repository,navigationService)
        {
            Title = "Info";
        }

        public override void OnNavigatedFrom(
            INavigationParameters parameters)
        {
        }

        public async override void OnNavigatedTo(
            INavigationParameters parameters)
        {
            SlipwaysCount = (await Repository.Slipways.GetAllAsync()).Count;
            WatersCount = (await Repository.Waters.GetAllAsync()).Count;
            MarinasCount = (await Repository.Marinas.GetAllAsync()).Count;
            StationsCount = (await Repository.Stations.GetAllAsync()).Count;
        }
    }
}

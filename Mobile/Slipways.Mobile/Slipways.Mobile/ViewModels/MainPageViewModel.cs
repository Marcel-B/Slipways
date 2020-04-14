using Prism.Events;
using Prism.Navigation;
using Prism.Services;
using Slipways.Mobile.Contracts;
using Slipways.Mobile.Events;
using Slipways.Mobile.Helpers;
using Slipways.Mobile.Views;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Slipways.Mobile.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public bool _slipwaysRdy;
        public bool SlipwaysRdy
        {
            get => _slipwaysRdy;
            set => SetProperty(ref _slipwaysRdy, value);
        }

        private bool _running;
        private IPageDialogService _dialogService;

        public bool Running
        {
            get => _running;
            set => SetProperty(ref _running, value);
        }

        public ICommand Navigate { get; set; }

        public MainPageViewModel(
            IPageDialogService dialogService,
            IRepositoryWrapper repository,
            IEventAggregator eventAggregator,
            INavigationService navigationService) : base(repository, navigationService)
        {
            _dialogService = dialogService;
            eventAggregator.GetEvent<InitializationReadyEvent>().Subscribe(Ready);

            Navigate = new Command(async (sender) =>
            {
                var pageName = sender switch
                {
                    CommandParameter.Slipways => typeof(SlipwaysListPage).Name,
                    CommandParameter.Marinas => typeof(MarinaListPage).Name,
                    CommandParameter.Waters => typeof(WaterListPage).Name,
                    CommandParameter.Info => typeof(InfoPage).Name,
                    CommandParameter.Services => typeof(ServicePage).Name,
                    CommandParameter.Map => typeof(MapPage).Name,
                    CommandParameter.Stations => typeof(StationsListPage).Name,
                    _ => string.Empty
                };
                //await _dialogService.DisplayAlertAsync("Alert", "You have been alerted", "OK");

                await navigationService
                .NavigateAsync(pageName)
                .ConfigureAwait(false);
            });
            Title = "slipways.de";
            Running = true;
        }

        public void Ready(
            bool rdy)
        {
            //await _dialogService.DisplayAlertAsync("Ready", "Initialization of data ready", "OK");
            Running = false;
        }

        public override void OnNavigatedFrom(
            INavigationParameters parameters)
        {
        }

        public override void OnNavigatedTo(
            INavigationParameters parameters)
        {
            //await _dataStore.LoadData()
            //    .ConfigureAwait(false);
        }
    }
}

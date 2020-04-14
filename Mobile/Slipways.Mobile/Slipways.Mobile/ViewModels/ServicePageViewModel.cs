using Prism.Events;
using Prism.Navigation;
using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;
using Slipways.Mobile.Helpers;

namespace Slipways.Mobile.ViewModels
{
    public class ServicePageViewModel : ListViewModel<Service>
    {
        public ServicePageViewModel(
            IEventAggregator eventAggregator,
            IRepositoryWrapper repository,
            INavigationService navigationService) : base(DataT.Service, repository, eventAggregator, navigationService)
        {
            Title = "Werkstätten";
        }

        public override void OnNavigatedFrom(
            INavigationParameters parameters)
        {
        }
    }
}

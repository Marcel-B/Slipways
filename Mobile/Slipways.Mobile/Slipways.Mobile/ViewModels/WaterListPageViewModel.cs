using Prism.Events;
using Prism.Navigation;
using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;
using Slipways.Mobile.Helpers;

namespace Slipways.Mobile.ViewModels
{
    public class WaterListPageViewModel : ListViewModel<Water>
    {
        public WaterListPageViewModel(
            IRepositoryWrapper repository,
            IEventAggregator eventAggregator,
            INavigationService navigationService) : base(DataT.Water, repository, eventAggregator, navigationService)
        {
            Title = "Gewässer";
        }

        public override void OnNavigatedFrom(
            INavigationParameters parameters)
        { }
    }
}

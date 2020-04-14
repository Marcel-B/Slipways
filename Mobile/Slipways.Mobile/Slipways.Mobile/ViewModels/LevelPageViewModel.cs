using Prism.Events;
using Prism.Navigation;
using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;
using Slipways.Mobile.Helpers;

namespace Slipways.Mobile.ViewModels
{
    public class LevelPageViewModel : ListViewModel<Station>
    {
        public LevelPageViewModel(
            IRepositoryWrapper repository,
            IEventAggregator eventAggregator,
            INavigationService navigationService) : base(DataT.Station, repository, eventAggregator, navigationService)
        {
            Title = "Pegel";
        }

        public override void OnNavigatedFrom(
            INavigationParameters parameters)
        {
        }
    }
}
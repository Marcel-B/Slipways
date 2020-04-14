using System;
using Prism.Events;
using Prism.Navigation;
using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;
using Slipways.Mobile.Helpers;
using Slipways.Mobile.Views;
using Xamarin.Forms;

namespace Slipways.Mobile.ViewModels
{
    public class StationsListPageViewModel : ListViewModel<Station>
    {
        public StationsListPageViewModel(
            IRepositoryWrapper repository,
            IEventAggregator eventAggregator,
            INavigationService navigationService) : base(DataT.Station, repository, eventAggregator, navigationService)
        {
            Title = "Stationen";
            ItemTappedCommand = new Command(async (sender) => {
                if (sender is Station station)
                    await navigationService.NavigateAsync(nameof(LevelPage));
            });
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
        }
    }
}

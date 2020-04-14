using Prism.Events;
using Prism.Navigation;
using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;
using Slipways.Mobile.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.Forms;

namespace Slipways.Mobile.ViewModels
{
    public class MarinaListPageViewModel : ListViewModel<Marina>
    {
        public MarinaListPageViewModel(
            IEventAggregator eventAggregator,
            IRepositoryWrapper repository,
            INavigationService navigationService) : base(DataT.Marina, repository, eventAggregator, navigationService)
        {
            Title = "Marinas";
            ItemTappedCommand = new Command(async (sender) =>
            {
                if (sender is Marina marina)
                {
                    var navigationParameters = new NavigationParameters
                    {
                        { DataT.Marina, marina }
                    };
                    await NavigationService.NavigateAsync("MarinaDetails", navigationParameters);
                }
            });
        }

        protected async override void Up(IEnumerable<Marina> datas)
        {
            if (Data == null)
                Data = new ObservableCollection<Marina>();

            Data.Clear();

            foreach (var data in datas.OrderBy(_ => _.Name))
            {
                data.Water = data.Water ?? await Repository.Waters.GetByUuidAsync(data.WaterPk);
                Data.Add(data);
            }
        }

        public override void OnNavigatedFrom(
            INavigationParameters parameters)
        { }
    }
}

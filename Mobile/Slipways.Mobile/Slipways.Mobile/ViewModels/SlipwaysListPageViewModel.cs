using Prism.Events;
using Prism.Navigation;
using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;
using Slipways.Mobile.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Xamarin.Forms;

namespace Slipways.Mobile.ViewModels
{
    public class SlipwaysListPageViewModel : ListViewModel<Slipway>
    {
        public SlipwaysListPageViewModel(
            INavigationService navigationService,
            IRepositoryWrapper repository,
            IEventAggregator eventAggregator) : base(
                DataT.Slipway,
                repository,
                eventAggregator, 
                navigationService)
        {
            ItemTappedCommand = new Command(async (sender) =>
            {
                //((ListView)sender).SelectedItem = null;

                if (sender is Slipway slipway)
                {
                    var navigationParameters = new NavigationParameters
                    {
                        { DataT.Slipway, slipway }
                    };
                    await NavigationService.NavigateAsync("SlipwayDetails", navigationParameters);
                }
            });
        }

        protected override void Up(
            IEnumerable<Slipway> datas)
        {
            if (Data == null)
                Data = new ObservableCollection<Slipway>();
            Data.Clear();
            foreach (var data in datas.OrderBy(_ => _.Name))
            {
                Data.Add(data);
            }
        }

        public async override void OnNavigatedTo(
            INavigationParameters parameters)
        {
            var slipways = await Repository.Slipways.GetAllAsync();
            if (slipways.Count == Data.Count)
                return;
            if (Data == null)
                Data = new ObservableCollection<Slipway>();
            Data.Clear();
            foreach (var data in slipways)
            {
                Data.Add(data);
            }
            //Up(slipways);
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
        }
    }
}

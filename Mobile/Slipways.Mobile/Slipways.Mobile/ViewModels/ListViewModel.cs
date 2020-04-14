using Prism.Events;
using Prism.Navigation;
using Slipways.Mobile.Contracts;
using Slipways.Mobile.Events;
using Slipways.Mobile.Helpers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Slipways.Mobile.ViewModels
{
    public abstract class ListViewModel<T> : ViewModelBase where T : IEntity, new()
    {
        public ListViewModel(
            string dataType,
            IRepositoryWrapper repository,
            IEventAggregator eventAggregator,
            INavigationService navigationService) : base(repository, navigationService)
        {
            DataType = dataType;
            Data = new ObservableCollection<T>();
            eventAggregator.GetEvent<UpdateReadyEvent<T>>().Subscribe(Update);
        }

        public string DataType { get; }
        public ICommand ItemTappedCommand { get; set; }

        private ObservableCollection<T> _data;
        public ObservableCollection<T> Data
        {
            get => _data;
            set => SetProperty(ref _data, value);
        }

        public virtual void Update(
            DataUpdateEventArgs<T> args)
        {
            if (args.Type.ToLower() == DataType)
            {
                Up(args.Data);
            }
        }

        protected virtual void Up(IEnumerable<T> datas)
        {
            if (Data == null)
                Data = new ObservableCollection<T>();

            Data.Clear();

            foreach (var data in datas.OrderBy(_ => _.Name))
                Data.Add(data);
        }

        public override abstract void OnNavigatedFrom(INavigationParameters parameters);

        public async override void OnNavigatedTo(
            INavigationParameters parameters)
        {
            var entities = await Repository.GetAllAsync<T>();
            if (entities == null)
                return;
            Up(entities);
        }
    }
}

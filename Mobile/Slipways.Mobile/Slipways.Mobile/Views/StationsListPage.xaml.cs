using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Slipways.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StationsListPage : ContentPage
    {
        public StationsListPage()
        {
            InitializeComponent();
        }

        void Handle_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            //if (e.Item == null)
            //    return;

            //await DisplayAlert("Item Tapped", "An item was tapped.", "OK");

            ////Deselect Item
            //((ListView)sender).SelectedItem = null;
        }
    }
}

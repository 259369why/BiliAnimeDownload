using BiliAnimeDownload.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BiliAnimeDownload.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HistroyPage : ContentPage
    {
        public HistroyPage()
        {
            InitializeComponent();
            var itemClear = new ToolbarItem()
            {
                Text = "清除全部"
            };
            itemClear.Clicked += ItemClear_Clicked;
            this.ToolbarItems.Add(itemClear);
        }

        private async void ItemClear_Clicked(object sender, EventArgs e)
        {
            var result = await DisplayAlert("询问", "确定要清除全部历史记录吗", "确定", "取消");
            if (result)
            {
                Util.ClearHistroy();
                lv.ItemsSource = null;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadHistory();
        }
        private void LoadHistory()
        {
            try
            {
                var ls = Util.GetHistroy();

                if (ls.Count != 0)
                {
                    lv.ItemsSource = ls.OrderByDescending(x => Convert.ToDateTime(x.date));
                }
            }
            catch (Exception)
            {
            }

        }
        private void lv_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {

            var data = lv.SelectedItem as HistroyModel;

            (this.Navigation.NavigationStack[0] as MainPage).DoGetAnimeInfo(data.id);

            this.Navigation.RemovePage(this);

            //this.Navigation.RemovePage(this.Navigation.NavigationStack.First());
            //this.Navigation.PushAsync(new MainPage(data.id));

            // lv.SelectedItem = null;
        }
    }

    public class HistroyModel
    {
        public string name { get; set; }
        public string type { get; set; }//anime or video
        public string date { get; set; }
        public string id { get; set; }
    }


}
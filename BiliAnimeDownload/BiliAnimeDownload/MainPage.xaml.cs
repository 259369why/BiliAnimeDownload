using BiliAnime.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xamarin.Forms;
//using Android.App;
using BiliAnimeDownload.Models;
using BiliAnimeDownload.Helpers;
using BiliAnimeDownload.Views;
using Newtonsoft.Json.Linq;
using Plugin.Permissions.Abstractions;
using Plugin.Permissions;

namespace BiliAnimeDownload
{
    public partial class MainPage : ContentPage
    {
        public event EventHandler<string> GetAnimeInfo;

        public MainPage()
        {
            InitializeComponent();
            GetAnimeInfo += MainPage_GetAnimeInfo;
        }
        public void DoGetAnimeInfo(string id)
        {
            if (GetAnimeInfo != null)
            {
                GetAnimeInfo(null, id);
            }
        }

        private void MainPage_GetAnimeInfo(object sender, string e)
        {
            txt_Sid.Text = e;
            btn_Go_Clicked(null, null);
        }


        protected async override void OnAppearing()
        {
            base.OnAppearing();


            if (BindingContext == null)
            {

                await Task.Delay(500);

                //txt_Sid.Text = "http://m.bilibili.com/bangumi/play/ss6339";
                //txt_Sid.Text = "av17254855";

                //txt_Sid.Text = "http://m.bilibili.com/bangumi/play/ss4187";
                Util.CheckUpdate(this, false);
            }
            _downlaodType = (DownlaodType)SettingHelper.GetDownMode();

            await CheckPermission();
        }
        private async Task<bool> CheckPermission()
        {
            try
            {

                var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Storage);
                if (status != PermissionStatus.Granted)
                {
                    Util.ShowLongToast("需要运行读写文件权限才能使用！");
                    if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Storage))
                    {
                        Util.ShowLongToast("需要运行读写文件权限才能使用！");
                    }
                    var results = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Storage);
                    //Best practice to always check that the key exists
                    if (results.ContainsKey(Permission.Storage))
                        status = results[Permission.Storage];

                }
                return status == PermissionStatus.Granted;
            }
            catch (Exception ex)
            {
                await DisplayAlert("失败", ex.Message, "OK");
                return false;
            }
        }


        VideoType _videoType = VideoType.Anime;
        DownlaodType _downlaodType = DownlaodType.Bilibili;
        string _sid = "";
        string _aid = "";
        private async void btn_Go_Clicked(object sender, EventArgs e)
        {
            if (txt_Sid.Text == null || txt_Sid.Text.Length == 0)
            {
                Util.ShowLongToast("请输入番剧地址");
                return;
            }
            if (txt_Sid.Text.Contains("av"))
            {
                _aid = Regex.Match(txt_Sid.Text, @"av(\d{1,9})", RegexOptions.Singleline | RegexOptions.IgnoreCase).Groups[1].Value;
                _videoType = VideoType.Video;
                BindingContext = null;
                ls.IsVisible = false;
                ls_Video.IsVisible = true;
                LoadVideoInfo();
            }
            else
            {
                if (txt_Sid.Text.Contains("ep"))
                {
                    _sid = await Util.BangumiEpidToSid(Regex.Match(txt_Sid.Text, @"ep(\d{1,9})", RegexOptions.Singleline | RegexOptions.IgnoreCase).Groups[1].Value);
                }
                else
                {
                    _sid = Regex.Match(txt_Sid.Text, @"(\d{1,9})", RegexOptions.Singleline | RegexOptions.IgnoreCase).Groups[1].Value;
                }

                _videoType = VideoType.Anime;
                BindingContext = null;
                ls.IsVisible = true;
                ls_Video.IsVisible = false;
                LoadAnimeInfo();
            }
            _downlaodType = (DownlaodType)SettingHelper.GetDownMode();

        }


        private async void LoadAnimeInfo()
        {
            try
            {

                Loading.IsVisible = true;
                var eresults = await HttpHelper.GetStringAsync(Api._BanInfoApi2(_sid));
                BangumiInfoModel model = JsonConvert.DeserializeObject<BangumiInfoModel>(eresults);
                if (model.code == 0)
                {
                    BindingContext = model.result;

                    if (model.result.title.Contains("僅"))
                    {
                        _videoType = VideoType.AreaAnime;

                        await DisplayAlert("说明", "该番是地区限制番,将会调用系统下载\r\n调用系统下载时清晰度设置无效\r\n请等待系统下载视频完毕后重启哔哩哔哩", "知道了");
                    }
                    Util.SaveHistroy(new HistroyModel()
                    {
                        date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        id = _sid,
                        name = model.result.title,
                        type = "anime"
                    });
                }
                else
                {
                    Util.ShowShortToast("加载失败,请检查网址是否正确");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("读取失败", ex.Message, "OK");
            }
            finally
            {
                Loading.IsVisible = false;
            }

        }


        private async void LoadVideoInfo()
        {
            try
            {
                Loading.IsVisible = true;
                var eresults = await HttpHelper.GetStringAsync(Api._VideoInfoApi(_aid));
                VideoInfoModel model = JsonConvert.DeserializeObject<VideoInfoModel>(eresults);
                if (model.code == 0)
                {
                    BindingContext = model.data;

                    if (model.data.rights.movie == 1)
                    {
                        _videoType = VideoType.Movie;
                        if (model.data.rights.pay == 1)
                        {
                            _videoType = VideoType.VipMovie;
                        }
                    }
                    Util.SaveHistroy(new HistroyModel()
                    {
                        date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        id = "av" + _aid,
                        name = model.data.title,
                        type = "video"
                    });
                }
                else if (model.code == -404)
                {
                    Util.ShowShortToast($"无法加载此内容，如果是番剧请输入sid或epid");
                }
                else
                {
                    Util.ShowShortToast($"加载失败,{model.message}");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("读取失败", ex.Message, "OK");
            }
            finally
            {
                Loading.IsVisible = false;
            }

        }


        private async void ls_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem == null)
            {
                return;
            }
            if (!await CheckPermission())
            {
                Util.ShowLongToast("你没给读写权限啊！！你没给读写权限啊！！你没给读写权限啊！！");
                return;
            }

            switch (_videoType)
            {
                case VideoType.Anime:
                case VideoType.AreaAnime:
                    StartAnimeDownload(((ListView)sender).SelectedItem);
                    break;
                case VideoType.Video:
                case VideoType.Movie:
                case VideoType.VipMovie:
                    StartVideoDownload(((ListView)sender).SelectedItem);
                    break;
                case VideoType.VipAnime:
                    //这个先不用管他
                    break;
                default:
                    break;
            }
            ((ListView)sender).SelectedItem = null;

        }


        private async void StartAnimeDownload(object par)
        {
            try
            {
                Loading.IsVisible = true;
                if (_videoType == VideoType.AreaAnime)
                {
                    _downlaodType = DownlaodType.System;
                }


                var item = par as episodesModel;
                var data = BindingContext as BangumiInfoModel;
                List<segment_listModel> segment_list = new List<segment_listModel>();
                var indexJsonContent = "";
                var entryJsonContent = "";
                var description = "超清";

                int q = 3;
                //转换下清晰度
                var quality = "lua.flv720.bb2api.64";
                var video_quality = 112;
                switch (SettingHelper.GetQuality())
                {
                    case 0:
                        quality = "lua.hdflv2.bb2api.bd";
                        description = "1080P";
                        q = 4;
                        video_quality = 112;
                        break;
                    case 1:
                        quality = "lua.flv.bb2api.80";
                        description = "超清";
                        video_quality = 80;
                        q = 3;
                        break;
                    case 2:
                        quality = "lua.flv720.bb2api.64";
                        description = "高清";
                        video_quality = 64;
                        q = 2;
                        break;
                    case 3:
                        quality = "lua.mp4.bb2api.16";
                        description = "清晰";
                        video_quality = 16;
                        q = 1;
                        break;
                    default:
                        break;
                }


                EntryModel entryModel = new EntryModel()
                {
                    season_id = data.season_id,
                    title = data.title,
                    cover = data.cover,
                    type_tag = quality,
                    time_create_stamp = Api.GetTimeSpan_2,
                    time_update_stamp = Api.GetTimeSpan_2,
                    prefered_video_quality = video_quality,
                    source = new sourceModel()
                    {
                        av_id = long.Parse(item.aid),
                        cid = item.cid,
                        website = "bangumi",
                        webvideo_id = ""
                    },
                    ep = new epModel()
                    {
                        av_id = long.Parse(item.aid),
                        danmaku = item.cid,
                        cover = item.cover,
                        episode_id = long.Parse(item.ep_id),
                        index = item.index,
                        orderindex = item.orderindex,
                        index_title = item.index_title,
                        page = item.page
                    }
                };
                //调用系统时将任务设置未完成
                if (_downlaodType == DownlaodType.System)
                {
                    entryModel.is_completed = true;
                }
                else
                {
                    entryModel.is_completed = false;
                }
                //当调用系统下载时才读取地址
                if (_downlaodType == DownlaodType.System)
                {

                    segment_list = await Util.GetVideoUrl(entryModel.ep.danmaku.ToString(), "https://www.bilibili.com/bangumi/play/ep" + entryModel.ep.episode_id, q, _sid, entryModel.ep.orderindex);
                    long _tbyte = 0;
                    long _timelength = 0;
                    foreach (var item1 in segment_list)
                    {
                        _tbyte += item1.bytes;
                        _timelength += item1.duration;
                    }
                    DownIndexModel downUrlModel = new DownIndexModel()
                    {
                        type_tag = quality,
                        description = description,
                        parse_timestamp_milli = Api.GetTimeSpan_2,
                        time_length = _timelength
                    };

                    downUrlModel.segment_list = segment_list;
                    List<player_codec_config_listModel> player_Codec_Config_ListModel = new List<player_codec_config_listModel>() {
                            new  player_codec_config_listModel(){ player="IJK_PLAYER" },
                            new player_codec_config_listModel(){player="ANDROID_PLAYER"}};
                    downUrlModel.player_codec_config_list = player_Codec_Config_ListModel;
                    indexJsonContent = JsonConvert.SerializeObject(downUrlModel);
                    entryModel.total_time_milli = downUrlModel.time_length;
                    entryModel.total_bytes = _tbyte;
                }


                //序列化entryModel
                entryJsonContent = JsonConvert.SerializeObject(entryModel);


                //开始一个下载任务
                StartDownModel startDownModel = new StartDownModel()
                {
                    downPath = SettingHelper.GetDownPath(),
                    entry_content = entryJsonContent,
                    av_id = entryModel.ep.av_id.ToString(),
                    danmaku_id = entryModel.ep.danmaku.ToString(),
                    episode_id = entryModel.ep.episode_id.ToString(),
                    index_content = indexJsonContent,
                    quality = quality,
                    season_id = _sid,
                    title = entryModel.ep.index + entryModel.ep.index_title,
                    urls = segment_list,
                    clientType = (ClientType)SettingHelper.GetClientMode(),
                    downlaodType = _downlaodType,
                    videoType = _videoType
                };
                var msg = Util.StartDownload(startDownModel);
                if (msg.code == 200)
                {
                    Util.ShowShortToast(msg.message);
                }
                else
                {
                    await DisplayAlert("创建任务失败", msg.message, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("好气啊，出错了", ex.Message, "OK");
            }
            finally
            {
                Loading.IsVisible = false;
            }
        }
        private async void StartVideoDownload(object par)
        {
            try
            {
                Loading.IsVisible = true;
                var item = par as pagesModel;
                var data = BindingContext as VideoInfoModel;
                List<segment_listModel> segment_list = new List<segment_listModel>();
                var indexJsonContent = "";
                var entryJsonContent = "";
                var description = "超清";

                int q = 3;
                //转换下清晰度
                var quality = "lua.flv720.bb2api.64";
                switch (SettingHelper.GetQuality())
                {
                    case 0:
                        quality = "lua.hdflv2.bb2api.bd";
                        description = "1080P";
                        q = 4;
                        break;
                    case 1:
                        quality = "lua.flv.bb2api.80";
                        description = "超清";
                        q = 3;
                        break;
                    case 2:
                        quality = "lua.flv720.bb2api.64";
                        description = "高清";
                        q = 2;
                        break;
                    case 3:
                        quality = "lua.mp4.bb2api.16";
                        description = "清晰";
                        q = 1;
                        break;
                    default:
                        break;
                }


                EntryModel entryModel = new EntryModel()
                {
                    avid = _aid,
                    title = data.title,
                    cover = data.pic,
                    type_tag = quality,
                    time_create_stamp = Api.GetTimeSpan_2,
                    time_update_stamp = Api.GetTimeSpan_2,
                    page_data = new page_dataModel()
                    {
                        cid = item.cid,
                        from = item.from,
                        part = item.part,
                        vid = item.vid,
                        tid = data.tid,
                        page = item.page
                    }
                };
                //调用系统下载时将任务设置未完成
                if (_downlaodType == DownlaodType.System)
                {
                    entryModel.is_completed = true;
                }
                else
                {
                    entryModel.is_completed = false;
                }
                //当调用系统下载时才读取地址
                if (_downlaodType == DownlaodType.System)
                {
                    segment_list = await Util.GetVideoUrl(item.cid.ToString(), "https://www.bilibili.com/video/av16111678", q, "", 0);
                    long _tbyte = 0;
                    long _timelength = 0;
                    foreach (var item1 in segment_list)
                    {
                        _tbyte += item1.bytes;
                        _timelength += item1.duration;
                    }
                    DownIndexModel downUrlModel = new DownIndexModel()
                    {
                        type_tag = quality,
                        description = description,
                        parse_timestamp_milli = Api.GetTimeSpan_2,
                        time_length = _timelength
                    };

                    downUrlModel.segment_list = segment_list;
                    List<player_codec_config_listModel> player_Codec_Config_ListModel = new List<player_codec_config_listModel>() {
                            new  player_codec_config_listModel(){ player="IJK_PLAYER" },
                            new player_codec_config_listModel(){player="ANDROID_PLAYER"}};
                    downUrlModel.player_codec_config_list = player_Codec_Config_ListModel;
                    indexJsonContent = JsonConvert.SerializeObject(downUrlModel);
                    entryModel.total_time_milli = downUrlModel.time_length;
                    entryModel.total_bytes = _tbyte;
                }


                //序列化entryModel
                entryJsonContent = JsonConvert.SerializeObject(entryModel);


                //开始一个下载任务
                StartDownModel startDownModel = new StartDownModel()
                {
                    downPath = SettingHelper.GetDownPath(),
                    entry_content = entryJsonContent,
                    av_id = _aid,
                    danmaku_id = item.cid.ToString(),
                    episode_id = "",
                    index_content = indexJsonContent,
                    quality = quality,
                    season_id = "",
                    title = item.page + " " + item.part,
                    urls = segment_list,
                    page = item.page,
                    clientType = (ClientType)SettingHelper.GetClientMode(),
                    downlaodType = _downlaodType,
                    videoType = _videoType
                };
                var msg = Util.StartDownload(startDownModel);
                if (msg.code == 200)
                {
                    Util.ShowShortToast(msg.message);
                }
                else
                {
                    await DisplayAlert("创建任务失败", msg.message, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("好气啊，出错了", ex.Message, "OK");
            }
            finally
            {
                Loading.IsVisible = false;
            }
        }
        private void menu_setting_Clicked(object sender, EventArgs e)
        {
            this.Navigation.PushAsync(new SettingPage());
        }

        private void menu_help_Clicked(object sender, EventArgs e)
        {
            this.Navigation.PushAsync(new HelpPage());
        }

        private void menu_histroy_Clicked(object sender, EventArgs e)
        {

            this.Navigation.PushAsync(new HistroyPage());

        }

        private void menu_download_Clicked(object sender, EventArgs e)
        {
            this.Navigation.PushAsync(new DownloadPage());
        }
    }





}

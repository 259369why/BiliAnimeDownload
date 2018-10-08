using BiliAnime.Helpers;
using BiliAnimeDownload.Models;
using BiliAnimeDownload.Views;
using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BiliAnimeDownload.Helpers
{
    public static class Util
    {
        public async static void CheckUpdate(Page page,bool isSetting)
        {
            try
            {

                var str = await("http://pic.iliili.cn/bilianime.json?ts=" + Api.GetTimeSpan_2).GetStringAsync();
                CheckUpdateModel model = JsonConvert.DeserializeObject<CheckUpdateModel>(str);

                if (model.versionCode != Util.GetVersioncode())
                {
                    
                    var d = await page.DisplayAlert("发现新版本" + model.version, model.versionMessage, "去更新", "知道了");
                    if (d)
                    {
                        OpenUri(model.updateUrl);
                    }

                }
                else
                {
                    if (isSetting)
                    {
                        ShowLongToast("已经是最新版本了");
                    }
                   
                }
            }
            catch (Exception)
            {
                if (isSetting)
                {
                    ShowLongToast("检查更新失败");
                }
            }

        }

        public async static Task<List<segment_listModel>> GetVideoUrl(string cid, string referer, int quality,string banId,int index)
        {
            try
            {
                List<segment_listModel> segment_list = new List<segment_listModel>();
                Flurl.Http.FlurlClient flurlClient = new FlurlClient(Api._playurlApi(cid, quality));

                var headers = new Dictionary<string, string>();

                headers.Add("Referer", referer);

                headers.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.84 Safari/537.36");
                flurlClient.WithHeaders(headers);
                var playUrlStr = await flurlClient.GetStringAsync();

                if (playUrlStr.Contains("<code>"))
                {
                    flurlClient.Url = Api._playurlApi2(cid, quality);

                    var re = await flurlClient.GetStringAsync();

                    FlvPlyaerUrlModel m = JsonConvert.DeserializeObject<FlvPlyaerUrlModel>(re);
                    // 港澳台的视频会返回一个版权受限的15秒视频，8986943.mp4，出现这个算失败，继续读取
                    if (m.code == 0&&!re.Contains("8986943") && (m.status == 13 && m.vip_status != 0))
                    {
                        foreach (var item in m.durl)
                        {
                            segment_list.Add(new segment_listModel()
                            {
                                url = item.url,
                                bytes = item.size,
                                duration = item.length
                            });
                        }
                    }
                    else
                    {
                        //换个API继续读取下载地址
                        flurlClient.Url = Api._playurlApi4(banId, cid,"");

                        var moeheaders = new Dictionary<string, string>();
                        moeheaders.Add("client", "bili-anime-downloader");
                        moeheaders.Add("ts", Convert.ToInt64((DateTime.Now - new DateTime(1970, 1, 1, 8, 0, 0, 0)).TotalSeconds).ToString());
                        moeheaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.84 Safari/537.36");
                        moeheaders.Add("version", Util.GetVersion());
                      
                        flurlClient.WithHeaders(moeheaders);
                        var re2 = await flurlClient.GetStringAsync();
                        JObject obj = JObject.Parse(re2);
                        if (Convert.ToInt32(obj["code"].ToString()) == 0)
                        {
                            //var urls = JsonConvert.DeserializeObject<List<string>>(obj["data"].ToString());
                            foreach (var item in obj["data"])
                            {
                                segment_list.Add(new segment_listModel()
                                {
                                    url = item["url"].ToString(),
                                    bytes = Convert.ToInt64(item["size"].ToString()),
                                    duration = Convert.ToInt64(item["length"].ToString())
                                });
                            }
                        }
                        else
                        {
                            Util.ShowShortToast("无法读取到下载地址\r\n"+obj["message"].ToString());
                            return new List<segment_listModel>();
                        }
                    }

                }
                else
                {
                    var mc = Regex.Matches(playUrlStr, @"<length>(.*?)</length>.*?<size>(.*?)</size>.*?<url><!\[CDATA\[(.*?)\]\]></url>", RegexOptions.Singleline);
                    foreach (Match url in mc)
                    {
                        segment_list.Add(new segment_listModel()
                        {
                            bytes = Convert.ToInt64(url.Groups[2].Value),
                            duration = Convert.ToInt64(url.Groups[1].Value),
                            url = url.Groups[3].Value
                        });
                        //_tbyte += Convert.ToInt64(url.Groups[2].Value);
                    }
                }
                //_timelength = Convert.ToInt64(Regex.Match(playUrlStr, @"<timelength>(.*?)</timelength>").Groups[1].Value);

                return segment_list;
            }
            catch (Exception)
            {
                Util.ShowShortToast("无法读取到下载地址\r\n可能是服务器正在缓存此视频，请稍后再试");
                return new List<segment_listModel>();
            }

        }


         /// <summary>
        /// 根据Epid取番剧ID
        /// </summary>
        /// <returns></returns>
        public async static Task<string> BangumiEpidToSid(string url)
        {
            try
            {
                if (!url.Contains("http"))
                {
                    url = "https://www.bilibili.com/bangumi/play/ep"+ url;
                }

                Flurl.Http.FlurlClient flurlClient = new FlurlClient(url);
                var re = await flurlClient.GetStringAsync();
                var data= Regex.Match(re, @"ss(\d+)").Groups[1].Value;
                if (data!="")
                {
                    return data;
                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                throw;
                return "";
            }
        }


        public static void ShowLongToast(string message)
        {
            DependencyService.Get<IShowToast>().LongAlert(message);
        }
        public static void ShowShortToast(string message)
        {
            
            DependencyService.Get<IShowToast>().ShortAlert(message);
        }

        public static int GetVersioncode( )
        {
            return DependencyService.Get<ISetting>().GetVersioncode();
        }
        public static string GetVersion()
        {
            
            return DependencyService.Get<ISetting>().GetVersion();
        }
        public static void SavaSetting(string name,string value)
        {
            DependencyService.Get<ISetting>().SavaSetting(name, value);
        }
        public static string GetSetting(string name)
        {
            return DependencyService.Get<ISetting>().GetSetting(name);
        }
        public static void PickFolder()
        {
            DependencyService.Get<ISetting>().PickFolder();
            
        }
        public static void OpenUri(string url)
        {
            DependencyService.Get<IUtils>().OpenUri(url);
        }
       

        public static void SaveHistroy(HistroyModel histroy)
        {
            //懒得用sql lite 了...
            var ls = new List<HistroyModel>();
            var str = GetSetting("histroy");
            if (str!="")
            {
                ls = Newtonsoft.Json.JsonConvert.DeserializeObject<List<HistroyModel>>(str);
            }
            var existitem = ls.Find(x => x.id == histroy.id);
            if (existitem != null)
            {
                existitem.date = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            }
            else
            {
                ls.Add(histroy);
            }
           
            SavaSetting("histroy", JsonConvert.SerializeObject(ls));
        }
        public static List<HistroyModel> GetHistroy()
        {
            List<HistroyModel> histroys = new List<HistroyModel>();
            try
            {
                var str = GetSetting("histroy");
                if (str != null && str != "")
                {
                    histroys = Newtonsoft.Json.JsonConvert.DeserializeObject<List<HistroyModel>>(str);
                }
            }
            catch (Exception)
            {
            }
            
            return histroys;
        }



        public static MsgModel StartDownload(StartDownModel m)
        {
           
            return DependencyService.Get<IDownloadHelper>().StartDownload(m);
        }






    }
}

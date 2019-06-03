using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using BiliAnimeDownload.Droid;
using Android.Content.PM;
using Android.Support.V4.Content;
using Android;

[assembly: Dependency(typeof(Utils))]
namespace BiliAnimeDownload.Droid
{
    public class Utils : IUtils
    {
        
        public void OpenUri(string url)
        {
            try
            {
                Android.Net.Uri uri = Android.Net.Uri.Parse(url);
                Intent intent = new Intent(Intent.ActionView, uri);
                Android.App.Application.Context.StartActivity(intent);

                //Intent browserIntent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(url));
                //Android.App.Application.Context.StartActivity(browserIntent);
            }
            catch (Exception ex)
            {
                Toast.MakeText(Android.App.Application.Context, "打不开链接："+ url, ToastLength.Long).Show();
               
            }
           
        }

        public bool CheckPermission()
        {
            if (ContextCompat.CheckSelfPermission(Android.App.Application.Context, Manifest.Permission.WriteExternalStorage) == (int)Permission.Granted)
            {
                Toast.MakeText(Android.App.Application.Context, "已经授权", ToastLength.Long).Show();
                return true;
                // We have permission, go ahead and use the camera.
            }
            else
            {
               
                Toast.MakeText(Android.App.Application.Context, "没有授权", ToastLength.Long).Show();
                return false;
                // Camera permission is not granted. If necessary display rationale & request.
            }
        }
    }
}
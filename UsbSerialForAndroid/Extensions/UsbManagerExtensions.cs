/* Copyright 2017 Tyler Technologies Inc.
 *
 * Project home page: https://github.com/anotherlab/xamarin-usb-serial-for-android
 * Portions of this library are based on usb-serial-for-android (https://github.com/mik3y/usb-serial-for-android).
 * Portions of this library are based on Xamarin USB Serial for Android (https://bitbucket.org/lusovu/xamarinusbserial).
 */

using System;
using System.Threading.Tasks;
using Android.Hardware.Usb;
using Android.App;
using Android.Content;
using Android.OS;
using System.Collections.Generic;

namespace Hoho.Android.UsbSerial.Util
{
    public static class UsbManagerExtensions
    {
        const string ACTION_USB_PERMISSION = "com.Hoho.Android.UsbSerial.Util.USB_PERMISSION";

        //static readonly Dictionary<Tuple<Context, UsbDevice>, TaskCompletionSource<bool>> taskCompletionSources =
        //    new Dictionary<Tuple<Context, UsbDevice>, TaskCompletionSource<bool>>();

        public static Task<bool> RequestPermissionAsync(this UsbManager manager, UsbDevice device, Context context)
        {
            var completionSource = new TaskCompletionSource<bool>();

            var usbPermissionReceiver = new UsbPermissionReceiver(completionSource);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
#pragma warning disable CA1416
                //this is checking that it is API 26 or greater, bu the compiler doesn't see it that way. So we have to add in a pragma
                context.RegisterReceiver(usbPermissionReceiver, new IntentFilter(ACTION_USB_PERMISSION), ReceiverFlags.Exported);
#pragma warning restore CA1416
            }
            else
            {
                context.RegisterReceiver(usbPermissionReceiver, new IntentFilter(ACTION_USB_PERMISSION));
            }


            // Targeting S+ (version 31 and above) requires that one of FLAG_IMMUTABLE or FLAG_MUTABLE be specified when creating a PendingIntent.
#if NET6_0_OR_GREATER
            PendingIntentFlags pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S ? PendingIntentFlags.Mutable : 0;
#else
            PendingIntentFlags pendingIntentFlags = Build.VERSION.SdkInt >= (BuildVersionCodes)31 ? (PendingIntentFlags).33554432 : 0;
#endif
            var intent = new Intent(ACTION_USB_PERMISSION);
            // we need this for Android 34+ as apparently, security. If you follow Google's docs,
            // you will spend a lot of time fighting with the API not actually replying with any
            // useful data... because Immutable Intents *can;t* be changed, so the API is basically
            // hobbled.
            if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake)
            {
                intent.SetPackage(context.PackageName);
            }

            var pendingIntent = PendingIntent.GetBroadcast(context, 0, intent, pendingIntentFlags);

            manager.RequestPermission(device, pendingIntent);

            return completionSource.Task;
        }

        class UsbPermissionReceiver(TaskCompletionSource<bool> completionSource) : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                var device = intent.GetParcelableExtra(UsbManager.ExtraDevice) as UsbDevice;
                var permissionGranted = intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false);
                context.UnregisterReceiver(this);
                completionSource.TrySetResult(permissionGranted);
            }
        }

    }
}
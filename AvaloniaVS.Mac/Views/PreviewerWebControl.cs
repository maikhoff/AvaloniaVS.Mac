using System;
using AppKit;
using WebKit;

namespace AvaloniaVS.Mac.Views
{
    public class PreviewerWebControl : NSView
    {
        WebView webview;
        public PreviewerWebControl()
        {
            webview = new WebView();
            webview.Frame = this.Frame;
            webview.AutoresizingMask = NSViewResizingMask.HeightSizable | NSViewResizingMask.WidthSizable;
            webview.MainFrameUrl = "http://127.0.0.1:5000";

            AddSubview(webview);
        }
    }
}


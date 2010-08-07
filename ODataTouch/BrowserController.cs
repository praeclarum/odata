//
// Copyright (c) 2009-2010 Krueger Systems, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
namespace OData.Touch
{
	public class BrowserController : UIViewController
	{
		UIWebView _browser;
		Del _del;

		public BrowserController (string url)
		{
			try {
				Initialize ();
				
				var nsurl = new NSUrl (url);

				var isMedia = url.EndsWith(".png") || url.EndsWith(".jpg") || url.EndsWith(".gif");

				_browser.ScalesPageToFit = !isMedia;
				
				_browser.LoadRequest (new NSUrlRequest (nsurl));
				
				NavigationItem.RightBarButtonItem = new UIBarButtonItem ("Safari", UIBarButtonItemStyle.Done, delegate {
					try {
						UIApplication.SharedApplication.OpenUrl (nsurl);
					} catch (Exception e2) {
						Log.Error (e2);
					}
				});
				
			} catch (Exception error) {
				Log.Error (error);
			}
		}

		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			return true;
		}

		public BrowserController (string title, string html)
		{
			try {
				Initialize ();
				
				Title = title;

				_browser.ScalesPageToFit = false;

				var h = @"<html><head><style>body{font-family:sans-serif;}</style></head><body>" + html + "</body></html>";

				_browser.LoadHtmlString (h, new NSUrl (""));

			} catch (Exception error) {
				Log.Error (error);
			}
		}

		void Initialize ()
		{
			_del = new Del (this);
			_browser = new UIWebView (View.Bounds);
			_browser.Delegate = _del;
			_browser.Frame = View.Bounds;
			_browser.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			View.AddSubview (_browser);
		}

		class Del : UIWebViewDelegate
		{
			BrowserController _c;
			public Del (BrowserController c)
			{
				_c = c;
			}
			public override void LoadingFinished (UIWebView webView)
			{
				try {
					var t = webView.EvaluateJavascript ("document.title");
					if (t.Length > 0) {
						_c.Title = t;
					}
				} catch (Exception error) {
					Log.Error (error);
				}
			}
		}
	}
}


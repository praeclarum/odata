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
using System.Collections.Generic;

namespace OData.Touch
{
	public class FeedsController : DialogViewController
	{
		public UserService Service { get; private set; }

		public event Action<UserFeed> FeedSelected;

		public FeedsController (UserService service, UserFeed feed) : base(UITableViewStyle.Grouped)
		{
			try {
				Service = service;

				LoadFeeds ();

			} catch (Exception error) {
				Log.Error (error);
			}
		}

		void LoadFeeds ()
		{
			List<UserFeed> feeds = null;

			using (var repo = new Repo ()) {

				feeds = repo.GetFeeds (Service);

				feeds.Sort ((x, y) => x.Category.CompareTo (y.Category));
			}

			DialogSection feedSection = null;

			foreach (var f in feeds) {

				if (feedSection == null || feedSection.Header != f.Category) {
					feedSection = new DialogSection (f.Category);
					Sections.Add (feedSection);
				}

				var e = new FeedElement (Service, f, UITableViewCellAccessory.None);
				e.Selected += delegate {
					if (FeedSelected != null) {
						FeedSelected (e.Feed);
					}
				};
				feedSection.Add (e);
			}
		}

		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			return true;
		}
	}

	public class FeedElement : StaticElement
	{
		public UserService Service { get; private set; }
		public UserFeed Feed { get; private set; }

		public FeedElement (UserService service, UserFeed feed, UITableViewCellAccessory acc) : base(feed.Name)
		{
			Accessory = acc;
			Service = service;
			Feed = feed;
		}
	}
}


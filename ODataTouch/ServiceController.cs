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
using MonoTouch.Foundation;
namespace OData.Touch
{
	public class ServiceController : DialogViewController
	{
		public UserService Service { get; private set; }

		List<DialogSection> _feeds;
		DialogSection _queries;
		DialogSection _loadingSection;
		LoadingElement _loadingElement;

		public ServiceController (UserService svc) : base(UITableViewStyle.Grouped)
		{
			try {
				Title = svc.Name;
				
				Service = svc;
				
				_queries = new DialogSection ("Queries");
				
				_feeds = new List<DialogSection> ();
				_feeds.Add (new DialogSection ("Feeds"));
				
				_loadingSection = new DialogSection ();
				_loadingElement = new LoadingElement ();
				_loadingElement.Start ();
				_loadingSection.Add (_loadingElement);
				
				Sections.AddRange (_feeds);
				
				Sections.Add (_loadingSection);
				
				NavigationItem.RightBarButtonItem = new UIBarButtonItem (UIBarButtonSystemItem.Add, HandleAddButton);
				
			} catch (Exception error) {
				Log.Error (error);
			}
		}

		public void PushDataViewController (UserFeed feed)
		{
			var c = new DataViewController (new UserQuery() {
				FeedId = feed.Id,
				Name = feed.Name,
				ServiceId = Service.Id
			}, feed.Url);
			NavigationController.PushViewController (c, true);
		}

		void HandleAddButton (object sender, EventArgs e)
		{
			try {
				
				var addC = new QueryController (Service, new UserQuery());
				var navC = new UINavigationController (addC);

				addC.Done += delegate {



					var q = new UserQuery {
						Filter = addC.Filter,
						FeedId = addC.Feed.Id,
						Name = addC.Name,
						OrderBy = addC.OrderBy,
						ServiceId = Service.Id
					};

					using (var repo = new Repo ()) {
						repo.Add (q);
					}

					addC.DismissModalViewControllerAnimated (true);
				};

				NavigationController.PresentModalViewController (navC, true);

			} catch (Exception error) {
				Log.Error (error);
			}
		}

		public override void ViewWillAppear (bool animated)
		{
			try {
				LoadData(false);
			}
			catch (Exception error) {
				Log.Error(error);
			}
		}

		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			return true;
		}

		void RemoveLoading ()
		{
			UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
			if (_loadingSection != null) {
				_loadingElement.Stop ();
				Sections.Remove (_loadingSection);
				_loadingSection = null;
			}
		}

		NetErrorAlert _netError = new NetErrorAlert ();

		void BeginDownloadFeeds ()
		{
			UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
			
			App.RunInBackground (delegate {
				
				Exception netError = null;
				
				using (var repo = new Repo ()) {
					
					try {
						var doc = new ServiceDocument (Service.ServiceRootUri);
						var fs = doc.Feeds;

						var feeds = new List<UserFeed> ();
						foreach (var f in fs) {
							feeds.Add (new UserFeed (f));
						}

						var metaDocs = new List<UserMetadataDocument> ();
						foreach (var m in doc.Metadata) {
							metaDocs.Add (new UserMetadataDocument (m));
						}

						repo.UpdateFeeds (feeds, Service);
						repo.UpdateMetadataDocuments (metaDocs, Service);

						Service.LastFeedUpdateTime = DateTime.UtcNow;

						repo.Save (Service);
					} catch (Exception ex) {
						netError = ex;
					}
				}

				if (netError != null) {
					App.RunInForeground (delegate {
						RemoveLoading ();
						_netError.ShowError (netError);
					});
				} else {
					App.RunInForeground (delegate {
						RemoveLoading ();
						LoadData (false);
					});
				}
			});
		}

		void LoadData (bool forceFeeds)
		{
			//
			// Load the data
			//
			List<UserQuery> queries = null;
			List<UserFeed> feeds = null;

			using (var repo = new Repo ()) {
				queries = repo.GetQueries (Service);

				feeds = repo.GetFeeds (Service);

				feeds.Sort ((x, y) => x.Category.CompareTo (y.Category));

				if (feeds.Count == 0 || Service.ShouldUpdateFeeds || forceFeeds) {
					BeginDownloadFeeds ();
				}
			}

			//
			// Update the UI
			//
			if (feeds.Count > 0) {
				RemoveLoading ();
				foreach (var f in _feeds) {
					Sections.Remove (f);
				}

				DialogSection feedSection = null;

				foreach (var f in feeds) {

					if (feedSection == null || feedSection.Header != f.Category) {
						feedSection = new DialogSection (f.Category);
						_feeds.Add (feedSection);
						Sections.Add (feedSection);
					}

					var e = new FeedElement (Service, f, UITableViewCellAccessory.DisclosureIndicator);
					e.Selected += delegate { PushDataViewController (e.Feed); };
					feedSection.Add (e);

				}
			}

			_queries.Clear ();
			foreach (var q in queries) {

				var e = new QueryElement (q);
				_queries.Add (e);

			}
			if (queries.Count > 0 && !Sections.Contains (_queries)) {
				Sections.Insert (0, _queries);
			}

			TableView.ReloadData ();
		}

		class QueryElement : StaticElement
		{
			public UserQuery Query { get; private set; }

			public QueryElement (UserQuery query) : base(query.Name)
			{
				Accessory = UITableViewCellAccessory.DisclosureIndicator;
				CellStyle = UITableViewCellStyle.Subtitle;
				Query = query;
			}

			public override void OnSelected (DialogViewController sender, NSIndexPath indexPath)
			{
				UserFeed feed = null;
				using (var repo = new Repo()) {
					feed = repo.GetFeed(Query.FeedId);
				}
				var c = new DataViewController(Query, feed.Url);
				sender.NavigationController.PushViewController(c, true);
			}

			public override void RefreshCell (UITableViewCell cell)
			{
				base.RefreshCell (cell);
				cell.DetailTextLabel.Text = Query.Filter;
			}
		}

	}

}


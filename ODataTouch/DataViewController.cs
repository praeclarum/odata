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
	public class DataViewController : DialogViewController
	{
		public UserFeed Feed { get; private set; }
		public UserQuery Query { get; private set; }
		public string Url { get; private set; }

		public int NumEntitiesPerRequest { get; set; }
		public bool PagingEnabled { get; set; }

		int _numGets = 0;

		DialogSection _moreSection;
		DialogSection _loadSection;
		LoadingElement _loadElement;

		int _index;

		public DataViewController (UserQuery query, string url) : base(UITableViewStyle.Grouped)
		{
			try {
				Query = query;
				Url = url;
				Title = Query.Name;
				
				PagingEnabled = true;
				NumEntitiesPerRequest = 20;
				_index = 0;
				
				_loadSection = new DialogSection ();
				_loadElement = new LoadingElement ();
				_loadSection.Add (_loadElement);
				_loadElement.Start ();
				Sections.Add (_loadSection);
				
				_moreSection = new DialogSection ();
				_moreSection.Add (new ActionElement ("More", GetMore));
				
				if (PagingEnabled) {
					Sections.Add (_moreSection);
				}
				
				using (var repo = new Repo ()) {
					Feed = repo.GetFeed (query.FeedId);
				}
				
				if (Query.Id > 0) {
					NavigationItem.RightBarButtonItem = new UIBarButtonItem (UIBarButtonSystemItem.Edit, HandleEdit);
				}
				
				GetMore ();
				
			} catch (Exception error) {
				Log.Error (error);
			}
		}

		void HandleEdit (object sender, EventArgs e)
		{
			try {

				UserService service = null;

				using (var repo = new Repo()) {
					service = repo.GetService(Query.ServiceId);
				}

				var editC = new QueryController (service, Query);
				var navC = new UINavigationController (editC);

				editC.Done += delegate {

					editC.DismissModalViewControllerAnimated (true);

					Query.Filter = editC.Filter;
					Query.FeedId = editC.Feed.Id;
					Query.Name = editC.Name;
					Query.OrderBy = editC.OrderBy;

					using (var repo = new Repo ()) {
						repo.Save (Query);
					}

					_numGets = 0;
					_index = 0;
					Sections.Clear ();
					
					_loadSection = new DialogSection ();
					_loadSection.Add (_loadElement);
					_loadElement.Start ();
					Sections.Add (_loadSection);
					
					GetMore ();
				};
				
				NavigationController.PresentModalViewController (navC, true);
				
			} catch (Exception error) {
				Log.Error (error);
			}
		}

		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			return true;
		}

		void AddToQuery (Dictionary<string, object> q)
		{
			if (!string.IsNullOrEmpty (Query.Filter)) {
				q["$filter"] = Query.Filter;
			}
			if (!string.IsNullOrEmpty (Query.OrderBy)) {
				q["$orderby"] = Query.OrderBy;
			}
		}

		NetErrorAlert _netError = new NetErrorAlert ();

		void GetMore ()
		{
			if (!PagingEnabled && _numGets > 0) {
				return;
			}
			
			if (PagingEnabled) {
				Sections.Remove (_moreSection);
				TableView.ReloadData ();
			}
			
			UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
			
			App.RunInBackground (delegate {
				
				var atom = "";
				
				try {
					var q = new Dictionary<string, object> ();
					
					AddToQuery (q);
					
					if (PagingEnabled) {
						q["$skip"] = _index;
						q["$top"] = NumEntitiesPerRequest;
					}
					
					try {
						
						atom = Http.Get (Url + "?" + Http.MakeQueryString (q));
						
					} catch (System.Net.WebException nex) {
						var hr = nex.Response as System.Net.HttpWebResponse;
						if (hr.StatusDescription.ToLowerInvariant ().IndexOf ("not implemented") >= 0 && PagingEnabled) {
							
							//
							// Try without paging
							//
							q.Remove ("$skip");
							q.Remove ("$top");
							atom = Http.Get (Url + "?" + Http.MakeQueryString (q));
							PagingEnabled = false;
						}
					}
					
					_numGets++;
					
					if (PagingEnabled) {
						_index += NumEntitiesPerRequest;
					}
				} catch (Exception netError) {
					Console.WriteLine (netError);
					App.RunInForeground (delegate { _netError.ShowError (netError); });
					return;
				}
				
				var ents = EntitySet.LoadFromAtom (atom);
				
				App.RunInForeground (delegate {
					
					UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
					
					if (_loadSection != null) {
						_loadElement.Stop ();
						Sections.Remove (_loadSection);
					}
					
					foreach (var e in ents) {
						
						var sec = new DialogSection (e.Title, e.Author);
						
						foreach (var prop in e.Properties) {
							sec.Add (new PropertyElement (e, prop));
						}
						
						foreach (var link in e.Links) {
							if (link.Rel == "edit")
								continue;
							sec.Add (new LinkElement (Feed, link));
						}
						
						Sections.Add (sec);
						
					}
					
					if (PagingEnabled && ents.Count >= NumEntitiesPerRequest) {
						Sections.Add (_moreSection);
					}
					
					TableView.ReloadData ();
					
				});
				
			});
		}

		class PropertyElement : StaticElement
		{
			public Entity Entity { get; private set; }
			public EntityProperty Property { get; private set; }
			public PropertyElement (Entity e, EntityProperty prop) : base(prop.Name)
			{
				Entity = e;
				CellStyle = UITableViewCellStyle.Value2;
				Property = prop;
			}
			protected override float GetHeight (float width)
			{
				return 22.0f;
			}
			bool LongText {
				get { return !Property.IsDateTime && Property.ValueText.Length > 24; }
			}
			public override void RefreshCell (UITableViewCell cell)
			{
				base.RefreshCell (cell);
				var more = (LongText || Property.LooksLikeHtml || Property.LooksLikeLink || (Entity.HasLocation && Entity.IsLocationProperty (Property)));
				cell.Accessory = more ? UITableViewCellAccessory.DisclosureIndicator : UITableViewCellAccessory.None;
				cell.SelectionStyle = more ? UITableViewCellSelectionStyle.Blue : UITableViewCellSelectionStyle.None;
				cell.DetailTextLabel.Text = Property.DisplayText;
			}
			public override void OnSelected (DialogViewController sender, MonoTouch.Foundation.NSIndexPath indexPath)
			{
				if (Property.LooksLikeLink) {
					var c = new BrowserController (Property.ValueText);
					sender.NavigationController.PushViewController (c, true);
				} else if (Property.LooksLikeHtml) {
					var c = new BrowserController (Property.Name, Property.ValueText);
					sender.NavigationController.PushViewController (c, true);
				} else if (Entity.IsLocationProperty (Property)) {
					var c = new MapController (Entity);
					sender.NavigationController.PushViewController (c, true);
				} else if (LongText) {
					var html = Html.Encode (Property.ValueText);
					var c = new BrowserController (Property.Name, html);
					sender.NavigationController.PushViewController (c, true);
				}
				base.OnSelected (sender, indexPath);
			}
		}

		class LinkElement : StaticElement
		{
			public UserFeed Feed { get; private set; }
			public Entity.Link Link { get; private set; }

			public LinkElement (UserFeed feed, Entity.Link link) : base(link.Name)
			{
				Feed = feed;
				Link = link;
			}
			protected override float GetHeight (float width)
			{
				return 33.0f;
			}
			public override void RefreshCell (UITableViewCell cell)
			{
				base.RefreshCell (cell);
				cell.Accessory = Link.IsSingleEntity ? UITableViewCellAccessory.DetailDisclosureButton : UITableViewCellAccessory.DisclosureIndicator;
			}
			public override void OnSelected (DialogViewController sender, MonoTouch.Foundation.NSIndexPath indexPath)
			{
				var newData = new DataViewController (new UserQuery {
					FeedId = Feed.Id,
					Name = Link.Href,
					ServiceId = Feed.ServiceId
				}, Feed.GetUrl (Link.Href));
				newData.PagingEnabled = !Link.IsSingleEntity;
				sender.NavigationController.PushViewController (newData, true);
			}
		}
	}
}


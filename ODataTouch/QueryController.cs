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

namespace OData.Touch
{
	public class QueryController : DialogViewController
	{
		TextFieldElement _nameElement;
		QueryFeedElement _feedElement;
		TextViewElement _filterElement;
		TextFieldElement _orderbyElement;

		DialogSection _propsSec;
		DialogSection _helpSec;

		ActionElement _helpElement;

		UserMetadataDocument _metaDoc = null;

		public int QueryId { get; private set; }

		public string Name {
			get { return _nameElement.Value.Trim (); }
		}
		public UserFeed Feed {
			get { return _feedElement.Value; }
		}
		public string Filter {
			get { return _filterElement.Value.Trim (); }
		}
		public string OrderBy {
			get { return _orderbyElement.Value.Trim (); }
		}

		public QueryController (UserService service, UserQuery query) : base(UITableViewStyle.Grouped)
		{
			try {
				
				QueryId = query.Id;
				
				Title = query.Name;
				
				if (query.Name.Length == 0) {
					Title = "Add Query";
				}
				
				_nameElement = new TextFieldElement ("Name", "Display Name", 70);
				_nameElement.Value = query.Name;
				_nameElement.TextField.AutocapitalizationType = UITextAutocapitalizationType.Words;
				_nameElement.TextField.AllEditingEvents += HandleNameElementTextFieldAllEditingEvents;
				
				using (var repo = new Repo ()) {
					_feedElement = new QueryFeedElement (service, repo.GetFeed (query.FeedId));
				}
				
				_filterElement = new TextViewElement ("Filter", 44 * 2);
				_filterElement.TextView.Font = UIFont.FromName ("Courier-Bold", 16);
				_filterElement.TextView.AutocorrectionType = UITextAutocorrectionType.No;
				_filterElement.TextView.ContentInset = new UIEdgeInsets (0, 0, 0, 0);
				_filterElement.TextView.Changed += delegate {
					try {
						if (_filterElement.TextView.Text.Contains ("\n")) {
							_filterElement.TextView.Text = _filterElement.TextView.Text.Replace ("\n", " ").Trim ();
							_filterElement.TextView.ResignFirstResponder ();
						}
					} catch (Exception err) {
						Log.Error (err);
					}
				};
				_filterElement.Value = query.Filter;
				
				_orderbyElement = new TextFieldElement ("Order", "Orderby Expression", 70);
				_orderbyElement.Value = query.OrderBy;
				
				var sec = new DialogSection ();
				sec.Add (_nameElement);
				sec.Add (_feedElement);
				sec.Add (_filterElement);
				sec.Add (_orderbyElement);
				
				Sections.Add (sec);
				
				_helpElement = new ActionElement ("Query Help", delegate {
					var b = new BrowserController ("Query Help", System.IO.File.ReadAllText ("QueryHelp.html"));
					NavigationController.PushViewController (b, true);
				});
				_helpSec = new DialogSection ();
				_helpSec.Add (_helpElement);
				Sections.Add (_helpSec);
				
				_propsSec = new DialogSection ("Properties");
				
				if (QueryId > 0) {
					var delElement = new ActionElement ("Delete Query", delegate {
						_deleteAlert = new UIAlertView ("", "Are you sure you wish to delete the query " + Name + "?", null, "Cancel", "Delete");
						_deleteAlert.Clicked += Handle_deleteAlertClicked;
						_deleteAlert.Show ();
					});
					var csec = new DialogSection ();
					csec.Add (delElement);
					Sections.Add (csec);
				}
				
				NavigationItem.LeftBarButtonItem = new UIBarButtonItem ("Cancel", UIBarButtonItemStyle.Bordered, HandleCancelButton);
				NavigationItem.RightBarButtonItem = new UIBarButtonItem ("Done", UIBarButtonItemStyle.Done, HandleDoneButton);
				
			} catch (Exception error) {
				Log.Error (error);
			}
		}

		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			return true;
		}

		public override void ViewWillAppear (bool animated)
		{
			try {
				RefreshProps ();
			} catch (Exception error) {
				Log.Error (error);
			}
		}

		void RefreshProps ()
		{
			Sections.Remove (_propsSec);

			if (_feedElement.Value != null) {

				Sections.Insert (Sections.IndexOf(_helpSec), _propsSec);

				if (_metaDoc == null || _metaDoc.BaseUrl != _feedElement.Value.BaseUrl) {
					using (var repo = new Repo ()) {
						_metaDoc = repo.FindMetadataDocument (_feedElement.Value.BaseUrl, _feedElement.Value.ServiceId);
					}
				}

				_propsSec.Clear ();
				if (_metaDoc != null) {

					var ent = _metaDoc.Metadata.FindEntityTypeForEntitySet (_feedElement.Value.Name);
					if (ent != null) {

						foreach (var p in ent.Properties) {

							if (p.IsBasicType) {
								_propsSec.Add (new PropertyElement (p.Name, p.TypeFullName));
							}
							else {
								var cent = _metaDoc.Metadata.FindEntityType(p.TypeFullName);
								if (cent != null) {
									foreach (var cp in cent.Properties) {
										if (cp.IsBasicType) {
											_propsSec.Add (new PropertyElement (p.Name +"/" + cp.Name, cp.TypeFullName));
										}
									}
								}
							}
						}
					}
				}
			}

			TableView.ReloadData ();
		}

		void Handle_deleteAlertClicked (object sender, UIButtonEventArgs e)
		{
			try {
				if (e.ButtonIndex == 1) {
					using (var repo = new Repo ()) {
						repo.DeleteQuery (QueryId);
					}
					DismissModalViewControllerAnimated (true);
				}
				
			} catch (Exception error) {
				Log.Error (error);
			}
		}

		UIAlertView _deleteAlert;

		void HandleNameElementTextFieldAllEditingEvents (object sender, EventArgs e)
		{
			try {
				Title = _nameElement.Value;
			} catch (Exception error) {
				Log.Error (error);
			}
		}

		public event Action Done;

		UIAlertView _noAlert = null;

		void HandleDoneButton (object sender, EventArgs e)
		{
			try {
				
				if (!string.IsNullOrEmpty (Filter) && !string.IsNullOrEmpty (Name) && (Feed != null)) {
					
					if (Done != null) {
						Done ();
					}
					
				} else {
					
					_noAlert = new UIAlertView ("", "Please completely fill in the Name, Feed, and Filter entries for the query.", null, "OK");
					_noAlert.Show ();
					
				}
				
			} catch (Exception error) {
				Log.Error (error);
			}
		}

		void HandleCancelButton (object sender, EventArgs e)
		{
			try {
				DismissModalViewControllerAnimated (true);
			} catch (Exception error) {
				Log.Error (error);
			}
		}

		class PropertyElement : DialogElement
		{

			public string Name { get; private set; }
			public string Type { get; private set; }

			public PropertyElement (string name, string type)
			{
				Name = name;
				Type = EntityTypeInfo.GetTypeShortName(type);
				CellStyle = UITableViewCellStyle.Value1;
			}

			protected override float GetHeight (float width)
			{
				return 22.0f;
			}

			public override void RefreshCell (UITableViewCell cell)
			{
				cell.SelectionStyle = UITableViewCellSelectionStyle.None;
				cell.TextLabel.Text = Name;
				cell.DetailTextLabel.Text = Type;
			}
		}

		class QueryFeedElement : StaticElement
		{
			public UserFeed Value { get; private set; }
			public UserService Service { get; private set; }

			public QueryFeedElement (UserService service, UserFeed feed) : base("Feed")
			{
				Service = service;
				Value = feed;
				CellStyle = UITableViewCellStyle.Value1;
			}

			public override void RefreshCell (UITableViewCell cell)
			{
				base.RefreshCell (cell);
				cell.DetailTextLabel.Text = Value != null ? Value.Name : "";
				cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
			}

			public override void OnSelected (DialogViewController sender, MonoTouch.Foundation.NSIndexPath indexPath)
			{
				var c = new FeedsController (Service, Value);
				c.FeedSelected += f =>
				{
					Value = f;
					c.NavigationController.PopViewControllerAnimated (true);
					sender.TableView.ReloadData ();
				};
				sender.NavigationController.PushViewController (c, true);
			}
		}
		
	}
}


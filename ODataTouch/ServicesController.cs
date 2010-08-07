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
using System.Collections.Generic;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.MessageUI;

namespace OData.Touch
{
	public class ServicesController : DialogViewController
	{
		DialogSection _servicesSection;

		List<UserService> _services;
		public List<UserService> Services {
			get { return _services; }
			set {
				_services = value;

				_servicesSection.Clear ();

				foreach (var s in _services) {
					_servicesSection.Add (new ServiceElement (s));
				}

				TableView.ReloadData ();
			}
		}

		MFMailComposeViewController _mail = null;

		public ServicesController () : base(UITableViewStyle.Grouped)
		{
			try {

				Title = "OData Services";

				_servicesSection = new DialogSection ();
				Sections.Add (_servicesSection);

				var asec = new DialogSection ();
				asec.Add (new ActionElement ("About", delegate {
					var c = new BrowserController ("About", "<h1>OData Browser</h1><p>By <a href='http://kruegersystems.com'>Krueger Systems, Inc.</a></p>");
					NavigationController.PushViewController (c, true);
				}));
				if (MFMailComposeViewController.CanSendMail) {

					asec.Add (new ActionElement ("Support", delegate {

						_mail = new MFMailComposeViewController();
						_mail.SetSubject("OData Browser");
						_mail.SetToRecipients(new string[]{"support@kruegersystems.com"});
						_mail.MailComposeDelegate = new MailDel();
						PresentModalViewController(_mail, true);

					}));
				}
				
				Sections.Add (asec);
				
				this.NavigationItem.HidesBackButton = false;
				
				NavigationItem.BackBarButtonItem = new UIBarButtonItem ("Services", UIBarButtonItemStyle.Bordered, delegate {
					try {
						NavigationController.PopViewControllerAnimated (true);
					} catch (Exception e1) {
						Log.Error (e1);
					}
				});
				
				NavigationItem.LeftBarButtonItem = new UIBarButtonItem ("Edit", UIBarButtonItemStyle.Bordered, HandleEditButton);
				
				NavigationItem.RightBarButtonItem = new UIBarButtonItem (UIBarButtonSystemItem.Add, HandleAddButton);
				
				BeginLoadingServices ();
				
			} catch (Exception error) {
				Log.Error (error);
			}
		}

		class MailDel : MFMailComposeViewControllerDelegate {
			public override void Finished (MFMailComposeViewController controller, MFMailComposeResult result, NSError error)
			{
				controller.DismissModalViewControllerAnimated(true);
			}
		}

		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			return true;
		}

		void BeginLoadingServices ()
		{
			App.RunInBackground (delegate {
				List<UserService> services = null;
				using (var repo = new Repo ()) {
					services = repo.GetActiveServices ();
				}
				App.RunInForeground (delegate { Services = services; });
			});
		}

		void HandleAddButton (object sender, EventArgs e)
		{
			try {
				
				var addC = new AddController (delegate { BeginLoadingServices (); });
				var navC = new UINavigationController (addC);
				NavigationController.PresentModalViewController (navC, true);
				
			} catch (Exception error) {
				Log.Error (error);
			}
		}

		void HandleEditButton (object sender, EventArgs e)
		{
			try {
				
				SetEditing (!Editing, true);
				
				NavigationItem.LeftBarButtonItem.Title = Editing ? "Done" : "Edit";
				
			} catch (Exception e1) {
				Log.Error (e1);
			}
		}

		class ServiceElement : StaticElement
		{
			public UserService Service { get; private set; }
			public ServiceElement (UserService svc) : base(svc.Name)
			{
				CanEdit = true;
				CellStyle = UITableViewCellStyle.Subtitle;
				Service = svc;
				Selected += PushServiceController;
			}
			public void PushServiceController (DialogViewController dvc)
			{
				var c = (ServicesController)dvc;
				
				var svcC = new ServiceController (Service);
				
				c.NavigationController.PushViewController (svcC, true);
			}
			public override void RefreshCell (UITableViewCell cell)
			{
				base.RefreshCell (cell);
				cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
				cell.DetailTextLabel.Text = Service.ServiceRootUri;
			}
			public override void OnDelete (DialogViewController sender, NSIndexPath indexPath)
			{
				using (var repo = new Repo ()) {
					repo.DeleteService (Service);
				}
				((ServicesController)sender).Sections[indexPath.Section].Remove (this);
			}
		}
	}
}


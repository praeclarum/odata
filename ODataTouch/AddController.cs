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
	public class AddController : DialogViewController
	{
		TextFieldElement _nameElement;
		TextFieldElement _urlElement;

		Action _doneAction;

		public AddController (Action doneAction) : base(UITableViewStyle.Grouped)
		{
			try {
				
				Title = "Add Service";
				
				_doneAction = doneAction;
				
				_nameElement = new TextFieldElement ("Name", "Display Name", 70);
				_nameElement.TextField.AutocapitalizationType = UITextAutocapitalizationType.Words;
				
				_urlElement = new TextFieldElement ("URL", "http://", 70);
				_urlElement.TextField.AutocapitalizationType = UITextAutocapitalizationType.None;
				_urlElement.TextField.KeyboardType = UIKeyboardType.Url;
				_urlElement.TextField.AutocorrectionType = UITextAutocorrectionType.No;
				
				var sec = new DialogSection ();
				sec.Add (_nameElement);
				sec.Add (_urlElement);
				
				Sections.Add (sec);
				
				NavigationItem.LeftBarButtonItem = new UIBarButtonItem ("Cancel", UIBarButtonItemStyle.Bordered, HandleCancelButton);
				NavigationItem.RightBarButtonItem = new UIBarButtonItem ("Done", UIBarButtonItemStyle.Done, HandleDoneButton);
				
			} catch (Exception error) {
				Log.Error (error);
			}
		}

		UIAlertView _noAlert;

		void HandleDoneButton (object sender, EventArgs e)
		{
			try {

				var service = new UserService {
					Name = _nameElement.Value.Trim (),
					ServiceRootUri = _urlElement.Value.Trim ()
				};

				if (service.Name.Length > 0 && service.ServiceRootUri.Length > 0) {

					using (var repo = new Repo ()) {
						repo.Add (service);
					}

					DismissModalViewControllerAnimated (true);

					_doneAction ();

				} else {

					_noAlert = new UIAlertView ("", "Please completely fill in the form to add the service.", null, "OK");
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
	}
}


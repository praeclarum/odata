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
using System.Collections.Generic;
using System.Drawing;
namespace OData.Touch
{
	public class DialogSection
	{
		public string Header { get; private set; }
		public string Footer { get; private set; }
		List<DialogElement> _elements = new List<DialogElement> ();

		public DialogSection () : this("", "")
		{
		}

		public DialogSection (string header) : this(header, "")
		{
		}

		public DialogSection (string header, string footer)
		{
			Header = header;
			Footer = footer;
		}

		public void Add (DialogElement element)
		{
			element.Section = this;
			_elements.Add (element);
		}

		public void Remove (DialogElement element)
		{
			element.Section = null;
			_elements.Remove (element);
		}

		public DialogElement NextElement (DialogElement element, Type elementType)
		{
			var start = _elements.IndexOf (element);
			for (var i = start + 1; i < _elements.Count; i++) {
				var e = _elements[i];
				if (e.GetType () == elementType) {
					return e;
				}
			}
			return null;
		}

		public DialogElement GetElement (int row)
		{
			return _elements[row];
		}

		public int NumElements {
			get { return _elements.Count; }
		}

		public void Clear ()
		{
			foreach (var e in _elements) {
				e.Section = null;
			}
			_elements.Clear ();
		}
	}

	public abstract class DialogElement
	{
		bool _needsMeasure;

		public NSString ReuseIdentifier { get; private set; }

		public UITableViewCellAccessory Accessory { get; protected set; }
		public UITableViewCellStyle CellStyle { get; protected set; }
		public bool CanEdit { get; protected set; }

		public DialogSection Section { get; set; }

		public delegate void SelectedEventHandler (DialogViewController sender);

		public event SelectedEventHandler Selected;

		public DialogElement ()
		{
			Accessory = UITableViewCellAccessory.None;
			CanEdit = false;
			_needsMeasure = true;
			ReuseIdentifier = new NSString (GetType ().Name);
			CellStyle = UITableViewCellStyle.Default;
		}

		public abstract void RefreshCell (UITableViewCell cell);

		public DialogElement NextElement (Type elementType)
		{
			if (Section == null)
				return null;
			else
				return Section.NextElement (this, elementType);
		}

		public UITableViewCell GetCell (UITableView table)
		{
			var c = table.DequeueReusableCell (ReuseIdentifier);
			c = new UITableViewCell (CellStyle, ReuseIdentifier);
			c.Accessory = Accessory;
			RefreshCell (c);
			return c;
		}

		protected virtual float GetHeight (float width)
		{
			return 44.0f;
		}

		float _height = 0;

		public void SetNeedsMeasure ()
		{
			_needsMeasure = true;
		}

		public float Height {
			get {
				if (_needsMeasure) {
					_height = GetHeight (300);
					_needsMeasure = false;
				}
				return _height;
			}
		}

		public virtual void OnSelected (DialogViewController sender, NSIndexPath indexPath)
		{
			if (Selected != null) {
				Selected (sender);
			}
		}

		public virtual void OnDelete (DialogViewController sender, NSIndexPath indexPath)
		{
		}
	}

	public class TextFieldElement : DialogElement
	{
		public string Caption { get; private set; }
		public float CaptionWidth { get; private set; }
		public UITextField TextField { get; private set; }

		public string Value {
			get { return TextField.Text ?? ""; }
			set { TextField.Text = value; }
		}

		public TextFieldElement (string caption, string placeholder, float captionWidth)
		{
			Caption = caption;
			CaptionWidth = captionWidth;
			TextField = new UITextField ();
			TextField.Placeholder = placeholder;
			TextField.ReturnKeyType = UIReturnKeyType.Next;
			TextField.Tag = 42;
			TextField.TextColor = UIColor.FromRGB(56/255.0f, 84/255.0f, 135/255.0f);

			TextField.ShouldReturn += delegate {
				try {

					var next = NextElement (typeof(TextFieldElement));

					if (next != null) {
						((TextFieldElement)next).TextField.BecomeFirstResponder ();
					} else {
						TextField.ResignFirstResponder ();
					}

					return true;

				} catch (Exception error) {
					Log.Error (error);
					return true;
				}
			};

		}
		public override void RefreshCell (UITableViewCell cell)
		{
			cell.SelectionStyle = UITableViewCellSelectionStyle.None;
			cell.TextLabel.Text = Caption;

			TextField.ReturnKeyType = NextElement (typeof(TextFieldElement)) != null ? UIReturnKeyType.Next : UIReturnKeyType.Default;

			var v = cell.ViewWithTag (42);
			if (v == null) {
				cell.ContentView.AddSubview (TextField);
			} else {
				if (v.Handle != TextField.Handle) {
					v.RemoveFromSuperview ();
					cell.ContentView.AddSubview (TextField);
				}
			}

			TextField.Frame = new RectangleF (CaptionWidth, 11, 300 - CaptionWidth, 22);
		}
	}

	public class TextViewElement : DialogElement
	{
		public string Caption { get; private set; }
		public UITextView TextView { get; private set; }
		public float TextViewHeight { get; private set; }

		public string Value {
			get { return TextView.Text ?? ""; }
			set { TextView.Text = value; }
		}

		public TextViewElement (string caption, float height)
		{
			Caption = caption;
			TextViewHeight = height;
			TextView = new UITextView();
			TextView.Tag = 42;
			TextView.TextColor = UIColor.FromRGB(56/255.0f, 84/255.0f, 135/255.0f);
		}
		protected override float GetHeight (float width)
		{
			return 22.0f + TextViewHeight;
		}
		public override void RefreshCell (UITableViewCell cell)
		{
			cell.SelectionStyle = UITableViewCellSelectionStyle.None;
			cell.TextLabel.Text = Caption;

			var v = cell.ViewWithTag (42);
			if (v == null) {
				cell.ContentView.AddSubview (TextView);
			} else {
				if (v.Handle != TextView.Handle) {
					v.RemoveFromSuperview ();
					cell.ContentView.AddSubview (TextView);
				}
			}

			TextView.Frame = new RectangleF (70, 11, 220, TextViewHeight);
		}
	}

	public class StaticElement : DialogElement
	{
		public string Caption { get; private set; }

		public StaticElement (string caption)
		{
			Caption = caption;
		}

		public override void RefreshCell (UITableViewCell cell)
		{
			cell.TextLabel.Text = Caption;
		}
	}

	public class ActionElement : StaticElement
	{
		public Action Action { get; set; }

		public ActionElement (string caption, Action action) : base(caption)
		{
			Action = action;
		}

		public override void OnSelected (DialogViewController sender, NSIndexPath indexPath)
		{
			sender.TableView.DeselectRow (indexPath, true);
			Action ();
			base.OnSelected (sender, indexPath);
		}
	}

	public class LoadingElement : StaticElement
	{
		UIActivityIndicatorView _activity;

		public LoadingElement () : base("Loading...")
		{
			_activity = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.Gray);
			var w = 22.0f;
			_activity.Frame = new System.Drawing.RectangleF (300 - w - 11, (44.0f - w) / 2, w, w);
			_activity.Tag = 42;
		}

		public void Start ()
		{
			_activity.StartAnimating ();
		}

		public void Stop ()
		{
			_activity.StopAnimating ();
		}

		public override void RefreshCell (UITableViewCell cell)
		{
			base.RefreshCell (cell);
			var v = cell.ViewWithTag (42);
			if (v == null) {
				cell.ContentView.AddSubview (_activity);
			}
		}
	}

	public class DialogViewController : UITableViewController
	{
		public List<DialogSection> Sections { get; private set; }

		Del _del;
		Data _data;

		public DialogViewController (UITableViewStyle s) : base(s)
		{
			try {
				Sections = new List<DialogSection> ();
				_del = new Del (this);
				_data = new Data (this);
				TableView.Delegate = _del;
				TableView.DataSource = _data;
			} catch (Exception error) {
				Log.Error (error);
			}
		}

		DialogElement GetElement (NSIndexPath path)
		{
			return Sections[path.Section].GetElement (path.Row);
		}

		class Del : UITableViewDelegate
		{
			DialogViewController _c;
			public Del (DialogViewController c)
			{
				_c = c;
			}
			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{
				try {
					_c.GetElement (indexPath).OnSelected (_c, indexPath);
				} catch (Exception error) {
					Log.Error (error);
				}
			}
			public override float GetHeightForRow (UITableView tableView, NSIndexPath indexPath)
			{
				return _c.GetElement (indexPath).Height;
			}
		}

		class Data : UITableViewDataSource
		{
			DialogViewController _c;
			public Data (DialogViewController c)
			{
				_c = c;
			}
			public override string TitleForHeader (UITableView tableView, int section)
			{
				try {
					return _c.Sections[section].Header;
				} catch (Exception error) {
					Log.Error (error);
					return "";
				}
			}
			public override string TitleForFooter (UITableView tableView, int section)
			{
				try {
					return _c.Sections[section].Footer;
				} catch (Exception error) {
					Log.Error (error);
					return "";
				}
			}
			public override int NumberOfSections (UITableView tableView)
			{
				try {
					return _c.Sections.Count;
				} catch (Exception error) {
					Log.Error (error);
					return 0;
				}
			}
			public override int RowsInSection (UITableView tableview, int section)
			{
				try {
					return _c.Sections[section].NumElements;
				} catch (Exception error) {
					Log.Error (error);
					return 0;
				}
			}
			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				try {
					var e = _c.GetElement (indexPath);
					return e.GetCell (tableView);
				} catch (Exception error) {
					Log.Error (error);
					return new UITableViewCell (UITableViewCellStyle.Default, "ERROR");
				}
			}
			public override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
			{
				try {
					
					if (editingStyle == UITableViewCellEditingStyle.Delete) {
						
						var e = _c.GetElement (indexPath);
						e.OnDelete (_c, indexPath);
						tableView.DeleteRows (new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Fade);
						
					}
					
				} catch (Exception error) {
					Log.Error (error);
				}
			}
			public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath)
			{
				try {
					return _c.GetElement (indexPath).CanEdit;
				} catch (Exception error) {
					Log.Error (error);
					return false;
				}
			}
		}
	}
}


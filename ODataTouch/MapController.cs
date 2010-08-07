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
using MonoTouch.MapKit;
using MonoTouch.UIKit;
using MonoTouch.CoreLocation;

namespace OData.Touch
{
	public class MapController : UIViewController
	{
		public Entity Entity { get; private set; }

		MKMapView _map;
		Pin _pin;

		public MapController (Entity entity)
		{
			try {
				Entity = entity;

				Title = Entity.EntityTypeName;

				_map = new MKMapView ();

				_pin = new Pin(Entity);

				_map.Frame = View.Bounds;
				_map.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

				_map.AddAnnotation(_pin);

				_map.Region = new MKCoordinateRegion (_pin.Coordinate, new MKCoordinateSpan (0.01, 0.01));

				View.AddSubview (_map);

				_map.SelectAnnotation(_pin, true);

			} catch (Exception error) {
				Log.Error (error);
			}
		}

		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			return true;
		}

		class Pin : MKAnnotation
		{
			public Entity Entity { get; private set; }
			public Pin (Entity entity)
			{
				try {
					Entity = entity;
				} catch (Exception ex) {
					Log.Error (ex);
				}
			}
			public override string Title {
				get {
					try {
						return Entity.EntityTypeName;
					}
					catch (Exception error) {
						Log.Error(error);
						return "";
					}
				}
			}
			public override CLLocationCoordinate2D Coordinate {
				get {
					try {
						return new CLLocationCoordinate2D (Entity.Latitude, Entity.Longitude);
					}
					catch (Exception error) {
						Log.Error(error);
						return new CLLocationCoordinate2D (0, 0);
					}
				}
				set {

				}
			}
		}
	}
}


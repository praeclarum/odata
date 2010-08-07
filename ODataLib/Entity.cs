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
namespace OData
{
	public class Entity
	{
		public string Id { get; set; }
		public string Title { get; set; }
		public string EntityTypeFullName { get; set; }
		public DateTime UpdatedTime { get; set; }
		public string Author { get; set; }

		public List<Link> Links { get; private set; }
		public List<EntityProperty> Properties { get; private set; }

		bool? _hasLocation;
		EntityProperty _latProp, _lonProp;
		public double Latitude { get; private set; }
		public double Longitude { get; private set; }
		public bool IsLocationProperty (EntityProperty p)
		{
			return (_latProp != null) && (_lonProp != null) && ((p == _latProp) || (p == _lonProp));
		}
		public bool HasLocation {
			get {
				if (!_hasLocation.HasValue) {
					foreach (var p in Properties) {
						if (_lonProp == null && p.Name.IndexOf ("ongitude") > 0) {
							_lonProp = p;
						} else if (_latProp == null && p.Name.IndexOf ("atitude") > 0) {
							_latProp = p;
						}
					}
					float lon = 0, lat = 0;
					_hasLocation = (_lonProp != null) && (_latProp != null) && float.TryParse (_lonProp.ValueText, out lon) && float.TryParse (_latProp.ValueText, out lat);
					Longitude = lon;
					Latitude = lat;
				}
				return _hasLocation.Value;
			}
		}

		public string EntityTypeName {
			get {
				var parts = EntityTypeFullName.Split ('.');
				return parts[parts.Length - 1];
			}
		}

		public Entity ()
		{
			Id = "";
			Title = "";
			EntityTypeFullName = "";
			UpdatedTime = DateTime.MinValue;
			Author = "";
			Links = new List<Link> ();
			Properties = new List<EntityProperty> ();
		}

		public class Link
		{
			public string Rel { get; set; }
			public string Name { get; set; }
			public string Href { get; set; }
			public string Type { get; set; }

			public bool IsSingleEntity {
				get { return Type.IndexOf ("type=entry") >= 0; }
			}
		}
	}

	public class EntityProperty
	{
		public string Name { get; set; }
		public string ValueType { get; set; }
		public string ValueText { get; set; }

		public EntityProperty ()
		{
			Name = "";
			ValueText = "";
			ValueType = "";
		}

		public bool IsDateTime { get { return ValueType == "Edm.DateTime"; } }

		string _displayText = null;

		public string DisplayText {
			get {
				if (_displayText == null) {
					if (ValueText.Length == 0) {
						_displayText = "";
					} else if (IsDateTime) {
						if (ValueText.Length > 0) {
							_displayText = DateTime.Parse (ValueText).ToLocalTime ().ToString ();
						}
					} else {
						_displayText = ValueText;
					}
				}
				
				return _displayText;
			}
		}

		public bool LooksLikeHtml {
			get { return (ValueText.IndexOf ("<a ") >= 0) || (ValueText.IndexOf ("<div") >= 0) || (ValueText.IndexOf ("<img") >= 0) || (ValueText.IndexOf ("<p") >= 0); }
		}

		public bool LooksLikeLink {
			get { return ValueText.StartsWith ("http"); }
		}
		
	}

	public class EntitySet
	{
		public static List<Entity> LoadFromAtom (string atom)
		{
			var xml = Xml.Parse (atom);
			var entries = xml.ElementsWithName ("entry");
			
			var r = new List<Entity> ();
			
			foreach (var a in entries) {
				
//				Console.WriteLine (a.OuterXml);
				
				try {
					var e = new Entity ();

					e.Id = a.ElementText ("id");
					e.Title = a.ElementText ("title");
					e.EntityTypeFullName = a.ElementsWithName ("category")[0].GetAttribute ("term");
					e.UpdatedTime = DateTime.Parse (a.ElementText ("updated"));

					foreach (var al in a.ElementsWithName ("link")) {
						var l = new Entity.Link ();
						l.Rel = al.GetAttribute ("rel");
						l.Name = al.GetAttribute ("title");
						l.Href = al.GetAttribute ("href");
						l.Type = al.GetAttribute ("type");
						e.Links.Add (l);
					}

					e.Links.Sort ((x, y) => x.Name.CompareTo (y.Name));
					
					foreach (var pn in a.ElementsWithName ("properties")[0].ChildNodes) {
						var pe = pn as System.Xml.XmlElement;
						if (pe == null)
							continue;
						
						var p = new EntityProperty ();
						p.Name = pe.LocalName;
						p.ValueText = pe.InnerText.Trim ();
						foreach (System.Xml.XmlAttribute attr in pe.Attributes) {
							if (attr.LocalName == "type") {
								p.ValueType = attr.InnerText;
							}
						}
						
						e.Properties.Add (p);
					}
					
					r.Add (e);
					
				} catch (Exception ex) {
					Console.WriteLine (ex);
				}
			}
			
			return r;
		}
	}
}


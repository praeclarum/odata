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
using System.Xml;


namespace OData
{
	public class MetadataDocument
	{
		public string BaseUrl { get; set; }
		public string DocumentBody { get; set; }

		public MetadataDocument ()
		{
			BaseUrl = "";
			DocumentBody = "";
		}

		Metadata _metadata;
		public Metadata Metadata {
			get {
				if (_metadata == null) {
					_metadata = new Metadata ();
					_metadata.Load (DocumentBody);
				}
				return _metadata;
			}
		}
	}

	public class Metadata
	{
		public List<EntityTypeInfo> EntityTypes { get; private set; }
		public List<EntitySetInfo> EntitySets { get; private set; }

		public Metadata ()
		{
			EntityTypes = new List<EntityTypeInfo> ();
			EntitySets = new List<EntitySetInfo> ();
		}

		public void Load (string doc)
		{
			try {
				var xml = Xml.Parse (doc);

				foreach (var schema in xml.ElementsWithName ("Schema")) {

					var ns = schema.GetAttribute ("Namespace");

					foreach (var et in schema.ElementsWithName ("EntityType")) {
						LoadEntityType (et, ns);
					}
					foreach (var et in schema.ElementsWithName ("ComplexType")) {
						LoadEntityType (et, ns);
					}
					foreach (var et in schema.ElementsWithName ("EntitySet")) {
						LoadEntitySet (et, ns);
					}
				}
			} catch (Exception error) {
				Console.WriteLine (error);
			}
		}

		void LoadEntitySet (XmlElement et, string ns)
		{
			var e = new EntitySetInfo {
				Name = et.GetAttribute ("Name").Trim (),
				Namespace = ns,
				EntityTypeFullName = et.GetAttribute ("EntityType").Trim ()
			};

			EntitySets.Add (e);
		}

		void LoadEntityType (XmlElement et, string ns)
		{
			var e = new EntityTypeInfo {
				Name = et.GetAttribute ("Name").Trim (),
				Namespace = ns
			};
			
			foreach (var p in et.ElementsWithName ("Property")) {
				
				var prop = new EntityPropertyInfo {
					Name = p.GetAttribute ("Name").Trim (),
					TypeFullName = p.GetAttribute ("Type").Trim (),
					IsKey = false
				};
				e.Properties.Add (prop);
				
			}
			
			foreach (var key in et.ElementsWithName ("Key")) {
				foreach (var p in key.ElementsWithName ("PropertyRef")) {
					var name = p.GetAttribute ("Name").Trim ();
					
					foreach (var prop in e.Properties) {
						if (prop.Name == name) {
							prop.IsKey = true;
						}
					}
				}
			}
			
			EntityTypes.Add (e);
		}

		public EntityTypeInfo FindEntityType (string fullName)
		{
			foreach (var t in EntityTypes) {
				if (t.FullName == fullName) {
					return t;
				}
			}
			return null;
		}

		public EntityTypeInfo FindEntityTypeForEntitySet (string entitySetName)
		{
			foreach (var s in EntitySets) {
				if (s.Name == entitySetName) {
					foreach (var t in EntityTypes) {
						if (t.FullName == s.EntityTypeFullName) {
							return t;
						}
					}
				}
			}
			return null;
		}
	}

	public class EntitySetInfo
	{
		public string Name { get; set; }
		public string Namespace { get; set; }
		public string EntityTypeFullName { get; set; }

		public EntitySetInfo ()
		{
			Name = "";
			EntityTypeFullName = "";
			Namespace = "";
		}

		public override string ToString ()
		{
			return string.Format ("[EntitySetInfo: Name={0}, Namespace={1}, EntityTypeFullName={2}]", Name, Namespace, EntityTypeFullName);
		}
	}

	public class EntityTypeInfo
	{
		public string Name { get; set; }
		public string Namespace { get; set; }
		public List<EntityPropertyInfo> Properties { get; private set; }

		public string FullName { get { return Namespace + "." + Name; } }

		public EntityTypeInfo ()
		{
			Name = "";
			Properties = new List<EntityPropertyInfo> ();
		}

		public static string GetTypeShortName(string name) {
			var dot = name.LastIndexOf('.');
			if (dot >= 0 && dot < name.Length-1) {
				return name.Substring(dot+1);
			}
			else {
				return name;
			}
		}

		public override string ToString ()
		{
			return string.Format ("[EntityTypeInfo: Name={0}, Namespace={1}, Properties.Count={2}]", Name, Namespace, Properties.Count);
		}
	}

	public class EntityPropertyInfo
	{
		public bool IsKey { get; set; }
		public string Name { get; set; }
		public string TypeFullName { get; set; }

		public bool IsBasicType { get { return TypeFullName.StartsWith("Edm."); } }

		public EntityPropertyInfo ()
		{
			IsKey = false;
			Name = "";
			TypeFullName = "";
		}
	}
}


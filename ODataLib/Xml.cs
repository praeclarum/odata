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
using System.IO;
using System.Collections.Generic;
using System.Xml;

namespace OData
{
	public static class Xml
	{
		public static bool IsXml (string xml)
		{
			var ixml = xml.IndexOf ("<?xml");
			return ixml >= 0 && ixml < 140;
		}

		public static XmlElement Parse (string xml)
		{
			var doc = new XmlDocument ();
			if (xml.Length > 0) {
				doc.LoadXml (xml);
			}
			return doc.DocumentElement;
		}

		public static XmlElement ElementWithId (this XmlDocument doc, string id)
		{
			return ElementWithId (doc.DocumentElement, id);
		}
		public static XmlElement ElementWithId (this XmlElement e, string id)
		{
			var eid = e.GetAttribute ("id");
			if (eid == id) {
				return e;
			} else {
				foreach (var ch in e.ChildNodes) {
					var ce = ch as XmlElement;
					if (ce != null) {
						var ee = ElementWithId (ce, id);
						if (ee != null) {
							return ee;
						}
					}
				}
			}
			return null;
		}

		public static string ElementText (this XmlElement e, string name)
		{
			var es = e.ElementsWithName (name);
			if (es.Count == 0)
				return "";
			else
				return es[es.Count - 1].InnerText;
		}

		public static List<XmlElement> ElementsWithName (this XmlDocument doc, string name)
		{
			return doc.DocumentElement.ElementsWithName (name);
		}

		public static List<XmlElement> ElementsWithName (this XmlElement e, string name)
		{
			var r = new List<XmlElement> ();
			ElementsWithName (e, name, r);
			return r;
		}

		static void ElementsWithName (XmlElement e, string name, List<XmlElement> elems)
		{
			if (e == null) {
				return;
			}
			if (e.LocalName == name) {
				elems.Add (e);
			}
			foreach (var ch in e.ChildNodes) {
				var ce = ch as XmlElement;
				if (ce != null) {
					ElementsWithName (ce, name, elems);
				}
			}
		}

		public static List<XmlElement> ElementsWithClass (this XmlElement e, string className)
		{
			var r = new List<XmlElement> ();
			ElementsWithClass (e, className, r);
			return r;
		}

		static void ElementsWithClass (XmlElement e, string className, List<XmlElement> elems)
		{
			var cls = e.GetAttribute ("class");
			var icls = cls.IndexOf (className);
			if (icls >= 0) {
				var frontEdge = (icls == 0) || (char.IsWhiteSpace (cls[icls - 1]));
				var backEdge = (icls + className.Length == cls.Length) || (char.IsWhiteSpace (cls[icls + className.Length]));
				if (frontEdge && backEdge) {
					elems.Add (e);
				}
			}
			foreach (var ch in e.ChildNodes) {
				var ce = ch as XmlElement;
				if (ce != null) {
					ElementsWithClass (ce, className, elems);
				}
			}
		}
	}
}


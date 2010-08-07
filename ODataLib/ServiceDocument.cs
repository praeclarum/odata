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
	public class ServiceDocument
	{
		public string ServiceRootUri { get; private set; }

		public List<Feed> Feeds { get; private set; }
		public List<MetadataDocument> Metadata { get; private set; }

		public ServiceDocument (string serviceRootUri)
		{
			ServiceRootUri = serviceRootUri;
			
			Metadata = new List<MetadataDocument> ();
			Feeds = GetFeeds ();
			Feeds.Sort ((x, y) => x.Category.CompareTo (y.Category));
		}

		List<Feed> GetFeeds ()
		{
			var r = new List<Feed> ();
			
			var data = Http.Get (ServiceRootUri);
			
			if (Xml.IsXml (data)) {
				GetFeedsFromAtom ("Feeds", data, r);
			} else {
				throw new NotSupportedException ("Only Atom Service Documents are supported");
			}
			
			return r;
		}

		void GetFeedsFromAtom (string category, string atom, List<Feed> feeds)
		{
			var doc = Xml.Parse (atom);
			
			var baseUrl = doc.GetAttribute ("xml:base").Trim ();
			
			var newFeeds = new List<Feed> ();
			
			foreach (var coll in doc.ElementsWithName ("collection")) {
				
				var href = coll.GetAttribute ("href").Trim ();
				var title = coll.ElementText ("title").Trim ();
				
				var feed = new Feed {
					Category = category,
					BaseUrl = baseUrl,
					Href = href,
					Name = title
				};
				
				newFeeds.Add (feed);
			}
			
			foreach (var f in newFeeds) {
				
				try {
					var subData = Http.Get (f.Url + "?$top=1");
					
					var subXml = Xml.Parse (subData);
					
					if (subXml.LocalName == "service") {
						
						GetFeedsFromAtom (f.Name, Http.Get (f.Url), feeds);
						
					} else {
						
						GetMetadata (f);
						
						feeds.Add (f);
						
					}
					
				} catch (Exception) {
				}
				
				
			}
		}

		void GetMetadata (Feed forFeed)
		{
			foreach (var m in Metadata) {
				if (m.BaseUrl == forFeed.BaseUrl) {
					return;
				}
			}

			var url = forFeed.GetUrl ("$metadata");

			var doc = new MetadataDocument { BaseUrl = forFeed.BaseUrl };

			doc.DocumentBody = Http.Get (url);
			
			Metadata.Add (doc);
		}
	}
}


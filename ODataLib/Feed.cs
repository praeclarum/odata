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

namespace OData
{
	public class Feed
	{
		public string Name { get; set; }
		public string Href { get; set; }
		public string BaseUrl { get; set; }
		public string Category { get; set; }

		public Feed ()
		{
			Name = "";
			Href = "";
			BaseUrl = "";
			Category = "";
		}

		public string Url {
			get { return CombineUrl (BaseUrl, Href); }
		}

		public string GetUrl (string href)
		{
			return CombineUrl (BaseUrl, href);
		}

		static string CombineUrl (string url, string href)
		{
			var u = url;
			if (!u.EndsWith ("/")) {
				u += "/";
			}
			u += href;
			return u;
		}

		public override string ToString ()
		{
			return string.Format ("[Feed: Name={0}, Href={1}, BaseUrl={2}, Category={3}]", Name, Href, BaseUrl, Category);
		}
	}
}


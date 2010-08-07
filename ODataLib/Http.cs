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
using System.Net;
using System.Text;
using System.Collections.Generic;

namespace OData
{
	public class Http
	{
		public static class UserAgents
		{
			public const string Macintosh1063 = "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10_6_3; en-us) AppleWebKit/531.22.7 (KHTML, like Gecko) Version/4.0.5 Safari/531.22.7";
			public const string IPhone40 = "Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_0 like Mac OS X; en-us) AppleWebKit/532.9 (KHTML, like Gecko) Version/4.0.5 Mobile/8A293 Safari/6531.22.7";
		}

		const string DefaultAcceptHeader = "application/atom+xml,application/xml,application/xhtml+xml,text/html;q=0.9,text/plain;q=0.8,image/png,*/*;q=0.5";

		public static string MakeQueryString(Dictionary<string,object> query) {
			var sb = new StringBuilder();
			var head = "";

			foreach (var kv in query) {
				sb.Append(head);
				sb.Append(Uri.EscapeDataString(kv.Key));
				sb.Append("=");
				sb.Append(Uri.EscapeDataString(kv.Value.ToString()));
				head = "&";
			}

			return sb.ToString();
		}

		public static string Get (string url)
		{
//			Console.WriteLine ("GET " + url);
			var req = (HttpWebRequest)WebRequest.Create (url);
			req.UserAgent = UserAgents.IPhone40;
			req.Accept = DefaultAcceptHeader;
			return ReadResponse (req);
		}

		public static string Post (string txt, string url, string referer)
		{
			var req = (HttpWebRequest)WebRequest.Create (url);
			req.Method = "POST";
			req.UserAgent = UserAgents.IPhone40;
			req.Accept = DefaultAcceptHeader;
			if (!string.IsNullOrEmpty (referer)) {
				req.Referer = referer;
			}
			var d = Encoding.UTF8.GetBytes (txt);
			req.ContentLength = d.Length;
			req.GetRequestStream ().Write (d, 0, d.Length);
			return ReadResponse (req);
		}

		static string ReadResponse (HttpWebRequest req)
		{
			using (var resp = (HttpWebResponse)req.GetResponse ()) {
				var ct = resp.Headers[HttpRequestHeader.ContentType];
				var enc = GetEncoding (ct);

				using (var r = new System.IO.StreamReader (resp.GetResponseStream (), enc)) {
					return r.ReadToEnd ();
				}
			}
		}

		public static Encoding GetEncoding (string contentTypeHeader)
		{
			if (contentTypeHeader == null) {
				return Encoding.UTF8;
			}

//			Console.WriteLine (contentTypeHeader);
			var c = contentTypeHeader.ToLowerInvariant ();
			if (c.IndexOf ("utf-8") >= 0 || c.IndexOf ("utf8") >= 0) {
				return Encoding.UTF8;
			} else {
				return Encoding.UTF8;
//				throw new NotSupportedException ("Unknown content type: " + contentTypeHeader);
			}
		}
	}

	public class Html {
		public static string Encode(string text) {
			var t = text;
			t = t.Replace("&", "&amp;");
			t = text.Replace("<", "&lt;");
			t = t.Replace(">", "&gt;");
			t = t.Replace("\n", "<p>");
			return t;
		}
	}
}


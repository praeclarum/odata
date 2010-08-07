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
using System.Linq;
using SQLite;

namespace OData.Touch
{
	public class Repo : IDisposable
	{
		SQLiteConnection _db;

		static string _path;

		static Repo ()
		{
			_path = System.IO.Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments), "OData.sqlite");
			Console.WriteLine (_path);
		}

		public Repo ()
		{
			_db = new SQLiteConnection (_path);
//			_db.Trace = true;
		}

		public void Dispose ()
		{
			_db.Close ();
		}

		public void Initialize ()
		{
			_db.CreateTable<UserService> ();
			_db.CreateTable<UserQuery> ();
			_db.CreateTable<UserFeed> ();
			_db.CreateTable<UserMetadataDocument> ();
		}

		public UserFeed GetFeed (int feedId)
		{
			var q = from f in _db.Table<UserFeed> ()
				where f.Id == feedId
				select f;
			return q.FirstOrDefault ();
		}

		public UserService GetService (int serviceId)
		{
			var q = from f in _db.Table<UserService> ()
				where f.Id == serviceId
				select f;
			return q.FirstOrDefault ();
		}

		public void Add (UserService service)
		{
			_db.Insert (service);
		}

		public void Save (UserQuery query)
		{
			_db.Update (query);
		}

		public void Add (UserQuery query)
		{
			_db.Insert (query);
		}

		public void DeleteQuery (int queryId)
		{
			_db.Execute ("delete from UserQuery where Id = ?", queryId);
		}

		public void DeleteService (UserService service)
		{
			_db.RunInTransaction (delegate {
				_db.Execute ("delete from UserFeed where ServiceId = ?", service.Id);
				_db.Execute ("delete from UserQuery where ServiceId = ?", service.Id);
				_db.Execute ("delete from UserService where Id = ?", service.Id);
			});
		}

		public List<UserQuery> GetQueries (UserService service)
		{
			var q = from qu in _db.Table<UserQuery> ()
				where qu.ServiceId == service.Id
				orderby qu.Name
				select qu;
			return q.ToList ();
		}

		public List<UserFeed> GetFeeds (UserService service)
		{
			var q = from f in _db.Table<UserFeed> ()
				where f.ServiceId == service.Id
				orderby f.Name
				select f;
			return q.ToList ();
		}

		public UserMetadataDocument FindMetadataDocument (string baseUrl, int serviceId)
		{
			var q = from f in _db.Table<UserMetadataDocument> ()
				where f.ServiceId == serviceId && f.BaseUrl == baseUrl
				select f;
			return q.FirstOrDefault ();
		}

		public void UpdateMetadataDocuments (List<UserMetadataDocument> metaDocs, UserService service)
		{
			_db.RunInTransaction (delegate {

				foreach (var f in metaDocs) {

					f.ServiceId = service.Id;

					var o = FindMetadataDocument (f.BaseUrl, service.Id);

					if (o != null) {
						f.Id = o.Id;
						_db.Update (f);
					} else {
						_db.Insert (f);
					}

				}

			});
		}

		UserFeed FindFeed (string name, int serviceId)
		{
			var q = from f in _db.Table<UserFeed> ()
				where f.ServiceId == serviceId && f.Name == name
				select f;
			return q.FirstOrDefault ();
		}

		public void UpdateFeeds (List<UserFeed> feeds, UserService service)
		{
			_db.RunInTransaction (delegate {

				foreach (var f in feeds) {

					f.ServiceId = service.Id;

					var o = FindFeed (f.Name, service.Id);

					if (o != null) {
						f.Id = o.Id;
						_db.Update (f);
					} else {
						_db.Insert (f);
					}

				}

			});
		}

		public void Save (UserService service)
		{
			_db.Update (service);
		}

		public List<UserService> GetActiveServices ()
		{
			var q = from s in _db.Table<UserService> ()
				orderby s.Name
				select s;
			return q.ToList ();
		}

		public void InsertDefaultServices ()
		{
			var r = new List<UserService> ();
			r.Add (new UserService {
				Name = "Netflix",
				ServiceRootUri = "http://odata.netflix.com/"
			});
			r.Add (new UserService {
				Name = "Devexpress Channel",
				ServiceRootUri = "http://media.devexpress.com/channel.svc"
			});
			r.Add (new UserService {
				Name = "vanGuide",
				ServiceRootUri = "http://vancouverdataservice.cloudapp.net/v1/"
			});
			r.Add (new UserService {
				Name = "Open Government Data Initiative",
				ServiceRootUri = "http://ogdi.cloudapp.net/v1/"
			});
			r.Add (new UserService {
				Name = "Nerd Dinner",
				ServiceRootUri = "http://www.nerddinner.com/Services/OData.svc/"
			});
			r.Add (new UserService {
				Name = "Northwind",
				ServiceRootUri = "http://services.odata.org/Northwind/Northwind.svc/"
			});
			r.Add (new UserService {
				Name = "Stack Overflow",
				ServiceRootUri = "http://odata.stackexchange.com/stackoverflow/atom"
			});
			r.Add (new UserService {
				Name = "Super User",
				ServiceRootUri = "http://odata.stackexchange.com/superuser/atom"
			});
			r.Add (new UserService {
				Name = "Server Fault",
				ServiceRootUri = "http://odata.stackexchange.com/serverfault/atom"
			});
			r.Add (new UserService {
				Name = "Meta Stack Overflow",
				ServiceRootUri = "http://odata.stackexchange.com/meta/atom"
			});
			_db.InsertAll (r);
		}

	}
}


/* This file is part of the db4o object database http://www.db4o.com

Copyright (C) 2004 - 2010  Versant Corporation http://www.versant.com

db4o is free software; you can redistribute it and/or modify it under
the terms of version 3 of the GNU General Public License as published
by the Free Software Foundation.

db4o is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or
FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
for more details.

You should have received a copy of the GNU General Public License along
with this program.  If not, see http://www.gnu.org/licenses/. */
using System;
using System.IO;
using Db4oUnit.Extensions;
using Db4oUnit.Extensions.Fixtures;
using Db4objects.Db4o;
using Db4objects.Db4o.Config;
using Db4objects.Db4o.IO;

namespace Db4oUnit.Extensions.Fixtures
{
	public class Db4oInMemory : AbstractSoloDb4oFixture
	{
		private static readonly string DbUri = "test_db";

		public Db4oInMemory() : base()
		{
		}

		public Db4oInMemory(IFixtureConfiguration fc) : this()
		{
			FixtureConfiguration(fc);
		}

		public override bool Accept(Type clazz)
		{
			if (!base.Accept(clazz))
			{
				return false;
			}
			if (typeof(IOptOutInMemory).IsAssignableFrom(clazz))
			{
				return false;
			}
			return true;
		}

		private readonly PagingMemoryStorage _storage = new PagingMemoryStorage(63);

		protected override IObjectContainer CreateDatabase(IConfiguration config)
		{
			return Db4oFactory.OpenFile(config, DbUri);
		}

		protected override IConfiguration NewConfiguration()
		{
			IConfiguration config = base.NewConfiguration();
			config.Storage = _storage;
			return config;
		}

		protected override void DoClean()
		{
			try
			{
				_storage.Delete(DbUri);
			}
			catch (IOException exc)
			{
				Sharpen.Runtime.PrintStackTrace(exc);
			}
		}

		public override string Label()
		{
			return BuildLabel("IN-MEMORY");
		}

		/// <exception cref="System.Exception"></exception>
		public override void Defragment()
		{
			Defragment(DbUri);
		}
	}
}

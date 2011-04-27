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
using Db4oUnit;
using Db4oUnit.Extensions;
using Db4oUnit.Extensions.Fixtures;
using Db4objects.Db4o;
using Db4objects.Db4o.Config;
using Db4objects.Db4o.Ext;
using Db4objects.Db4o.Internal;
using Db4objects.Db4o.Internal.Threading;

namespace Db4oUnit.Extensions.Fixtures
{
	public abstract class AbstractSoloDb4oFixture : AbstractDb4oFixture
	{
		private IExtObjectContainer _db;

		protected AbstractSoloDb4oFixture()
		{
		}

		public sealed override void Open(IDb4oTestCase testInstance)
		{
			Assert.IsNull(_db);
			IConfiguration config = CloneConfiguration();
			ApplyFixtureConfiguration(testInstance, config);
			_db = CreateDatabase(config).Ext();
			ListenToUncaughtExceptions(ThreadPool());
			PostOpen(testInstance);
		}

		private IThreadPool4 ThreadPool()
		{
			return ThreadPoolFor(_db);
		}

		/// <exception cref="System.Exception"></exception>
		public override void Close()
		{
			try
			{
				PreClose();
			}
			finally
			{
				if (null != _db)
				{
					Assert.IsTrue(_db.Close());
					try
					{
						ThreadPool().Join(3000);
					}
					finally
					{
						_db = null;
					}
				}
			}
		}

		public override bool Accept(Type clazz)
		{
			return !typeof(IOptOutSolo).IsAssignableFrom(clazz);
		}

		public override IExtObjectContainer Db()
		{
			return _db;
		}

		public override LocalObjectContainer FileSession()
		{
			return (LocalObjectContainer)_db;
		}

		public override void ConfigureAtRuntime(IRuntimeConfigureAction action)
		{
			action.Apply(Config());
		}

		protected virtual void PreClose()
		{
		}

		protected virtual void PostOpen(IDb4oTestCase testInstance)
		{
		}

		protected abstract IObjectContainer CreateDatabase(IConfiguration config);
	}
}

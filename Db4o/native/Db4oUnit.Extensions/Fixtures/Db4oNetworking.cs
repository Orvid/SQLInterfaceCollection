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
#if SILVERLIGHT

using System;
using System.Collections;
using Db4objects.Db4o.Config;
using Db4objects.Db4o.Ext;
using Db4objects.Db4o.Internal;
using Db4oUnit.Fixtures;

namespace Db4oUnit.Extensions.Fixtures
{
	public class Db4oNetworking : IMultiSessionFixture
	{
		public Db4oNetworking()
		{
			throw new NotImplementedException();
		}

		public Db4oNetworking(string label)
		{
			throw new NotImplementedException();
		}

		string ILabeled.Label()
		{
			throw new NotImplementedException();
		}

		void IDb4oFixture.Open(IDb4oTestCase testInstance)
		{
			throw new NotImplementedException();
		}

		void IDb4oFixture.Close()
		{
			throw new NotImplementedException();
		}

		void IDb4oFixture.Reopen(IDb4oTestCase testInstance)
		{
			throw new NotImplementedException();
		}

		void IDb4oFixture.Clean()
		{
			throw new NotImplementedException();
		}

		LocalObjectContainer IDb4oFixture.FileSession()
		{
			throw new NotImplementedException();
		}

		IExtObjectContainer IDb4oFixture.Db()
		{
			throw new NotImplementedException();
		}

		IConfiguration IDb4oFixture.Config()
		{
			throw new NotImplementedException();
		}

		bool IDb4oFixture.Accept(Type clazz)
		{
			throw new NotImplementedException();
		}

		void IDb4oFixture.Defragment()
		{
			throw new NotImplementedException();
		}

		void IDb4oFixture.ConfigureAtRuntime(IRuntimeConfigureAction action)
		{
			throw new NotImplementedException();
		}

		void IDb4oFixture.FixtureConfiguration(IFixtureConfiguration configuration)
		{
			throw new NotImplementedException();
		}

		void IDb4oFixture.ResetConfig()
		{
			throw new NotImplementedException();
		}

		IList IDb4oFixture.UncaughtExceptions()
		{
			throw new NotImplementedException();
		}

		IExtObjectContainer IMultiSessionFixture.OpenNewSession(IDb4oTestCase testInstance)
		{
			throw new NotImplementedException();
		}
	}
}

#endif
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
using Db4oUnit.Fixtures;
using Db4oUnit.Tests.Fixtures;

namespace Db4oUnit.Tests.Fixtures
{
	public class Set4TestSuite : FixtureBasedTestSuite
	{
		public static void Main(string[] args)
		{
			new ConsoleTestRunner(typeof(Set4TestSuite)).Run();
		}

		public override IFixtureProvider[] FixtureProviders()
		{
			return new IFixtureProvider[] { new SubjectFixtureProvider(new IDeferred4[] { new 
				_IDeferred4_17(), new _IDeferred4_21() }), new MultiValueFixtureProvider(new object
				[][] { new object[] {  }, new object[] { "foo", "bar", "baz" }, new object[] { "foo"
				 }, new object[] { 42, -1 } }) };
		}

		private sealed class _IDeferred4_17 : IDeferred4
		{
			public _IDeferred4_17()
			{
			}

			public object Value()
			{
				return new CollectionSet4();
			}
		}

		private sealed class _IDeferred4_21 : IDeferred4
		{
			public _IDeferred4_21()
			{
			}

			public object Value()
			{
				return new HashtableSet4();
			}
		}

		public override Type[] TestUnits()
		{
			return new Type[] { typeof(Set4TestUnit) };
		}
		//			Iterable4TestUnit.class,
	}
}

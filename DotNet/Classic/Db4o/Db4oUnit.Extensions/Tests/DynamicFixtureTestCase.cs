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
using System.Collections;
using Db4oUnit;
using Db4oUnit.Extensions;
using Db4oUnit.Extensions.Fixtures;
using Db4oUnit.Extensions.Tests;
using Db4oUnit.Fixtures;

namespace Db4oUnit.Extensions.Tests
{
	public class DynamicFixtureTestCase : ITestSuiteBuilder
	{
		public virtual IEnumerator GetEnumerator()
		{
			// The test case simply runs FooTestSuite
			// with a Db4oInMemory fixture to ensure the 
			// the db4o fixture can be successfully propagated
			// to FooTestUnit#test.
			return new Db4oTestSuiteBuilder(new Db4oInMemory(), typeof(DynamicFixtureTestCase.FooTestSuite
				)).GetEnumerator();
		}

		/// <summary>One of the possibly many test units.</summary>
		/// <remarks>One of the possibly many test units.</remarks>
		public class FooTestUnit : AbstractDb4oTestCase
		{
			private readonly object[] values = MultiValueFixtureProvider.Value();

			public virtual void Test()
			{
				Assert.IsNotNull(Db());
				Assert.IsNotNull(values);
			}
		}

		/// <summary>The test suite which binds together fixture providers and test units.</summary>
		/// <remarks>The test suite which binds together fixture providers and test units.</remarks>
		public class FooTestSuite : FixtureTestSuiteDescription
		{
			public FooTestSuite()
			{
				{
					FixtureProviders(new IFixtureProvider[] { new MultiValueFixtureProvider(new object
						[][] { new object[] { "foo", "bar" }, new object[] { 1, 42 } }) });
					TestUnits(new Type[] { typeof(DynamicFixtureTestCase.FooTestUnit) });
				}
			}
		}
	}
}

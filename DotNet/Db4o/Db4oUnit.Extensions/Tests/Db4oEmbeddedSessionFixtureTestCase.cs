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
using Db4oUnit;
using Db4oUnit.Extensions;
using Db4oUnit.Extensions.Fixtures;
using Db4oUnit.Extensions.Tests;

namespace Db4oUnit.Extensions.Tests
{
	public class Db4oEmbeddedSessionFixtureTestCase : ITestCase
	{
		internal readonly Db4oEmbeddedSessionFixture subject = new Db4oEmbeddedSessionFixture
			();

		public virtual void TestDoesNotAcceptRegularTest()
		{
			Assert.IsFalse(subject.Accept(typeof(Db4oEmbeddedSessionFixtureTestCase.RegularTest
				)));
		}

		public virtual void TestAcceptsDb4oTest()
		{
			Assert.IsTrue(subject.Accept(typeof(Db4oEmbeddedSessionFixtureTestCase.Db4oTest))
				);
		}

		public virtual void TestDoesNotAcceptOptOutCS()
		{
			Assert.IsFalse(subject.Accept(typeof(Db4oEmbeddedSessionFixtureTestCase.OptOutTest
				)));
		}

		public virtual void TestDoesNotAcceptOptOutAllButNetworkingCS()
		{
			Assert.IsFalse(subject.Accept(typeof(Db4oEmbeddedSessionFixtureTestCase.OptOutAllButNetworkingCSTest
				)));
		}

		public virtual void TestAcceptsOptOutNetworking()
		{
			Assert.IsTrue(subject.Accept(typeof(Db4oEmbeddedSessionFixtureTestCase.OptOutNetworkingTest
				)));
		}

		internal class RegularTest : ITestCase
		{
		}

		internal class Db4oTest : IDb4oTestCase
		{
		}

		internal class OptOutTest : IOptOutMultiSession
		{
		}

		internal class OptOutNetworkingTest : IOptOutNetworkingCS
		{
		}

		internal class OptOutAllButNetworkingCSTest : IOptOutAllButNetworkingCS
		{
		}
	}
}

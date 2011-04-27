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
using Db4oUnit.Tests;

namespace Db4oUnit.Tests
{
	public class ClassLevelFixtureTestTestCase : ITestCase
	{
		private static int _count;

		public virtual void Test()
		{
			_count = 0;
			TestResult result = new TestResult();
			new TestRunner(new ReflectionTestSuiteBuilder(typeof(ClassLevelFixtureTestTestCase.SimpleTestSuite
				))).Run(result);
			Assert.AreEqual(3, _count);
			Assert.AreEqual(1, result.TestCount);
			Assert.AreEqual(0, result.Failures.Count);
		}

		public class SimpleTestSuite : IClassLevelFixtureTest
		{
			public static void ClassSetUp()
			{
				ClassLevelFixtureTestTestCase._count++;
			}

			public static void ClassTearDown()
			{
				ClassLevelFixtureTestTestCase._count++;
			}

			public virtual void Test()
			{
				ClassLevelFixtureTestTestCase._count++;
			}
		}
	}
}

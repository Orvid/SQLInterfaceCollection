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
using Db4oUnit.Tests;
using Db4objects.Db4o.Foundation;

namespace Db4oUnit.Tests
{
	public class FrameworkTestCase : ITestCase
	{
		public static readonly Exception Exception = new Exception();

		public virtual void TestRunsGreen()
		{
			TestResult result = new TestResult();
			new TestRunner(Iterators.SingletonIterable(new RunsGreen())).Run(result);
			Assert.IsTrue(result.Failures.Count == 0, "not green");
		}

		public virtual void TestRunsRed()
		{
			TestResult result = new TestResult();
			new TestRunner(Iterators.SingletonIterable(new RunsRed(Exception))).Run(result);
			Assert.IsTrue(result.Failures.Count == 1, "not red");
		}

		public static void RunTestAndExpect(ITest test, int expFailures)
		{
			RunTestAndExpect(test, expFailures, true);
		}

		public static void RunTestAndExpect(ITest test, int expFailures, bool checkException
			)
		{
			RunTestAndExpect(Iterators.SingletonIterable(test), expFailures, checkException);
		}

		public static void RunTestAndExpect(IEnumerable tests, int expFailures, bool checkException
			)
		{
			TestResult result = new TestResult();
			new TestRunner(tests).Run(result);
			if (expFailures != result.Failures.Count)
			{
				Assert.Fail(result.Failures.ToString());
			}
			if (checkException)
			{
				for (IEnumerator iter = result.Failures.GetEnumerator(); iter.MoveNext(); )
				{
					TestFailure failure = (TestFailure)iter.Current;
					Assert.AreEqual(Exception, failure.Reason);
				}
			}
		}
	}
}

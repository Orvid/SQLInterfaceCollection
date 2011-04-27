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
using System.Collections;
using Db4oUnit;
using Db4oUnit.Tests;
using Db4objects.Db4o.Foundation;
using Sharpen.Lang;

namespace Db4oUnit.Tests
{
	public class TestLifeCycleTestCase : ITestCase
	{
		public virtual void TestLifeCycle()
		{
			ByRef tearDownCalled = ByRef.NewInstance(false);
			RunsLifeCycle._tearDownCalled.With(tearDownCalled, new _IRunnable_11());
			Assert.IsTrue((((bool)tearDownCalled.value)));
		}

		private sealed class _IRunnable_11 : IRunnable
		{
			public _IRunnable_11()
			{
			}

			public void Run()
			{
				IEnumerator tests = new ReflectionTestSuiteBuilder(typeof(RunsLifeCycle)).GetEnumerator
					();
				ITest test = (ITest)Iterators.Next(tests);
				FrameworkTestCase.RunTestAndExpect(test, 1);
			}
		}
	}
}

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
	public class ExceptionInTearDownDoesNotShadowTestCase : ITestCase
	{
		public static readonly string InTestMessage = "in test";

		public static readonly string InTeardownMessage = "in teardown";

		public class RunsWithExceptions : ITestLifeCycle
		{
			public virtual void SetUp()
			{
			}

			public virtual void TearDown()
			{
				throw new Exception(InTeardownMessage);
			}

			/// <exception cref="System.Exception"></exception>
			public virtual void TestMethod()
			{
				throw FrameworkTestCase.Exception;
			}
		}

		public class RunsWithExceptionInTearDown : ITestLifeCycle
		{
			public virtual void SetUp()
			{
			}

			public virtual void TearDown()
			{
				throw FrameworkTestCase.Exception;
			}

			/// <exception cref="System.Exception"></exception>
			public virtual void TestMethod()
			{
			}
		}

		public virtual void TestExceptions()
		{
			IEnumerator tests = new ReflectionTestSuiteBuilder(typeof(ExceptionInTearDownDoesNotShadowTestCase.RunsWithExceptions
				)).GetEnumerator();
			ITest test = (ITest)Iterators.Next(tests);
			FrameworkTestCase.RunTestAndExpect(test, 1);
		}

		public virtual void TestExceptionInTearDown()
		{
			IEnumerator tests = new ReflectionTestSuiteBuilder(typeof(ExceptionInTearDownDoesNotShadowTestCase.RunsWithExceptionInTearDown
				)).GetEnumerator();
			ITest test = (ITest)Iterators.Next(tests);
			FrameworkTestCase.RunTestAndExpect(test, 1);
		}
	}
}

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
	public class ReflectionTestSuiteBuilderTestCase : ITestCase
	{
		private sealed class ExcludingReflectionTestSuiteBuilder : ReflectionTestSuiteBuilder
		{
			public ExcludingReflectionTestSuiteBuilder(Type[] classes) : base(classes)
			{
			}

			protected override bool IsApplicable(Type clazz)
			{
				return clazz != typeof(ReflectionTestSuiteBuilderTestCase.NotAccepted);
			}
		}

		public class NonTestFixture
		{
		}

		public virtual void TestUnmarkedTestFixture()
		{
			ReflectionTestSuiteBuilder builder = new ReflectionTestSuiteBuilder(typeof(ReflectionTestSuiteBuilderTestCase.NonTestFixture
				));
			AssertFailingTestCase(typeof(ArgumentException), builder);
		}

		public class Accepted : ITestCase
		{
			public virtual void Test()
			{
			}
		}

		public class NotAccepted : ITestCase
		{
			public virtual void Test()
			{
			}
		}

		public virtual void TestNotAcceptedFixture()
		{
			ReflectionTestSuiteBuilder builder = new ReflectionTestSuiteBuilderTestCase.ExcludingReflectionTestSuiteBuilder
				(new Type[] { typeof(ReflectionTestSuiteBuilderTestCase.Accepted), typeof(ReflectionTestSuiteBuilderTestCase.NotAccepted
				) });
			Assert.AreEqual(1, Iterators.Size(builder.GetEnumerator()));
		}

		public class ConstructorThrows : ITestCase
		{
			public static readonly Exception Error = new Exception("no way");

			public ConstructorThrows()
			{
				throw Error;
			}

			public virtual void Test1()
			{
			}

			public virtual void Test2()
			{
			}
		}

		public virtual void TestConstructorFailuresAppearAsFailedTestCases()
		{
			ReflectionTestSuiteBuilder builder = new ReflectionTestSuiteBuilder(typeof(ReflectionTestSuiteBuilderTestCase.ConstructorThrows
				));
			Assert.AreEqual(2, Iterators.ToArray(builder.GetEnumerator()).Length);
		}

		private Exception AssertFailingTestCase(Type expectedError, ReflectionTestSuiteBuilder
			 builder)
		{
			IEnumerator tests = builder.GetEnumerator();
			FailingTest test = (FailingTest)Iterators.Next(tests);
			Assert.AreSame(expectedError, test.Error().GetType());
			return test.Error();
		}
	}
}

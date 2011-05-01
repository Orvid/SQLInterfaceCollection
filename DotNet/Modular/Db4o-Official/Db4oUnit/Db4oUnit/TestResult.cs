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
using System.IO;
using Db4oUnit;
using Db4oUnit.Util;

namespace Db4oUnit
{
	public class TestResult : Printable, ITestListener
	{
		private TestFailureCollection _failures = new TestFailureCollection();

		private int _testCount = 0;

		private readonly StopWatch _watch = new StopWatch();

		public TestResult()
		{
		}

		public virtual void TestStarted(ITest test)
		{
			++_testCount;
		}

		public virtual void TestFailed(ITest test, Exception failure)
		{
			_failures.Add(new TestFailure(test.Label(), failure));
		}

		public virtual void Failure(string msg, Exception failure)
		{
		}

		public virtual int TestCount
		{
			get
			{
				return _testCount;
			}
		}

		public virtual bool Green
		{
			get
			{
				return _failures.Count == 0;
			}
		}

		public virtual TestFailureCollection Failures
		{
			get
			{
				return _failures;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Print(TextWriter writer)
		{
			if (Green)
			{
				writer.Write("GREEN (" + _testCount + " tests) - " + ElapsedString() + TestPlatform
					.NewLine);
				return;
			}
			writer.Write("RED (" + _failures.Count + " out of " + _testCount + " tests failed) - "
				 + ElapsedString() + TestPlatform.NewLine);
			_failures.Print(writer);
		}

		private string ElapsedString()
		{
			return _watch.ToString();
		}

		public virtual void RunStarted()
		{
			_watch.Start();
		}

		public virtual void RunFinished()
		{
			_watch.Stop();
		}
	}
}

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
using System.IO;
using Db4oUnit;
using Db4objects.Db4o.Foundation;

namespace Db4oUnit
{
	public class ConsoleTestRunner
	{
		private readonly IEnumerable _suite;

		private readonly bool _reportToFile;

		public ConsoleTestRunner(IEnumerator suite) : this(suite, true)
		{
		}

		public ConsoleTestRunner(IEnumerator suite, bool reportToFile)
		{
			if (null == suite)
			{
				throw new ArgumentException("suite");
			}
			_suite = Iterators.Iterable(suite);
			_reportToFile = reportToFile;
		}

		public ConsoleTestRunner(IEnumerable suite) : this(suite, true)
		{
		}

		public ConsoleTestRunner(IEnumerable suite, bool reportToFile)
		{
			if (null == suite)
			{
				throw new ArgumentException("suite");
			}
			_suite = suite;
			_reportToFile = reportToFile;
		}

		public ConsoleTestRunner(Type clazz) : this(new ReflectionTestSuiteBuilder(clazz)
			)
		{
		}

		public virtual int Run()
		{
			return Run(TestPlatform.GetStdErr());
		}

		protected virtual TestResult CreateTestResult()
		{
			return new TestResult();
		}

		public virtual int Run(TextWriter writer)
		{
			TestResult result = CreateTestResult();
			new TestRunner(_suite).Run(new CompositeTestListener(new ConsoleListener(writer), 
				result));
			ReportResult(result, writer);
			return result.Failures.Count;
		}

		private void Report(Exception x)
		{
			TestPlatform.PrintStackTrace(TestPlatform.GetStdErr(), x);
		}

		private void ReportResult(TestResult result, TextWriter writer)
		{
			if (_reportToFile)
			{
				ReportToTextFile(result);
			}
			Report(result, writer);
		}

		private void ReportToTextFile(TestResult result)
		{
			try
			{
				TextWriter writer = TestPlatform.OpenTextFile("db4ounit.log");
				try
				{
					Report(result, writer);
				}
				finally
				{
					writer.Close();
				}
			}
			catch (IOException e)
			{
				Report(e);
			}
		}

		private void Report(TestResult result, TextWriter writer)
		{
			try
			{
				result.Print(writer);
				writer.Flush();
			}
			catch (IOException e)
			{
				Report(e);
			}
		}
	}
}

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

namespace Db4oUnit
{
	public class TestFailure : Printable
	{
		private readonly string _testLabel;

		private readonly Exception _failure;

		public TestFailure(string test, Exception failure)
		{
			_testLabel = test;
			_failure = failure;
		}

		public virtual string TestLabel
		{
			get
			{
				return _testLabel;
			}
		}

		public virtual Exception Reason
		{
			get
			{
				return _failure;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Print(TextWriter writer)
		{
			writer.Write(_testLabel);
			writer.Write(": ");
			// TODO: don't print the first stack trace elements
			// which reference db4ounit.Assert methods
			TestPlatform.PrintStackTrace(writer, _failure);
		}
	}
}

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
	public class ConsoleListener : ITestListener
	{
		private readonly TextWriter _writer;

		public ConsoleListener(TextWriter writer)
		{
			_writer = writer;
		}

		public virtual void RunFinished()
		{
		}

		public virtual void RunStarted()
		{
		}

		public virtual void TestFailed(ITest test, Exception failure)
		{
			PrintFailure(failure);
		}

		public virtual void TestStarted(ITest test)
		{
			Print(test.Label());
		}

		private void PrintFailure(Exception failure)
		{
			if (failure == null)
			{
				Print("\t!");
			}
			else
			{
				Print("\t! " + failure);
			}
		}

		private void Print(string message)
		{
			try
			{
				_writer.Write(message + TestPlatform.NewLine);
				_writer.Flush();
			}
			catch (IOException x)
			{
				TestPlatform.PrintStackTrace(_writer, x);
			}
		}

		public virtual void Failure(string msg, Exception failure)
		{
			Print("\t ! " + msg);
			PrintFailure(failure);
		}
	}
}

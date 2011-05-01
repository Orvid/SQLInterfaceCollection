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
using Db4oUnit;

namespace Db4oUnit
{
	public class CompositeTestListener : ITestListener
	{
		private readonly ITestListener _listener1;

		private readonly ITestListener _listener2;

		public CompositeTestListener(ITestListener listener1, ITestListener listener2)
		{
			_listener1 = listener1;
			_listener2 = listener2;
		}

		public virtual void RunFinished()
		{
			_listener1.RunFinished();
			_listener2.RunFinished();
		}

		public virtual void RunStarted()
		{
			_listener1.RunStarted();
			_listener2.RunStarted();
		}

		public virtual void TestFailed(ITest test, Exception failure)
		{
			_listener1.TestFailed(test, failure);
			_listener2.TestFailed(test, failure);
		}

		public virtual void TestStarted(ITest test)
		{
			_listener1.TestStarted(test);
			_listener2.TestStarted(test);
		}

		public virtual void Failure(string msg, Exception failure)
		{
			_listener1.Failure(msg, failure);
			_listener2.Failure(msg, failure);
		}
	}
}

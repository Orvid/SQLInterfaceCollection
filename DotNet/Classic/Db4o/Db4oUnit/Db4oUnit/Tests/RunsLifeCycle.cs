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
using Db4objects.Db4o.Foundation;

namespace Db4oUnit.Tests
{
	public class RunsLifeCycle : ITestCase, ITestLifeCycle
	{
		public static DynamicVariable _tearDownCalled = DynamicVariable.NewInstance();

		private bool _setupCalled = false;

		public virtual void SetUp()
		{
			_setupCalled = true;
		}

		public virtual void TearDown()
		{
			TearDownCalled().value = true;
		}

		public virtual bool SetupCalled()
		{
			return _setupCalled;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMethod()
		{
			Assert.IsTrue(_setupCalled);
			Assert.IsTrue(!(((bool)TearDownCalled().value)));
			throw FrameworkTestCase.Exception;
		}

		private ByRef TearDownCalled()
		{
			return ((ByRef)_tearDownCalled.Value);
		}
	}
}

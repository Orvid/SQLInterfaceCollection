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
using Db4objects.Db4o.Foundation;

namespace Db4oUnit
{
	/// <summary>A test that always fails with a specific exception.</summary>
	/// <remarks>A test that always fails with a specific exception.</remarks>
	public class FailingTest : ITest
	{
		private readonly Exception _error;

		private readonly string _label;

		public FailingTest(string label, Exception error)
		{
			_label = label;
			_error = error;
		}

		public virtual string Label()
		{
			return _label;
		}

		public virtual Exception Error()
		{
			return _error;
		}

		public virtual void Run()
		{
			throw new TestException(_error);
		}

		public virtual bool IsLeafTest()
		{
			return true;
		}

		public virtual ITest Transmogrify(IFunction4 fun)
		{
			return ((ITest)fun.Apply(this));
		}
	}
}

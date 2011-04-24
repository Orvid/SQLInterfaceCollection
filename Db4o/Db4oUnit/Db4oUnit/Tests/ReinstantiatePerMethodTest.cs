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

namespace Db4oUnit.Tests
{
	public class ReinstantiatePerMethodTest : ITestCase
	{
		private int a = 0;

		public virtual void Test1()
		{
			Assert.AreEqual(0, a);
			a = 1;
		}

		public virtual void Test2()
		{
			Assert.AreEqual(0, a);
			a = 2;
		}
	}
}

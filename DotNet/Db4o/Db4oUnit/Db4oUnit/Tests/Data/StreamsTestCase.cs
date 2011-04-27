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
using System.Collections;
using Db4oUnit;
using Db4objects.Db4o.Foundation;

namespace Db4oUnit.Tests.Data
{
	public class StreamsTestCase : ITestCase
	{
		public virtual void TestSeries()
		{
			Collection4 calls = new Collection4();
			IEnumerator series = Iterators.Series(string.Empty, new _IFunction4_11(calls)).GetEnumerator
				();
			Assert.IsTrue(series.MoveNext());
			Assert.IsTrue(series.MoveNext());
			Iterator4Assert.AreEqual(new object[] { string.Empty, "*" }, calls.GetEnumerator(
				));
		}

		private sealed class _IFunction4_11 : IFunction4
		{
			public _IFunction4_11(Collection4 calls)
			{
				this.calls = calls;
			}

			public object Apply(object value)
			{
				calls.Add(value);
				return value + "*";
			}

			private readonly Collection4 calls;
		}
	}
}

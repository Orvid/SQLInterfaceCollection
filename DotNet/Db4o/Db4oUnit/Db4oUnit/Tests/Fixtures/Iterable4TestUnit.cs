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
using Db4oUnit.Fixtures;

namespace Db4oUnit.Tests.Fixtures
{
	public class Iterable4TestUnit : ITestCase
	{
		private readonly IEnumerable subject = (IEnumerable)SubjectFixtureProvider.Value(
			);

		private readonly object[] data = MultiValueFixtureProvider.Value();

		public virtual void TestElements()
		{
			IEnumerator elements = subject.GetEnumerator();
			for (int i = 0; i < data.Length; ++i)
			{
				Assert.IsTrue(elements.MoveNext());
				Assert.AreEqual(data[i], elements.Current);
			}
			Assert.IsFalse(elements.MoveNext());
		}
	}
}

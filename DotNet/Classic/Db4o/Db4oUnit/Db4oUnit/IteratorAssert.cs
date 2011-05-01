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

namespace Db4oUnit
{
	public class IteratorAssert
	{
		public static void AreEqual(IEnumerable expected, IEnumerable actual)
		{
			AreEqual(expected.GetEnumerator(), actual.GetEnumerator());
		}

		public static void AreEqual(IEnumerator expected, IEnumerator actual)
		{
			if (null == expected)
			{
				Assert.IsNull(actual);
				return;
			}
			Assert.IsNotNull(actual);
			while (expected.MoveNext())
			{
				Assert.IsTrue(actual.MoveNext());
				Assert.AreEqual(expected.Current, actual.Current);
			}
			Assert.IsFalse(actual.MoveNext());
		}

		public static void AreEqual(object[] expected, IEnumerator iterator)
		{
			ArrayList v = new ArrayList();
			for (int i = 0; i < expected.Length; i++)
			{
				v.Add(expected[i]);
			}
			AreEqual(v.GetEnumerator(), iterator);
		}

		public static void SameContent(object[] expected, IEnumerable actual)
		{
			IList expectedList = new ArrayList();
			for (int expectedObjectIndex = 0; expectedObjectIndex < expected.Length; ++expectedObjectIndex)
			{
				object expectedObject = expected[expectedObjectIndex];
				expectedList.Add(expectedObject);
			}
			SameContent(expectedList, actual);
		}

		public static void SameContent(IEnumerable expected, IEnumerable actual)
		{
			SameContent(expected.GetEnumerator(), actual.GetEnumerator());
		}

		public static void SameContent(IEnumerator expected, IEnumerator actual)
		{
			Collection4 allExpected = new Collection4();
			while (expected.MoveNext())
			{
				allExpected.Add(expected.Current);
			}
			while (actual.MoveNext())
			{
				object current = actual.Current;
				bool removed = allExpected.Remove(current);
				if (!removed)
				{
					Unexpected(current);
				}
			}
			Assert.IsTrue(allExpected.IsEmpty(), allExpected.ToString());
		}

		private static void Unexpected(object element)
		{
			Assert.Fail("Unexpected element: " + element);
		}
	}
}

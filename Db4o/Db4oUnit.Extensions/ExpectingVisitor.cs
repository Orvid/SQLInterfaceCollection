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
using Db4oUnit.Extensions;
using Db4objects.Db4o.Foundation;
using Sharpen;

namespace Db4oUnit.Extensions
{
	public class ExpectingVisitor : IVisitor4
	{
		private const bool Debug = false;

		private readonly object[] _expected;

		private readonly bool _obeyOrder;

		private readonly Collection4 _unexpected = new Collection4();

		private bool _ignoreUnexpected;

		private int _cursor;

		private sealed class _object_24 : object
		{
			public _object_24()
			{
			}

			public override string ToString()
			{
				return "[FOUND]";
			}
		}

		private static readonly object Found = new _object_24();

		public ExpectingVisitor(object[] results, bool obeyOrder, bool ignoreUnexpected)
		{
			_expected = new object[results.Length];
			System.Array.Copy(results, 0, _expected, 0, results.Length);
			_obeyOrder = obeyOrder;
			_ignoreUnexpected = ignoreUnexpected;
		}

		public ExpectingVisitor(object[] results) : this(results, false, false)
		{
		}

		public ExpectingVisitor(object singleObject) : this(new object[] { singleObject }
			)
		{
		}

		/// <summary>Expect empty</summary>
		public ExpectingVisitor() : this(new object[0])
		{
		}

		public virtual void Visit(object obj)
		{
			if (_obeyOrder)
			{
				VisitOrdered(obj);
			}
			else
			{
				VisitUnOrdered(obj);
			}
		}

		private void VisitOrdered(object obj)
		{
			if (_cursor < _expected.Length)
			{
				if (AreEqual(_expected[_cursor], obj))
				{
					Ods("Expected OK: " + obj.ToString());
					_expected[_cursor] = Found;
					_cursor++;
					return;
				}
			}
			Unexpected(obj);
		}

		private void Unexpected(object obj)
		{
			if (_ignoreUnexpected)
			{
				return;
			}
			_unexpected.Add(obj);
			Ods("Unexpected: " + obj);
		}

		private void VisitUnOrdered(object obj)
		{
			for (int i = 0; i < _expected.Length; i++)
			{
				object expectedItem = _expected[i];
				if (AreEqual(obj, expectedItem))
				{
					Ods("Expected OK: " + obj);
					_expected[i] = Found;
					return;
				}
			}
			Unexpected(obj);
		}

		private bool AreEqual(object obj, object expectedItem)
		{
			return expectedItem == obj || (expectedItem != null && obj != null && expectedItem
				.Equals(obj));
		}

		private static void Ods(string message)
		{
		}

		public virtual void AssertExpectations()
		{
			if (_unexpected.Size() > 0)
			{
				Assert.Fail("UNEXPECTED: " + _unexpected.ToString());
			}
			for (int i = 0; i < _expected.Length; i++)
			{
				Assert.AreSame(Found, _expected[i]);
			}
		}

		public static Db4oUnit.Extensions.ExpectingVisitor CreateExpectingVisitor(int value
			, int count)
		{
			int[] values = new int[count];
			for (int i = 0; i < values.Length; i++)
			{
				values[i] = value;
			}
			return new Db4oUnit.Extensions.ExpectingVisitor(IntArrays4.ToObjectArray(values));
		}

		public static Db4oUnit.Extensions.ExpectingVisitor CreateExpectingVisitor(int[] keys
			)
		{
			return new Db4oUnit.Extensions.ExpectingVisitor(IntArrays4.ToObjectArray(keys));
		}

		public static Db4oUnit.Extensions.ExpectingVisitor CreateSortedExpectingVisitor(int
			[] keys)
		{
			return new Db4oUnit.Extensions.ExpectingVisitor(IntArrays4.ToObjectArray(keys), true
				, false);
		}

		public static Db4oUnit.Extensions.ExpectingVisitor CreateExpectingVisitor(int expectedID
			)
		{
			return CreateExpectingVisitor(expectedID, 1);
		}
	}
}

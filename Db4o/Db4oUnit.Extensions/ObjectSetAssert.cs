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
using System.Collections;
using Db4oUnit;
using Db4oUnit.Extensions;
using Db4objects.Db4o;
using Db4objects.Db4o.Foundation;

namespace Db4oUnit.Extensions
{
	public class ObjectSetAssert
	{
		public static void SameContent(IObjectSet objectSet, object[] expectedItems)
		{
			Iterator4Assert.SameContent(Iterators.Iterate(expectedItems), Iterate(objectSet));
		}

		public static void AreEqual(IObjectSet objectSet, object[] expectedItems)
		{
			Iterator4Assert.AreEqual(expectedItems, Iterate(objectSet));
		}

		public static IEnumerator Iterate(IObjectSet objectSet)
		{
			return new ObjectSetAssert.ObjectSetIterator4(objectSet);
		}

		internal class ObjectSetIterator4 : IEnumerator
		{
			private static readonly object Invalid = new object();

			private IObjectSet _objectSet;

			private object _current;

			public ObjectSetIterator4(IObjectSet collection)
			{
				_objectSet = collection;
			}

			public virtual object Current
			{
				get
				{
					if (_current == Invalid)
					{
						throw new InvalidOperationException();
					}
					return _current;
				}
			}

			public virtual bool MoveNext()
			{
				if (_objectSet.HasNext())
				{
					_current = _objectSet.Next();
					return true;
				}
				_current = Invalid;
				return false;
			}

			public virtual void Reset()
			{
				_objectSet.Reset();
				_current = Invalid;
			}
		}
	}
}

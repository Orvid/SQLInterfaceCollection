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
using Db4oUnit.Mocking;
using Db4objects.Db4o.Foundation;

namespace Db4oUnit.Mocking
{
	public class MethodCallRecorder : IEnumerable
	{
		private readonly Collection4 _calls = new Collection4();

		public virtual IEnumerator GetEnumerator()
		{
			return _calls.GetEnumerator();
		}

		public virtual void Record(MethodCall call)
		{
			_calls.Add(call);
		}

		public virtual void Reset()
		{
			_calls.Clear();
		}

		/// <summary>Asserts that the method calls were the same as expectedCalls.</summary>
		/// <remarks>
		/// Asserts that the method calls were the same as expectedCalls.
		/// Unfortunately we cannot call this method 'assert' because
		/// it's a keyword starting with java 1.5.
		/// </remarks>
		/// <param name="expectedCalls"></param>
		public virtual void Verify(MethodCall[] expectedCalls)
		{
			Iterator4Assert.AreEqual(expectedCalls, GetEnumerator());
		}

		public virtual void VerifyUnordered(MethodCall[] expectedCalls)
		{
			Iterator4Assert.SameContent(expectedCalls, GetEnumerator());
		}
	}
}

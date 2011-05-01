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
using Db4oUnit.Extensions;
using Sharpen;

namespace Db4oUnit.Extensions
{
	public abstract class ComposibleTestSuite : Db4oTestSuite
	{
		protected Type[] ComposeTests(Type[] testCases)
		{
			return Concat(testCases, ComposeWith());
		}

		protected virtual Type[] ComposeWith()
		{
			return new Type[0];
		}

		public static Type[] Concat(Type[] testCases, Type[] otherTests)
		{
			Type[] result = new Type[otherTests.Length + testCases.Length];
			System.Array.Copy(testCases, 0, result, 0, testCases.Length);
			System.Array.Copy(otherTests, 0, result, testCases.Length, otherTests.Length);
			return result;
		}
	}
}

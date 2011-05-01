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
using Db4objects.Db4o.Foundation;
using Sharpen;

namespace Db4oUnit.Extensions
{
	public class IntArrays4
	{
		public static int[] Fill(int[] array, int value)
		{
			for (int i = 0; i < array.Length; ++i)
			{
				array[i] = value;
			}
			return array;
		}

		public static int[] Concat(int[] a, int[] b)
		{
			int[] array = new int[a.Length + b.Length];
			System.Array.Copy(a, 0, array, 0, a.Length);
			System.Array.Copy(b, 0, array, a.Length, b.Length);
			return array;
		}

		public static int Occurences(int[] values, int value)
		{
			int count = 0;
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i] == value)
				{
					count++;
				}
			}
			return count;
		}

		public static int[] Clone(int[] bars)
		{
			int[] array = new int[bars.Length];
			System.Array.Copy(bars, 0, array, 0, bars.Length);
			return array;
		}

		public static object[] ToObjectArray(int[] values)
		{
			object[] ret = new object[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				ret[i] = values[i];
			}
			return ret;
		}

		public static IEnumerator NewIterator(int[] values)
		{
			return new ArrayIterator4(ToObjectArray(values));
		}
	}
}

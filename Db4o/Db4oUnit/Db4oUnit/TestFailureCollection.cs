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
using System.IO;
using Db4oUnit;
using Db4objects.Db4o.Foundation;

namespace Db4oUnit
{
	public class TestFailureCollection : Printable, IEnumerable
	{
		private readonly Collection4 _failures = new Collection4();

		public virtual IEnumerator GetEnumerator()
		{
			return _failures.GetEnumerator();
		}

		public virtual int Count
		{
			get
			{
				return _failures.Size();
			}
		}

		public virtual void Add(TestFailure failure)
		{
			_failures.Add(failure);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Print(TextWriter writer)
		{
			PrintSummary(writer);
			PrintDetails(writer);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void PrintSummary(TextWriter writer)
		{
			int index = 1;
			IEnumerator e = GetEnumerator();
			while (e.MoveNext())
			{
				writer.Write(index.ToString());
				writer.Write(") ");
				writer.Write(((TestFailure)e.Current).TestLabel);
				writer.Write(TestPlatform.NewLine);
				++index;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void PrintDetails(TextWriter writer)
		{
			int index = 1;
			IEnumerator e = GetEnumerator();
			while (e.MoveNext())
			{
				writer.Write(TestPlatform.NewLine);
				writer.Write(index.ToString());
				writer.Write(") ");
				((Printable)e.Current).Print(writer);
				writer.Write(TestPlatform.NewLine);
				++index;
			}
		}
	}
}

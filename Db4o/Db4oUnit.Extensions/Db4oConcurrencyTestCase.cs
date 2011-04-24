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
using Db4oUnit.Extensions;
using Db4objects.Db4o.Foundation;

namespace Db4oUnit.Extensions
{
	/// <exclude></exclude>
	public class Db4oConcurrencyTestCase : Db4oClientServerTestCase
	{
		private bool[] _done;

		/// <exception cref="System.Exception"></exception>
		protected override void Db4oSetupAfterStore()
		{
			InitTasksDoneFlag();
			base.Db4oSetupAfterStore();
		}

		private void InitTasksDoneFlag()
		{
			_done = new bool[ThreadCount()];
		}

		protected virtual void MarkTaskDone(int seq, bool done)
		{
			_done[seq] = done;
		}

		/// <exception cref="System.Exception"></exception>
		protected virtual void WaitForAllTasksDone()
		{
			while (!AreAllTasksDone())
			{
				Runtime4.Sleep(1);
			}
		}

		private bool AreAllTasksDone()
		{
			for (int i = 0; i < _done.Length; ++i)
			{
				if (!_done[i])
				{
					return false;
				}
			}
			return true;
		}
	}
}

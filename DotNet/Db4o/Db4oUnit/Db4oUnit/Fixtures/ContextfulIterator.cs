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
using Db4oUnit.Fixtures;
using Db4objects.Db4o.Foundation;
using Sharpen.Lang;

namespace Db4oUnit.Fixtures
{
	public class ContextfulIterator : Contextful, IEnumerator
	{
		private readonly IEnumerator _delegate;

		public ContextfulIterator(IEnumerator delegate_)
		{
			_delegate = delegate_;
		}

		public virtual object Current
		{
			get
			{
				return Run(new _IClosure4_17(this));
			}
		}

		private sealed class _IClosure4_17 : IClosure4
		{
			public _IClosure4_17(ContextfulIterator _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public object Run()
			{
				return this._enclosing._delegate.Current;
			}

			private readonly ContextfulIterator _enclosing;
		}

		public virtual bool MoveNext()
		{
			BooleanByRef result = new BooleanByRef();
			Run(new _IRunnable_26(this, result));
			return result.value;
		}

		private sealed class _IRunnable_26 : IRunnable
		{
			public _IRunnable_26(ContextfulIterator _enclosing, BooleanByRef result)
			{
				this._enclosing = _enclosing;
				this.result = result;
			}

			public void Run()
			{
				result.value = this._enclosing._delegate.MoveNext();
			}

			private readonly ContextfulIterator _enclosing;

			private readonly BooleanByRef result;
		}

		public virtual void Reset()
		{
			Run(new _IRunnable_35(this));
		}

		private sealed class _IRunnable_35 : IRunnable
		{
			public _IRunnable_35(ContextfulIterator _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public void Run()
			{
				this._enclosing._delegate.Reset();
			}

			private readonly ContextfulIterator _enclosing;
		}
	}
}

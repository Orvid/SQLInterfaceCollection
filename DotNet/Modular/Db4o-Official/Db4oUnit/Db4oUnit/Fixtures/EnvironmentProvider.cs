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
	public class EnvironmentProvider : IFixtureProvider
	{
		private sealed class _FixtureVariable_7 : FixtureVariable
		{
			public _FixtureVariable_7()
			{
			}

			public override void With(object value, IRunnable runnable)
			{
				base.With(value, new _IRunnable_9(value, runnable));
			}

			private sealed class _IRunnable_9 : IRunnable
			{
				public _IRunnable_9(object value, IRunnable runnable)
				{
					this.value = value;
					this.runnable = runnable;
				}

				public void Run()
				{
					Environments.RunWith((IEnvironment)value, runnable);
				}

				private readonly object value;

				private readonly IRunnable runnable;
			}
		}

		private readonly FixtureVariable _variable = new _FixtureVariable_7();

		public virtual FixtureVariable Variable()
		{
			return _variable;
		}

		public virtual IEnumerator GetEnumerator()
		{
			return Iterators.SingletonIterator(Environments.NewConventionBasedEnvironment());
		}
	}
}

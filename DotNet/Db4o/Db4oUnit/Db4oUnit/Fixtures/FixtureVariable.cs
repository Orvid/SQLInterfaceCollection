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
using Db4oUnit.Fixtures;
using Db4objects.Db4o.Foundation;
using Sharpen.Lang;

namespace Db4oUnit.Fixtures
{
	public class FixtureVariable
	{
		public static Db4oUnit.Fixtures.FixtureVariable NewInstance(string label)
		{
			return new Db4oUnit.Fixtures.FixtureVariable(label);
		}

		private readonly string _label;

		public FixtureVariable() : this(string.Empty)
		{
		}

		public FixtureVariable(string label)
		{
			_label = label;
		}

		public virtual string Label
		{
			get
			{
				return _label;
			}
		}

		public override string ToString()
		{
			return _label;
		}

		public virtual object With(object value, IClosure4 closure)
		{
			return Inject(value).Run(closure);
		}

		public virtual void With(object value, IRunnable runnable)
		{
			Inject(value).Run(runnable);
		}

		private FixtureContext Inject(object value)
		{
			return CurrentContext().Add(this, value);
		}

		public virtual object Value
		{
			get
			{
				FixtureContext.Found found = CurrentContext().Get(this);
				if (null == found)
				{
					throw new InvalidOperationException();
				}
				return (object)found.value;
			}
		}

		private FixtureContext CurrentContext()
		{
			return FixtureContext.Current;
		}
	}
}

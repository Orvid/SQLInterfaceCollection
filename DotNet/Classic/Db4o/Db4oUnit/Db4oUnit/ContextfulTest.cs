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
using Db4oUnit.Fixtures;
using Db4objects.Db4o.Foundation;
using Sharpen.Lang;

namespace Db4oUnit
{
	public class ContextfulTest : Contextful, ITest
	{
		private ITestFactory _factory;

		public ContextfulTest(ITestFactory factory)
		{
			_factory = factory;
		}

		public virtual string Label()
		{
			return (string)Run(new _IClosure4_18(this));
		}

		private sealed class _IClosure4_18 : IClosure4
		{
			public _IClosure4_18(ContextfulTest _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public object Run()
			{
				return this._enclosing.TestInstance().Label();
			}

			private readonly ContextfulTest _enclosing;
		}

		public virtual bool IsLeafTest()
		{
			return ((bool)Run(new _IClosure4_26(this)));
		}

		private sealed class _IClosure4_26 : IClosure4
		{
			public _IClosure4_26(ContextfulTest _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public object Run()
			{
				return this._enclosing.TestInstance().IsLeafTest();
			}

			private readonly ContextfulTest _enclosing;
		}

		public virtual void Run()
		{
			Run(new _IRunnable_34(this));
		}

		private sealed class _IRunnable_34 : IRunnable
		{
			public _IRunnable_34(ContextfulTest _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public void Run()
			{
				this._enclosing.TestInstance().Run();
			}

			private readonly ContextfulTest _enclosing;
		}

		private ITest TestInstance()
		{
			return _factory.NewInstance();
		}

		public virtual ITest Transmogrify(IFunction4 fun)
		{
			return ((ITest)fun.Apply(this));
		}
	}
}

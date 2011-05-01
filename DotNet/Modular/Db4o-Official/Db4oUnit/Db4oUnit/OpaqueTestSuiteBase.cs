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
using Db4objects.Db4o.Foundation;

namespace Db4oUnit
{
	public abstract class OpaqueTestSuiteBase : ITest
	{
		private IClosure4 _tests;

		public OpaqueTestSuiteBase(IClosure4 tests)
		{
			_tests = tests;
		}

		public virtual void Run()
		{
			ITestExecutor executor = ((ITestExecutor)Environments.My(typeof(ITestExecutor)));
			IEnumerator tests = ((IEnumerator)_tests.Run());
			try
			{
				SuiteSetUp();
				while (tests.MoveNext())
				{
					executor.Execute(((ITest)tests.Current));
				}
				SuiteTearDown();
			}
			catch (Exception exc)
			{
				executor.Fail(this, exc);
			}
		}

		public virtual bool IsLeafTest()
		{
			return false;
		}

		protected virtual IClosure4 Tests()
		{
			return _tests;
		}

		public virtual ITest Transmogrify(IFunction4 fun)
		{
			return Transmogrified(new _IClosure4_38(this, fun));
		}

		private sealed class _IClosure4_38 : IClosure4
		{
			public _IClosure4_38(OpaqueTestSuiteBase _enclosing, IFunction4 fun)
			{
				this._enclosing = _enclosing;
				this.fun = fun;
			}

			public object Run()
			{
				return Iterators.Map(((IEnumerator)this._enclosing.Tests().Run()), new _IFunction4_40
					(fun));
			}

			private sealed class _IFunction4_40 : IFunction4
			{
				public _IFunction4_40(IFunction4 fun)
				{
					this.fun = fun;
				}

				public object Apply(object test)
				{
					return ((ITest)fun.Apply(((ITest)test)));
				}

				private readonly IFunction4 fun;
			}

			private readonly OpaqueTestSuiteBase _enclosing;

			private readonly IFunction4 fun;
		}

		protected abstract Db4oUnit.OpaqueTestSuiteBase Transmogrified(IClosure4 tests);

		/// <exception cref="System.Exception"></exception>
		protected abstract void SuiteSetUp();

		/// <exception cref="System.Exception"></exception>
		protected abstract void SuiteTearDown();

		public abstract string Label();
	}
}

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
using System.Reflection;
using Db4oUnit;
using Db4oUnit.Extensions;
using Db4objects.Db4o.Foundation;

namespace Db4oUnit.Extensions
{
	public class Db4oTestSuiteBuilder : ReflectionTestSuiteBuilder
	{
		private IDb4oFixture _fixture;

		public Db4oTestSuiteBuilder(IDb4oFixture fixture, Type clazz) : this(fixture, new 
			Type[] { clazz })
		{
		}

		public Db4oTestSuiteBuilder(IDb4oFixture fixture, Type[] classes) : base(classes)
		{
			Fixture(fixture);
		}

		private void Fixture(IDb4oFixture fixture)
		{
			if (null == fixture)
			{
				throw new ArgumentNullException("fixture");
			}
			_fixture = fixture;
		}

		protected override bool IsApplicable(Type clazz)
		{
			return _fixture.Accept(clazz);
		}

		protected override ITest CreateTest(object instance, MethodInfo method)
		{
			ITest test = base.CreateTest(instance, method);
			return new _TestDecorationAdapter_38(test, test);
		}

		private sealed class _TestDecorationAdapter_38 : TestDecorationAdapter
		{
			public _TestDecorationAdapter_38(ITest test, ITest baseArg1) : base(baseArg1)
			{
				this.test = test;
			}

			public override string Label()
			{
				return "(" + Db4oFixtureVariable.Fixture().Label() + ") " + test.Label();
			}

			private readonly ITest test;
		}

		protected override object WithContext(IClosure4 closure)
		{
			return Db4oFixtureVariable.FixtureVariable.With(_fixture, closure);
		}
	}
}

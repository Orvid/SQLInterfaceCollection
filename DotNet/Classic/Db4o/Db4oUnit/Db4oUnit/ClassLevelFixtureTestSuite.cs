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
using Db4oUnit;
using Db4objects.Db4o.Foundation;
using Db4objects.Db4o.Internal;

namespace Db4oUnit
{
	public class ClassLevelFixtureTestSuite : OpaqueTestSuiteBase
	{
		public static readonly string TeardownMethodName = "classTearDown";

		public static readonly string SetupMethodName = "classSetUp";

		private readonly Type _clazz;

		public ClassLevelFixtureTestSuite(Type clazz, IClosure4 tests) : base(tests)
		{
			_clazz = clazz;
		}

		/// <exception cref="System.Exception"></exception>
		protected override void SuiteSetUp()
		{
			Reflection4.InvokeStatic(_clazz, SetupMethodName);
		}

		/// <exception cref="System.Exception"></exception>
		protected override void SuiteTearDown()
		{
			Reflection4.InvokeStatic(_clazz, TeardownMethodName);
		}

		public override string Label()
		{
			return _clazz.FullName;
		}

		protected override OpaqueTestSuiteBase Transmogrified(IClosure4 tests)
		{
			return new Db4oUnit.ClassLevelFixtureTestSuite(_clazz, tests);
		}
	}
}

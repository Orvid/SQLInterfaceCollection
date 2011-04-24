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

namespace Db4oUnit
{
	/// <summary>Support for hierarchically chained test suites.</summary>
	/// <remarks>
	/// Support for hierarchically chained test suites.
	/// In the topmost test package define an AllTests class which extends
	/// ReflectionTestSuite and returns all subpackage.AllTests classes as
	/// testCases. Example:
	/// package org.acme.tests;
	/// public class AllTests extends ReflectionTestSuite {
	/// protected Class[] testCases() {
	/// return new Class[] {
	/// org.acme.tests.subsystem1.AllTests.class,
	/// org.acme.tests.subsystem2.AllTests.class,
	/// };
	/// }
	/// }
	/// </remarks>
	public abstract class ReflectionTestSuite : ITestSuiteBuilder
	{
		public virtual IEnumerator GetEnumerator()
		{
			return new ReflectionTestSuiteBuilder(TestCases()).GetEnumerator();
		}

		protected abstract Type[] TestCases();

		public virtual int Run()
		{
			return new ConsoleTestRunner(GetEnumerator()).Run();
		}
	}
}

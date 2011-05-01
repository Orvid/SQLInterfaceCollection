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
using Db4oUnit.Extensions;
using Db4oUnit.Extensions.Tests;
using Db4oUnit.Mocking;
using Db4objects.Db4o.Config;
using Db4objects.Db4o.Foundation;

namespace Db4oUnit.Extensions.Tests
{
	public class SimpleDb4oTestCase : AbstractDb4oTestCase
	{
		public static readonly DynamicVariable RecorderVariable = DynamicVariable.NewInstance
			();

		public class Data
		{
		}

		protected override void Configure(IConfiguration config)
		{
			Record(new MethodCall("fixture", new object[] { Fixture() }));
			Record(new MethodCall("configure", new object[] { config }));
		}

		private void Record(MethodCall call)
		{
			Recorder().Record(call);
		}

		private MethodCallRecorder Recorder()
		{
			return ((MethodCallRecorder)RecorderVariable.Value);
		}

		protected override void Store()
		{
			Record(new MethodCall("store", new object[] {  }));
			Fixture().Db().Store(new SimpleDb4oTestCase.Data());
		}

		public virtual void TestResultSize()
		{
			Record(new MethodCall("testResultSize", new object[] {  }));
			Assert.AreEqual(1, Fixture().Db().QueryByExample(typeof(SimpleDb4oTestCase.Data))
				.Count);
		}
	}
}

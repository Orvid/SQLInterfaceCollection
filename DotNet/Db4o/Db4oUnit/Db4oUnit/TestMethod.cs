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
using Db4objects.Db4o.Foundation;

namespace Db4oUnit
{
	/// <summary>Reflection based db4ounit.Test implementation.</summary>
	/// <remarks>Reflection based db4ounit.Test implementation.</remarks>
	public class TestMethod : ITest
	{
		private readonly object _subject;

		private readonly MethodInfo _method;

		public TestMethod(object instance, MethodInfo method)
		{
			if (null == instance)
			{
				throw new ArgumentException("instance");
			}
			if (null == method)
			{
				throw new ArgumentException("method");
			}
			_subject = instance;
			_method = method;
		}

		public virtual object GetSubject()
		{
			return _subject;
		}

		public virtual MethodInfo GetMethod()
		{
			return _method;
		}

		public virtual string Label()
		{
			return _subject.GetType().FullName + "." + _method.Name;
		}

		public override string ToString()
		{
			return "TestMethod(" + _method + ")";
		}

		public virtual void Run()
		{
			bool exceptionInTest = false;
			try
			{
				try
				{
					SetUp();
					Invoke();
				}
				catch (TargetInvocationException x)
				{
					exceptionInTest = true;
					throw new TestException(x.InnerException);
				}
				catch (Exception x)
				{
					exceptionInTest = true;
					throw new TestException(x);
				}
			}
			finally
			{
				try
				{
					TearDown();
				}
				catch (Exception exc)
				{
					if (!exceptionInTest)
					{
						throw;
					}
					Sharpen.Runtime.PrintStackTrace(exc);
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		protected virtual void Invoke()
		{
			_method.Invoke(_subject, new object[0]);
		}

		protected virtual void TearDown()
		{
			if (_subject is ITestLifeCycle)
			{
				try
				{
					((ITestLifeCycle)_subject).TearDown();
				}
				catch (Exception e)
				{
					throw new TearDownFailureException(e);
				}
			}
		}

		protected virtual void SetUp()
		{
			if (_subject is ITestLifeCycle)
			{
				try
				{
					((ITestLifeCycle)_subject).SetUp();
				}
				catch (Exception e)
				{
					throw new SetupFailureException(e);
				}
			}
		}

		public virtual bool IsLeafTest()
		{
			return true;
		}

		public virtual ITest Transmogrify(IFunction4 fun)
		{
			return ((ITest)fun.Apply(this));
		}
	}
}

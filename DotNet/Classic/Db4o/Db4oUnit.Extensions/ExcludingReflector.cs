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
using Db4objects.Db4o.Foundation;
using Db4objects.Db4o.Reflect;

namespace Db4oUnit.Extensions
{
	public class ExcludingReflector : Db4objects.Db4o.Reflect.Net.NetReflector
	{
		private readonly Collection4 _excludedClasses;

		public ExcludingReflector(Type[] excludedClasses)
		{
			_excludedClasses = new Collection4();
			for (int claxxIndex = 0; claxxIndex < excludedClasses.Length; ++claxxIndex)
			{
				Type claxx = excludedClasses[claxxIndex];
				_excludedClasses.Add(claxx.FullName);
			}
		}

		public ExcludingReflector(ByRef loaderClass, Type[] excludedClasses)
		{
			_excludedClasses = new Collection4();
			for (int claxxIndex = 0; claxxIndex < excludedClasses.Length; ++claxxIndex)
			{
				Type claxx = excludedClasses[claxxIndex];
				_excludedClasses.Add(claxx.FullName);
			}
		}

		public ExcludingReflector(Collection4 excludedClasses)
		{
			_excludedClasses = excludedClasses;
		}

		public ExcludingReflector(ByRef loaderClass, Collection4 excludedClasses)
		{
			_excludedClasses = excludedClasses;
		}

		public override object DeepClone(object obj)
		{
			return new Db4oUnit.Extensions.ExcludingReflector(_excludedClasses);
		}

		public override IReflectClass ForName(string className)
		{
			if (_excludedClasses.Contains(className))
			{
				return null;
			}
			return base.ForName(className);
		}

		public override IReflectClass ForClass(Type clazz)
		{
			if (_excludedClasses.Contains(clazz.FullName))
			{
				return null;
			}
			return base.ForClass(clazz);
		}
	}
}

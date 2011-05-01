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
using System.Reflection;
using Db4objects.Db4o.IO;
using Db4objects.Db4o.Reflect.Net;

namespace Db4oUnit.Extensions
{
	public class Db4oUnitPlatform
	{
		public static bool IsPascalCase()
		{
			return true;
		}

	    public static bool IsUserField(FieldInfo field)
	    {
	        if (field.IsStatic) return false;
            if (NetField.IsTransient(field)) return false;
	        if (field.Name.IndexOf("$") != -1) return false;
	        return true;
	    }

		public static IStorage NewPersistentStorage()
		{
#if SILVERLIGHT
			return new IsolatedStorageStorage();
#else
			return new FileStorage();
#endif
		}
	}
}

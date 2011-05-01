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
using System.IO;
using Db4objects.Db4o.Foundation.IO;
using Db4objects.Db4o.Internal;
using Sharpen;

namespace Db4oUnit.Extensions.Util
{
	public class CrossPlatformServices
	{
		public static string SimpleName(string typeName)
		{
			int index = typeName.IndexOf(',');
			if (index < 0)
			{
				return typeName;
			}
			return Sharpen.Runtime.Substring(typeName, 0, index);
		}

		public static string FullyQualifiedName(Type klass)
		{
			return ReflectPlatform.FullyQualifiedName(klass);
		}

		public static string DatabasePath(string fileName)
		{
			string path = Runtime.GetProperty("db4ounit.file.path");
			if (path == null || path.Length == 0)
			{
				path = ".";
			}
			else
			{
				System.IO.Directory.CreateDirectory(path);
			}
			return Path.Combine(path, fileName);
		}
	}
}

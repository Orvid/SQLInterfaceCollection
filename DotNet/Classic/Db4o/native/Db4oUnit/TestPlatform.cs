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
namespace Db4oUnit
{
	using System;
	using System.IO;
	using System.Reflection;

	public class TestPlatform
	{
#if CF
        public static string NewLine = "\n";
#else
	    public static string NewLine = Environment.NewLine;
#endif

		// will be assigned from the outside on CF
		public static TextWriter Out;

        public static TextWriter Error;
        
		static TestPlatform()
		{
			Out = Console.Out;
            Error = Console.Error;
		}
		
		public static void PrintStackTrace(TextWriter writer, Exception e)
		{	
			writer.Write(e);
		}

        public static TextWriter GetNullWriter()
        {
            return new NullTextWriter();
        }
        
        public static TextWriter GetStdErr()
		{
			return Error;
		}
		
		public static void EmitWarning(string warning)
		{
			Out.WriteLine(warning);
		}		

		public static bool IsStatic(MethodInfo method)
		{
			return method.IsStatic;
		}

		public static bool IsPublic(MethodInfo method)
		{
			return method.IsPublic;
		}

		public static bool HasParameters(MethodInfo method)
		{
			return method.GetParameters().Length > 0;
		}

        public static TextWriter OpenTextFile(string fname)
        {
            return new StreamWriter(fname);
        }
	}
}

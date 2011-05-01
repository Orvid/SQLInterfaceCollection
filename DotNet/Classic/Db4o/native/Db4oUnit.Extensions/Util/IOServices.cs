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
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Db4oUnit.Extensions.Util
{
	public class IOServices
	{
		public static string FindParentDirectory(string path)
		{
#if !CF
			string parent = Path.GetFullPath("..");
			while (true)
			{
				if (Directory.Exists(Path.Combine(parent, path))) return parent;
				string oldParent = parent;
				parent = Path.GetDirectoryName(parent);
				if (parent == oldParent || parent == null) break;
			}
#endif
			return null;
		}

		public static void WriteFile(string fname, string contents)
		{
			CreateParentDirectories(fname);
			using (StreamWriter writer = new StreamWriter(fname))
			{
				writer.Write(contents);
			}
		}

		private static void CreateParentDirectories(string fname)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(fname));
		}

		public static string JoinQuotedArgs(string[] args)
        {
            return JoinQuotedArgs(' ', args);
        }

        public static string JoinQuotedArgs(char separator, params string[] args)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string arg in args)
            {
                if (builder.Length > 0) builder.Append(separator);
                builder.Append(Quote(arg));
            }
            return builder.ToString();
        }

        public static string Quote(string s)
        {
            if (s.StartsWith("\"")) return s;
            return "\"" + s + "\"";
		}

#if !CF && !SILVERLIGHT
		public static string Exec(string program, params string[] arguments)
		{
			return Exec(program, JoinQuotedArgs(arguments));
		}

		private static string Exec(string program, string arguments)
		{
			ProcessStartInfo psi = new ProcessStartInfo(program);
			psi.UseShellExecute = false;
			psi.Arguments = arguments;
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardError = true;
			psi.WorkingDirectory = Path.GetTempPath();
			psi.CreateNoWindow = true;

			Process p = Process.Start(psi);
			string stdout = p.StandardOutput.ReadToEnd();
			string stderr = p.StandardError.ReadToEnd();
			p.WaitForExit();
            if (p.ExitCode != 0) throw new ApplicationException(stdout + stderr);
			return stdout + stderr;
		}

        public static string CopyEnclosingAssemblyTo(Type type, string directory)
		{
			return CopyTo(type.Assembly.Location, directory);
		}

#endif

		public static string BuildTempPath(string fname)
		{
#if SILVERLIGHT
			return "/temp" + DateTime.Now.Ticks + "/" + fname;
#elif !CF && !MONO
			return Path.Combine(Environment.GetEnvironmentVariable("TEMP"), fname);
#else
			return Path.Combine(Path.GetTempPath(), fname);
#endif
		}

		public static string CopyTo(string fname, string targetDirectory)
		{
			Directory.CreateDirectory(targetDirectory);
			string targetFileName = Path.Combine(targetDirectory, Path.GetFileName(fname));
			File.Copy(fname, targetFileName, true);
			return targetFileName;
		}

	}
}

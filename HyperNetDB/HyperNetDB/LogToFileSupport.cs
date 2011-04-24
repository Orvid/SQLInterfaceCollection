#region LICENSE
/*
	HyperNetDatabase: An Single-Tier Database engine for C# .
	Copyright (c) 2004 Manuel Lucas Viñas Livschitz

	This file is part of HyperNetDatabase.

    HyperNetDatabase is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    HyperNetDatabase is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with HyperNetDatabase; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/
#endregion
using System;

namespace LogToFileSupport
{
	/// <summary>
	/// LogToFileSupport.
	/// </summary>
	public class FileLog
	{
		//const string ClassErrCode="HNDLOG";

		private string LogFilename = "hdblog";
		/// <summary>
		/// ctor with file
		/// </summary>
		/// <param name="fname"></param>
		public FileLog(string fname)
		{
			LogFilename=fname;
		}
		/// <summary>
		/// default ctor
		/// </summary>
		public FileLog()
		{
		}		
		#region LogToFile: Saves a string in the log file
		/// <summary>
		/// Saves a string in the log file
		/// </summary>
		/// <param name="message">Message</param>
		/// <param name="subject">Subject</param>
		public void LogToFile(string subject, string message)
		{
			//const string FuncErrCode=ClassErrCode+".0001";
			try
			{
				lock(this)
				{
					if(System.IO.File.Exists(LogFilename+".txt"))
					{
						try
						{
							System.IO.FileInfo fi = new System.IO.FileInfo(""+LogFilename+".txt");
							try
							{
								if(fi.Length>300000)
								{
									try
									{
										System.IO.File.Copy(LogFilename+".txt",LogFilename+".bak",true);
									}
									catch(Exception)
									{
									}
									System.IO.File.Delete(LogFilename+".txt");
								}
							}
							catch(Exception)
							{
							}
						}
						catch(Exception)
						{
						}
					}
					System.IO.TextWriter tw = new System.IO.StreamWriter(""+LogFilename+".txt",true);
					tw.Write("[{0:s}] "+subject+": ",DateTime.Now);
					string[] parts = message.Split('\n');
					if(parts.Length==1)
						tw.WriteLine(parts[0]);
					else
					{
						tw.WriteLine("");
						foreach(string p in parts)
							tw.WriteLine(new string('\t',2)+p);
					}
					tw.Flush();
					tw.Close();
				}
			}
			catch
			{
				//throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
	}
}

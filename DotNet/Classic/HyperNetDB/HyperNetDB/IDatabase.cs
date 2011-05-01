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
using System.IO;
using System.Collections;
using System.Text;
using System.Data;
namespace HyperNetDatabase
{
	/// <summary>
	/// IDatabase.
	/// </summary>
	public interface IDatabase
	{
        /// <summary>
        /// File name
        /// </summary>
		string Filename{ get; }
        /// <summary>
        /// Names of tables
        /// </summary>
        /// <param name="Names"></param>
		void GetTableNames(out string[] Names);
        /// <summary>
        /// A simple select
        /// </summary>
        /// <param name="Fields"></param>
        /// <param name="From_TableName"></param>
        /// <param name="Where_NameCondValue"></param>
        /// <returns></returns>
		DataTable Select(string[] Fields, string From_TableName, object[,] Where_NameCondValue);
	}
}

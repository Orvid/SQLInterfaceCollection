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
using System.Data;
namespace System.Data
{
	/// <summary>
	/// SQLOps. Based on snippets at http://weblogs.sqlteam.com/davidm/
	/// </summary>
	public class SQLOps
	{
		#region Project: Filter fields on DataTable
		/// <summary>
		/// In TSQL, this equates to explicitly naming the column/s we want in a Select clause.	
		/// Our implementation has no renaming ability (The AS keyword in TSQL) but does include 
		/// an option to exclude the columns versus the default of including the columns.
		/// </summary>
		/// <param name="Table"></param>
		/// <param name="Columns"></param>
		/// <param name="Include"></param>
		/// <returns></returns>
		/// <remarks> Create a copy of the table
		/// Prepare array for column names to remove.
		/// Find Columns to remove and add to array.
		/// Loop through array and remove from table
		/// Return Table.
		/// </remarks>
		public static DataTable Project(DataTable Table, DataColumn[] Columns, bool Include)

		{

			DataTable table = Table.Copy();

			table.TableName = "Project";

			int columns_to_remove = Include ? (Table.Columns.Count - Columns.Length) : Columns.Length ;

			string[] columns = new String[columns_to_remove];

			int z = 0;

			for(int i = 0; i < table.Columns.Count; i++)

			{

				string column_name = table.Columns[i].ColumnName;

				bool is_in_list = false;

				for(int x = 0; x < Columns.Length; x++)

				{

					if(column_name == Columns[x].ColumnName)

					{

						is_in_list = true;

						break;

					}                       

				}

				if(is_in_list ^ Include)

					columns[z++] = column_name;

			}

 

			foreach(string s in columns)

			{

				table.Columns.Remove(s);

			}

 

			return table;

 

		}

 
		/// <summary>
		/// In TSQL, this equates to explicitly naming the column/s we want in a Select clause.	
		/// </summary>
		/// <param name="Table"></param>
		/// <param name="Columns"></param>
		/// <returns></returns>
		public static DataTable Project(DataTable Table, DataColumn[] Columns)

		{

			return Project(Table,Columns,true);

		}     

 
		/// <summary>
		/// In TSQL, this equates to explicitly naming the column/s we want in a Select clause.	
		/// </summary>
		/// <param name="Table"></param>
		/// <param name="Columns"></param>
		/// <returns></returns>
		public static DataTable Project(DataTable Table, params     string[] Columns)

		{

			DataColumn[] columns = new DataColumn[Columns.Length];

			for(int i = 0; i < Columns.Length; i++)

			{

				columns[i] = Table.Columns[Columns[i]];

			}

			return Project(Table, columns, true);

		}
		/// <summary>
		/// In TSQL, this equates to explicitly naming the column/s we want in a Select clause.	
		/// </summary>
		/// <param name="Table"></param>
		/// <param name="Include"></param>
		/// <param name="Columns"></param>
		/// <returns></returns>
		public static DataTable Project(DataTable Table, bool Include, params string[] Columns)

		{

			DataColumn[] columns = new DataColumn[Columns.Length];

			for(int i = 0; i < Columns.Length; i++)

			{

				columns[i] = Table.Columns[Columns[i]];

			}

			return Project(Table, columns, Include);

		}
		#endregion
		#region Union: Append 2 tables
		/// <summary>
		///  Append 2 tables
		/// </summary>
		/// <param name="First"></param>
		/// <param name="Second"></param>
		/// <returns></returns>
		public static DataTable Union (DataTable First, DataTable Second)
		{

			//Result table

			DataTable table = new DataTable("Union");

			//Build new columns

			DataColumn[] newcolumns = new DataColumn[First.Columns.Count];

			for(int i=0; i < First.Columns.Count; i++)

			{

				newcolumns[i] = new DataColumn(First.Columns[i].ColumnName, First.Columns[i].DataType);

			}

			//add new columns to result table

			table.Columns.AddRange(newcolumns);

			table.BeginLoadData();

			//Load data from first table

			foreach(DataRow row in First.Rows)

			{

				table.LoadDataRow(row.ItemArray,true);

			}

			//Load data from second table

			foreach(DataRow row in Second.Rows)

			{

				table.LoadDataRow(row.ItemArray,true);

			}

			table.EndLoadData();

			return table;


		}
		#endregion
		#region Product: The Product Method is the equivalent of the CROSS JOIN expression in TSQL.
		/// <summary>
		/// The Product Method is the equivalent of the CROSS JOIN expression in TSQL.
		/// </summary>
		/// <param name="First"></param>
		/// <param name="Second"></param>
		/// <returns></returns>
		/// <remarks>Create new empty table<br>
		/// Add columns from First table to empty table.<br>
		/// Add columns from Secondtable to empty table. Rename if necessary<br>
		/// Loop through First table and for each row loop through Second table and add rows via array manipulation.<br>
		/// Return Table.</remarks>
		public static DataTable Product(DataTable First, DataTable Second)

		{

			DataTable table = new DataTable("Product");

			//Add Columns from First

			for(int i = 0; i < First.Columns.Count; i++)

			{

				table.Columns.Add(new DataColumn(First.Columns[i].ColumnName,First.Columns[i].DataType));

			}

                  

			//Add Columns from Second

			for(int i = 0; i < Second.Columns.Count; i++)

			{

				//Beware Duplicates

				if(!table.Columns.Contains(Second.Columns[i].ColumnName))

					table.Columns.Add(new DataColumn(Second.Columns[i].ColumnName,Second.Columns[i].DataType));

				else

					table.Columns.Add(new DataColumn(Second.Columns[i].ColumnName + "_Second",Second.Columns[i].DataType));

			}

 

			table.BeginLoadData();        

			foreach(DataRow parentrow in First.Rows)

			{

				object[] firstarray = parentrow.ItemArray;

				foreach(DataRow childrow in Second.Rows)

				{

					object[] secondarray = childrow.ItemArray;

					object[] productarray = new object[firstarray.Length+secondarray.Length];

					Array.Copy(firstarray,0,productarray,0,firstarray.Length) ;

					Array.Copy(secondarray,0,productarray,firstarray.Length,secondarray.Length) ;

 

					table.LoadDataRow(productarray,true);

				}

			}

			table.EndLoadData();

      

			return table;

		}

		#endregion
		#region Difference: It is also refered to as MINUS and is simply all the rows that are in the First table but not the Second.
		/// <summary>
		/// It is also refered to as MINUS and is simply all the rows that are in the First table but not the Second.
		/// </summary>
		/// <param name="First"></param>
		/// <param name="Second"></param>
		/// <returns></returns>
		/// <remarks>
		/// Create new empty table<br>
		/// Create a DataSet and add tables.<br>
		/// Get a reference to all columns in both tables<br>
		/// Create a DataRelation<br>
		/// Using the DataRelation add rows with no child rows.<br>
		/// Return table<br>
		/// </remarks>
		public static DataTable Difference(DataTable First, DataTable Second)

		{

			//Create Empty Table

			DataTable table = new DataTable("Difference");

 

			//Must use a Dataset to make use of a DataRelation object

			using(DataSet ds = new DataSet())

			{

				//Add tables

				ds.Tables.AddRange(new DataTable[]{First.Copy(),Second.Copy()});

				//Get Columns for DataRelation

				DataColumn[] firstcolumns  = new DataColumn[ds.Tables[0].Columns.Count];

				for(int i = 0; i < firstcolumns.Length; i++)

				{

					firstcolumns[i] = ds.Tables[0].Columns[i];

				}

 

				DataColumn[] secondcolumns = new DataColumn[ds.Tables[1].Columns.Count];

				for(int i = 0; i < secondcolumns.Length; i++)

				{

					secondcolumns[i] = ds.Tables[1].Columns[i];

				}

				//Create DataRelation

				DataRelation r = new DataRelation(string.Empty,firstcolumns,secondcolumns,false);

				ds.Relations.Add(r);

 

				//Create columns for return table

				for(int i = 0; i < First.Columns.Count; i++)

				{

					table.Columns.Add(First.Columns[i].ColumnName, First.Columns[i].DataType);

				}

 

				//If First Row not in Second, Add to return table.

				table.BeginLoadData();

				foreach(DataRow parentrow in ds.Tables[0].Rows)

				{

					DataRow[] childrows = parentrow.GetChildRows(r);

					if(childrows == null || childrows.Length == 0)

						table.LoadDataRow(parentrow.ItemArray,true);    

				}

				table.EndLoadData();

			}

      

			return table;

		}

		#endregion
		#region Join:
		//FJC = First Join Column
		//SJC = Second Join Column
		/// <summary>
		/// INNER JOIN
		/// </summary>
		/// <param name="First"></param>
		/// <param name="Second"></param>
		/// <param name="FJC"></param>
		/// <param name="SJC"></param>
		/// <returns></returns>
		/// <remarks>
		/// This JOIN method is equivalent to the TSQL INNER JOIN expression using equality.<br>
		/// This method returns all columns from both tables.<br>
		/// Once again, column name collision is avoided by appending "_Second" to the columns affected.<br>
		/// There are a total of 3 signatures for this method.<br>
		/// In summary the code works as follows:<br>
		/// Create new empty table<br>
		/// Create a DataSet and add tables.<br>
		/// Get a reference to Join columns<br>
		/// Create a DataRelation<br>
		/// Construct JOIN table columns<br>
		/// Using the DataRelation add rows with matching related rows using array manipulation<br>
		/// Return table<br>
		/// </remarks>
		public static DataTable Join (DataTable First, DataTable Second, DataColumn[] FJC, DataColumn[] SJC)

		{

			//Create Empty Table

			DataTable table = new DataTable("Join");

 

			// Use a DataSet to leverage DataRelation

			using(DataSet ds = new DataSet())

			{

				//Add Copy of Tables

				ds.Tables.AddRange(new DataTable[]{First.Copy(),Second.Copy()});

 

				//Identify Joining Columns from First

				DataColumn[] parentcolumns  = new DataColumn[FJC.Length];

				for(int i = 0; i < parentcolumns.Length; i++)

				{

					parentcolumns[i] = ds.Tables[0].Columns[FJC[i].ColumnName];

				}

				//Identify Joining Columns from Second

				DataColumn[] childcolumns  = new DataColumn[SJC.Length];

				for(int i = 0; i < childcolumns.Length; i++)

				{

					childcolumns[i] = ds.Tables[1].Columns[SJC[i].ColumnName];

				}

 

				//Create DataRelation

				DataRelation r = new DataRelation(string.Empty,parentcolumns,childcolumns,false);

				ds.Relations.Add(r);

 

				//Create Columns for JOIN table

				for(int i = 0; i < First.Columns.Count; i++)

				{

					table.Columns.Add(First.Columns[i].ColumnName, First.Columns[i].DataType);

				}

				for(int i = 0; i < Second.Columns.Count; i++)

				{

					//Beware Duplicates

					if(!table.Columns.Contains(Second.Columns[i].ColumnName))

						table.Columns.Add(Second.Columns[i].ColumnName, Second.Columns[i].DataType);

					else

						table.Columns.Add(Second.Columns[i].ColumnName + "_Second", Second.Columns[i].DataType);

				}

 

				//Loop through First table

				table.BeginLoadData();

				foreach(DataRow firstrow in ds.Tables[0].Rows)

				{

					//Get "joined" rows

					DataRow[] childrows = firstrow.GetChildRows(r);

					if(childrows != null && childrows.Length > 0)

					{

						object[] parentarray = firstrow.ItemArray;                              

						foreach(DataRow secondrow in childrows)

						{

							object[] secondarray = secondrow.ItemArray;

							object[] joinarray = new object[parentarray.Length+secondarray.Length];

							Array.Copy(parentarray,0,joinarray,0,parentarray.Length);

							Array.Copy(secondarray,0,joinarray,parentarray.Length,secondarray.Length);

							table.LoadDataRow(joinarray,true);

						}

					}

				}

				table.EndLoadData();

			}

 

			return table;

		}

 
		/// <summary>
		/// Join operator
		/// </summary>
		/// <param name="First"></param>
		/// <param name="Second"></param>
		/// <param name="FJC"></param>
		/// <param name="SJC"></param>
		/// <returns></returns>
		public static DataTable Join (DataTable First, DataTable Second, DataColumn FJC, DataColumn SJC)

		{

			return SQLOps.Join(First, Second, new DataColumn[]{FJC}, new DataColumn[]{SJC});

		}
		/// <summary>
		/// Join operator
		/// </summary>
		/// <param name="First"></param>
		/// <param name="Second"></param>
		/// <param name="FJC"></param>
		/// <param name="SJC"></param>
		/// <returns></returns>
		public static DataTable Join (DataTable First, DataTable Second, string FJC, string SJC)

		{

			return SQLOps.Join(First, Second, new DataColumn[]{First.Columns[FJC]}, new DataColumn[]{Second.Columns[SJC]});

		}

		#endregion
		#region Intersect: INTERSECT is simply all rows that are in the First table and the Second table
		/// <summary>
		/// INTERSECT is simply all rows that are in the First table and the Second table
		/// </summary>
		/// <param name="First"></param>
		/// <param name="Second"></param>
		/// <returns></returns>
		/// <remarks>
		/// In summary the code works as follows:<br>
		///	Get a reference to all columns<br>
		///	Join on all columns<br>
		///	Return table<br>
		/// </remarks>
		public static DataTable Intersect(DataTable First, DataTable Second)
		{

			//Get reference to Columns in First

			DataColumn[] firstcolumns  = new DataColumn[First.Columns.Count];

			for(int i = 0; i < firstcolumns.Length; i++)

			{

				firstcolumns[i] = First.Columns[i];

			}

			//Get reference to Columns in Second

			DataColumn[] secondcolumns  = new DataColumn[Second.Columns.Count];

			for(int i = 0; i < secondcolumns.Length; i++)

			{

				secondcolumns[i] = Second.Columns[i];

			}

			//JOIN ON all columns

			DataTable table = SQLOps.Join(First, Second, firstcolumns, secondcolumns);

			table.TableName = "Intersect";

			return table;

		}
		#endregion
		#region Distinct: Removes the equal rows
// In summary the code works as follows:<br>
//	Create new table<br>
//  Add Distinct columns and prepare sort expression<br>
//  Select all sorted rows<br>
//  Loop over rows and check against previous row<br>
//  Add only unique rows<br>
//  Return table<br>
		private static bool RowEqual(object[] Values, object[] OtherValues)

		{

			if(Values == null)

				return false;

 

			for(int i = 0; i < Values.Length; i++)

			{

				if(!Values[i].Equals(OtherValues[i]))

					return false;

			}                       

			return true;

		} 
		/// <summary>
		/// Removes the equal rows
		/// </summary>
		/// <param name="Table"></param>
		/// <param name="Columns"></param>
		/// <returns></returns>
		public static DataTable Distinct(DataTable Table, DataColumn[] Columns)

		{

			//Empty table

			DataTable table = new DataTable("Distinct");

			//Sort variable

			string sort = string.Empty;

 

			//Add Columns & Build Sort expression

			for(int i = 0; i < Columns.Length; i++)

			{

				table.Columns.Add(Columns[i].ColumnName,Columns[i].DataType);

				sort += Columns[i].ColumnName + ",";

			}

			//Select all rows and sort

			DataRow[] sortedrows = Table.Select(string.Empty,sort.Substring(0,sort.Length-1));

      

			object[] currentrow = null;

			object[] previousrow = null;

      

			table.BeginLoadData();

			foreach(DataRow row in sortedrows)

			{

				//Current row

				currentrow = new object[Columns.Length];

				for(int i = 0; i < Columns.Length; i++)

				{

					currentrow[i] = row[Columns[i].ColumnName];

				}

 

				//Match Current row to previous row

				if(!SQLOps.RowEqual(previousrow, currentrow))

					table.LoadDataRow(currentrow,true);

 

				//Previous row

				previousrow = new object[Columns.Length];

				for(int i = 0; i < Columns.Length; i++)

				{

					previousrow[i] = row[Columns[i].ColumnName];

				}

 

			}

			table.EndLoadData();

			return table;

 

		}

 
		/// <summary>
		/// Removes the equal rows
		/// </summary>
		/// <param name="Table"></param>
		/// <param name="Column"></param>
		/// <returns></returns>
		public static DataTable Distinct(DataTable Table, DataColumn Column)

		{

			return Distinct(Table, new DataColumn[]{Column});

		}
		/// <summary>
		/// Removes the equal rows
		/// </summary>
		/// <param name="Table"></param>
		/// <param name="Column"></param>
		/// <returns></returns>
		public static DataTable Distinct(DataTable Table, string Column)

		{

			return Distinct(Table, Table.Columns[Column]);

		}
		/// <summary>
		/// Removes the equal rows
		/// </summary>
		/// <param name="Table"></param>
		/// <param name="Columns"></param>
		/// <returns></returns>
		public static DataTable Distinct(DataTable Table, params string[] Columns)

		{

			DataColumn[] columns = new DataColumn[Columns.Length];

			for(int i = 0; i < Columns.Length; i++)

			{

				columns[i] = Table.Columns[Columns[i]];

      

			}

			return Distinct(Table, columns);

		}
		/// <summary>
		/// Removes the equal rows
		/// </summary>
		/// <param name="Table"></param>
		/// <returns></returns>
		public static DataTable Distinct(DataTable Table)

		{

			DataColumn[] columns = new DataColumn[Table.Columns.Count];

			for(int i = 0; i < Table.Columns.Count; i++)

			{

				columns[i] = Table.Columns[i];

      

			}

			return Distinct(Table, columns);

		}

		#endregion
		#region Divide: Essential the DIVIDE operator is the inverse of Product
		/// <summary>
		/// Essential the DIVIDE operator is the inverse of Product
		/// </summary>
		/// <param name="DEND"></param>
		/// <param name="DOR"></param>
		/// <param name="BY"></param>
		/// <returns></returns>
		public static DataTable Divide(DataTable DEND, DataTable DOR, DataColumn BY)

		{

			//First Create Distinct DEND table projected over BY column

			DataTable distinct = SQLOps.Distinct(DEND, BY);

			//Product of distinct and DOR

			DataTable product = SQLOps.Product(distinct,DOR);

			//Difference of product and DEND

			DataTable difference = SQLOps.Difference(product, DEND);

			//Project over BY column

			difference = SQLOps.Project(difference,new DataColumn[]{difference.Columns[BY.ColumnName]});

			//Difference of distinct AND difference

			DataTable table = SQLOps.Difference(distinct,difference);

      

			table.TableName = "Divide";

			return table;

		}

		#endregion
	}
}

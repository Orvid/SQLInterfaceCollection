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
using System.Collections;
using System.IO;
using System.Data;
/// <summary>Single-Tier Database engine</summary>
namespace HyperNetDatabase.R2
{
	/// <summary>
	/// Calling interface
	/// </summary>
	public interface IHNDBSVR
	{
		#region -Sequences
		/// <summary>
		/// Next sequence value (and autoincrement)
		/// </summary>
		/// <param name="name">Sequence name</param>
        /// <param name="DBID"></param>
		/// <returns></returns>
		long seqNextValue(string DBID, string name);
		#endregion
		#region Insert: Inserts data into a Table
		/// <summary>
		/// Inserts data into a Table
		/// <example>
		/// Example:
		/// <code>
		/// using  HyperNetDatabase.R2;
		/// ...
		/// Database  db  =  new  Database();
		/// db.Open("file.hnd");  //  creates  or  opens  database
		/// ...
		/// string StockName = "peppers";
		/// DataTable  result  =  db.Insert("Stock", new object[,]{ {"NAME",StockName}, {"QTY",0.5m} } );
		/// ...
		/// </code>
		/// Is the same as: INSERT INTO Stock (NAME,QTY) VALUES (@StockName,0.5);
		/// </example>		
		/// <code>
		/// 
		/// </code>
		/// <include file='../XML_DOC.xml' path='REPEATED_COMMENTS/SET_EXPRESSION/*' />
		/// </summary>
		/// <param name="TableName"></param>
		/// <param name="NamesAndValues">SET expression</param>
		void Insert(string DBID, string TableName, object[,] NamesAndValues);
		#endregion
		#region Update: Update query
		/// <summary>
		/// SQL UPDATE query
		/// <example>
		/// Example:
		/// <code>
		/// using  HyperNetDatabase.R2;
		/// ...
		/// Database  db  =  new  Database();
		/// db.Open("file.hnd");  //  creates  or  opens  database
		/// ...
		/// string StockName = "peppers";
		/// DataTable  result  =  db.Update("Stock", 
		/// new object[,]{ {"NAME",StockName}, {"QTY",0.5m} },
		/// new object[,]{ {"NAME","=","pepperoni"} } 
		/// );
		/// ...
		/// </code>
		/// Is the same as: UPDATE Stock SET NAME=@StockName, QTY=0.5 WHERE NAME=pepperoni 
		/// </example>	
		/// <code>
		/// 
		/// </code>
		/// <include file='../XML_DOC.xml' path='REPEATED_COMMENTS/WHERE_SINTAX/*' />	
		/// <code>
		/// 
		/// </code>
		/// <include file='../XML_DOC.xml' path='REPEATED_COMMENTS/SET_EXPRESSION/*' />	
		/// </summary>
		/// <param name="From_TableName"></param>
		/// <param name="Set">SET expression</param>
		/// <param name="Where_NameCondValue">WHERE expresion</param>
		/// <returns></returns>
		void Update(string DBID, string From_TableName, object[,] Set, object[,] Where_NameCondValue);
		#endregion
		#region Delete: Delete query
		/// <summary>
		/// SQL DELETE query
		/// <example>
		/// Example:
		/// <code>
		/// using  HyperNetDatabase.R2;
		/// ...
		/// Database  db  =  new  Database();
		/// db.Open("file.hnd");  //  creates  or  opens  database
		/// ...
		/// string StockName = "peppers";
		/// DataTable  result  =  db.Delete("Stock", 
		/// new object[,]{ {"NAME","=",StockName} } 
		/// );
		/// ...
		/// </code>
		/// Is the same as: DELETE Stock WHERE NAME=@StockName 
		/// </example>		
		/// <code>
		/// 
		/// </code>
		/// <include file='../XML_DOC.xml' path='REPEATED_COMMENTS/WHERE_SINTAX/*' />
		/// </summary>
		/// <param name="From_TableName">Table name</param>
		/// <param name="Where_NameCondValue">WHERE expresion</param>
		/// <returns></returns>
		void Delete(string DBID, string From_TableName, object[,] Where_NameCondValue);
		#endregion
		#region Select: Select query
		/// <summary>
		/// SQL Select query.
		/// <example>
		/// Example:
		/// <code>
		/// using  HyperNetDatabase.R2;
		/// ...
		/// Database  db  =  new  Database();
		/// db.Open("file.hnd");  //  creates  or  opens  database
		/// ...
		/// string StockName = "peppers";
		/// DataTable  result  =  db.Select(null,"Stock", 
		/// new object[,]{ {"NAME","=",StockName} } 
		/// );
		/// ...
		/// result  =  db.Select(new string[]{"NAME"},"Stock", 
		/// new object[,]{ {"NAME","=",StockName} } 
		/// );
		/// ...
		/// </code>
		/// Is the same as: 
		/// <list type="bullet">
		/// <item><description>SELECT * FROM Stock WHERE NAME=@StockName </description></item>
		/// <item><description>SELECT NAME FROM Stock WHERE NAME=@StockName </description></item>
		/// </list>
		/// </example>		
		/// <include file='../XML_DOC.xml' path='REPEATED_COMMENTS/WHERE_SINTAX/*' />
		/// </summary>
		/// <param name="Fields">Array of strings of field names or null (means new string[]{"*"})</param>
		/// <param name="From_TableName">Name of the table</param>
		/// <param name="Where_NameCondValue">WHERE expresion</param>
		/// <returns></returns>
		DataTable Select(string DBID, string[] Fields, string From_TableName, object[,] Where_NameCondValue);
		/// <summary>
		/// The same as Select but uses a faster DataTable class for large datasets.
		/// </summary>
		/// <param name="Fields"></param>
		/// <param name="From_TableName"></param>
		/// <param name="Where_NameCondValue"></param>
		/// <returns></returns>
		DataTable2 Select2(string DBID, string[] Fields, string From_TableName, object[,] Where_NameCondValue);
		#endregion
		#region -GetTableNames: Gets all tables in this database
		/// <summary>
		/// Gets all tables in this database
		/// </summary>
		void GetTableNames(string DBID, out string[] Names);
		#endregion
		#region -ForcedInsert and select
		/// <summary>
		/// Obtains a value and if it not exists obtains it's default value
		/// </summary>
		/// <param name="TableName">Name of the table</param>
		/// <param name="KeyField">Key field in table (where part in Select clause)</param>
		/// <param name="KeyValue">Key value (where part in Select clause)</param>
		/// <param name="ValueField">Value field</param>
		/// <param name="DefValue">Default value if no results where found</param>
		/// <returns></returns>
		object ForcedSelect(string DBID, string TableName, string KeyField, object KeyValue, string ValueField, object DefValue);
		/// <summary>
		/// Obtains a value and if it not exists obtains it's default value
		/// </summary>
		object ForcedSelect(string DBID, string TableName, object[,] Where, string ValueField, object DefValue);
		/// <summary>
		/// More known as Update or Insert (Sets values by a keyfield)
		/// <p>Inserts if the condition does not match or updates if the condition matches.</p>
		/// <example>
		/// <code>
		/// db.ForcedInsert( "Stock", "NAME", "Peppers", "Qty", 0.5m );
		/// </code>
		/// Is the same as:
		/// <code>
		/// SELECT Count(*) FROM STOCK WHERE NAME="Peppers";
		/// if count > 0 then
		///	UPDATE STOCK SET Qty=0.5 WHERE NAME="Peppers";
		///	else
		///	INSERT INTO STOCK (NAME,Qty) VALUES ("Peppers",0.5);
		///	</code>
		/// </example>
		/// </summary>
		/// <param name="TableName">Name of the table</param>
		/// <param name="KeyField">Key field in table (where part in Select clause)</param>
		/// <param name="KeyValue">Key value (where part in Select clause)</param>
		/// <param name="ValueField">Value field</param>
		/// <param name="Value">Default value if no results where found</param>
		/// <returns></returns>
		void ForcedInsert(string DBID, string TableName, string KeyField, object KeyValue, string ValueField, object Value);
		/// <summary>
		/// More known as Update or Insert (Sets values by a keyfield). 
		/// <p>Inserts if the condition does not match or updates if the condition matches.</p>
		/// <example>
		/// <code>
		/// db.ForcedInsert( "Stock", new object[,]{{"NAME", "Peppers"}}, new object[,]{{"Qty",0.5m}} );
		/// </code>
		/// Is the same as:
		/// <code>
		/// SELECT Count(*) FROM STOCK WHERE NAME="Peppers";
		/// if count > 0 then
		///	UPDATE STOCK SET Qty=0.5 WHERE NAME="Peppers";
		///	else
		///	INSERT INTO STOCK (NAME,Qty) VALUES ("Peppers",0.5);
		///	</code>
		/// </example>
		/// </summary>
		/// <param name="TableName">Name of the table</param>
		/// <param name="KeysAndValues">Pairs of keys and values</param>
		/// <param name="FieldAndDefaultValue">Pairs of fields and default values without keyfield</param>
		void ForcedInsert(string DBID, string TableName, object[,] KeysAndValues, object[,] FieldAndDefaultValue);
		#endregion
	}
}

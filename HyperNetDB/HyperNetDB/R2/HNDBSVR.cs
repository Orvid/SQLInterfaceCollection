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
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

/// <summary>Single-Tier Database engine</summary>
namespace HyperNetDatabase.R2
{
	/// <summary>
	/// Database
	/// </summary>
	public class HNDBSVR : System.MarshalByRefObject,IHNDBSVR
	{
		public static Hashtable htDatabasePool = new Hashtable();
		public HNDBSVR()
		{
			
		}
		/// <summary>
		/// Registers an instance of the database
		/// </summary>
		/// <param name="DBID"></param>
		/// <param name="db"></param>
		public static void RegisterDatabase(string DBID, HyperNetDatabase.R2.Database db)
		{
			htDatabasePool[DBID]=db;
		}
		/// <summary>
		/// Initialize remoting server
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		public static bool Startup(int port)
		{
			try
			{
//				Hashtable htParams  = new Hashtable();
//				htParams["port"]=port;
//				htParams["name"]="hndb";
				TcpServerChannel channel = new TcpServerChannel(port);
				
				ChannelServices.RegisterChannel(channel);
				RemotingConfiguration.RegisterWellKnownServiceType(
					typeof(HNDBSVR),
					"HNDBSVR",
					WellKnownObjectMode.SingleCall);
			}
			catch(Exception e)
			{
				return false;
			}
			return true;
		}
		public static TcpClientChannel ClientChannel=null;
		/// <summary>
		/// Client-side call
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		public static IHNDBSVR Connect(string ServerName_or_IP, int port)
		{
			try
			{
				lock(ChannelServices.RegisteredChannels)
				{
					bool bRegister=false;
					foreach(IChannel chan in ChannelServices.RegisteredChannels)
					{
						if(chan.ChannelName=="tcp")
						{
							bRegister=true;
						}
					}
					if(!bRegister)
					{
						try
						{
							TcpClientChannel chan = new TcpClientChannel();
							ChannelServices.RegisterChannel(chan);
						}
						catch
						{
						}
					}
				}

				IHNDBSVR obj = (IHNDBSVR)Activator.GetObject(typeof(IHNDBSVR), 
					"tcp://"+ServerName_or_IP+":"+port.ToString()+"/HNDBSVR");
				return obj;
			}
			catch(Exception e)
			{
				return null;
			}
		}
		#region -Sequences
		/// <summary>
		/// Next sequence value (and autoincrement)
		/// </summary>
		/// <param name="name">Sequence name</param>
		/// <returns></returns>
		public long seqNextValue(string DBID, string name)
		{
			return (htDatabasePool[DBID] as HyperNetDatabase.R2.Database).seqNextValue(name);
		}
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
		public void Insert(string DBID, string TableName, object[,] NamesAndValues)
		{
			(htDatabasePool[DBID] as HyperNetDatabase.R2.Database).Insert(TableName,NamesAndValues);
		}
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
		public void Update(string DBID, string From_TableName, object[,] Set, object[,] Where_NameCondValue)
		{
			(htDatabasePool[DBID] as HyperNetDatabase.R2.Database).Update(From_TableName,Set,Where_NameCondValue);
		}
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
		public void Delete(string DBID, string From_TableName, object[,] Where_NameCondValue)
		{
			(htDatabasePool[DBID] as HyperNetDatabase.R2.Database).Delete(From_TableName,Where_NameCondValue);
		}
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
		public DataTable Select(string DBID, string[] Fields, string From_TableName, object[,] Where_NameCondValue)
		{
			return (htDatabasePool[DBID] as HyperNetDatabase.R2.Database).Select(Fields,From_TableName,Where_NameCondValue);
		}
		/// <summary>
		/// The same as Select but uses a faster DataTable class for large datasets.
		/// </summary>
		/// <param name="Fields"></param>
		/// <param name="From_TableName"></param>
		/// <param name="Where_NameCondValue"></param>
		/// <returns></returns>
		public DataTable2 Select2(string DBID, string[] Fields, string From_TableName, object[,] Where_NameCondValue)
		{
			return (htDatabasePool[DBID] as HyperNetDatabase.R2.Database).Select2(Fields,From_TableName,Where_NameCondValue);	
		}
		#endregion
		#region -GetTableNames: Gets all tables in this database
		/// <summary>
		/// Gets all tables in this database
		/// </summary>
		public void GetTableNames(string DBID, out string[] Names)
		{
			(htDatabasePool[DBID] as HyperNetDatabase.R2.Database).GetTableNames(out Names);
		}
		#endregion
		//const string ClassErrCode="HND_DB";
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
		public object ForcedSelect(string DBID, string TableName, string KeyField, object KeyValue, string ValueField, object DefValue)
		{
			DataTable2 dt = Select2(DBID,new string[]{ValueField},TableName, new object[,]{{KeyField,"=",KeyValue}});
			if(dt.Rows.Length>0)
				return dt.GetValue(0,0);
			else 
				return DefValue;
		}
		/// <summary>
		/// Obtains a value and if it not exists obtains it's default value
		/// </summary>
		public object ForcedSelect(string DBID, string TableName, object[,] Where, string ValueField, object DefValue)
		{
			DataTable2 dt = Select2(DBID,new string[]{ValueField},TableName, Where);
			if(dt.Rows.Length>0)
				return dt.GetValue(0,0);
			else 
				return DefValue;
		}
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
		public void ForcedInsert(string DBID, string TableName, string KeyField, object KeyValue, string ValueField, object Value)
		{
			(htDatabasePool[DBID] as HyperNetDatabase.R2.Database).ForcedInsert( TableName,  KeyField,  KeyValue,  ValueField,  Value);

		}
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
		public void ForcedInsert(string DBID, string TableName, object[,] KeysAndValues, object[,] FieldAndDefaultValue)
		{
			(htDatabasePool[DBID] as HyperNetDatabase.R2.Database).ForcedInsert( TableName, KeysAndValues, FieldAndDefaultValue);
			
		}
		#endregion

	}
}

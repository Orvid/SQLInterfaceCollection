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
using LogToFileSupport;

/// <summary>
/// [R1 is deprecated use R2]
/// </summary>
namespace HyperNetDatabase.R1
{
	/// <summary>
	/// Database
	/// </summary>
	public class Database : FileLog, IDisposable, IDatabase
	{
		private FileStream fslock=null;
		#region QueryCache: Fucntions for Cache
		private int QueryCacheMaxLen = 10;
		private ArrayList QueryCache = new ArrayList(); // dont be affected by flush
		private void QueryCacheDestroy(string From_TableName)
		{
			lock(QueryCache)
			{
				Stack QueryCache2 = new Stack();
				foreach(QueryCacheEntry q in QueryCache)
				{
					if(q.From_TableName==From_TableName)
					{
						QueryCache2.Push(q);
					}
				}
				foreach(QueryCacheEntry q in QueryCache2)
					QueryCache.Remove(q);
			}
		}
		/// <summary>
		/// Called from select when a physical query has been done
		/// </summary>
		/// <param name="Fields"></param>
		/// <param name="From_TableName"></param>
		/// <param name="Where_NameCondValue"></param>
		/// <param name="dt"></param>
		private void QueryCacheSet(string[] Fields, string From_TableName, object[,] Where_NameCondValue,DataTable dt)
		{
			lock(QueryCache)
			{
				if(QueryCache.Count>=QueryCacheMaxLen)
					QueryCache.RemoveAt(0);
				QueryCacheEntry q = new QueryCacheEntry();
				q.Fields=Fields;
				q.From_TableName=From_TableName;
				q.Where_NameCondValue=Where_NameCondValue;
				q.dt=dt;
				QueryCache.Add(q);
			}
		}
		/// <summary>
		/// Called from select statement
		/// </summary>
		/// <param name="Fields"></param>
		/// <param name="From_TableName"></param>
		/// <param name="Where_NameCondValue"></param>
		/// <returns></returns>
		private DataTable QueryCacheGet(string[] Fields, string From_TableName, object[,] Where_NameCondValue)
		{
			lock(QueryCache)
			{
				QueryCacheEntry q = QueryCacheGet2(Fields,From_TableName,Where_NameCondValue);
				if(q!=null)
				{ 
					// Push it to the end to prevent deletion
					QueryCache.Remove(q);
					QueryCache.Add(q);
					return q.dt.Copy(); // return a clone to prevent user modifications
				}
				return null;
			}
		}
		private QueryCacheEntry QueryCacheGet2(string[] Fields, string From_TableName, object[,] Where_NameCondValue)
		{
			lock(QueryCache)
			{
				foreach(QueryCacheEntry q in QueryCache)
				{
					if(q.From_TableName==From_TableName)
					{
						if(q.Fields.Length!=Fields.Length) break;
						for(int f=0;f<q.Fields.Length;f++) if(q.Fields[f]!=Fields[f]) break;
						if(q.Where_NameCondValue.GetLength(0)!=Where_NameCondValue.GetLength(0)) break;
						bool bCancel=false;
						for(int f=0;f<q.Where_NameCondValue.GetLength(0);f++)
							for(int k=0;k<3;k++)
								if(q.Where_NameCondValue[f,k]!=Where_NameCondValue[f,k]) 
								{
									bCancel=true;
									break;
								}
						if(bCancel) break;
						return q;
					}
				}
			}
			return null;
		}
		 class QueryCacheEntry
		{
			public string[] Fields; 
			public string From_TableName; 
			public object[,] Where_NameCondValue;
			public DataTable dt;
		}
		#endregion
		#region [0000] Constructor
		/// <summary>
		/// [R1 is deprecated use R2] Default constructor
		/// </summary>
		/// <param name="fname"></param>
		public Database(string fname/*, byte[] passwd*/)
		{
			const string FuncErrCode=ClassErrCode+".0000";
			try
			{
				if(fname.ToLower().EndsWith(".hdb"))
					fname=fname.Substring(0,fname.Length-4);
				DatabaseFilePath=System.IO.Path.GetFullPath(fname);
				try
				{
					fslock = new FileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+"global.lck",FileMode.Create,FileAccess.Write,FileShare.None);
				}
				catch
				{
					throw new Exception("HyperNetDatabase error: Database in use.");
				}
				
				if(!File.Exists(this.DatabaseFilePath+".hdb"))
				{
					FileStream fs = FileStream2.NewFileStream(this.DatabaseFilePath+".hdb",FileMode.Create,FileAccess.Write,FileShare.None);
					BinaryWriter bw = new BinaryWriter(fs);
//					bw.Write(magic);
//					bw.Write((long)0);
//					bw.Write((long)0);
//					bw.Flush();
					bw.Close();
				}
				else
				{
					FileStream fs = FileStream2.NewFileStream(this.DatabaseFilePath+".hdb",FileMode.Open,FileAccess.Read,FileShare.None);
					BinaryReader br = new BinaryReader(fs);
//					if(magic!=br.ReadInt64())
//					{
//						throw new Exception("HyperNetDatabase error: Corrupted at magic.");
//					}
//					br.ReadInt64(); // seq de campos
//					br.ReadInt64(); // seq de filas
					br.Close();
				}

				// Autoseq table
				this.AddTableIfNotExist(tblSequences);
				this.AddFieldIfNotExist(tblSequences, new Field("SEQNAME","",FieldIndexing.Unique,40));
				this.AddFieldIfNotExist(tblSequences, new Field("SEQVALUE",(long)0,FieldIndexing.None));
				this.AddFieldIfNotExist(tblSequences, new Field("SEQINCREMENT",(long)1,FieldIndexing.None));
				this.AddFieldIfNotExist(tblSequences, new Field("SEQLOOP",false,FieldIndexing.None));
				this.AddFieldIfNotExist(tblSequences, new Field("SEQMAXVALUE",long.MaxValue,FieldIndexing.None));

//				AppDomain.CurrentDomain.ProcessExit+=new EventHandler(CurrentDomain_ProcessExit);

				// Fix tables
				string[] tnames;
				GetTableNames(out tnames);
				foreach(string tname in tnames)
				{
					CheckTable(tname,true);
				}
				Flush();
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion 
		#region Sequences
		/// <summary>
		/// Sequences table name
		/// </summary>
		internal const string tblSequences="$Sequences";
		/// <summary>
		/// [R1 is deprecated use R2] Creates a sequence.
		/// </summary>
		/// <param name="name">Sequence name</param>
		/// <param name="initial_value">Initial value</param>
		/// <param name="increment">Increment</param>
		/// <param name="loop">It loops?</param>
		/// <param name="maxval">Max value for looping (modulus)</param>
		public void seqCreate(string name, long initial_value, long increment, bool loop, long maxval)
		{
			const string FuncErrCode=ClassErrCode+".0022";
			try
			{
				this.Insert(tblSequences, new object[,]{ {"SEQNAME",name}, {"SEQVALUE",initial_value}, {"SEQINCREMENT",increment}, {"SEQLOOP",loop}, {"SEQMAXVALUE",maxval}  });
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		/// <summary>
		/// [R1 is deprecated use R2] Creates a sequence, non looping.
		/// </summary>
		/// <param name="name">Sequence name</param>
		/// <param name="initial_value">Initial value</param>
		/// <param name="increment">Increment</param>
		public void seqCreate(string name, long initial_value, long increment)
		{
			const string FuncErrCode=ClassErrCode+".0025";
			try
			{
				this.Insert(tblSequences, new object[,]{ {"SEQNAME",name}, {"SEQVALUE",initial_value}, {"SEQINCREMENT",increment}  });
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		/// <summary>
		/// [R1 is deprecated use R2] Creates a sequence, non looping, increment by one.
		/// </summary>
		/// <param name="name">Sequence name</param>
		/// <param name="initial_value">Initial value</param>
		public void seqCreate(string name, long initial_value)
		{
			const string FuncErrCode=ClassErrCode+".0026";
			try
			{
				this.Insert(tblSequences, new object[,]{ {"SEQNAME",name}, {"SEQVALUE",initial_value}  });
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		/// <summary>
		/// [R1 is deprecated use R2] Creates a sequence, non looping, increment by one, initial value 0.
		/// </summary>
		/// <param name="name">Sequence name</param>
		public void seqCreate(string name)
		{
			const string FuncErrCode=ClassErrCode+".0027";
			try
			{
				this.Insert(tblSequences, new object[,]{ {"SEQNAME",name}  });
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		/// <summary>
		/// [R1 is deprecated use R2] Next sequence value (and autoincrement)
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public long seqNextValue(string name)
		{
			const string FuncErrCode=ClassErrCode+".0023";
			try
			{
				lock(this.TableBlocking)
				{
					DataTable dt = this.Select( new string[]{"*"}, tblSequences, new object[,]{{ "SEQNAME","=",name}});
					if(dt.Rows.Count==0)
						throw new Exception("Sequence not found.");
					DataRow dr = dt.Rows[0];
					long newVal = (long)dr["SEQVALUE"] + (long)dr["SEQINCREMENT"];
					if( (bool)dr["SEQLOOP"] )
					{
						if( newVal >= (long)dr["SEQMAXVALUE"] )
						{
							if( (bool)dr["SEQLOOP"] )
								newVal = newVal % (long)dr["SEQMAXVALUE"];
							else
								throw new Exception("Sequence overflow.");
						}
					}
					this.Update( tblSequences, new object[,]{{"SEQVALUE",newVal}}, new object[,]{{ "SEQNAME","=",name}});
					return newVal;
				}
				
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		/// <summary>
		/// [R1 is deprecated use R2] Current sequence value
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public long seqCurrentValue(string name)
		{
			const string FuncErrCode=ClassErrCode+".0028";
			try
			{
				DataTable dt = this.Select( new string[]{"*"}, tblSequences, new object[,]{{ "SEQNAME","=",name}});
				if(dt.Rows.Count==0)
					throw new Exception("Sequence not found.");
				DataRow dr = dt.Rows[0];
				long Val = (long)dr["SEQVALUE"];
				return Val;
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		/// <summary>
		/// [R1 is deprecated use R2] Sequence drop
		/// </summary>
		/// <param name="name"></param>
		public void seqDrop(string name)
		{
			const string FuncErrCode=ClassErrCode+".0024";
			try
			{
				DataTable dt = this.Select( new string[]{"*"}, tblSequences, new object[,]{{ "SEQNAME","=",name}});
				if(dt.Rows.Count>0)
				{
					this.Delete( tblSequences, new object[,]{{ "SEQNAME","=",name}});
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		/// <summary>
		/// [R1 is deprecated use R2] Sequence exists?
		/// </summary>
		/// <param name="name"></param>
		public bool seqExists(string name)
		{
			const string FuncErrCode=ClassErrCode+".0029";
			try
			{
				DataTable dt = this.Select( new string[]{"*"}, tblSequences, new object[,]{{ "SEQNAME","=",name}});
				if(dt.Rows.Count>0)
					return true;
				return false;
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0002] Insert: Inserts data into a Table
		/// <summary>
		/// [R1 is deprecated use R2] Inserts data into a Table
		/// </summary>
		/// <param name="TableName"></param>
		/// <param name="NamesAndValues"></param>
		public void Insert(string TableName, object[,] NamesAndValues)
		{
			const string FuncErrCode=ClassErrCode+".0002";
			try
			{
				lock(this.TableBlocking)
				{
					if(!this.TableBlocking.Contains(TableName))
						this.TableBlocking[TableName] = new SortedList();// index cache
				}
				lock(this.TableBlocking[TableName])
				{
					CheckTable(TableName,false);
					Field[] flds = GetFields(TableName);
					QueryCacheDestroy(TableName);

					// Columns not existent
					for(int n=0;n<NamesAndValues.GetLength(0);n++)
					{
						bool bFound=false;
						foreach(Field f in flds)
						{
							if(f.Name==NamesAndValues[n,0].ToString())
							{
								bFound=true;
								break;
							}
						}
						if(!bFound)
							throw new Exception("Column "+NamesAndValues[n,0].ToString()+" unrecognized");
					}

					// Put defaults
					Variant[] data = new Variant[flds.Length];
					for(int m=0;m<flds.Length;m++)
					{
						bool bFound=false;
						for(int n=0;n<NamesAndValues.GetLength(0);n++)
						{
							if(flds[m].Name==NamesAndValues[n,0].ToString())
							{
								try
								{
									data[m]=Variant.Object2Variant(NamesAndValues[n,1],flds[m].type);
								}
								catch(Exception ex)
								{
									throw new Exception(ex.Message+". While converting value of type '"+NamesAndValues[n,1].GetType().ToString()+"' to field: '"+flds[m].Name+"' with type: '"+flds[m].type.ToString()+"'.");
								}
								bFound=true;
								break;
							}
						}
						if(!bFound)
							data[m]=flds[m].DefaultValue;
					}

					// Check constraints
					for(int m=0;m<flds.Length;m++)
					{
						if(flds[m].bIndexed&&flds[m].bUnique)
						{
							Index ndx = this.GetIndex(TableName,flds[m],false);
							if(ndx.ht.ContainsKey(data[m].obj))
								throw new Exception("Insert violates '"+flds[m].Name+"' field for table '"+TableName+"'.");
						}
					}

					// Deleted field set to deleted to prevent errors
					data[0]=Variant.Object2Variant(true);

					// Look for a hole
					long index;
					if(true)
					{
						HFI hfi = HFI.Read(TableName,DatabaseFilePath);
						index=hfi.rowseq;
						Index ndx = GetIndex(TableName,flds[0],false);
						if(ndx.ht[true]!=null)
						{
							ArrayList al = (ndx.ht[true] as ArrayList);
							if(al.Count>0)
							{
								index = (long)al[al.Count-1];
								al.RemoveAt(al.Count-1);
								this.SetDirtyIndexInDisc(TableName,flds[0]);
								if(ndx.ht[false]==null)
									ndx.ht[false]=new ArrayList();
								(ndx.ht[false] as ArrayList).Add(index);
							}
							else
							{// deleted row list is empty
								hfi.rowseq++;
								hfi.Write(TableName,DatabaseFilePath);
								if(ndx.ht[false]==null)
									ndx.ht[false]=new ArrayList();
								(ndx.ht[false] as ArrayList).Add(index);
							}
						}
						else
						{// deleted row list is empty
							hfi.rowseq++;
							hfi.Write(TableName,DatabaseFilePath);
							if(ndx.ht[false]==null)
								ndx.ht[false]=new ArrayList();
							(ndx.ht[false] as ArrayList).Add(index);
						}
					}
					

					// Data insertion
					for(int m=0;m<flds.Length;m++)
					{
						FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+TableName+".hc"+flds[m].seq.ToString(),FileMode.Open,FileAccess.Write,FileShare.None);
						BinaryWriter bw = new BinaryWriter(fs);
						bw.Write((bool)true);
						bw.Flush();
						fs.Position=1+flds[m].DataSize()*index;
						flds[m].WriteData(this,bw,data[m],true);
						bw.Flush();
						fs.Position=0;
						bw.Write((bool)false);
						bw.Flush();
						bw.Close();
						// Insert in index (differs from delete flag, because delete flag needs an update index)
						if((m>0)&&(flds[m].bIndexed))
						{
							Index ndx = GetIndex(TableName,flds[m],false);
							object key = data[m].obj;
							if(flds[m].bUnique)
							{
								ndx.ht[key]=index;
							}
							else
							{
								if(ndx.ht[key]==null)
									ndx.ht[key] = new ArrayList();
								(ndx.ht[key] as ArrayList).Add(index);
							}
							this.SetDirtyIndexInDisc(TableName,flds[m]);
						}
					}

					// Deleted field set to not deleted - In The index is marked as false
					data[0]=Variant.Object2Variant(false);
					// Data insertion
					if(true)
					{
						FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+TableName+".hc"+flds[0].seq.ToString(),FileMode.Open,FileAccess.Write,FileShare.None);
						BinaryWriter bw = new BinaryWriter(fs);
						bw.Write((bool)true);
						bw.Flush();
						fs.Position=1+flds[0].DataSize()*index;
						flds[0].WriteData(this,bw,data[0],true);
						bw.Flush();
						fs.Position=0;
						bw.Write((bool)false);
						bw.Flush();
						bw.Close();
					}
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0001] Update: Update query
		/// <summary>
		/// [R1 is deprecated use R2] Update query
		/// </summary>
		/// <param name="From_TableName"></param>
		/// <param name="Set"></param>
		/// <param name="Where_NameCondValue"></param>
		/// <returns></returns>
		public void Update(string From_TableName, object[,] Set, object[,] Where_NameCondValue)
		{
			const string FuncErrCode=ClassErrCode+".0001";
			try
			{
				lock(this.TableBlocking)
				{
					if(!this.TableBlocking.Contains(From_TableName))
						this.TableBlocking[From_TableName] = new SortedList();// index cache
				}
				lock(this.TableBlocking[From_TableName])
				{

					string TableName=From_TableName;
					QueryCacheDestroy(TableName);
					CheckTable(TableName,false);

					// Build table fields without deletion field
					Hashtable htTableFields = new Hashtable(); 
					if(true)
					{
						Field[] flds = GetFields(TableName);
						foreach(Field f in flds) if(f.Name!=Database.DeletedFieldName) htTableFields[f.Name]=f;
					}

					// Check 'Set'
					Hashtable SetFields = new Hashtable();
					for(int n=0;n<Set.GetLength(0);n++)
					{
						string fname = Set[n,0].ToString();
						if(!htTableFields.ContainsKey(fname))
							throw new Exception(FuncErrCode+": Field '"+fname+"' do not exist in this table.");
						if(SetFields.ContainsKey(fname))
							throw new Exception(FuncErrCode+": Field '"+fname+"' is repeated in the set clause of this update command.");
						Field f = (htTableFields[fname] as Field);
						object val = Set[n,1];
						Variant v = Variant.Object2Variant( val, f.type );
						object obj = v.obj;
						SetFields[fname]=obj;
					}

					// Check constraints
					foreach(string fname in SetFields.Keys)
					{ 
						Field f = htTableFields[fname] as Field;
						if(f.bIndexed&&f.bUnique)
						{
							Index ndx = this.GetIndex(TableName,f,false);
							if(ndx.ht.ContainsKey( SetFields[fname] ))
								throw new Exception(FuncErrCode+": Insert violates '"+f.Name+"' field for table '"+TableName+"'.");
						}
					}

					// Get the rowids
					ArrayList rowids;
					ExecuteWhere(From_TableName,Where_NameCondValue,out rowids);

					// Set fields
					if(rowids==null) throw new Exception(FuncErrCode+": Where condition returned null rows.");
					
					// Data set
					//for(int m=1;m<flds.Length;m++)
					foreach(string name in SetFields.Keys)
					{	
						Field f = htTableFields[name] as Field;
						FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+TableName+".hc"+f.seq.ToString(),FileMode.Open,FileAccess.ReadWrite,FileShare.None);
						BinaryWriter bw = new BinaryWriter(fs);
						BinaryReader br = new BinaryReader(fs);
						bw.Write((bool)true);
						foreach(long rowid in rowids)
						{
							fs.Position=1+f.DataSize()*rowid;
							object oldkey = f.ReadData(this,br);
							fs.Position=1+f.DataSize()*rowid;
							Variant v = Variant.Object2Variant( SetFields[f.Name], f.type);
							f.WriteData(this,bw,v,true);
							if(f.bIndexed)
							{
								Index ndx = GetIndex(TableName,f,false);
								object key = SetFields[f.Name];
								if((key as IComparable).CompareTo(oldkey as IComparable)!=0)
								{
									if(f.bUnique)
									{
										ndx.ht.Remove(oldkey);
										ndx.ht[key]=rowid;
									}
									else
									{
										if(ndx.ht[oldkey]==null)
										{
											ndx.ht[oldkey] = new ArrayList(); // Avoid throwing an exception
											this.LogToFile(FuncErrCode+": Warning","[Fixed] Indexed data not detected in index.");
										}
										else
											(ndx.ht[oldkey] as ArrayList).Remove(rowid);
										if(ndx.ht[key]==null)
											ndx.ht[key] = new ArrayList();
										(ndx.ht[key] as ArrayList).Add(rowid);
									}
								}
							}
						}
						if(f.bIndexed)
							this.SetDirtyIndexInDisc(TableName,f);
						fs.Position=0;
						bw.Write((bool)false);
						fs.Flush();
						fs.Close();
					}

					
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0003] Delete: Delete query
		/// <summary>
		/// [R1 is deprecated use R2] Delete query
		/// </summary>
		/// <param name="From_TableName"></param>
		/// <param name="Where_NameCondValue"></param>
		/// <returns></returns>
		public void Delete(string From_TableName, object[,] Where_NameCondValue)
		{
			const string FuncErrCode=ClassErrCode+".0003";
			try
			{
				lock(this.TableBlocking)
				{
					if(!this.TableBlocking.Contains(From_TableName))
						this.TableBlocking[From_TableName] = new SortedList();// index cache
				}
				lock(this.TableBlocking[From_TableName])
				{
					string TableName=From_TableName;
					QueryCacheDestroy(TableName);
					CheckTable(TableName,false);
					Field[] flds = GetFields(TableName);

					// Get the rowids
					ArrayList rowids;
					ExecuteWhere(From_TableName,Where_NameCondValue,out rowids);

					// Delete fields
					if(rowids!=null)
					{
						BinaryWriter br0 = null;
						try
						{
							FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+TableName+".hc"+flds[0].seq.ToString(),FileMode.Open,FileAccess.Write,FileShare.None);
							br0 = new BinaryWriter(fs);
							if(rowids.Count>0)
								this.SetDirtyIndexInDisc(TableName,flds[0]);
							foreach(long rowid in rowids)
							{
								br0.BaseStream.Position=1+flds[0].DataSize()*rowid;
								br0.Write((bool)true);
								br0.Flush();
							}
						}
						finally
						{
							if(br0!=null)
							{
								br0.Flush();
								br0.Close();
							}
						}
					
						// Update in memory indexes
						Index ndx = this.GetIndex(TableName,flds[0],false);
						foreach(long rowid in rowids)
						{
							if(ndx.ht[true]==null) ndx.ht[true]= new ArrayList();
							if(ndx.ht[false]==null) ndx.ht[false]= new ArrayList();
							ArrayList deleted = ndx.ht[true] as ArrayList;
							ArrayList notdeleted = ndx.ht[false] as ArrayList;
							notdeleted.Remove(rowid);
							deleted.Add(rowid);
						}
						for(int n=1;n<flds.Length;n++)
						{
							if(flds[n].bIndexed)
							{
								this.SetDirtyIndexInDisc(TableName,flds[n]);
								if(flds[n].bUnique)
								{// Para claves únicas
									ndx = this.GetIndex(TableName,flds[n],false);
									foreach(long rowid in rowids)
									{
										int i = ndx.ht.IndexOfValue(rowid);
										ndx.ht.RemoveAt(i);
									}
								}
								else
								{// Para claves no únicas
									FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+TableName+".hc"+flds[n].seq.ToString(),FileMode.Open,FileAccess.Read,FileShare.None);
									BinaryReader br = new BinaryReader(fs);
									ndx = this.GetIndex(TableName,flds[n],false);
									foreach(long rowid in rowids)
									{
										fs.Position=1+flds[n].DataSize()*rowid;
										object key = flds[n].ReadData(this,br);
										if(ndx.ht[key]!=null)
										{
											ArrayList slots = ndx.ht[key] as ArrayList;
											slots.Remove(rowid);
										}
									}
									br.Close();
								}
							}
						}
					}
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0004] Select: Select query
		/// <summary>
		/// [R1 is deprecated use R2] Select query
		/// </summary>
		/// <param name="Fields"></param>
		/// <param name="From_TableName"></param>
		/// <param name="Where_NameCondValue"></param>
		/// <returns></returns>
		public DataTable Select(string[] Fields, string From_TableName, object[,] Where_NameCondValue)
		{
			const string FuncErrCode=ClassErrCode+".0004";
			try
			{
				if(Fields==null) Fields = new string[]{"*"};
				if(Where_NameCondValue==null) Where_NameCondValue = new object[0,0];
				lock(this.TableBlocking)
				{
					if(!this.TableBlocking.Contains(From_TableName))
						this.TableBlocking[From_TableName] = new SortedList();// index cache
				}
				lock(this.TableBlocking[From_TableName])
				{
					string TableName=From_TableName;
					//CheckTable(TableName,false);
					Field[] flds = GetFields(TableName);

					// map of fields in table
					Hashtable FieldMap = new Hashtable();
					foreach(Field f in flds)
					{
						FieldMap[f.Name]=f;
					}

					// Columns not existent
					bool Asterisk=false;
					ArrayList SelectedFields = new ArrayList();
					for(int n=0;n<Fields.Length;n++)
					{
						bool bFound=false;
						if(Fields[n]=="*")
						{
							Asterisk=true;
							break;
						}
						foreach(Field f in flds)
						{
							if(f.Name==Fields[n])
							{
								bFound=true;
								SelectedFields.Add(f);
								break;
							}
						}
						if(!bFound)
							throw new Exception("Column "+Fields[n]+" unrecognized");
					}
					if(Asterisk)
					{
						SelectedFields = new ArrayList(flds);
					}
					
					// Consult cache
					DataTable cacheTable = QueryCacheGet(Fields,From_TableName,Where_NameCondValue);
					if(cacheTable!=null) return cacheTable;

//					// Remove deletion field
//					if(SelectedFields.Contains(DeletedFieldName))
//						SelectedFields.Remove(DeletedFieldName);

					ArrayList rowids;
					ExecuteWhere(From_TableName,Where_NameCondValue,out rowids);

					// Build datatable	
					ICollection dataTableFields;
					if(Asterisk)
					{
						ArrayList al = new ArrayList(flds);
						al.RemoveAt(0);// delete field
						dataTableFields=al;
					}
					else
					{
						dataTableFields=SelectedFields;
					}

					DataTable dt = new DataTable(TableName);
					foreach(Field f in dataTableFields)
					{
						System.Type t=null;
						switch(f.type)
						{
							case FieldType.ftBoolean:
								t=typeof(bool);
								break;
							case FieldType.ftDecimal:
								t=typeof(decimal);
								break;
							case FieldType.ftDateTime:
								t=typeof(DateTime);
								break;
							case FieldType.ftInt32:
								t=typeof(int);
								break;
							case FieldType.ftInt64:
								t=typeof(long);
								break;
							case FieldType.ftString: 
								t=typeof(string);
								break;
						}
						dt.Columns.Add(f.Name,t);
					}

					if(rowids!=null)
					{
						BinaryReader[] br = new BinaryReader[dataTableFields.Count];
						try
						{
							int p=0;
							foreach(Field f in dataTableFields)
							{
								FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+TableName+".hc"+f.seq.ToString(),FileMode.Open,FileAccess.Read,FileShare.None);
								br[p++] = new BinaryReader(fs);
							}
							dt.BeginLoadData();
							foreach(long rowid in rowids)
							{
								object[] values = new object[dataTableFields.Count];
								p=0;
								foreach(Field f in dataTableFields)
								{
									br[p].BaseStream.Position=1+f.DataSize()*rowid;
									values[p]=f.ReadData(this,br[p]);
									p++;
								}
								dt.LoadDataRow(values,true);
							}
							dt.EndLoadData();
						}
						finally
						{
							for(int p=0;p<dataTableFields.Count;p++)
							{
								if(br[p]!=null)
									br[p].Close();
							}
						}
					}
					QueryCacheSet(Fields,From_TableName,Where_NameCondValue,dt);
					return dt;
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		class WhereComp : IComparer
		{
			#region IComparer Members

			public int Compare(object x, object y)
			{
				int xx = (int)((x as object[])[3]);
				int yy = (int)((y as object[])[3]);
				if(xx<yy) return -1;
				else if (xx>yy) return 1;
				return 0;
			}

			#endregion
		}
		#endregion
		#region Filename
		/// <summary>
		/// [R1 is deprecated use R2] 
		/// </summary>
		internal string DatabaseFilePath=null;
		/// <summary>
		/// [R1 is deprecated use R2] Filename of the database
		/// </summary>
		public string Filename
		{
			get
			{
				return DatabaseFilePath;
			}
		}
		#endregion
		const long magic = 0x0332299;
		#region [0005] GetTableNames: Gets all tables in this database
		/// <summary>
		/// [R1 is deprecated use R2] Gets all tables in this database
		/// </summary>
		public void GetTableNames(out string[] Names)
		{
			const string FuncErrCode=ClassErrCode+".0005";
			try
			{
				lock(this.TableBlocking)
				{
					string fp=System.IO.Path.GetDirectoryName(DatabaseFilePath);
					string[] names2 = Directory.GetFiles(fp,"*.hfi");
					Names = new string[names2.Length];
					for(int n=0;n<Names.Length;n++)
					{
						Names[n] = System.IO.Path.GetFileNameWithoutExtension(names2[n]);
					}
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0006] Flush: Stores all data in database and clear local buffers.
		/// <summary>
		/// [R1 is deprecated use R2] Stores all data in database and clear local buffers.
		/// </summary>
		public void Flush()
		{
			const string FuncErrCode=ClassErrCode+".0006";
			try
			{
				lock(this.TableBlocking)
				{
					foreach(string table in TableBlocking.Keys)
					{
						CheckTable(table,true);
						(TableBlocking[table] as SortedList).Clear();
					}
					//TableBlocking.Clear();
					FieldsCache.Clear();
					
					ArrayList fhce = new ArrayList();
					foreach(FileHandlingCacheEntry e in FileStream2.FileHandlingCache)
					{
						if(e.uses==0)
						{
							fhce.Add(e);
						}
					}
					foreach(FileHandlingCacheEntry e in fhce)
						FileStream2.CloseHandle(e);

					if(FileStream2.FileHandlingCache.Count!=0)
						throw new Exception("Flush detected leaks!.");
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0007] dtor
		/// <summary>
		/// Destructor
		/// </summary>
		~Database()
		{
			const string FuncErrCode=ClassErrCode+".0007";
			try
			{
				if(fslock!=null)
				{
					fslock.Close();
					fslock=null;
				}
				Flush();
				QueryCache=null;
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		/// <summary>
		/// [R1 is deprecated use R2] 
		/// </summary>
		internal const string DeletedFieldName = "$Deleted";
		#region [0008] AddTable
		/// <summary>
		/// Adds a table
		/// </summary>
		public void AddTable(string Name)
		{
			const string FuncErrCode=ClassErrCode+".0008";
			try
			{
				lock(this.TableBlocking)
				{
					string[] tblnames;
					GetTableNames(out tblnames);
					if(new ArrayList(tblnames).Contains(Name))
						throw new Exception("HyperNetDatabase error: Table already present.");
					string tbl = Name;//Path.GetDirectoryName(DatabaseFilePath)+"\\"+Name;
					if(true)// Fat0
					{
						FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+tbl+".hf0",FileMode.Create,FileAccess.Write,FileShare.None);
						BinaryWriter bw = new BinaryWriter(fs);
						bw.Write(magic);
						bw.Write((long)0); // zero fields
						bw.Write((long)0); // FieldSeq number
						bw.Flush();
						bw.Close();
					}
					// Create field defs file (HyperFieldIndex)
					new HFI().Write(tbl,DatabaseFilePath);

					Field f = new Field(DeletedFieldName,true,FieldIndexing.IndexedNotUnique);
					AddField(tbl,f);
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0009] DropTable
		/// <summary>
		/// [R1 is deprecated use R2] Removes a table
		/// </summary>
		public void DropTable(string Name)
		{
			const string FuncErrCode=ClassErrCode+".0009";
			try
			{
				lock(this.TableBlocking)
				{
					this.Flush();
					string[] files = Directory.GetFiles( System.IO.Path.GetDirectoryName(DatabaseFilePath), Name+".h*");
					foreach(string f in files)
					{
						FileStream2.CloseHandle(f);
						File.Delete( f );
					}
					QueryCacheDestroy(Name);
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0030] GetTableLock
		/// <summary>
		/// [R1 is deprecated use R2] Gets a table lock for lock{} statements
		/// </summary>
		/// <param name="TableName"></param>
		/// <returns></returns>
		public object GetTableLock(string TableName)
		{
			const string FuncErrCode=ClassErrCode+".0030";
			try
			{
				lock(this.TableBlocking)
				{
					string[] tblnames;
					GetTableNames(out tblnames);
					if(!new ArrayList(tblnames).Contains(TableName))
						throw new Exception("HyperNetDatabase error: Table not present.");

					// set entry
					if(!this.TableBlocking.Contains(TableName))
						this.TableBlocking[TableName] = new SortedList();
					return this.TableBlocking[TableName];
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0010] AddField
		/// <summary>
		/// [R1 is deprecated use R2] Adds a table
		/// </summary>
		public void AddField(string TableName, Field f)
		{
			const string FuncErrCode=ClassErrCode+".0010";
			try
			{
				lock(this.TableBlocking)
				{
					string[] tblnames;
					GetTableNames(out tblnames);
					if(!new ArrayList(tblnames).Contains(TableName))
						throw new Exception("HyperNetDatabase error: Table not present.");

					// set entry
					if(!this.TableBlocking.Contains(TableName))
						this.TableBlocking[TableName] = new SortedList();
				}
				lock(this.TableBlocking[TableName])
				{
					string tbl = TableName;
					QueryCacheDestroy(TableName);
					Field[] flds = GetFields(TableName);
					foreach(Field i in flds)
					{
						if(f.Name==i.Name)
							throw new Exception("HyperNetDatabase error: Column already present.");
					}
					FieldsCache[TableName]=null; // cancel Field Cache
					long fatid,rownum,fseq;
					HFI hfi = HFI.Read(tbl,DatabaseFilePath);
					fatid = hfi.fatid;
					
					rownum = hfi.rowseq;

					// gets new field seq
					fseq = hfi.fieldseq;
					hfi.fieldseq++;
					hfi.Write(tbl,DatabaseFilePath); 

					fatid = (fatid + 1) % 2;

					if(true)// Fat
					{
						// Create fat file
						FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+tbl+".hf"+fatid.ToString(),FileMode.Create,FileAccess.Write,FileShare.None);
						BinaryWriter bw = new BinaryWriter(fs,System.Text.Encoding.UTF8);
						bw.Write(magic);
						bw.Write((long)(flds.Length+1));
	 
						for(int n=0;n<flds.Length;n++)
							flds[n].Write(bw);
						f.seq=fseq;
						f.Write(bw);
						bw.Flush();
						bw.Close();
						

//						try
//						{
//
//							string fileName = System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+tbl+".hf"+((fatid + 1) % 2).ToString();
////							FileStream2.CloseHandle(fileName);
//							File.Delete(fileName);
//						}
//						catch
//						{
//						}

						// Create column data
						fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+tbl+".hc"+fseq.ToString(),FileMode.Create,FileAccess.Write,FileShare.None);
						bw = new BinaryWriter(fs,System.Text.Encoding.UTF8);
						bw.Write((bool)true); // edition flag
						bw.Flush();
						for(int n=0;n<rownum;n++)
						{
							f.WriteDefaultData(bw,false);
						}
						bw.Flush();
						bw.BaseStream.Position=0;
						bw.Write((bool)false); // edition flag
						bw.Flush();
						bw.Close();

						// Change fat pointer
						hfi.fatid=fatid;
						hfi.Write(tbl,DatabaseFilePath);
					}

				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		SortedList FieldsCache = new SortedList();
		#region [0011] GetFields: Get fieldnames of a table
		/// <summary>
		/// [R1 is deprecated use R2] Fields for users
		/// </summary>
		/// <param name="Name"></param>
		/// <returns></returns>
		public Field[] GetUserFields(string Name)
		{
			Field[] fs = GetFields(Name);
			ArrayList al = new ArrayList();
			foreach(Field f in fs)
			{
				if(f.Name!=DeletedFieldName)
					al.Add(f);
			}
			return (Field[])al.ToArray(typeof(Field));
		}
		/// <summary>
		/// [R1 is deprecated use R2] Get fieldnames of a table
		/// </summary>
		internal Field[] GetFields(string Name)
		{
			const string FuncErrCode=ClassErrCode+".0011";
			try
			{
				lock(this.TableBlocking)
				{
					if(FieldsCache[Name]==null)
					{
						string[] tblnames;
						GetTableNames(out tblnames);
						if(!new ArrayList(tblnames).Contains(Name))
							throw new Exception("HyperNetDatabase error: Table not present.");
						string tbl = Name;//Path.GetDirectoryName(DatabaseFilePath)+"\\"+Name;
						long fatid;
						if(true)
						{
							FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+tbl+".hfi",FileMode.Open,FileAccess.Read,FileShare.None);
							BinaryReader br = new BinaryReader(fs);
							if(br.ReadInt64()!=magic)
								throw new Exception("HyperNetDatabase error: Magic failed.");
							fatid = br.ReadInt64();
							br.Close();
						}
						if(true)
						{
							FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+tbl+".hf"+fatid.ToString(),FileMode.Open,FileAccess.Read,FileShare.None);
							BinaryReader br = new BinaryReader(fs,System.Text.Encoding.UTF8);
							br.ReadInt64(); // read magic
							long fields = br.ReadInt64();
							Field[] rv = new Field[fields];
							for(int n=0;n<fields;n++)
							{
								rv[n]=new Field();
								rv[n].Read(br);
							}
							br.Close();
							FieldsCache[Name]=rv;
							return rv;
						}
					}
					else
					{
						return (Field[])FieldsCache[Name];
					}
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		internal SortedList TableBlocking = new SortedList();
//		/// <summary>
//		/// indexfname as set 
//		/// </summary>
//		internal SortedList Table2DeletedIndex = new SortedList();
		#region [HND003] HFI: Información de la tabla
		/// <summary>
		/// Información de la tabla
		/// </summary>
		internal class HFI
		{
			const string ClassErrCode="HND003";
			internal long fatid=0;
			internal bool IndexingDirty=false;
			internal long rowseq=0;
			internal long fieldseq=0;
			internal bool encrypted=false;
			#region [0001] Read
			/// <summary>
			/// Read table info
			/// </summary>
			/// <param name="TableName"></param>
			/// <param name="DatabaseFilePath"></param>
			/// <returns></returns>
			internal static HFI Read(string TableName, string DatabaseFilePath)
			{
				const string FuncErrCode=ClassErrCode+".0001";
				try
				{
					HFI hfi = new HFI();
					FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+TableName+".hfi",FileMode.Open,FileAccess.Read,FileShare.None);
					BinaryReader br = new BinaryReader(fs);
					if(br.ReadInt64()!=magic)
						throw new Exception("HyperNetDatabase error: Magic failed.");
					hfi.fatid = br.ReadInt64();
					hfi.IndexingDirty = br.ReadBoolean();
					hfi.rowseq = br.ReadInt64();
					hfi.fieldseq = br.ReadInt64();
					hfi.encrypted = br.ReadBoolean();
					br.Close();
				return hfi;
				}
				catch(Exception ex)
				{
					throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
				}
			}
			#endregion
			#region [0002] Write
			/// <summary>
			/// Write table info
			/// </summary>
			/// <param name="TableName"></param>
			/// <param name="DatabaseFilePath"></param>
			internal void Write(string TableName, string DatabaseFilePath)
			{
				const string FuncErrCode=ClassErrCode+".0002";
				try
				{
					FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+TableName+".hfi",FileMode.Create,FileAccess.Write,FileShare.None);
					BinaryWriter bw = new BinaryWriter(fs);
					bw.Write(magic);
					bw.Write((long)fatid); // FAT 0 is active
					bw.Write(IndexingDirty); // IndexDirty es falso
					bw.Write((long)rowseq); // Record number
					bw.Write((long)fieldseq); // FieldSeq number
					bw.Write((bool)encrypted); // Encrypted
					bw.Flush();
					bw.Close();
				}
				catch(Exception ex)
				{
					throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
				}
			}
			#endregion
		}
		#endregion
		#region [0012] SetDirtyIndexInDisc
		/// <summary>
		/// Invoked when in memory index is modified
		/// </summary>
		void SetDirtyIndexInDisc(string TableName, Field f)
		{
			const string FuncErrCode=ClassErrCode+".0012";
			try
			{
				string indexname = System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+TableName+".hi"+f.seq.ToString();
				FileStream fs = FileStream2.NewFileStream(indexname,FileMode.Open,FileAccess.Write,FileShare.None);
				BinaryWriter bw = new BinaryWriter(fs);
				bw.Write((bool)true);
				bw.Flush();
				bw.Close();
//
//				if(!Table2DeletedIndex.ContainsKey(indexname))
//				{
//					FileStream2.CloseHandle(indexname);
//					File.Delete(indexname);
//					Table2DeletedIndex[indexname]=0; // set the flag
//				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0013] CheckTable_FixContent: Fixes unfinished writes in a previous table grow
		/// <summary>
		/// [R1 is deprecated use R2] Fixes unfinished writes in a previous table grow
		/// </summary>
		/// <param name="TableName"></param>
		/// <param name="f"></param>
		unsafe internal void CheckTable_FixContent(string TableName, Field f)
		{
			const string FuncErrCode=ClassErrCode+".0013";
			try
			{
				lock(this.TableBlocking)
				{
					if(!this.TableBlocking.Contains(TableName))
						this.TableBlocking[TableName] = new SortedList();// index cache
				}
				lock(this.TableBlocking[TableName])
				{
					// Check content
					if(true)
					{
						FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+TableName+".hc"+f.seq.ToString(),FileMode.Open,FileAccess.ReadWrite,FileShare.None);
						BinaryReader br = new BinaryReader(fs);
						bool flag = br.ReadBoolean();
						if(flag)
						//if(true)
						{
							HFI hfi = HFI.Read(TableName,DatabaseFilePath);
							long supposedLen = (f.DataSize()*hfi.rowseq)+sizeof(bool);
							long offset=fs.Length-supposedLen;
							if(fs.Length<supposedLen)
							{
								// Recovering corrupt data
								long start = (fs.Length-sizeof(bool))/f.DataSize();
								fs.Position=start*f.DataSize()+sizeof(bool);
								BinaryWriter bw = new BinaryWriter(fs);
								for(long n=start;n<hfi.rowseq;n++)
								{
									f.WriteDefaultData(bw,false);
								}
								bw.Flush();
								bw.BaseStream.Position=0;
								bw.Write((bool)false); // edition flag
								bw.Flush();
								
								// Destroy the index
								if(f.bIndexed)
								{
									(TableBlocking[TableName] as SortedList)[f.seq]=null;
									SetDirtyIndexInDisc(TableName,f);
								}
								try
								{
									LogToFile("Warning","Recovering unfinished column.'"+TableName+"' by "+offset.ToString()+" bytes");
								}
								catch
								{
								}
							}
						}
						fs.Flush();
						fs.Close();
					}
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0014] CheckTable: Check content and flushes indexes if the bFlush flag is on.
		/// <summary>
		/// [R1 is deprecated use R2] Check content and flushes indexes if the bFlush flag is on.
		/// </summary>
		/// <param name="TableName"></param>
		/// <param name="bFlush"></param>
		unsafe internal void CheckTable(string TableName, bool bFlush)
		{
			const string FuncErrCode=ClassErrCode+".0014";
			try
			{
				lock(this.TableBlocking)
				{
					if(this.TableBlocking[TableName]==null)
						this.TableBlocking[TableName] = new SortedList();// index cache
				}
				lock(this.TableBlocking[TableName])
				{
					Field[] fields = this.GetFields(TableName);
					HFI hfi = HFI.Read(TableName,DatabaseFilePath);
					foreach(Field f in fields)
					{
						CheckTable_FixContent(TableName,f);

						// Check indexes
						if(f.bIndexed) 
						{
							string indexname = TableName+".hi"+f.seq.ToString();
							if(!IndexFileExists(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+indexname))
							{
								//SetIndexAsExistant(indexname);
								FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+indexname,FileMode.Create,FileAccess.Write,FileShare.None);
								BinaryWriter bw = new BinaryWriter(fs);
								bw.Write((bool)true);
								bw.Flush();
								bw.Close();
							}
							if(true)
							{
								FileStream fs =FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+indexname,FileMode.Open,FileAccess.ReadWrite,FileShare.None);
								BinaryReader br = new BinaryReader(fs);
								bool flag = br.ReadBoolean();
								if(true)//if(flag)
								{
									// Check index cache
									SortedList ht = (this.TableBlocking[TableName] as SortedList);
									if(ht[f.seq]==null)
									{
										GetIndex(TableName,f,true);
									}
									if(ht[f.seq]!=null)
									{
										if(bFlush)
										{
											Index ndx = ht[f.seq] as Index;
//											SoapFormatter formatter = new SoapFormatter();
//											formatter.Serialize(fs, ndx);
											BinaryWriter bw = new BinaryWriter(fs);
											ndx.WriteIndexData(this,bw); // new things
											fs.Position=0;
											bw.Write((bool)false);
											bw.Flush();
										}
									}
									else
									{
										throw new Exception("");
									}
								}
								fs.Flush();
								fs.Close();
							}
						}
					}
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0015] GetIndex: Reads or regenerates an index for a field in a table
		/// <summary>
		/// [R1 is deprecated use R2] Reads or regenerates an index for a field in a table
		/// </summary>
		/// <param name="TableName"></param>
		/// <param name="f"></param>
		/// <param name="bForceRegen"></param>
		/// <value></value>
		Index GetIndex(string TableName, Field f, bool bForceRegen)
		{
			const string FuncErrCode=ClassErrCode+".0015";
			try
			{
				lock(this.TableBlocking)
				{
					if(!this.TableBlocking.Contains(TableName))
						this.TableBlocking[TableName] = new SortedList();// index cache
				}
				lock(this.TableBlocking[TableName])
				{
					if((this.TableBlocking[TableName] as SortedList)[f.seq]!=null)
						return (this.TableBlocking[TableName] as SortedList)[f.seq] as Index;
					bool bRegen=true;
					if(!bForceRegen)
					{
						string indexname = TableName+".hi"+f.seq.ToString();
						if(IndexFileExists(indexname))
						{
							FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+indexname,FileMode.Open,FileAccess.ReadWrite,FileShare.None);
							BinaryReader br = new BinaryReader(fs);
							bool flag = br.ReadBoolean();
							if(flag)
							{
								bRegen=true;
							}
							else
							{
								bRegen=false;
//								SoapFormatter formatter = new SoapFormatter();
//								Index ndx = (Index)formatter.Deserialize(fs);
								Index ndx = Index.ReadIndexData(this,new BinaryReader(fs));
								(this.TableBlocking[TableName] as SortedList)[f.seq]=ndx;
							}
							fs.Close();
						}
					}
					if(bRegen)
					{// unique key
						if(!f.bIndexed) throw new Exception("Not indexed field");
						
						if(f.bUnique)
						{
							Index ndx = new Index();
							ndx.bUnique=true;
							CheckTable_FixContent(TableName,f); // por si las moscas
							FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+TableName+".hc"+f.seq.ToString(),FileMode.Open,FileAccess.Read,FileShare.None);
							BinaryReader br = new BinaryReader(fs);
							bool flag = br.ReadBoolean();
							if(flag)
							{
								throw new Exception("Database corruption");
							}
							else
							{
								HFI hfi = HFI.Read(TableName,DatabaseFilePath);
								for(long n=0;n<hfi.rowseq;n++)
								{
									object o = f.ReadData(this,br);
									ndx.ht[o]=n;
								}
								(this.TableBlocking[TableName] as SortedList)[f.seq]=ndx;
							}
							br.Close();
						}
						else
						{// not unique key
							Index ndx = new Index();
							ndx.bUnique=false;
							CheckTable_FixContent(TableName,f); // por si las moscas
							FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+TableName+".hc"+f.seq.ToString(),FileMode.Open,FileAccess.Read,FileShare.None);
							BinaryReader br = new BinaryReader(fs);
							bool flag = br.ReadBoolean();
							if(flag)
							{
								throw new Exception("Database corruption");
							}
							else
							{
								HFI hfi = HFI.Read(TableName,DatabaseFilePath);
								for(long n=0;n<hfi.rowseq;n++)
								{
									object o = f.ReadData(this,br);
									if(ndx.ht[o]==null)
										ndx.ht[o]=new ArrayList();
									(ndx.ht[o] as ArrayList).Add(n);
								}
								(this.TableBlocking[TableName] as SortedList)[f.seq]=ndx;
							}
							br.Close();
						}
					}
					return (this.TableBlocking[TableName] as SortedList)[f.seq] as Index;
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0017] ExecuteWhere: Executes the where part and return the rowids affected
		/// <summary>
		/// [R1 is deprecated use R2] 
		/// </summary>
		/// <param name="From_TableName"></param>
		/// <param name="Where_NameCondValue"></param>
		/// <param name="rowids"></param>
		internal void ExecuteWhere(string From_TableName, object[,] Where_NameCondValue, out ArrayList rowids)
		{
			const string FuncErrCode=ClassErrCode+".0017";
			try
			{
				string TableName=From_TableName;
				CheckTable(TableName,false);
				if(Where_NameCondValue==null) Where_NameCondValue=new object[0,0];
				Field[] flds = GetFields(TableName);

				// map of fields in table
				Hashtable FieldMap = new Hashtable();
				foreach(Field f in flds)
				{
					FieldMap[f.Name]=f;
				}

				// Values in names with same type as field
				ArrayList Where = new ArrayList();
				Hashtable UsedFieldMap = new Hashtable();
				for(int n=0;n<Where_NameCondValue.GetLength(0);n++)
				{
					string fieldname = Where_NameCondValue[n,0].ToString();
					string op =	Where_NameCondValue[n,1].ToString();
					object val = Where_NameCondValue[n,2];


					bool oprec = (op=="=")||(op==">")||(op=="<")||(op=="!=")||(op==">=")||(op=="<=");
					if(!oprec)
						throw new Exception("Operand '"+op+"' unrecognized");

					if(!FieldMap.ContainsKey(fieldname)) throw new Exception("Column "+fieldname+" unrecognized");
					Field f = FieldMap[fieldname] as Field;
					UsedFieldMap[f]=f;

					int priority=0;
					if(f.bIndexed&&f.bUnique&&(op=="=")) 
						priority=-4;
					else if(f.bIndexed&&(op=="="))
						priority=-3;
					else if(f.bIndexed&&f.bUnique&&((op=="<")||(op==">")||(op==">=")||(op=="<=")))
						priority=-2;
					else if(f.bIndexed&&((op=="<")||(op==">")||(op==">=")||(op=="<=")))
						priority=-1;
					
					object v=Variant.Object2Variant(val,f.type).obj;

					Where.Add(new object[]{f,op,v,priority});
				}

				// Sorting of fields to make search faster
				if(true)
				{
					// Order:
					// Indexed-unique with = condition -4
					// Indexed with = condition -3
					// Indexed-unique with < or > condition -2
					// Indexed with < or > condition -1
					// The rest 0
					Where.Sort(new WhereComp());
				}

				// Let's do the search
				
				
				HFI hfi = HFI.Read(TableName,this.DatabaseFilePath);
				if(Where.Count>0)
				{
					rowids = null;
					for(int n=0;(n<Where.Count);n++)
					{
						Field f = (Where[n] as object[])[0] as Field;
						string op = (Where[n] as object[])[1] as string;
						object val = (Where[n] as object[])[2] as object;

						if(f.bIndexed)
						{
							if(f.bUnique)
							{
								if(op=="=")
								{
									Index ndx = this.GetIndex(TableName,f,false);
									if(ndx.ht.ContainsKey(val))
									{
										if(rowids==null)
										{
											rowids = new ArrayList();
											rowids.Add(ndx.ht[val]);
											continue;
										}
										else if(rowids.Contains(val))
										{
											rowids = new ArrayList();
											rowids.Add(ndx.ht[val]);
											continue;
										}
										else
										{
											rowids = new ArrayList();
											break;
										}
									}
									else
									{
										rowids = new ArrayList();
										break;
									}
								}
// beta begin
								if(op=="!=")
								{
									Index ndx = this.GetIndex(TableName,f,false);
									if(ndx.ht.ContainsKey(val))
									{// the value exists in the index
										// If the rowids collection do not exist already
										if(rowids==null)
										{
											// Fill a rowid collection without the removed rowid
											rowids = new ArrayList( ndx.ht.Values );
											rowids.Remove(ndx.ht[val]);
											continue;
										}
										// If the rowids collection already exists
										else
										{
											// If contains the value to exclude
											if(rowids.Contains(val))
											{
												rowids.Remove(ndx.ht[val]);
												if(rowids.Count==0) break; 
												continue;
											}
											// If do not contains the value to exclude
											else
											{
												continue;
											}
										}
									}
									else
									{// the value do not exist in the index
										continue;
									}
								}
//end beta
							}
							else
							{// clave no única
								if(rowids==null)
								{
									rowids = new ArrayList();
									Index ndx = this.GetIndex(TableName,flds[0],false);
									if(ndx.ht[false]==null) ndx.ht[false] = new ArrayList();
									ArrayList nondeleted_rowids = ndx.ht[false] as ArrayList;
									rowids.AddRange( nondeleted_rowids); 
								}
								// begin op =
								if(op=="=")
								{
									Index ndx = this.GetIndex(TableName,f,false);
									if(ndx.ht.ContainsKey(val))
									{
										ArrayList selectedRows = ndx.ht[val] as ArrayList;
										Hashtable precedingRows = new Hashtable();
										foreach(long rowid in rowids)
										{
											precedingRows[rowid]=0;
										}
										ArrayList new_rowids = new ArrayList();
										foreach(long selrow in selectedRows)
										{
											if(precedingRows.Contains(selrow))
											{
												new_rowids.Add(selrow);
											}
										}
										rowids=new_rowids;
										continue;
									}
									else
									{
										rowids = new ArrayList();
										break;
									}
								}
								// end of op =
								// begin op !=
								if(op=="!=")
								{
									Index ndx = this.GetIndex(TableName,f,false);
									if(ndx.ht.ContainsKey(val))
									{
										ArrayList selectedRows = ndx.ht[val] as ArrayList;
										foreach(long rowid in selectedRows)
											rowids.Remove(rowid);
										if(rowids.Count==0) break; 
										continue;
									}
									else
									{
										continue;
									}
								}
								// end of op !=
								// begin op >
								if((op=="<")||(op==">")||(op=="<=")||(op==">=")) 
								{
									Index ndx = this.GetIndex(TableName,f,false);

									// Metemos en un set todos los ids de fila que llevamos hasta ahora
									// para luego irlas poniendo en una lista de seleccionadas
									Hashtable precedingRows = new Hashtable();
									foreach(long rowid in rowids)
										precedingRows[rowid]=0;
									

									Hashtable selectedRowIds = new Hashtable();
									IComparable v = (IComparable)val;
									foreach(object key in ndx.ht.Keys)
									{
										IComparable o = (IComparable)key;
										bool bAdd=false;
										if((op==">")&&(o.CompareTo(v)>0)) bAdd=true;
										if((op=="<")&&(o.CompareTo(v)<0)) bAdd=true;
										if((op==">=")&&(o.CompareTo(v)>=0)) bAdd=true;
										if((op=="<=")&&(o.CompareTo(v)<=0)) bAdd=true;
										if(bAdd)
										{
											ArrayList selectedRows = ndx.ht[key] as ArrayList;
											foreach(long rowid in selectedRows)
											{// si alguna rowid está en precedingRows la muevo a selectedRowIds
												if(precedingRows.ContainsKey(rowid))
												{
													precedingRows.Remove(rowid);
													selectedRowIds.Add(rowid,0);
												}
											}
										}
									}

									// ahora tenemos en selectedRowIds la colección 
									// de rowids filtrada en la columna de claves
									rowids = new ArrayList( selectedRowIds.Keys );
									if(rowids.Count==0) break; 
									continue;
								}
								else
								{
									throw new Exception("Unsupported operator");
								}
								// end of op >
							}
						}
						// Linear search
						if(rowids==null)
						{
							rowids = new ArrayList();
							Index ndx = this.GetIndex(TableName,flds[0],false);
							if(ndx.ht[false]==null) ndx.ht[false] = new ArrayList();
							ArrayList nondeleted_rowids = ndx.ht[false] as ArrayList;
							rowids.AddRange( nondeleted_rowids); 
						}
						if(true)
						{
							FileStream fs = FileStream2.NewFileStream(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+TableName+".hc"+f.seq.ToString(),FileMode.Open,FileAccess.ReadWrite,FileShare.None);
							BinaryReader br = new BinaryReader(fs);
							ArrayList new_rowids = new ArrayList();
							foreach(long rowid in rowids)
							{
								fs.Position=1+f.DataSize()*rowid;
								IComparable o = (IComparable)f.ReadData(this,br);
								IComparable v = (IComparable)val;
								if((op=="=")&&(o.CompareTo(v)==0)) new_rowids.Add(rowid);
								if((op==">")&&(o.CompareTo(v)>0)) new_rowids.Add(rowid);
								if((op=="<")&&(o.CompareTo(v)<0)) new_rowids.Add(rowid);
								if((op=="!=")&&(o.CompareTo(v)!=0)) new_rowids.Add(rowid);
								if((op==">=")&&(o.CompareTo(v)>=0)) new_rowids.Add(rowid);
								if((op=="<=")&&(o.CompareTo(v)<=0)) new_rowids.Add(rowid);
							}
							br.Close();
							rowids=new_rowids;
							if(rowids.Count==0) break;
						}
					}
				}
				else
				{
					rowids = new ArrayList();
					Index ndx = this.GetIndex(TableName,flds[0],false);
					if(ndx.ht[false]==null) ndx.ht[false] = new ArrayList();
					ArrayList nondeleted_rowids = ndx.ht[false] as ArrayList;
					rowids.AddRange( nondeleted_rowids); 
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion	
		#region [0018] AddFieldIfNotExist
		/// <summary>
		/// [R1 is deprecated use R2] Adds a field if it not exists
		/// </summary>
		public void AddFieldIfNotExist(string TableName, Field f)
		{
			const string FuncErrCode=ClassErrCode+".0018";
			try
			{
				lock(this.TableBlocking)
				{
					Field[] flds = this.GetFields(TableName);
					foreach(Field i in flds)
					{
						if(i.Name==f.Name)
							return;
					}
					this.AddField(TableName,f);
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0019] AddTableIfNotExist
		/// <summary>
		/// [R1 is deprecated use R2] Adds a table if it not exists
		/// </summary>
		public void AddTableIfNotExist(string TableName)
		{
			const string FuncErrCode=ClassErrCode+".0019";
			try
			{
				lock(this.TableBlocking)
				{
					string[] ts;
					this.GetTableNames(out ts);
					foreach(string s in ts)
					{
						if(s==TableName)
							return;
					}
					this.AddTable(TableName);
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0020] DataRow2NameAndValue: Converts a DataRow in a NameAndValue object with skipRows
		/// <summary>
		/// [R1 is deprecated use R2] Converts a DataRow in a NameAndValue object with skipRows.
		/// </summary>
		/// <param name="dr">DataRow - a Row in the resulted DataTable of a Select command.</param>
		/// <param name="skipRows">An array of strings to skip, usually the primary key of the table in Update statements.</param>
		/// <remarks>Used when you had make a select query and you want to reuse the result to make another one.</remarks>
		public static object[,] DataRow2NameAndValue(DataRow dr, string[] skipRows)
		{
			const string FuncErrCode=ClassErrCode+".0020";
			try
			{
				//lock(this.TableBlocking)
				//{
					if(skipRows==null)
						skipRows= new string[0];
					foreach(string sr in skipRows)
					{
						bool bFound=false;
						foreach(DataColumn dc in dr.Table.Columns)
							if(dc.ColumnName==sr)
							{
								bFound=true;
								break;
							}
						if(!bFound)
						{
							throw new Exception("'"+sr+"' field not found in DataRow.");
						}
					}
					object[,] rv = new object[dr.Table.Columns.Count-skipRows.Length,2];
					int n=0;
					ArrayList alSkipRows = new ArrayList(skipRows);
					foreach(DataColumn dc in dr.Table.Columns)
					{
						if(!alSkipRows.Contains(dc.ColumnName))
						{
							rv[n,0]=dc.ColumnName;
							rv[n,1]=dr[n];
							n++;
						}
					}
					return rv;
				//}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0021] Lock
		/// <summary>
		/// [R1 is deprecated use R2] Candado global, se usa con lock para evitar que se hagan queries durante una secuencia de ellas.
		/// EJ: Para insertar o cambiar primero hago un select.
		/// </summary>
		public object Lock
		{
			get
			{
				return TableBlocking;
			}
		}
		#endregion
		private bool IndexFileExists(string indexname)
		{
			try
			{
				FileStream fs = FileStream2.NewFileStream(indexname,FileMode.Open,FileAccess.Read,FileShare.None);
				BinaryReader br = new BinaryReader(fs);
				bool bValid = br.ReadBoolean();
				br.Close();
				return bValid;
			}
			catch
			{
				return false;
			}
//			if(Table2DeletedIndex.ContainsKey(indexname)) return false;
//			bool bex = File.Exists(System.IO.Path.GetDirectoryName(DatabaseFilePath)+"\\"+indexname);
//			if(!bex)
//				Table2DeletedIndex[indexname]=0; // set the flag
//			return bex;
		}
//		private void SetIndexAsExistant(string indexname)
//		{
//			Table2DeletedIndex.Remove(indexname);
//		}
		/// <summary>
		/// [R1 is deprecated use R2] 
		/// </summary>
		const string ClassErrCode="HND_DB";
		#region IDisposable Members
		/// <summary>
		/// [R1 is deprecated use R2] 
		/// </summary>
		public void Dispose()
		{
			// TODO:  Add Database.Dispose implementation
			Flush();
		}

		#endregion
//
//		private void CurrentDomain_ProcessExit(object sender, EventArgs e)
//		{
//			this.Flush();
//		}
		/// <summary>
		/// [R1 is deprecated use R2] Obtains a value and if it not exists obtains it's default value
		/// </summary>
		/// <param name="TableName">Name of the table</param>
		/// <param name="KeyField">Key field in table (where part in Select clause)</param>
		/// <param name="KeyValue">Key value (where part in Select clause)</param>
		/// <param name="ValueField">Value field</param>
		/// <param name="DefValue">Default value if no results where found</param>
		/// <returns></returns>
		public object ForcedSelect(string TableName, string KeyField, object KeyValue, string ValueField, object DefValue)
		{
			DataTable dt = Select(new string[]{ValueField},TableName, new object[,]{{KeyField,"=",KeyValue}});
			if(dt.Rows.Count>0)
				return dt.Rows[0][0];
			else 
				return DefValue;
		}
		/// <summary>
		/// [R1 is deprecated use R2] More known as Update or Insert (Sets values by a keyfield)
		/// </summary>
		/// <param name="TableName">Name of the table</param>
		/// <param name="KeyField">Key field in table (where part in Select clause)</param>
		/// <param name="KeyValue">Key value (where part in Select clause)</param>
		/// <param name="ValueField">Value field</param>
		/// <param name="Value">Default value if no results where found</param>
		/// <returns></returns>
		public void ForcedInsert(string TableName, string KeyField, object KeyValue, string ValueField, object Value)
		{
			lock(this.GetTableLock(TableName))
			{
				System.Data.DataTable dt2 = Select(new string[]{ValueField},TableName, new object[,]{{KeyField,"=",KeyValue}});
				if(dt2.Rows.Count>0)
					Update(TableName, new object[,]{{ValueField,Value}}, new object[,]{{KeyField,"=",KeyValue}});
				else
					Insert(TableName, new object[,]{{KeyField,KeyValue},{ValueField,Value}});
			}
		}
		/// <summary>
		/// [R1 is deprecated use R2] More known as Update or Insert (Sets values by a keyfield)
		/// </summary>
		/// <param name="TableName">Name of the table</param>
		/// <param name="KeysAndValues">Pairs of keys and values</param>
		/// <param name="FieldAndDefaultValue">Pairs of fields and default values without keyfield</param>
		public void ForcedInsert(string TableName, object[,] KeysAndValues, object[,] FieldAndDefaultValue)
		{
			if(KeysAndValues.GetLength(1)!=2)
				throw new Exception("Bad size on KeysAndValues");
			int nops = KeysAndValues.GetLength(0);
			string[] KeyFields = new string[ nops ];
			for(int k=0;k<nops;k++) 
				KeyFields[k]=KeysAndValues[k,0].ToString();
			object[,] Where = new object[ nops, 3];
			for(int k=0;k<nops;k++) 
			{
				Where[k,0]=KeysAndValues[k,0];
				Where[k,1]="=";
				Where[k,2]=KeysAndValues[k,1];
			}
			lock(this.GetTableLock(TableName))
			{
				System.Data.DataTable dt2 = Select(KeyFields,TableName, Where);
				if(dt2.Rows.Count>0)
				{
					Update(TableName, FieldAndDefaultValue, Where);
				}
				else
				{
					object[,] SET = new object[FieldAndDefaultValue.GetLength(0)+nops,2];
					for(int n=0;n<FieldAndDefaultValue.GetLength(0);n++)
					{
						SET[n,0]=FieldAndDefaultValue[n,0];
						SET[n,1]=FieldAndDefaultValue[n,1];
					}
					// Include keys in insert
					for(int n=0;n<nops;n++)
					{
						SET[FieldAndDefaultValue.GetLength(0)+n,0]=KeysAndValues[n,0];
						SET[FieldAndDefaultValue.GetLength(0)+n,1]=KeysAndValues[n,1];
					}
					Insert(TableName, SET);
				}
			}
		}
	}
	/// <summary>
	/// Not used
	/// </summary>
	public enum State{ 
		/// <summary>
		/// BD open
		/// </summary>
		Open,
		/// <summary>
		/// BD closed
		/// </summary>
		Closed};
	/// <summary>
	/// Field types
	/// </summary>
	public enum FieldType{
		/// <summary>
		/// Fixed length String
		/// </summary>
		ftString=0,
		/// <summary>
		/// int
		/// </summary>
		ftInt32=1,
		/// <summary>
		/// long
		/// </summary>
		ftInt64=2,
		/// <summary>
		/// bool
		/// </summary>
		ftBoolean=3,
		/// <summary>
		/// decimal
		/// </summary>
		ftDecimal=4,
		/// <summary>
		/// DateTime
		/// </summary>
		ftDateTime=5,
	};
	/// <summary>
	/// An entry in the file handler cache
	/// </summary>
	internal class FileHandlingCacheEntry
	{
		internal FileMode fm;
		internal FileStream2 fs;
		internal string filename;
		internal int uses=0;
		internal FileHandlingCacheEntry(string filename, FileStream2 fs, FileMode fm)
		{
			this.fs=fs;
			this.fm=fm;
			this.filename=filename;
			uses=1;
		}
	}
	internal class FileStream2 : FileStream
	{
		internal FileHandlingCacheEntry e;
		internal const int Limit = 30;
		internal static ArrayList FileHandlingCache = new ArrayList();
//		string path;
		internal FileStream2(
			string path,
			FileMode mode,
			FileAccess access,
			FileShare share
			):base(path,mode,access,share)
		{
//			this.path=path;
		}
		internal static FileStream NewFileStream(
			string path,
			FileMode mode,
			FileAccess access,
			FileShare share
			)
		{
//			TextWriter tw = new StreamWriter("filelog.txt",true);
//			tw.WriteLine("Open "+path);
//			tw.Close();
//			return new FileStream2(path,mode,access,share);
			foreach(FileHandlingCacheEntry e in FileHandlingCache)
			{
				if(e.filename==path)
				{
					if((e.fm==mode)&&(e.fs.CanSeek))
					{
						e.uses++;
						e.fs.Position=0;
						return e.fs;
					}
					else
					{
						CloseHandle(e);
						break;
					}
				}
			}
			if(FileHandlingCache.Count>Limit)
			{
				int trys = Limit-FileHandlingCache.Count;
				for(int t=0;t<trys;t++)
				{
					FileHandlingCacheEntry se = null;
					foreach(FileHandlingCacheEntry e in FileHandlingCache)
					{
						if(e.uses==0)
						{
							se=e;
							break;
						}
					}
					if(se!=null)
						CloseHandle(se);
				}
			}
			FileStream2 fs = new FileStream2(path,mode,FileAccess.ReadWrite,FileShare.None);
			FileHandlingCacheEntry en = new FileHandlingCacheEntry(path,fs,mode);
			fs.e=en;
			FileHandlingCache.Add( en );
			return fs;
		}
		internal static void CloseHandle(string path)
		{
			foreach(FileHandlingCacheEntry e in FileHandlingCache)
			{
				if(e.filename==path)
				{
					CloseHandle(e);
					break;
				}
			}
		}
		internal static void CloseHandle(FileHandlingCacheEntry e)
		{
			try
			{
				e.fs.RealClose();
				e.fs=null;
			}
			catch
			{
			}
			FileHandlingCache.Remove(e);
		}
		public void RealClose()
		{
			base.Close();
		}
		public override void Close()
		{
//			TextWriter tw = new StreamWriter("filelog.txt",true);
//			tw.WriteLine("Close "+path);
//			tw.Close();
//			base.Close();
			e.fs.Flush();
			if(e.uses>0)
				e.uses--;
			if(e.fm==FileMode.Create)
				CloseHandle(e);
		}
	}
	internal class Index
	{
		public bool bUnique=false;
		public SortedList ht = new SortedList();
		const string ClassErrCode="HNDNDX";
		#region [0001] ReadIndexData
		internal static Index ReadIndexData(Database db, BinaryReader br)
		{
			const string FuncErrCode=ClassErrCode+".0001";
			try
			{
				lock(db.TableBlocking)
				{
					Index ndx = new Index();
					ndx.bUnique = br.ReadBoolean();
					if(ndx.bUnique)
					{
						int nkeys = br.ReadInt32();
						for(int k=0;k<nkeys;k++)
						{
							Variant v = Variant.ReadFromFieldDef(br);
							ndx.ht[v.obj]=br.ReadInt64();
						}
					}
					else
					{
						int nkeys = br.ReadInt32();
						for(int k=0;k<nkeys;k++)
						{
							Variant v = Variant.ReadFromFieldDef(br);
							ndx.ht[v.obj] = new ArrayList();
							int values = br.ReadInt32();
							for(int vl=0;vl<values;vl++)
							{
								long rowid=br.ReadInt64();
								(ndx.ht[v.obj] as ArrayList).Add(rowid);
							}
						}
					}
					return ndx;
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0002] WriteIndexData
		internal void WriteIndexData(Database db, BinaryWriter bw)
		{
			const string FuncErrCode=ClassErrCode+".0002";
			try
			{
				lock(db.TableBlocking)
				{
					bw.Write(bUnique);
					if(bUnique)
					{
						bw.Write((int)this.ht.Keys.Count);
						foreach(object key in this.ht.Keys)
						{
							Variant v = Variant.Object2Variant(key);
							v.WriteToFieldDef(bw,false);
							long rowid = (long)this.ht[key];
							bw.Write(rowid);
						}
					}
					else
					{
						bw.Write((int)this.ht.Keys.Count);
						foreach(object key in this.ht.Keys)
						{
							Variant v = Variant.Object2Variant(key);
							v.WriteToFieldDef(bw,false);
							ArrayList al = this.ht[key] as ArrayList;
							bw.Write((int)al.Count);
							foreach(long rowid in al)
								bw.Write(rowid);
						}
					}
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
	}
	/// <summary>
	/// Indexes and fields
	/// </summary>
	public enum FieldIndexing{
		/// <summary>
		/// No index
		/// </summary>
		None,
		/// <summary>
		/// Index with non-repeatable value
		/// </summary>
		Unique,
		/// <summary>
		/// Index with repeated keys
		/// </summary>
		IndexedNotUnique
	};
	#region [HNDFLD] Field
	/// <summary>
	/// A field definition in a table
	/// </summary>
	public class Field
	{
		const string ClassErrCode="HNDFLD";
		/// <summary>
		/// Field Name
		/// </summary>
		public string Name;
		/// <summary>
		/// Field type
		/// </summary>
		public FieldType type;
		/// <summary>
		/// Default value
		/// </summary>
		public Variant DefaultValue = new Variant();
		/// <summary>
		/// String length
		/// </summary>
		public int len=100;
		internal long seq; // field id
		internal bool bIndexed=false;
		internal bool bUnique=false;
		/// <summary>
		/// Indexing
		/// </summary>
		public FieldIndexing Indexing
		{
			get
			{
				if(bIndexed)
				{
					if(bUnique)
					{
						return FieldIndexing.Unique;
					}
					else
						return FieldIndexing.IndexedNotUnique;
				}
				return FieldIndexing.None;
			}
			set
			{
				if(value==FieldIndexing.None)
				{
					bIndexed=false;
					bUnique=false;
				}else
				if(value==FieldIndexing.IndexedNotUnique)
				{
					bIndexed=true;
					bUnique=false;
				}else
				{
					bIndexed=true;
					bUnique=true;
				}
			}
		}
		#region [0000] ctor
		internal Field()
		{
		}
		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="Name"></param>
		/// <param name="defval"></param>
		/// <param name="Indexing"></param>
		public Field(string Name,bool defval,FieldIndexing Indexing)
		{
			this.Name=Name; this.DefaultValue=Variant.Object2Variant(defval); this.Indexing=Indexing; this.type=FieldType.ftBoolean;
		}
		/// <summary>
		///  ctor
		/// </summary>
		/// <param name="Name"></param>
		/// <param name="defval"></param>
		/// <param name="Indexing"></param>
		public Field(string Name,decimal defval,FieldIndexing Indexing)
		{
			this.Name=Name; this.DefaultValue=Variant.Object2Variant(defval); this.Indexing=Indexing; this.type=FieldType.ftDecimal;
		}
		/// <summary>
		///  ctor
		/// </summary>
		/// <param name="Name"></param>
		/// <param name="defval"></param>
		/// <param name="Indexing"></param>
		public Field(string Name,DateTime defval,FieldIndexing Indexing)
		{
			this.Name=Name; this.DefaultValue=Variant.Object2Variant(defval); this.Indexing=Indexing; this.type=FieldType.ftDateTime;
		}
		/// <summary>
		///  ctor
		/// </summary>
		/// <param name="Name"></param>
		/// <param name="defval"></param>
		/// <param name="Indexing"></param>
		public Field(string Name,int defval,FieldIndexing Indexing)
		{
			this.Name=Name; this.DefaultValue=Variant.Object2Variant(defval); this.Indexing=Indexing; this.type=FieldType.ftInt32;
		}
		/// <summary>
		///  ctor
		/// </summary>
		/// <param name="Name"></param>
		/// <param name="defval"></param>
		/// <param name="Indexing"></param>
		public Field(string Name,long defval,FieldIndexing Indexing)
		{
			this.Name=Name; this.DefaultValue=Variant.Object2Variant(defval); this.Indexing=Indexing; this.type=FieldType.ftInt64;
		}
		/// <summary>
		///  ctor
		/// </summary>
		/// <param name="Name"></param>
		/// <param name="defval"></param>
		/// <param name="Indexing"></param>
		/// <param name="len"></param>
		public Field(string Name,string defval,FieldIndexing Indexing, int len)
		{
			this.Name=Name; this.DefaultValue=Variant.Object2Variant(defval); this.Indexing=Indexing; this.type=FieldType.ftString; this.len=len;
		}
		#endregion
		#region Read: [0001] Reads field def
		/// <summary>
		/// Reads field def
		/// </summary>
		/// <param name="br"></param>
		internal void Read(BinaryReader br)
		{
			const string FuncErrCode=ClassErrCode+".0001";
			try{
				Name=br.ReadString();
				type=(FieldType)br.ReadByte();
				this.DefaultValue=Variant.ReadFromFieldDef(br);
				len=br.ReadInt32();
				seq=br.ReadInt64();
				bIndexed=br.ReadBoolean();
				bUnique=br.ReadBoolean();
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region Write: [0002] Writes field def
		/// <summary>
		/// Writes field def
		/// </summary>
		/// <param name="bw"></param>
		internal void Write(BinaryWriter bw)
		{
			const string FuncErrCode=ClassErrCode+".0002";
			try{
				bw.Write(Name);
				bw.Write((byte)type);
				this.DefaultValue.WriteToFieldDef(bw);
				bw.Write(len);
				bw.Write(seq);
				bw.Write(bIndexed);
				bw.Write(bUnique);
				bw.Flush();
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region DataSize: [0003] Returns a cell data size
		/// <summary>
		/// Returns a cell data size
		/// </summary>
		/// <returns></returns>
		unsafe internal long DataSize()
		{
			const string FuncErrCode=ClassErrCode+".0003";
			try{
				switch(type)
				{
					case FieldType.ftBoolean:
						return sizeof(bool);
			
					case FieldType.ftDecimal:
						return sizeof(decimal);
					case FieldType.ftDateTime:
						return sizeof(long);
					case FieldType.ftInt32:
						return sizeof(int);
			
					case FieldType.ftInt64:
						return sizeof(long);

					case FieldType.ftString: //long total = 2*len+8 bytes
						return (2*len+8)+sizeof(int)/* Num chars*/;
			
					default:
						throw new Exception(FuncErrCode+": Field type not recognized");
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region WriteDefaultData: [0004] Writes a default cell data
		/// <summary>
		/// Writes a default cell data
		/// </summary>
		/// <param name="bw"></param>
		/// <param name="bCommit"></param>
		internal void WriteDefaultData(BinaryWriter bw, bool bCommit)
		{
			const string FuncErrCode=ClassErrCode+".0004";
			try{
				Field f = this;
				switch(f.type)
				{
					case FieldType.ftBoolean:
						bw.Write((bool)f.DefaultValue.obj);
						break;
					case FieldType.ftDecimal:
						bw.Write((decimal)f.DefaultValue.obj);
						break;
					case FieldType.ftDateTime:
						bw.Write(((DateTime)f.DefaultValue.obj).Ticks);
						break;
					case FieldType.ftInt32:
						bw.Write((int)f.DefaultValue.obj);
						break;
					case FieldType.ftInt64:
						bw.Write((long)f.DefaultValue.obj);
						break;
					case FieldType.ftString: //long total = 2*len+8 bytes
						string str = (string)f.DefaultValue.obj;
						if(str.Length>f.len) str=str.Substring(0,f.len);
						byte[] bytes = System.Text.Encoding.Unicode.GetBytes(str);
						byte[] padding = new byte[(2*f.len+8)-bytes.Length];
						bw.Write((int)bytes.Length);
						bw.Write(bytes);
						bw.Write(padding);
						break;
				}
				if(bCommit)
					bw.Flush();
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region WriteData: [0005] Writes cell data
		internal void WriteData(Database db, BinaryWriter bw, Variant v, bool bCommit)
		{
			const string FuncErrCode=ClassErrCode+".0005";
			try
			{
//				// enc
//				BinaryWriter oldbw = bw;
//				MemoryStream ms = new MemoryStream();
//				bw = new BinaryWriter(ms);
//				// enc
				object obj = v.obj;
				if(v.type!=type)
					throw new Exception(FuncErrCode+": Different types.");
				Field f = this;
				switch(f.type)
				{
					case FieldType.ftBoolean:
						bw.Write((bool)obj);
						break;
					case FieldType.ftDecimal:
						bw.Write((decimal)obj);
						break;
					case FieldType.ftDateTime:
						bw.Write(((DateTime)obj).Ticks);
						break;
					case FieldType.ftInt32:
						bw.Write((int)obj);
						break;
					case FieldType.ftInt64:
						bw.Write((long)obj);
						break;
					case FieldType.ftString: //long total = 2*len+8 bytes
						string str = (string)obj;
						if(str.Length>f.len) str=str.Substring(0,f.len);
						byte[] bytes = System.Text.Encoding.Unicode.GetBytes(str);
						byte[] padding = new byte[(2*f.len+8)-bytes.Length];
						bw.Write((int)bytes.Length);
						bw.Write(bytes);
						bw.Write(padding);
						break;
				}
//				// enc
//				bw = oldbw;
//				long seed = bw.BaseStream.Position;
//				byte[] bytes2 = ms.ToArray();
//				for(int n=0;n<bytes2.Length;n++)
//				{
//					int pos = ((n%7)*((n+3)%103)) % bytes2.Length;
//					bytes2[n] = (byte)(((int)bytes2[n]) ^ ((int)db.passwd[n]));
//				}
//				bw.Write(bytes2);
//				// enc
				if(bCommit)
					bw.Flush();
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region ReadData: [0006] Reads a value into a datatable
		internal object ReadData(Database db, BinaryReader br)
		{
			const string FuncErrCode=ClassErrCode+".0006";
			try
			{
//				// dec
//				BinaryReader oldbw = bw;
//				MemoryStream ms = new MemoryStream();
//				bw = new BinaryWriter(ms);
//				// dec
				Field f = this;
				switch(f.type)
				{
					case FieldType.ftBoolean:
						return br.ReadBoolean();
					case FieldType.ftDecimal:
						return br.ReadDecimal();
					case FieldType.ftDateTime:
						return new DateTime(br.ReadInt64());
					case FieldType.ftInt32:
						return br.ReadInt32();
					case FieldType.ftInt64:
						return br.ReadInt64();
					case FieldType.ftString: //long total = 2*len+8 bytes
						long p = br.BaseStream.Position;
						byte[] bytes = new byte[br.ReadInt32()];
						bytes=br.ReadBytes(bytes.Length);
						string str =System.Text.Encoding.Unicode.GetString(bytes);
						br.BaseStream.Position = p+DataSize();
						return str;
					default:
						return null;
				}
//				// dec
//				bw = oldbw;
//				long seed = bw.BaseStream.Position;
//				byte[] bytes2 = ms.ToArray();
//				for(int n=0;n<bytes2.Length;n++)
//				{
//					int pos = ((n%7)*((n+3)%103)) % bytes2.Length;
//					bytes2[n] = (byte)(((int)bytes2[n]) ^ ((int)db.passwd[n]));
//				}
//				bw.Write(bytes2);
//				// dec
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
	}
	#endregion
	#region [HNDVAR] Variant
	/// <summary>
	/// A variant for default values
	/// </summary>
	public class Variant
	{
		const string ClassErrCode="HNDVAR";
		/// <summary>
		/// Type
		/// </summary>
		public FieldType type;
		/// <summary>
		/// Value
		/// </summary>
		public object obj;
		/// <summary>
		/// ctor
		/// </summary>
		public Variant()
		{
			type=FieldType.ftInt32;
			obj=(int)0;
		}
		#region [0001] WriteToFieldDef: Writes a definition to disk
		internal void WriteToFieldDef(BinaryWriter _bw, bool bCommit)
		{
			const string FuncErrCode=ClassErrCode+".0001";
			try{
				if(bCommit) _bw.Flush();
				BinaryWriter bw = new BinaryWriter(_bw.BaseStream,System.Text.Encoding.Unicode);
				bw.Write((int)type);
				switch(type)
				{
					case FieldType.ftBoolean:
						bw.Write((bool)obj);
						break;
					case FieldType.ftDecimal:
						bw.Write((decimal)obj);
						break;
					case FieldType.ftDateTime:
						bw.Write(((DateTime)obj).Ticks);
						break;
					case FieldType.ftInt32:
						bw.Write((int)obj);
						break;
					case FieldType.ftInt64:
						bw.Write((long)obj);
						break;
					case FieldType.ftString: 
						bw.Write((string)obj);
						break;
					default:
						throw new Exception(FuncErrCode+": Field type not recognized");
				}
				bw.Flush();
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		internal void WriteToFieldDef(BinaryWriter _bw)
		{
			WriteToFieldDef(_bw,true);
		}
		#endregion
		#region [0002] ReadFromFieldDef: Reads a definition to disk
		/// <summary>
		/// Reads a definition to disk
		/// </summary>
		/// <param name="_br"></param>
		/// <returns></returns>
		internal static Variant ReadFromFieldDef(BinaryReader _br)
		{
			Variant v= new Variant();
			const string FuncErrCode=ClassErrCode+".0002";
			try{
				BinaryReader br = new BinaryReader(_br.BaseStream,System.Text.Encoding.Unicode);
				v.type=(FieldType)br.ReadInt32();
				switch(v.type)
				{
					case FieldType.ftBoolean:
						v.obj=br.ReadBoolean();
						break;
					case FieldType.ftDecimal:
						v.obj=br.ReadDecimal();
						break;
					case FieldType.ftDateTime:
						v.obj=new DateTime(br.ReadInt64());
						break;
					case FieldType.ftInt32:
						v.obj=br.ReadInt32();
						break;
					case FieldType.ftInt64:
						v.obj=br.ReadInt64();
						break;
					case FieldType.ftString: 
						v.obj=br.ReadString();
						break;
					default:
						throw new Exception(FuncErrCode+": Field type not recognized");
				}
				return v;
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0003] Object2Variant: Converts an object into a variant and tries to make it as tgt
		/// <summary>
		/// Converts an object into a variant and tries to make it as tgt
		/// </summary>
		/// <param name="o"></param>
		/// <param name="tgt"></param>
		/// <returns></returns>
		internal static Variant Object2Variant(object o, FieldType tgt)
		{
			const string FuncErrCode=ClassErrCode+".0003";
			try
			{
				if(o.GetType()==typeof(long))
				{
					if(tgt==FieldType.ftDecimal)
						return Object2Variant( Convert.ToDecimal(o) );
					if(tgt==FieldType.ftInt32)
					{
						if( (long)o > ((long)int.MaxValue) )
							throw new Exception(FuncErrCode+": Overflow, can't convert value.");
						if( (long)o < ((long)int.MinValue) )
							throw new Exception(FuncErrCode+": Underflow, can't convert value.");
						return Object2Variant( Convert.ToInt32(o) );
					}
					if(tgt==FieldType.ftString)
						return Object2Variant( o.ToString() );
					if(tgt!=FieldType.ftInt64)
						throw new Exception(FuncErrCode+": Can't convert value.");
					else
						return Object2Variant( o );
				}
				else
					if(o.GetType()==typeof(int))
				{
					if(tgt==FieldType.ftDecimal)
						return Object2Variant( Convert.ToDecimal(o) );
					if(tgt==FieldType.ftInt64)
						return Object2Variant( Convert.ToInt64(o) );
					if(tgt==FieldType.ftString)
						return Object2Variant( o.ToString() );
					if(tgt!=FieldType.ftInt32)
						throw new Exception(FuncErrCode+": Can't convert value.");
					else
						return Object2Variant( o );
				}
				else
				{
					Variant v = Object2Variant(o);
					if(v.type!=tgt)
						throw new Exception(FuncErrCode+": Can't convert value.");
					else
						return v;
				}
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region [0004] Object2Variant: Converts an object into a variant
		/// <summary>
		/// Converts an object into a variant
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		internal static Variant Object2Variant(object o)
		{
			Variant v= new Variant();
			const string FuncErrCode=ClassErrCode+".0004";
			try{
				if(o.GetType()==typeof(bool))
				{
					v.type=FieldType.ftBoolean;
					v.obj=o;
				}
				else if(o.GetType()==typeof(decimal))
				{
					v.type=FieldType.ftDecimal;
					v.obj=o;
				}
				else if(o.GetType()==typeof(DateTime))
				{
					v.type=FieldType.ftDateTime;
					v.obj=o;
				}
				else if(o.GetType()==typeof(int))
				{
					v.type=FieldType.ftInt32;
					v.obj=o;
				}
				else if(o.GetType()==typeof(long))
				{
					v.type=FieldType.ftInt64;
					v.obj=o;
				}
				else if(o.GetType()==typeof(string))
				{
					v.type=FieldType.ftString;
					v.obj=o;
				}
				else
				{
					throw new Exception(FuncErrCode+": Field type not recognized");
				}
				return v;
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
	}
	#endregion
}

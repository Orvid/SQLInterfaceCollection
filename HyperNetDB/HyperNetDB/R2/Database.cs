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

/// <summary>Single-Tier Database engine</summary>
namespace HyperNetDatabase.R2
{
	/// <summary>
	/// Database
	/// </summary>
	public class Database : FileLog, IDatabase
	{
		#region QueryCache: Functions for Cache
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
		private void QueryCacheSet(string[] Fields, string From_TableName, object[,] Where_NameCondValue,DataTable2 dt)
		{
			lock(QueryCache)
			{
				if(QueryCache.Count>=QueryCacheMaxLen)
					QueryCache.RemoveAt(0);
				QueryCacheEntry q = new QueryCacheEntry();
				q.Fields=(string[])Fields.Clone();
				q.From_TableName=From_TableName;
				q.Where_NameCondValue= new object[Where_NameCondValue.GetLength(0),3];
				for(int n=0;n<Where_NameCondValue.GetLength(0);n++)
				{
					q.Where_NameCondValue[n,0]=Where_NameCondValue[n,0];
					q.Where_NameCondValue[n,1]=Where_NameCondValue[n,1];
					q.Where_NameCondValue[n,2]=Where_NameCondValue[n,2];
				}
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
		private DataTable2 QueryCacheGet(string[] Fields, string From_TableName, object[,] Where_NameCondValue)
		{
			lock(QueryCache)
			{
				QueryCacheEntry q = QueryCacheGet2(Fields,From_TableName,Where_NameCondValue);
				if(q!=null)
				{ 
					// Push it to the end to prevent deletion
					QueryCache.Remove(q);
					QueryCache.Add(q);
					return (DataTable2)q.dt.Clone(); // return a clone to prevent user modifications
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
						bool bExitLoop=false;
						for(int f=0;f<q.Fields.Length;f++)
						{
							if(q.Fields[f]!=Fields[f]) 
							{
								bExitLoop=true;
								break;
							}
						}
						if(bExitLoop) break;
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
			public DataTable2 dt;
		}
		#endregion	
		FileStream fsDB=null;
		string DatabaseFilePath;
		//int NextTID; // next table identifier
		int NextFID=0; // next file id
		BinaryReader br=null; 
		BinaryWriter bw=null;
		SortedList slFID2Pages = new SortedList(); // value: arraylist of pages sorted by seq (key==-1 for deleted pages)
		ArrayList PeekPagesByFID(int fid){ 
			if(slFID2Pages[fid]==null) 
				slFID2Pages[fid] = new ArrayList(); 
			return slFID2Pages[fid] as ArrayList;
		}
		//SortedSet PagesInUse = new SortedSet();
		SortedSet DeletedPages = new SortedSet();
		/// <summary>
		/// Filename of the database
		/// </summary>
		public string Filename
		{
			get
			{
				return DatabaseFilePath;
			}
		}
		#region -Dump
		/// <summary>
		/// Dumps pages state
		/// </summary>
		public void Dump()
		{
			lock(this.TableBlocking)
			{
				StreamWriter sw = new StreamWriter("HNDDump.txt");
				int npages = (int)(fsDB.Length / PageSize);
				sw.WriteLine("Page dump:");
				for(int n=0;n<npages;n++)
					DumpPage(sw,n,1);
				sw.Close();
			}
		}
		void DumpPage(StreamWriter sw,int page,int ident)
		{
			fsDB.Position=page*PageSize;
			bool deleted = br.ReadBoolean();
			byte type = br.ReadByte();
			int fid = br.ReadInt32();
			sw.Write( new string(' ',ident) );
			sw.Write( "Page num: "+page.ToString("N4")+" " );
			sw.Write( "Deleted: "+deleted.ToString()+" " );
			if(!deleted)
			{
				sw.Write( "TID: "+fid.ToString()+" " );
				if(type==Database.TablePageType)
				{
					sw.Write( "Type: Table   " );
					sw.Write( "fseq: "+br.ReadInt32().ToString("G04")+" " );
					sw.Write( "rownum: "+br.ReadInt64().ToString()+" " );
					sw.Write( "tname: "+br.ReadString().ToString()+" " );
				}
				else if(type==Database.FieldPageType)
				{
					sw.Write( "Type: Field   " );
					Field fld = new Field();
					fld.Read(br);// 4-field
					sw.Write( "fseq: "+fld.seq.ToString()+" " );
					sw.Write( "name: "+fld.Name.ToString()+" " );
					sw.Write( "type: "+fld.type.ToString()+" " );
				}
				else if(type==Database.ContentPageType)
				{
					sw.Write( "Type: Content " );
					sw.Write( "fseq: "+br.ReadInt32().ToString("G04")+" " );
					sw.Write( "PageOrder: "+br.ReadInt32().ToString("G04")+" " );
				}
			}
			sw.WriteLine("");
		}
		#endregion
		#region -Page handling
		/// <summary>
		/// Gets an avaible deleted page and marks it as in use.
		/// </summary>
		/// <returns></returns>
		int LockAvaiblePage()
		{ 
			while(DeletedPages.Count<1) 
				GROW(); 
			int page = (int)DeletedPages.GetOne();
			DeletedPages.Remove(page);
			//PagesInUse.Add(page);
			return page; 
		}
		/// <summary>
		/// Unmarks a deleted page as in use.
		/// </summary>
		/// <returns></returns>
		void UnlockAvaiblePage(int page)
		{ 
			//if(!PagesInUse.Contains(page))
			//	throw new Exception("Page is not locked.");
			DeletedPages.Add(page);
			//PagesInUse.Remove(page);
		}
		/// <summary>
		/// Grows database
		/// </summary>
		void GROW()
		{
			const int growth = 512;
			for(int n=0;n<growth;n++)
			{
				fsDB.Seek(0,SeekOrigin.End);
				int page = (int)(fsDB.Length / (long)Database.PageSize);
				long recpos = fsDB.Length;
				try
				{
					bw.Write( true ); // deleted page
					//bw.Flush();
					bw.BaseStream.SetLength(recpos+Database.PageSize);	
				}
				catch(Exception)
				{
					bw.BaseStream.SetLength(recpos);
					throw new Exception("Insufficient disk space.");
				}
				DeletedPages.Add(page);
			}
			bw.Flush();
		}
		#endregion
		Hashtable TableName2TID = new Hashtable(); // tablenames to the fid where they are declared
		Hashtable TID2Def = new Hashtable(); // fid to the table def
		private class TableNameDef
		{
			public readonly int TableFID=0;// This value is readonly
			public TableNameDef(int fid, int PageOfTableDef)
			{
				TableFID=fid;
				this.PageOfTableDef=PageOfTableDef;
			}
			// In page data ->
			public int fseq=0;
			public long rownum=0;
			public string tname;
			// <- In page data end
			public readonly int PageOfTableDef;
			public SortedList fseq2FieldDef = new SortedList(); // contais Field instances
		}
		const int PageSize=4096;
		const int ContentPageDataOffset=32; // we left an offset
		const int IntSize=4;
		const byte TablePageType=01;
		const byte ContentPageType=03;
		//const byte IndexPageType=04; - Currently only in memory indexes
		const byte FieldPageType=02;
		/// <summary>
		/// Connection state
		/// </summary>
		public State State
		{
			get
			{
				return (fsDB==null)?State.Closed:State.Open;
			}
		}
		#region -Constructor
		/// <summary>
		/// Default constructor
		/// </summary>
		public Database()
		{
		}
		#endregion
		#region -Close
		/// <summary>
		/// Closes a database
		/// </summary>
		public void Close()
		{
			lock(this.TableBlocking)
			{
				if(fsDB!=null) fsDB.Close();
				fsDB=null;
				this.br=null;
				this.bw=null;
				this.DeletedPages=null;
				//this.FieldsCache= new SortedList();
				this.NextFID=0;
				this.QueryCache=new ArrayList();;
				this.slFID2Pages=null;
				this.TableName2TID=null;
				this.TableBlocking= new SortedList();
				this.TID2Def=null;
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}
		#endregion
		#region -RaiseExceptionIfOpened
		/// <summary>
		/// Exception if opened
		/// </summary>
		void RaiseExceptionIfOpened()
		{
			if(State==State.Open)
				throw new Exception("HyperNetDatabase error: Database already opened.");
		}
		#endregion
		#region -RaiseExceptionIfClosed
		/// <summary>
		/// Exception if closed
		/// </summary>
		void RaiseExceptionIfClosed()
		{
			if(State==State.Closed)
				throw new Exception("HyperNetDatabase error: Database already opened.");
		}
		#endregion
		#region -RaiseExceptionIfTableDoesNotExists
		/// <summary>
		/// Exception if table doesn't exist
		/// </summary>
		void RaiseExceptionIfTableDoesNotExists(string tablename)
		{
			if(!this.TableName2TID.ContainsKey(tablename))
				throw new Exception("Table doesn't exist.");
		}
		#endregion
		#region -PreloadIndexes
		/// <summary>
		/// Preloads indexes for table to make reads faster
		/// </summary>
		/// <param name="TableName"></param>
		public void PreloadIndexes(string TableName)
		{
			this.RaiseExceptionIfClosed();
			this.RaiseExceptionIfTableDoesNotExists(TableName);
			int tid = (int)this.TableName2TID[TableName];
			TableNameDef tnd = this.TID2Def[tid] as TableNameDef;
			foreach(Field f in tnd.fseq2FieldDef.Values)
			{
				if(f.bIndexed)
					this.GetIndex(TableName,f);
			}
		}
		#endregion
		#region -Open
		/// <summary>
		/// Open
		/// </summary>
		/// <param name="fname"></param>
		public void Open(string fname)
		{
			lock(this.TableBlocking)
			{
				RaiseExceptionIfOpened();
				if(fname.ToLower().EndsWith(".hnd"))
					fname=fname.Substring(0,fname.Length-4);
				DatabaseFilePath=System.IO.Path.GetFullPath(fname)+".hnd";

				// Initial values
				if(!File.Exists(this.DatabaseFilePath))
				{
					try
					{
						fsDB = new FileStream(this.DatabaseFilePath,FileMode.Create,FileAccess.ReadWrite,FileShare.None,8*1024);
					}
					catch
					{
						throw new Exception("Can't create file.");
					}
				}
				else
				{
					try
					{
						fsDB = new FileStream(this.DatabaseFilePath,FileMode.Open,FileAccess.ReadWrite,FileShare.None,8*1024);
					}
					catch
					{
						throw new Exception("Database in use.");
					}
				}
				long len = (fsDB.Length/PageSize); len*=PageSize;
				if(fsDB.Length>len)
				{
					this.LogToFile("Warning","File size fixed.");
					fsDB.SetLength(len);
				}
				slFID2Pages = new SortedList();
				TableName2TID = new Hashtable();
				TID2Def = new Hashtable();
				pcInit();
				//PagesInUse = new SortedSet();
				DeletedPages = new SortedSet();
				br = new BinaryReader(fsDB,System.Text.Encoding.Unicode);
				bw = new BinaryWriter(fsDB,System.Text.Encoding.Unicode);

				// check log file
				if(true)
				{
					string lfn = DatabaseFilePath+".hlg";
					if(File.Exists(lfn))
					{
						FileStream lf = new FileStream(lfn,FileMode.Open,FileAccess.ReadWrite,FileShare.None);
						BinaryReader lfr = new BinaryReader(lf,System.Text.Encoding.Unicode);
						try
						{
							if((lfr.BaseStream.Length>0)&&lfr.ReadBoolean())
							{// recover from last crash
								byte logtype = lfr.ReadByte();
								if(logtype==0)
								{// delete pages op
									this.LogToFile("Warning","Deleted pages fixed.");
									ArrayList al = new ArrayList();
									int cnt = lfr.ReadInt32();
									for(int n=0;n<cnt;n++)
									{
										al.Add( lfr.ReadInt32() );
									}
									for(int n=0;n<cnt;n++)
									{
										bw.BaseStream.Position=PageSize*( (int)al[n] );
										bw.Write( true ); // deleted
									}
									bw.Flush();
									lf.SetLength(0);
									lf.Flush();
								}
								if(logtype==1)
								{// rollback pages
									this.LogToFile("Warning","Rollback modified pages.");
									int pcount = lfr.ReadInt32(); // num of pages

									for(int p=0;p<pcount;p++)
									{
										int page = lfr.ReadInt32();
										fsDB.Position=PageSize*page;
										byte[] buf = lfr.ReadBytes( Database.PageSize );
										bw.Write( buf );
									}

									bw.Flush();
									lf.SetLength(0);
									lf.Flush();
								}
							}
						}
						catch
						{
							Close();
							throw new Exception("Can't recover from last crash.");
						}
						finally
						{
							lf.Close();
						}
					}
				}
				ArrayList pagePurgatory = new ArrayList();
				Hashtable htFieldsByTID = new Hashtable();// contains Hastables by field seq
				Hashtable htDataByTID = new Hashtable(); // key: tid + fieldseq + dataseq = pageno
				Set ProcessedPages = new SortedSet();
				#region 1st Pass: Scan deleted pages and table pages
				NextFID=-1;
				try
				{
					int pos=0; // page counter
					fsDB.Position=0;
					while(fsDB.Position<fsDB.Length)
					{	
						// leemos info de página
						long ptr = br.BaseStream.Position;
						bool bPageIsDeleted = br.ReadBoolean();
						if(bPageIsDeleted)
						{
							ProcessedPages.Add(pos);
							this.DeletedPages.Add(pos);
						}
						else
						{
							byte bPageType = br.ReadByte();
							int fid = br.ReadInt32();
							if(bPageType==TablePageType)
							{
								ProcessedPages.Add(pos);
								TableNameDef tnd = new TableNameDef(fid,pos);
								tnd.fseq=br.ReadInt32();
								tnd.rownum=br.ReadInt64();
								tnd.tname = br.ReadString();
								TID2Def[fid]=tnd;
								TableName2TID[tnd.tname]=fid;
							}
							else if(bPageType==FieldPageType)
							{// Page is a field def, store it for further processing
								ProcessedPages.Add(pos);
								int tid = fid;
								//TableNameDef tnd = TID2Def[tid] as TableNameDef;
								Field fld = new Field();
								fld.Read(br);// 4-field
								fld.tid=tid;
								fld.PageOfFieldSef=pos;

								if(!htFieldsByTID.ContainsKey(tid))
									htFieldsByTID[tid]=new Hashtable();
								Hashtable htFieldsBySeq = htFieldsByTID[tid] as Hashtable;

								// avoid repeated fields
								bool bAvoid=false;
								foreach(Field f in htFieldsBySeq.Values)
								{
									if(f.Name==fld.Name)
									{
										bAvoid=true;
										break;
									}
								}
								if(!bAvoid)
								{
									htFieldsBySeq[fld.seq]=fld;
									//tnd.fseq2FieldDef[fld.seq]=fld;
								}
								else
								{
									pagePurgatory.Add(pos);
								}
							}
							else if(bPageType==ContentPageType)
							{ 
								int tid = fid;
								if(!htDataByTID.ContainsKey(tid))
									htDataByTID[tid]=new Hashtable();
								Hashtable htDataByFSeq = htDataByTID[tid] as Hashtable;

								long fseq = br.ReadInt32(); // 4º seq of field
								if(!htDataByFSeq.ContainsKey(fseq))
									htDataByFSeq[fseq]=new ArrayList();
								ArrayList alDataByOrder = htDataByFSeq[fseq] as ArrayList;

								int seq = br.ReadInt32(); // 5º data page order
								while(alDataByOrder.Count<=seq)
									alDataByOrder.Add(-1);
								alDataByOrder[seq]=pos;
							}
							NextFID = Math.Max( NextFID, fid );
							PeekPagesByFID(fid).Add(pos);
						}
						fsDB.Position = Database.PageSize + ptr;
						pos++;
					}
					NextFID++;
				}
				catch(Exception ex)
				{
					this.LogToFile(ex.Message,ex.StackTrace);
					this.Close();
					throw new Exception("Database corrupted.");
				}
				#endregion
				#region 2nd Pass: Field integration
//				try
//				{
					foreach(int tid in htFieldsByTID.Keys)
					{
						TableNameDef tnd = TID2Def[tid] as TableNameDef;
						Hashtable htFieldsBySeq = htFieldsByTID[tid] as Hashtable;
						foreach(long seq in htFieldsBySeq.Keys)
						{
							tnd.fseq2FieldDef[seq]=htFieldsBySeq[seq];
						}
					}
//
//					int pos=0; // page counter
//					fsDB.Position=0;
//					while(fsDB.Position<fsDB.Length)
//					{	
//						// leemos info de página
//						long ptr = br.BaseStream.Position;
//						if(!ProcessedPages.Contains(pos))
//						{
//							bool bPageIsDeleted = br.ReadBoolean();// 1-deleted
//							if(bPageIsDeleted)
//							{
//								// skip
//							}
//							else
//							{
//								byte bPageType = br.ReadByte();// 2-type
//								int tid = br.ReadInt32(); // 3-fid of table
//								if(bPageType==FieldPageType)
//								{
//									ProcessedPages.Add(pos);
//									TableNameDef tnd = TID2Def[tid] as TableNameDef;
//									Field fld = new Field();
//									fld.Read(br);// 4-field
//									fld.tid=tid;
//									fld.PageOfFieldSef=pos;
//
//									// avoid repeated fields
//									bool bAvoid=false;
//									foreach(Field f in tnd.fseq2FieldDef.Values)
//									{
//										if(f.Name==fld.Name)
//										{
//											bAvoid=true;
//											break;
//										}
//									}
//									if(!bAvoid)
//									{
//										tnd.fseq2FieldDef[fld.seq]=fld;
//									}
//									else
//									{
//										pagePurgatory.Add(pos);
//									}
//								}
//							}
//						}
//						fsDB.Position = Database.PageSize + ptr;
//						pos++;
//					}
//				}
//				catch(Exception ex)
//				{
//					this.LogToFile(ex.Message,ex.StackTrace);
//					this.Close();
//					throw new Exception("Database corrupted.");
//				}
				#endregion
				#region 3nd Pass: Locate data for fields
				try
				{
					foreach(int tid in htDataByTID.Keys)
					{
						TableNameDef tnd = TID2Def[tid] as TableNameDef;
						Hashtable htDataByFSeq = htDataByTID[tid] as Hashtable;
						foreach(long seq in htDataByFSeq.Keys)
						{
							ArrayList alDataByOrder = htDataByFSeq[seq] as ArrayList;
							if(!tnd.fseq2FieldDef.ContainsKey(seq))
							{
								pagePurgatory.AddRange( alDataByOrder );
							}
							else
							{
								Field fld = tnd.fseq2FieldDef[seq] as Field;
								fld.DataFID=alDataByOrder;
							}
						}
					}
//					int pos=0; // page counter
//					fsDB.Position=0;
//					while(fsDB.Position<fsDB.Length)
//					{	
//						// leemos info de página
//						long ptr = br.BaseStream.Position;
//						if(!ProcessedPages.Contains(pos))
//						{
//							bool bPageIsDeleted = br.ReadBoolean();// 1º deleted is on?
//							if(bPageIsDeleted)
//							{
//								// skip
//							}
//							else
//							{
//								byte bPageType = br.ReadByte();// 2º Type
//								int tid = br.ReadInt32();// 3º fid of table
//								if(bPageType==ContentPageType)
//								{ 
//									long fseq = br.ReadInt32(); // 4º seq of field
//									int seq = br.ReadInt32(); // 5º data page order
//									TableNameDef tnd = TID2Def[tid] as TableNameDef;
//									if(!tnd.fseq2FieldDef.ContainsKey(fseq))
//									{
//										pagePurgatory.Add(pos);
//									}
//									Field fld = tnd.fseq2FieldDef[fseq] as Field;
//									while(fld.DataFID.Count<=seq)
//										fld.DataFID.Add(-1);
//									fld.DataFID[seq]=pos;
//								}
//							}
//						}
//						fsDB.Position = Database.PageSize + ptr;
//						pos++;
//					}
					foreach(TableNameDef tnd in TID2Def.Values)
						foreach(Field f in tnd.fseq2FieldDef.Values)
							foreach(int page in f.DataFID)
								if(page==-1)
									throw new Exception("Database corrupted.");
				}
				catch(Exception ex)
				{
					this.LogToFile(ex.Message,ex.StackTrace);
					this.Close();
					throw new Exception("Database corrupted.");
				}
				#endregion
				foreach(TableNameDef tnd in TID2Def.Values)
					foreach(Field f in tnd.fseq2FieldDef.Values)
					{
						// grow if it is needed
						if(tnd.rownum>0)
						{
							int valSize = (int)f.DataSize();
							long Capacity = (PageSize-ContentPageDataOffset)/valSize;
							ArrayList pages = f.DataFID;
							while((pages.Count*Capacity)<tnd.rownum)
							{
								int datapage = this.LockAvaiblePage();
								bw.BaseStream.Position = (datapage*PageSize);
								bw.Write( true );
								bw.Flush();
								bw.Write( (byte)Database.ContentPageType );
								bw.Write( tnd.TableFID );
								bw.Write( (int)f.seq );
								bw.Write( f.DataFID.Count );
								bw.Flush();
								for(int c=0;c<Capacity;c++)
								{
									bw.BaseStream.Position = (datapage*PageSize)+ContentPageDataOffset+c*valSize;
									f.WriteDefaultData(bw,false);
								}
								bw.Flush();
								bw.BaseStream.Position = (datapage*PageSize);
								bw.Write( (bool)false );
								bw.Flush();
								pages.Add(datapage);
								PeekPagesByFID(tnd.TableFID).Add(datapage);
								this.InvalidatePage(datapage);
							}
						}
					}


				// Autoseq table
				this.AddTableIfNotExist(tblSequences);
				this.AddFieldIfNotExist(tblSequences, new Field("SEQNAME","",FieldIndexing.Unique,40));
				this.AddFieldIfNotExist(tblSequences, new Field("SEQVALUE",(long)0,FieldIndexing.None));
				this.AddFieldIfNotExist(tblSequences, new Field("SEQINCREMENT",(long)1,FieldIndexing.None));
				this.AddFieldIfNotExist(tblSequences, new Field("SEQLOOP",false,FieldIndexing.None));
				this.AddFieldIfNotExist(tblSequences, new Field("SEQMAXVALUE",long.MaxValue,FieldIndexing.None));

				// Autoseq table
				this.AddTableIfNotExist(tblAlterTbl);
				this.AddFieldIfNotExist(tblAlterTbl, new Field("TNAME","",FieldIndexing.None,80));
				this.AddFieldIfNotExist(tblAlterTbl, new Field("FSRC","",FieldIndexing.None,80));
				this.AddFieldIfNotExist(tblAlterTbl, new Field("FTMP","",FieldIndexing.None,80));
				this.AddFieldIfNotExist(tblAlterTbl, new Field("STATE",(int)1,FieldIndexing.None));

				// Unknown bugfix -> Purge pages
				foreach(int i in pagePurgatory)
				{
					if(i==-1) continue;
					bw.BaseStream.Position = (i*PageSize);
					bw.Write( true );
					bw.Flush();
				}
			}
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		private const string tblAlterTbl = "$ALTER_TBL";
		#endregion 
		#region -Sequences
		/// <summary>
		/// Sequences table name
		/// </summary>
		internal const string tblSequences="$Sequences";
		/// <summary>
		/// Creates a sequence.
		/// </summary>
		/// <param name="name">Sequence name</param>
		/// <param name="initial_value">Initial value</param>
		/// <param name="increment">Increment</param>
		/// <param name="loop">It loops?</param>
		/// <param name="maxval">Max value for looping (modulus)</param>
		public void seqCreate(string name, long initial_value, long increment, bool loop, long maxval)
		{
			
				this.Insert(tblSequences, new object[,]{ {"SEQNAME",name}, {"SEQVALUE",initial_value}, {"SEQINCREMENT",increment}, {"SEQLOOP",loop}, {"SEQMAXVALUE",maxval}  });
			
		}
		/// <summary>
		/// Creates a sequence, non looping.
		/// </summary>
		/// <param name="name">Sequence name</param>
		/// <param name="initial_value">Initial value</param>
		/// <param name="increment">Increment</param>
		public void seqCreate(string name, long initial_value, long increment)
		{
			
				this.Insert(tblSequences, new object[,]{ {"SEQNAME",name}, {"SEQVALUE",initial_value}, {"SEQINCREMENT",increment}  });
		
		}
		/// <summary>
		/// Creates a sequence, non looping, increment by one.
		/// </summary>
		/// <param name="name">Sequence name</param>
		/// <param name="initial_value">Initial value</param>
		public void seqCreate(string name, long initial_value)
		{
			
				this.Insert(tblSequences, new object[,]{ {"SEQNAME",name}, {"SEQVALUE",initial_value}  });
			
		}
		/// <summary>
		/// Creates a sequence, non looping, increment by one, initial value 0.
		/// </summary>
		/// <param name="name">Sequence name</param>
		public void seqCreate(string name)
		{

				this.Insert(tblSequences, new object[,]{ {"SEQNAME",name}  });

		}
		/// <summary>
		/// Next sequence value (and autoincrement)
		/// </summary>
		/// <param name="name">Sequence name</param>
		/// <returns></returns>
		public long seqNextValue(string name)
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
		/// <summary>
		/// Set sequence value
		/// </summary>
		/// <param name="name">Sequence name</param>
		/// <param name="val">Value</param>
		/// <returns></returns>
		public long seqSetValue(string name, long val)
		{
			
			lock(this.TableBlocking)
			{
				DataTable dt = this.Select( new string[]{"*"}, tblSequences, new object[,]{{ "SEQNAME","=",name}});
				if(dt.Rows.Count==0)
					throw new Exception("Sequence not found.");

				DataRow dr = dt.Rows[0];
				long newVal = val;
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
		/// <summary>
		/// Current sequence value
		/// </summary>
		/// <param name="name">Sequence name</param>
		/// <returns></returns>
		public long seqCurrentValue(string name)
		{
		
				DataTable dt = this.Select( new string[]{"*"}, tblSequences, new object[,]{{ "SEQNAME","=",name}});
				if(dt.Rows.Count==0)
					throw new Exception("Sequence not found.");
				DataRow dr = dt.Rows[0];
				long Val = (long)dr["SEQVALUE"];
				return Val;
		
		}
		/// <summary>
		/// Sequence drop
		/// </summary>
		/// <param name="name">Sequence name</param>
		public void seqDrop(string name)
		{
		
				DataTable dt = this.Select( new string[]{"*"}, tblSequences, new object[,]{{ "SEQNAME","=",name}});
				if(dt.Rows.Count>0)
				{
					this.Delete( tblSequences, new object[,]{{ "SEQNAME","=",name}});
				}
			
		}
		/// <summary>
		/// Sequence exists?
		/// </summary>
		/// <param name="name">Sequence name</param>
		public bool seqExists(string name)
		{
			
				DataTable dt = this.Select( new string[]{"*"}, tblSequences, new object[,]{{ "SEQNAME","=",name}});
				if(dt.Rows.Count>0)
					return true;
				return false;
			
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
		public void Insert(string TableName, object[,] NamesAndValues)
		{
				lock(this.TableBlocking)
				{
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
							if((flds[m].Name==NamesAndValues[n,0].ToString())&&(NamesAndValues[n,1]!=System.DBNull.Value))
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
							Index ndx = this.GetIndex(TableName,flds[m]);
							if(ndx.ExistsKey(data[m].obj))
								throw new Exception("Insert violates '"+flds[m].Name+"' field for table '"+TableName+"'.");
						}
					}

					// Deleted field set to deleted to prevent errors
					data[0]=Variant.Object2Variant(false);

					// Look for a hole
					long index=-1;
					bool bHoleSucc=false;
					int tid = (int)TableName2TID[TableName];
					TableNameDef tnd = TID2Def[tid] as TableNameDef;
					if(true)
					{
						Index ndx = GetIndex(TableName,flds[0]);
						if(ndx.ExistsKey(true))
						{
							index = ndx.PeekOne(true);
							ndx.RemoveByRowid(index);
							//ndx.Rem(true,index);
							bHoleSucc=true;
						}
						if(!bHoleSucc)
						{// deleted row list is empty
							// Grow all columns if needed before making any changes
							foreach(Field f in tnd.fseq2FieldDef.Values)
							{
								int valSize = (int)f.DataSize();
								int Capacity = (PageSize-ContentPageDataOffset)/valSize;
								if(tnd.rownum==(Capacity*f.DataFID.Count))
								{
									int newpage = this.LockAvaiblePage();
									bw.BaseStream.Position=PageSize*newpage;
									bw.Write( true );
									bw.Flush();
									bw.Write( (byte)Database.ContentPageType );
									bw.Write( tnd.TableFID );
									int fseq = (int)f.seq;
									bw.Write( fseq );
									int seq = f.DataFID.Count;
									bw.Write( seq );
									bw.Flush();
									bw.BaseStream.Position=PageSize*newpage;
									bw.Write( false );
									bw.Flush();
									f.DataFID.Add(newpage);
									PeekPagesByFID(tid).Add(newpage);
									this.InvalidatePage(newpage);
								}
							}
							index=tnd.rownum;
						}
					}
					

					// Data insertion from last to first to make activation last
					for(int m=(flds.Length-1);m>=0;m--)
					{
						Field f = flds[m];
						int valSize = (int)f.DataSize();
						long Capacity = (PageSize-ContentPageDataOffset)/valSize;
						ArrayList pages = f.DataFID;
						if((pages.Count*Capacity)<tnd.rownum)
							throw new Exception("Row num corrupted.");
						long npage = index / Capacity;
						long offset = index % Capacity;
						int page = (int)pages[(int)npage];
						br.BaseStream.Position = (page*PageSize)+ContentPageDataOffset+offset*valSize;
						f.WriteData(bw,data[m],false);// CAUTION
						this.InvalidatePage(page);
					}
					bw.BaseStream.Flush();

					// Insert in index (differs from delete flag, because delete flag needs an update index)
					for(int m=(flds.Length-1);m>=0;m--)
					{
						if(flds[m].bIndexed)
						{
							Index ndx = GetIndex(TableName,flds[m]);
							object key = data[m].obj;
							ndx.Add(key,index,flds[m].Name);
						}
					}
					// write rownum
					if(!bHoleSucc)
					{
						tnd.rownum++;
						fsDB.Position = tnd.PageOfTableDef*PageSize+1+1+4+4;
						bw.Write( (long)tnd.rownum );
						bw.Flush();
					}
				}

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
		public void Update(string From_TableName, object[,] Set, object[,] Where_NameCondValue)
		{

				lock(this.TableBlocking)
				{
					this.RaiseExceptionIfClosed();
					string TableName=From_TableName;
					QueryCacheDestroy(TableName);


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
							throw new Exception("Field '"+fname+"' do not exist in this table.");
						if(SetFields.ContainsKey(fname))
							throw new Exception("Field '"+fname+"' is repeated in the set clause of this update command.");
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
							Index ndx = this.GetIndex(TableName,f);
							if(ndx.ExistsKey( SetFields[fname] ))
								throw new Exception("Insert violates '"+f.Name+"' field for table '"+TableName+"'.");
						}
					}

					// Get the rowids
					Set ROWIDS;
					ExecuteWhere(From_TableName,Where_NameCondValue,out ROWIDS);

					// Set fields
					if(ROWIDS==null) throw new Exception("Where condition returned null rows.");
					
					// Data set
					int tid = (int)TableName2TID[TableName];
					TableNameDef tnd = TID2Def[tid] as TableNameDef;

					// Create roll-back to prevent blackouts

					// make log file of old data
					string lfn = DatabaseFilePath+".hlg";
					FileStream lf;
					try
					{
						lf = new FileStream(lfn,FileMode.Create,FileAccess.Write,FileShare.None);
					}
					catch
					{
						throw new Exception("Insufficient disk space.");
					}
					BinaryWriter lfw=null;
					try
					{
						lfw = new BinaryWriter(lf,System.Text.Encoding.Unicode);
						lfw.Write( (bool)false ); // not valid
						lfw.Flush();
						lfw.Write( (byte)1 ); // overwrite pages operation 
						Set PAGES = new HashedSet();
						foreach(string name in SetFields.Keys)
						{
							Field f = htTableFields[name] as Field;
							int valSize = (int)f.DataSize();
							long Capacity = (PageSize-ContentPageDataOffset)/valSize;
							ArrayList pages = f.DataFID;
							foreach(long rowid in ROWIDS)
							{
								long npage = rowid / Capacity;
								int page = (int)pages[(int)npage];
								PAGES.Add( page );
							}
						}
						lfw.Write( (int)PAGES.Count ); // num of pages involved
						foreach(int page in PAGES)
						{
							br.BaseStream.Position = (page*PageSize);
							byte[] buf = br.ReadBytes( Database.PageSize );
							lfw.Write( (int)page ); // page id
							lfw.Write( buf ); // page
						}
						lfw.Flush();
						try
						{
							lfw.BaseStream.Position=0;
							lfw.Write( (bool)true ); // valid
							lfw.Flush();
						}
						catch
						{
							// aborting log file
							try
							{
								lfw.BaseStream.SetLength(0);
							}
							catch
							{
							}
							throw;
						}
					}
					catch
					{
						try
						{
							if(lfw!=null)
								lfw.Close();
						}
						catch
						{
						}
						throw new Exception("Error while writing rollback, update operation cancelled. (Insufficient disk space?)");
					}

					// Do the changes
					foreach(string name in SetFields.Keys)
					{	
						Field f = htTableFields[name] as Field;
						int valSize = (int)f.DataSize();
						long Capacity = (PageSize-ContentPageDataOffset)/valSize;
						ArrayList pages = f.DataFID;
						if((pages.Count*Capacity)<tnd.rownum)
							throw new Exception("Row num corrupted.");


						foreach(long rowid in ROWIDS)
						{
							object oldkey;
							try
							{
								long npage = rowid / Capacity;
								long offset = rowid % Capacity;
								int page = (int)pages[(int)npage];
								//br.BaseStream.Position = (page*PageSize)+ContentPageDataOffset+offset*valSize;
								//oldkey = f.ReadData(br);
								oldkey = f.ReadData( this.PageReader(page,ContentPageDataOffset+offset*valSize) );
								br.BaseStream.Position = (page*PageSize)+ContentPageDataOffset+offset*valSize;
								Variant v = Variant.Object2Variant( SetFields[f.Name], f.type);
								f.WriteData(bw,v,false); // CAUTION
								this.InvalidatePage(page);
							}
							catch(Exception ex)
							{
								this.Close();
								this.LogToFile(ex.Message,ex.StackTrace);
								throw new Exception("Fatal error while writting data into database.");
							}

							if(f.bIndexed)
							{
								Index ndx;
								try
								{
									ndx = GetIndex(TableName,f);
								}
								catch(Exception ex)
								{
									this.Close();
									this.LogToFile(ex.Message,ex.StackTrace);
									throw new Exception("Fatal error while reading index database.");
								}
								object key = SetFields[f.Name];
								try
								{	
									if((key as IComparable).CompareTo(oldkey as IComparable)!=0)
									{
										//if(f.type==FieldType.ftDateTime)
											ndx.RemoveByRowid(rowid);
										//else
											ndx.Add(key,rowid,f.Name);
									}
								}
								catch(Exception ex)
								{
									this.Close();
									this.LogToFile(ex.Message,ex.StackTrace);
									throw new Exception("Fatal error while changing key. Table:"+TableName+", Field:"+f.Name+", OldKey:"+oldkey.ToString()+", NewKey:"+key.ToString()+".");
								}
							}
						}
						bw.BaseStream.Flush();
					}

					// clear log
					lfw.BaseStream.SetLength(0);
					lfw.Flush();
					lfw.Close();
				}

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
		public void Delete(string From_TableName, object[,] Where_NameCondValue)
		{
		
				lock(this.TableBlocking)
				{
					string TableName=From_TableName;
					QueryCacheDestroy(TableName);

					Field[] flds = GetFields(TableName);

					// Get the rowids
					Set ROWIDS;
					ExecuteWhere(From_TableName,Where_NameCondValue,out ROWIDS);

					// Delete fields
					if(ROWIDS!=null)
					{
						int tid = (int)TableName2TID[TableName];
						TableNameDef tnd = TID2Def[tid] as TableNameDef;
						Field f = flds[0];
						int valSize = (int)f.DataSize();
						long Capacity = (PageSize-ContentPageDataOffset)/valSize;
						ArrayList pages = f.DataFID;
						if((pages.Count*Capacity)<tnd.rownum)
							throw new Exception("Row num corrupted.");

						
						Index ndx = this.GetIndex(TableName,flds[0]);
						foreach(long rowid in ROWIDS)
						{
							long npage = rowid / Capacity;
							long offset = rowid % Capacity;
							int page = (int)pages[(int)npage];
							br.BaseStream.Position = (page*PageSize)+ContentPageDataOffset+offset*valSize;
							bw.Write((bool)true);
							bw.Flush();
							this.InvalidatePage(page);

							// Update in memory indexes
							//ndx.Rem(false,rowid);
							ndx.RemoveByRowid(rowid);
							ndx.Add(true,rowid,flds[0].Name);
							
						}
						
						for(int n=1;n<flds.Length;n++)
						{
							if(flds[n].bIndexed)
							{
								ndx = this.GetIndex(TableName,flds[n]);
								foreach(long rowid in ROWIDS)
								{
									ndx.RemoveByRowid(rowid);
								}
							}
						}
					}
				}
		
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
		public DataTable Select(string[] Fields, string From_TableName, object[,] Where_NameCondValue)
		{
			DataTable2 dt2 = Select2(Fields,From_TableName,Where_NameCondValue);
			DataTable dt = dt2.SchemaOnly.Clone();
			dt.TableName=From_TableName;
			dt.BeginLoadData();
			dt.MinimumCapacity=dt2.Rows.Length;
			for(int rownum=0;rownum<dt2.Rows.Length;rownum++)
			{
				dt.Rows.Add( (object[])dt2.Rows[rownum] );			
			}
			dt.AcceptChanges();
			dt.EndLoadData();	
			return dt;
		}
		/// <summary>
		/// The same as Select but uses a faster DataTable class for large datasets.
		/// </summary>
		/// <param name="Fields"></param>
		/// <param name="From_TableName"></param>
		/// <param name="Where_NameCondValue"></param>
		/// <returns></returns>
		public DataTable2 Select2(string[] Fields, string From_TableName, object[,] Where_NameCondValue)
		{
			
			
				if(Fields==null) Fields = new string[]{"*"};
				if(Where_NameCondValue==null) Where_NameCondValue = new object[0,0];
				lock(this.TableBlocking)
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
					DataTable2 cacheTable = QueryCacheGet(Fields,From_TableName,Where_NameCondValue);
					if(cacheTable!=null) return cacheTable;

//					// Remove deletion field
//					if(SelectedFields.Contains(DeletedFieldName))
//						SelectedFields.Remove(DeletedFieldName);

					Set ROWIDS;
					ExecuteWhere(From_TableName,Where_NameCondValue,out ROWIDS);

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
					DataTable2 dt2=null;
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

					if(ROWIDS!=null)
					{
						int tid = (int)TableName2TID[TableName];
						TableNameDef tnd = TID2Def[tid] as TableNameDef;

						object[][] result = new object[ROWIDS.Count][];

						int fnum=0;
						foreach(Field f in dataTableFields)
						{
							if((!f.bIndexed)||(ROWIDS.Count<2))
							{
								int valSize = (int)f.DataSize();
								long Capacity = (PageSize-ContentPageDataOffset)/valSize;
								ArrayList pages = f.DataFID;
								if((pages.Count*Capacity)<tnd.rownum)
									throw new Exception("Row num corrupted.");

								int rownum=0;
								foreach(long rowid in ROWIDS)
								{
									if(fnum==0) result[rownum] = new object[dataTableFields.Count];
								
								
									long npage = rowid / Capacity;
									long offset = rowid % Capacity;
									int page = (int)pages[(int)npage];
									//br.BaseStream.Position = (page*PageSize)+ContentPageDataOffset+offset*valSize;
									//object oldkey = f.ReadData(br);
									object oldkey = f.ReadData( this.PageReader(page,ContentPageDataOffset+offset*valSize) );

									result[rownum][fnum]=oldkey;
									rownum++;
								}
							}
							else
							{// accelerator for large rows
								Index ndx = GetIndex(TableName,f);
								SortedList sl = null;
								try
								{
									sl = ndx.Row2KeySL();
								}
								catch(Exception ex)
								{
									this.Close();
									this.LogToFile(ex.Message,ex.StackTrace);
									throw new Exception("Reverse index error in "+TableName+"."+f.Name+" .");
								}
								int rownum=0;
								foreach(long rowid in ROWIDS)
								{
									if(fnum==0) result[rownum] = new object[dataTableFields.Count];
									object oldkey = sl[rowid];
									result[rownum][fnum]=oldkey;
									rownum++;
								}
							}
							fnum++;
						}
						dt2 = new DataTable2(dt,result);
					}
					else
						dt2 = new DataTable2(dt,new object[0]);
					
					QueryCacheSet(Fields,From_TableName,Where_NameCondValue,dt2);
					return dt2;
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
		#region -GetTableNames: Gets all tables in this database
		/// <summary>
		/// Gets all tables in this database
		/// </summary>
		public void GetTableNames(out string[] Names)
		{
			lock(this.TableBlocking)
			{
				RaiseExceptionIfClosed();
				Names = (string[])new ArrayList( TableName2TID.Keys ).ToArray(typeof(string));
			}
		}
		#endregion
		#region -ExistsTable: Returns true if the table exists
		/// <summary>
		/// Returns true if the table exists
		/// </summary>
		public bool ExistsTable(string TableName)
		{
			lock(this.TableBlocking)
			{
				RaiseExceptionIfClosed();
				return TableName2TID.ContainsKey(TableName);
			}
		}
		#endregion
		#region -DropFieldIfExists
		/// <summary>
		/// Drops a field if exists.
		/// </summary>
		/// <param name="TableName"></param>
		/// <param name="FieldName"></param>
		public void DropFieldIfExists(string TableName, string FieldName){
			lock(this.TableBlocking){
				if(this.ExistsTable(TableName))
				{
					if(ExistsField(TableName,FieldName))
						DropField(TableName,FieldName);
				}
			}
		}
		#endregion
		#region -FlushIndexes: Destroys in memory indexes.
		/// <summary>
		/// Destroys in memory indexes. Do this if your were walking for many tables and you memory resources are low.
		/// </summary>
		public void FlushIndexes()
		{
			lock(this.TableBlocking)
			{
				foreach(string table in TableBlocking.Keys)
					(TableBlocking[table] as SortedList).Clear();
				//FieldsCache.Clear();
			}
		}
		#endregion
		/// <summary>
		/// Renombra una tabla
		/// </summary>
		/// <param name="Name">Nombre de tabla antigüo</param>
		/// <param name="NewName">Nombre de tabla nuevo</param>
		public void RenameTable(string Name, string NewName)
		{
			lock(this.TableBlocking)
			{
				this.RaiseExceptionIfClosed();

				if(!TableName2TID.Contains(Name))
					throw new Exception("Table not present.");

				TableNameDef m_tndSRC = TableName2TID[Name] as TableNameDef;
				m_tndSRC.tname = NewName;
				TableName2TID.Remove( Name );
				TableName2TID[ m_tndSRC.tname ] = m_tndSRC;

				this.fsDB.Position = m_tndSRC.PageOfTableDef * PageSize;
				this.fsDB.Position += 1;//bw.Write( (bool)true ); // deleted
				//bw.Flush();
				this.fsDB.Position += 1;//bw.Write( (byte)TablePageType ); // pagetype
				//int tid = this.NextFID++;
				this.fsDB.Position += 4;//bw.Write( (int)tid ); // tid
				//tnd = new TableNameDef(tid,TablePage);
				//tnd.fseq=0;
				this.fsDB.Position += 4;//bw.Write( tnd.fseq );
				//tnd.rownum=0;
				this.fsDB.Position += 8;//bw.Write( tnd.rownum );
				//tnd.tname=Name;
				bw.Write( m_tndSRC.tname );
				bw.Flush();
			}
		}
		internal const string DeletedFieldName = "$Deleted";
		#region -AddTable
		/// <summary>
		/// Adds a table
		/// </summary>
		public void AddTable(string Name)
		{
			lock(this.TableBlocking)
			{
				this.RaiseExceptionIfClosed();
				if(TableName2TID.Contains(Name))
					throw new Exception("Table already present.");

				int TablePage = LockAvaiblePage();

				TableNameDef tnd=null;
				try
				{
					this.fsDB.Position = TablePage * PageSize;
					bw.Write( (bool)true ); // deleted
					bw.Flush();
					bw.Write( (byte)TablePageType ); // pagetype
					int tid = this.NextFID++;
					bw.Write( (int)tid ); // tid

					tnd = new TableNameDef(tid,TablePage);
					tnd.fseq=0;
					bw.Write( tnd.fseq );
					tnd.rownum=0;
					bw.Write( tnd.rownum );
					tnd.tname=Name;
					bw.Write( tnd.tname );
					bw.Flush();

					this.fsDB.Position = TablePage * PageSize;
					bw.Write( (bool)false ); // non-deleted
					bw.Flush();
					TID2Def[tid]=tnd;
					TableName2TID[tnd.tname]=tid;
					PeekPagesByFID(tid).Add(TablePage);
				}
				catch(Exception ex)
				{
					this.LogToFile(ex.Message,ex.StackTrace);
					throw new Exception("Write error.");
				}
				Field f = new Field(DeletedFieldName,true,FieldIndexing.IndexedNotUnique);
				AddField(tnd.tname,f);
			}
		}
		#endregion
		#region -DropTableIfExists: Drops a table if the table exists
		/// <summary>
		/// Drops a table if the table exists
		/// </summary>
		public bool DropTableIfExists(string TableName)
		{
			lock(this.TableBlocking)
			{
				RaiseExceptionIfClosed();
				if(ExistsTable(TableName))
				{
					DropTable(TableName);
					return true;
				}
				else return false;
			}
		}
		#endregion
		#region -DropTable
		/// <summary>
		/// Removes a table
		/// </summary>
		public void DropTable(string Name)
		{
			try
			{
				lock(this.TableBlocking)
				{
					this.RaiseExceptionIfClosed();
					if(!this.TableName2TID.ContainsKey(Name)) return;

					// log-secure operation

					// make log file 
					string lfn = DatabaseFilePath+".hlg";
					FileStream lf = new FileStream(lfn,FileMode.Create,FileAccess.Write,FileShare.None);
					BinaryWriter lfw = new BinaryWriter(lf,System.Text.Encoding.Unicode);
					lfw.Write( (bool)false ); // not valid
					lfw.Write( (byte)0 ); // delete pages operation 

					// make deleted pages inventory
					ArrayList al = new ArrayList();
					int tid = (int)TableName2TID[Name];
					al.AddRange( PeekPagesByFID(tid) );
					lfw.Write( (int)al.Count );
					foreach(int page in al)
						lfw.Write( page );
					lfw.Flush();
					lfw.BaseStream.Position=0;
					lfw.Write( (bool)true ); // valid
					lfw.Flush();
					
					// In memory deletion
					QueryCacheDestroy(Name);
					//FieldsCache[Name]=null; // cancel Field Cache
					TableBlocking.Remove(Name);
					TID2Def.Remove(tid);
					TableName2TID.Remove(Name);

					// deletion
					for(int n=0;n<al.Count;n++)
					{
						int page = (int)al[n];
						this.InvalidatePage(page);
						bw.BaseStream.Position=PageSize*( page );
						bw.Write( true ); // deleted
						DeletedPages.Add( page );
					}
					bw.Flush();
					lf.SetLength(0);
					lf.Close();
				}
			}
			catch(Exception ex)
			{
				this.LogToFile(ex.Message,ex.StackTrace);
				throw new Exception("Write error.");
			}
		}
		#endregion
		#region -ExistsField
		/// <summary>
		/// Checks if a given field exists
		/// </summary>
		/// <param name="TableName">Table name</param>
		/// <param name="FieldName">Field name</param>
		/// <returns></returns>
		public bool ExistsField(string TableName, string FieldName) {
			lock(this.TableBlocking) {
				this.RaiseExceptionIfClosed();
				// check table
				if(!this.TableName2TID.ContainsKey(TableName)){
					throw new Exception("Table not found.");
				}
				// check field
				Field[] flds = GetFields(TableName);
				Field fld=null;
				bool bMatch=false;
				foreach(Field i in flds) {
					if(FieldName==i.Name){
						fld=i;
						bMatch=true;
					}
				}
				if(!bMatch) return false;
				return true;
			}
		}
		#endregion
		#region -DropField
		/// <summary>
		/// Removes a field
		/// </summary>
		public void DropField(string TableName, string FieldName) {
			lock(this.TableBlocking) {
				this.RaiseExceptionIfClosed();
				// check table
				if(!this.TableName2TID.ContainsKey(TableName)){
					throw new Exception("Table not found.");
				}
				// check field
				Field[] flds = GetFields(TableName);
				Field fld=null;
				bool bMatch=false;
				foreach(Field i in flds) {
					if(FieldName==i.Name){
						fld=i;
						bMatch=true;
					}
				}
				if(!bMatch) throw new Exception("Field not found.");

				QueryCacheDestroy(TableName);// flush QueryCache
				pcInit();// flush paging cache
				// lookup table def
				// fld.PageOfFieldSef: pag de la def del campo
				// fld.DataFID: páginas de datos de la tabla
				int TID = (int)this.TableName2TID[TableName];
				TableNameDef tnd = this.TID2Def[TID] as TableNameDef;
				// rem field from tnd
				tnd.fseq2FieldDef.Remove(fld.seq); 
				// rem field from indexing
				if(this.TableBlocking.Contains(TableName))
					if((this.TableBlocking[TableName] as SortedList)[fld.seq]!=null)
						(this.TableBlocking[TableName] as SortedList).Remove(fld.seq); 
				// rem field page and data pages
				try {
					// log-secure operation
					ArrayList al = new ArrayList();
					al.Add( fld.PageOfFieldSef );
					al.AddRange( fld.DataFID );

					// make log file 
					string lfn = DatabaseFilePath+".hlg";
					FileStream lf = new FileStream(lfn,FileMode.Create,FileAccess.Write,FileShare.None);
					BinaryWriter lfw = new BinaryWriter(lf,System.Text.Encoding.Unicode);
					lfw.Write( (bool)false ); // not valid
					lfw.Write( (byte)0 ); // delete pages operation 

					// make deleted pages inventory
					lfw.Write( (int)al.Count );
					foreach(int page in al)
						lfw.Write( page );
					lfw.Flush();
					lfw.BaseStream.Position=0;
					lfw.Write( (bool)true ); // valid
					lfw.Flush();

					// deletion
					for(int n=0;n<al.Count;n++) {
						int page = (int)al[n];
						this.InvalidatePage(page);
						bw.BaseStream.Position=PageSize*( page );
						bw.Write( true ); // deleted
						DeletedPages.Add( page );
					}
					bw.Flush();
					lf.SetLength(0);
					lf.Close();
				
				}
				catch(Exception ex) {
					this.LogToFile(ex.Message,ex.StackTrace);
					throw new Exception("Write error.");
				}
			}
		}
		#endregion
		#region -GetTableLock
		/// <summary>
		/// Gets a table lock for lock{} statements
		/// </summary>
		/// <param name="TableName"></param>
		/// <returns></returns>
		public object GetTableLock(string TableName)
		{
			lock(this.TableBlocking)
			{
				if(!this.TableName2TID.Contains(TableName))
					throw new Exception("Table not present.");

				// set entry
				if(!this.TableBlocking.Contains(TableName))
					this.TableBlocking[TableName] = new SortedList();
				return this.TableBlocking[TableName];
			}
		}
		#endregion
		#region -AddField
		/// <summary>
		/// Adds a field
		/// </summary>
		public void AddField(string TableName, Field f)
		{	
			lock(this.TableBlocking)// Total blocking required
			{

				string tbl = TableName;
				QueryCacheDestroy(TableName);
				Field[] flds = GetFields(TableName);
				foreach(Field i in flds)
				{
					if(f.Name==i.Name)
						throw new Exception("Column already present.");
				}
				//FieldsCache[TableName]=null; // cancel Field Cache
				int tid = (int)this.TableName2TID[TableName];
				TableNameDef tnd = this.TID2Def[tid] as TableNameDef;
				try
				{
					// auto increment field seq
					int fseq = tnd.fseq++;
					this.fsDB.Position = tnd.PageOfTableDef * PageSize;
					this.fsDB.Position += 1; // skip delete
					this.fsDB.Position += 1; // skip type
					this.fsDB.Position += 4; // skip fid
					bw.Write( tnd.fseq );
					bw.Flush();

					// build page
					int page = this.LockAvaiblePage();
					this.fsDB.Position = page*PageSize;
					bw.Write( (bool)true ); // deleted
					bw.Flush();
					bw.Write( (byte)FieldPageType ); 
					bw.Write( tid );
					f.seq=fseq;
					f.tid=tid;
					f.Write(bw);
					bw.Flush();
					f.PageOfFieldSef=page;
					f.DataFID = new ArrayList();
					this.fsDB.Position = page*PageSize;
					bw.Write( (bool)false ); // active
					bw.Flush();
					tnd.fseq2FieldDef[f.seq]=f;
					PeekPagesByFID(tid).Add(page);

					// grow if it is needed
					if(tnd.rownum>0)
					{
						int valSize = (int)f.DataSize();
						long Capacity = (PageSize-ContentPageDataOffset)/valSize;
						ArrayList pages = f.DataFID;
						while((pages.Count*Capacity)<tnd.rownum)
						{
							int datapage = this.LockAvaiblePage();
							bw.BaseStream.Position = (datapage*PageSize);
							bw.Write( true );
							bw.Flush();
							bw.Write( (byte)Database.ContentPageType );
							bw.Write( tnd.TableFID );
							bw.Write( (int)f.seq );
							bw.Write( f.DataFID.Count );
							bw.Flush();
							for(int c=0;c<Capacity;c++)
							{
								bw.BaseStream.Position = (datapage*PageSize)+ContentPageDataOffset+c*valSize;
								f.WriteDefaultData(bw,false);
							}
							bw.Flush();
							bw.BaseStream.Position = (datapage*PageSize);
							bw.Write( (bool)false );
							bw.Flush();
							pages.Add(datapage);
							PeekPagesByFID(tid).Add(datapage);
							this.InvalidatePage(datapage);
						}
					}
				}
				catch(Exception ex)
				{
					this.LogToFile(ex.Message,ex.StackTrace);
					throw new Exception("Unhandled exception at AddField.");
				}
			}
			
		}
		#endregion
		//SortedList FieldsCache = new SortedList();
		#region -GetFields: Get fieldnames of a table
		/// <summary>
		/// Fields for users
		/// </summary>
		/// <param name="Name"></param>
		/// <returns></returns>
		public Field[] GetUserFields(string Name)
		{
			lock(this.TableBlocking)
			{
				try
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
				catch(Exception ex)
				{
					this.LogToFile(ex.Message,ex.StackTrace);
					throw new Exception("Uncached exception at GetUserFields.");
				}
			}
		}
		/// <summary>
		/// Get fieldnames of a table
		/// </summary>
		internal Field[] GetFields(string Name)
		{
			lock(this.TableBlocking)
			{
				
					if(!TableName2TID.Contains(Name))
						throw new Exception("Table not present.");
					int tid = (int)TableName2TID[Name];
					TableNameDef tnd = this.TID2Def[tid] as TableNameDef;
					Field[] flds = new Field[tnd.fseq2FieldDef.Values.Count];
					tnd.fseq2FieldDef.Values.CopyTo(flds,0);
					return flds;
				
			}
		}
		#endregion
		internal SortedList TableBlocking = new SortedList();
		#region -GetIndex: Reads or regenerates an index for a field in a table
		/// <summary>
		/// Reads or regenerates an index for a field in a table
		/// </summary>
		/// <param name="TableName"></param>
		/// <param name="f"></param>
		/// <value></value>
		Index GetIndex(string TableName, Field f)
		{

				lock(this.GetTableLock(TableName))
				{
					if((this.TableBlocking[TableName] as SortedList)[f.seq]!=null)
						return (this.TableBlocking[TableName] as SortedList)[f.seq] as Index;
	
					// unique key
					if(!f.bIndexed) 
						throw new Exception("Not indexed field.");

					Index ndx = new Index();
					ndx.bUnique=f.bUnique;

					int tid = (int)TableName2TID[TableName];
					TableNameDef tnd = TID2Def[tid] as TableNameDef;
					int valSize = (int)f.DataSize();
					int Capacity = (PageSize-ContentPageDataOffset)/valSize;
					ArrayList pages = f.DataFID;
					if((pages.Count*Capacity)<tnd.rownum)
						throw new Exception("Row num corrupted.");
					if(f.seq==0)
					{
						for(int row=0;row<tnd.rownum;row++)
						{
							int npage = row / Capacity;
							int offset = row % Capacity;
							int page = (int)pages[npage];
							//br.BaseStream.Position = (page*PageSize)+ContentPageDataOffset+offset*valSize;
							//object val = f.ReadData(br);
							object val = f.ReadData( this.PageReader(page,ContentPageDataOffset+offset*valSize) );
							ndx.Add(val,row,f.Name);
						}
					}
					else
					{// exclude deleted
						Index ndxDeleted = GetIndex(TableName,tnd.fseq2FieldDef[(long)0] as Field);
						Set dset = ndxDeleted.GetRowSet(true);
						for(long row=0;row<tnd.rownum;row++)
						{
							if(!dset.Contains(row))
							{
								int npage = (int)(row / Capacity);
								int offset = (int)( row % Capacity);
								int page = (int)pages[npage];
								//br.BaseStream.Position = (page*PageSize)+ContentPageDataOffset+offset*valSize;
								//object val = f.ReadData(br);
								object val = f.ReadData( this.PageReader(page,ContentPageDataOffset+offset*valSize) );
								ndx.Add(val,row,f.Name);
							}
						}
					}
					(this.TableBlocking[TableName] as SortedList)[f.seq]=ndx;
					return ndx;
				}

		}
		#endregion
		#region ExecuteWhere: Executes the where part and return the rowids affected
		internal void ExecuteWhere(string From_TableName, object[,] Where_NameCondValue, out Set ROWIDS)
		{
			
		
				string TableName=From_TableName;
				if(Where_NameCondValue==null) Where_NameCondValue=new object[0,0];
				Field[] flds = GetFields(TableName);

			// Assertion
			if(true)
			{
				Index DELNDX = GetIndex(From_TableName,flds[0]);
				int __rows1 = DELNDX.GetRowCountForKey(false); // non-deleted rows count
				for(int k=1;k<flds.Length;k++)
				{
					if(flds[k].bIndexed)
					{
						Index _NDX = GetIndex(From_TableName,flds[k]);
						if(_NDX.ReverseCount()!=__rows1)
							throw new Exception("Corruption error.");
					}
				}
			}

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
				if(Where.Count>0)
				{
					ROWIDS = null;
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
									Index ndx = this.GetIndex(TableName,f);
									if(ndx.ExistsKey(val))
									{
										long row = ndx.PeekOne(val);
										if(ROWIDS==null)
										{
											ROWIDS = new SortedSet();
											ROWIDS.Add( row );
											continue;
										}
										else if(ROWIDS.Contains( row ))
										{
											ROWIDS = new SortedSet();
											ROWIDS.Add( row );
											continue;
										}
										else
										{
											ROWIDS = new SortedSet();
											break;
										}
									}
									else
									{
										ROWIDS = new SortedSet();
										break;
									}
								}
//// beta begin
//								if(op=="!=")
//								{
//									Index ndx = this.GetIndex(TableName,f);
//									if(ndx.ExistsUnique(val))
//									{// the value exists in the index
//										// If the rowids collection do not exist already
//										if(ROWIDS==null)
//										{
//											// Fill a rowid collection without the removed rowid
//											rowids = new ArrayList( ndx.ht.Values );
//											rowids.Remove(ndx.ht[val]);
//											continue;
//										}
//										// If the rowids collection already exists
//										else
//										{
//											// If contains the value to exclude
//											if(rowids.Contains(val))
//											{
//												rowids.Remove(ndx.ht[val]);
//												if(rowids.Count==0) break; 
//												continue;
//											}
//											// If do not contains the value to exclude
//											else
//											{
//												continue;
//											}
//										}
//									}
//									else
//									{// the value do not exist in the index
//										continue;
//									}
//								}
////end beta
							}
							else
							{// clave no única
								if(ROWIDS==null)
								{
									Index ndx = this.GetIndex(TableName,flds[0]);
									ROWIDS = new SortedSet( ndx.GetRowSet(false) );
								}
								// begin op =
								if(op=="=")
								{
									Index ndx = this.GetIndex(TableName,f);
									Set hs = ndx.GetRowSet(val);
									if(hs.Count<ROWIDS.Count)
									{// hs is smaller than ROWIDS
										Set newSet = new SortedSet();
										foreach(long row in hs)
										{
											if(ROWIDS.Contains(row))
												newSet.Add(row);
										}
										ROWIDS=newSet;
									}
									else
									{// ROWIDS is smaller than hs
										Set newSet = new SortedSet();
										foreach(long row in ROWIDS)
										{
											if(hs.Contains(row))
												newSet.Add(row);
										}
										ROWIDS=newSet;
									}
									//ROWIDS = ROWIDS.Intersect( hs);
									if(ROWIDS.Count==0) break;
									else continue;
								}
								// end of op =
								// begin op !=
								if(op=="!=")
								{
									Index ndx = this.GetIndex(TableName,f);
									Set hs = ndx.GetRowSet(val);
									ROWIDS = ROWIDS.Minus( hs);
									if(ROWIDS.Count==0) break;
									else continue;
								}
								// end of op !=
								// begin op >
								if((op=="<")||(op==">")||(op=="<=")||(op==">=")) 
								{
									Index ndx = this.GetIndex(TableName,f);

									// Metemos en un set todos los ids de fila que llevamos hasta ahora
									// para luego irlas poniendo en una lista de seleccionadas
									Set nSet = new SortedSet();
									IComparable v = (IComparable)val;
									foreach(object key in ndx.Keys)
									{
										IComparable o = (IComparable)key;
										bool bAdd=false;
										if((op==">")&&(o.CompareTo(v)>0)) bAdd=true;
										else if((op=="<")&&(o.CompareTo(v)<0)) bAdd=true;
										else if((op==">=")&&(o.CompareTo(v)>=0)) bAdd=true;
										else if((op=="<=")&&(o.CompareTo(v)<=0)) bAdd=true;
										if(bAdd) 
											nSet = nSet.Union( ndx.GetRowSet(key) );
									}
									ROWIDS = ROWIDS.Intersect( nSet );
									if(ROWIDS.Count==0) break; 
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
						if(ROWIDS==null)
						{
							Index ndx = this.GetIndex(TableName,flds[0]);
							ROWIDS = new SortedSet( ndx.GetRowSet(false) ); 
						}
						if(true)
						{
							int tid = (int)TableName2TID[TableName];
							TableNameDef tnd = TID2Def[tid] as TableNameDef;
							int valSize = (int)f.DataSize();
							int Capacity = (PageSize-ContentPageDataOffset)/valSize;
							ArrayList pages = f.DataFID;
							if((pages.Count*Capacity)<tnd.rownum)
								throw new Exception("Row num corrupted.");
							ArrayList new_rowids = new ArrayList();
							for(int row=0;row<tnd.rownum;row++)
							{
								long rowid = (long)row;
								if(ROWIDS.Contains(rowid))
								{
									int npage = row / Capacity;
									int offset = row % Capacity;
									int page = (int)pages[npage];
									//br.BaseStream.Position = (page*PageSize)+ContentPageDataOffset+offset*valSize;
									//object data = f.ReadData(br);
									object data = f.ReadData( this.PageReader(page,ContentPageDataOffset+offset*valSize) );
									IComparable o = (IComparable)data;
									IComparable v = (IComparable)val;
								
									if((op=="=")&&(o.CompareTo(v)==0)) {}
									else if((op==">")&&(o.CompareTo(v)>0)) {}
									else if((op=="<")&&(o.CompareTo(v)<0)) {}
									else if((op=="!=")&&(o.CompareTo(v)!=0)) {}
									else if((op==">=")&&(o.CompareTo(v)>=0)) {}
									else if((op=="<=")&&(o.CompareTo(v)<=0)) {}
									else 
										ROWIDS.Remove( rowid );
								}
							}
							if(ROWIDS.Count==0) break;
						}
					}
				}
				else
				{
					Index ndx = this.GetIndex(TableName,flds[0]);
					ROWIDS = new SortedSet( ndx.GetRowSet(false) );
				}
			// Assertion
			if(true)
			{
				Index DELNDX = GetIndex(From_TableName,flds[0]);
				int __rows1 = DELNDX.GetRowCountForKey(false); // non-deleted rows count
				for(int k=1;k<flds.Length;k++)
				{
					if(flds[k].bIndexed)
					{
						Index _NDX = GetIndex(From_TableName,flds[k]);
						if(_NDX.ReverseCount()!=__rows1)
							throw new Exception("Corruption error.");
					}
				}
			}
		}
		#endregion	
		#region AddFieldIfNotExist
		/// <summary>
		/// Adds a field if it not exists
		/// </summary>
		public void AddFieldIfNotExist(string TableName, Field f)
		{
			lock(this.TableBlocking)
			{
				Field[] flds = this.GetFields(TableName);
				Field DeletedField=null;
				foreach(Field i in flds)
				{
					if(i.Name==DeletedFieldName)
					{
						DeletedField=i;
						break;
					}
				}
				if(DeletedField==null) 
					throw new Exception("Deleted field not found.");
				foreach(Field i in flds)
				{
					if(i.Name==f.Name)
					{
						if((f.Indexing==FieldIndexing.IndexedNotUnique) && (i.Indexing==FieldIndexing.Unique))
						{
							// convert a field into non unique
							bw.BaseStream.Position = i.PageOfFieldSef*PageSize+1+1+4;
							i.bUnique=false;
							i.Write(bw);
							bw.Flush();
							if(this.TableBlocking[TableName]!=null)
								this.TableBlocking.Remove(TableName);
						}
						else if((f.Indexing==FieldIndexing.IndexedNotUnique) && (i.Indexing==FieldIndexing.None))
						{
							// convert a field into non unique
							bw.BaseStream.Position = i.PageOfFieldSef*PageSize+1+1+4;
							i.bIndexed=true;
							i.bUnique=false;
							i.Write(bw);
							bw.Flush();

							// This is not necessary but...
							if(this.TableBlocking[TableName]!=null)
								this.TableBlocking.Remove(TableName);
						}
						else if(i.type==FieldType.ftInt32 && f.type==FieldType.ftInt64)
						{// Upgrading Int32 to Int64
							this.LogToFile("Upgrading Int32 to Int64","Detected on field '"+i.Name+"', table '"+TableName+"'");
							// Añadimos un campo temporal
							Field F = new Field();
							F.Indexing=i.Indexing;
							F.Name=i.Name+"$tmp";
							F.type=f.type;
							F.DefaultValue=f.DefaultValue;

							this.Insert(tblAlterTbl,
								new object[,]{{"TNAME",TableName},{"STATE",0},{"FSRC",i.Name},{"FTMP",F.Name}});

							this.LogToFile("Upgrading Int32 to Int64","Temp field");
							AddField(TableName,F);

							this.ForcedInsert(tblAlterTbl,
								new object[,]{{"TNAME",TableName},{"FSRC",i.Name}},
								new object[,]{{"STATE",1}}
								);

							this.LogToFile("Upgrading Int32 to Int64","Copying data");
							int tid = (int)this.TableName2TID[TableName];
							TableNameDef tnd = this.TID2Def[tid] as TableNameDef;
							long[] data = new long[tnd.rownum];
							if(true)
							{// READ SOURCE DATA
								int valSize = (int)i.DataSize();
								long Capacity = (PageSize-ContentPageDataOffset)/valSize;
								ArrayList pages = i.DataFID;
								if((pages.Count*Capacity)<tnd.rownum)
									throw new Exception("Row num corrupted.");

								for(long rowid=0L;rowid<tnd.rownum;rowid++)
								{								
									long npage = rowid / Capacity;
									long offset = rowid % Capacity;
									int page = (int)pages[(int)npage];
									int SRC = (int)i.ReadData( this.PageReader(page,ContentPageDataOffset+offset*valSize) );
									data[rowid]=(long)SRC;
								}
							}
							if(true)
							{// WRITE SOURCE DATA
								int valSize = (int)F.DataSize();
								long Capacity = (PageSize-ContentPageDataOffset)/valSize;
								ArrayList pages = F.DataFID;
								if((pages.Count*Capacity)<tnd.rownum)
									throw new Exception("Row num corrupted.");

								for(long rowid=0L;rowid<tnd.rownum;rowid++)
								{								
									long npage = rowid / Capacity;
									long offset = rowid % Capacity;
									int page = (int)pages[(int)npage];
									bw.BaseStream.Position=ContentPageDataOffset+offset*valSize;
									Variant v = new Variant();
									v.obj=data[rowid];
									v.type=F.type;
									F.WriteData(bw,v,true);
									this.InvalidatePage(page);
								}
							}

							// COPY ENDED
							this.ForcedInsert(tblAlterTbl,
								new object[,]{{"TNAME",TableName},{"FSRC",i.Name}},
								new object[,]{{"STATE",2}}
								);

							this.LogToFile("Upgrading Int32 to Int64","Drop original");
							this.DropField(TableName,i.Name);

							// SOURCE FIELD DROPPED
							this.ForcedInsert(tblAlterTbl,
								new object[,]{{"TNAME",TableName},{"FSRC",i.Name}},
								new object[,]{{"STATE",3}}
								);

							// RENAME FIELD
							this.LogToFile("Upgrading Int32 to Int64","Replace original");
							//int page = this.LockAvaiblePage();
							this.fsDB.Position = F.PageOfFieldSef*PageSize;
							this.fsDB.Position += 1; //bw.Write( (bool)true ); // deleted
							//bw.Flush();
							this.fsDB.Position += 1; //bw.Write( (byte)FieldPageType ); 
							this.fsDB.Position += 4; //bw.Write( tid );
							F.Name=i.Name;
							//f.seq=fseq;
							//f.tid=tid;
							F.Write(bw);
							bw.Flush();
							//f.PageOfFieldSef=page;

							// PROCESS ENDED
							this.Delete(tblAlterTbl,
								new object[,]{{"TNAME","=",TableName},{"FSRC","=",i.Name}}
								);						

							// Force indexing flushing
							if(this.TableBlocking[TableName]!=null)
								this.TableBlocking.Remove(TableName);
						}
						return;
					}
					
				}
				this.AddField(TableName,f);
			}
		}
		#endregion
		#region -AddTableIfNotExist
		/// <summary>
		/// Adds a table if it not exists
		/// </summary>
		public void AddTableIfNotExist(string TableName)
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
		#endregion
		#region -DataRow2NameAndValue: Converts a DataRow in a NameAndValue object with skipRows
		/// <summary>
		/// Converts a DataRow in a NameAndValue object with skipRows.
		/// </summary>
		/// <param name="dr">DataRow - a Row in the resulted DataTable of a Select command.</param>
		/// <param name="skipRows">An array of strings to skip, usually the primary key of the table in Update statements.</param>
		/// <remarks>Used when you had make a select query and you want to reuse the result to make another one.</remarks>
		public static object[,] DataRow2NameAndValue(DataRow dr, string[] skipRows)
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
		#endregion
		#region -Lock
		/// <summary>
		/// Global lock, used to lock other thread to make concurrent queries.
		/// Example: Read and then insert.
		/// </summary>
		public object Lock
		{
			get
			{
				return TableBlocking;
			}
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
		public object ForcedSelect(string TableName, string KeyField, object KeyValue, string ValueField, object DefValue)
		{
			DataTable2 dt = Select2(new string[]{ValueField},TableName, new object[,]{{KeyField,"=",KeyValue}});
			if(dt.Rows.Length>0)
				return dt.GetValue(0,0);
			else 
				return DefValue;
		}
		/// <summary>
		/// Obtains a value and if it not exists obtains it's default value
		/// </summary>
		public object ForcedSelect(string TableName, object[,] Where, string ValueField, object DefValue)
		{
			DataTable2 dt = Select2(new string[]{ValueField},TableName, Where);
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
		#endregion
		#region Import from R1
		/// <summary>
		/// Imports from R1 to an R2 empty database.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool ImportFromR1( string path )
		{
			// Destination bd (this) must have only one table (seqs)
			string[] localTables; 
			this.GetTableNames(out localTables);
			if(localTables.Length!=1) return false;
			HyperNetDatabase.R1.Database r1DB = new HyperNetDatabase.R1.Database(path);
			string[] srcTables;
			r1DB.GetTableNames(out srcTables);
			foreach(string table in srcTables)
			{
				this.AddTableIfNotExist(table);
				HyperNetDatabase.R1.Field[] srcfs = r1DB.GetFields(table);
				foreach(HyperNetDatabase.R1.Field srcf in srcfs)
				{
					HyperNetDatabase.R2.Field newf = new Field();
					newf.DefaultValue=Variant.Object2Variant(srcf.DefaultValue.obj);
					newf.len=srcf.len;
					newf.Name=srcf.Name;
					newf.type= (FieldType)((int)srcf.type);
					newf.bIndexed=srcf.bIndexed;
					newf.bUnique=srcf.bUnique;
					this.AddFieldIfNotExist(table,newf);
				}
				DataTable srcdt = r1DB.Select(null, table, null);
				foreach(DataRow dr in srcdt.Rows)
				{
					object[] values = dr.ItemArray;
					object[,] NameAndValues = new object[srcdt.Columns.Count,2];
					for(int n=0;n<values.Length;n++)
					{
						NameAndValues[n,0] = srcdt.Columns[n].ColumnName;
						NameAndValues[n,1] = values[n];
					}
					this.Insert(table,NameAndValues);
				}
			}
			return true;
		}
		#endregion
		#region PageCache
		internal class PCEntry
		{
			internal BinaryReader br=null;
			//internal int refcnt=0;
			internal int page=0;
		}
		const int PageCacheSize=1223;
		//int RefCount=0;
		//const int MaxRefCount=200;
		PCEntry[] pcPageID2Entry = null;
		//Set pcPages = null;
		//ArrayList pcPageID2Rank = null;
		void pcInit()
		{
			pcPageID2Entry = new PCEntry[PageCacheSize];
		}
		BinaryReader PageReader(int page, long offset)
		{
			BinaryReader b = PageReader(page);
			b.BaseStream.Position=offset;
			return b;
		}
		BinaryReader PageReader(int page)
		{
			int hole = page % PageCacheSize;
			
			if((pcPageID2Entry[hole]!=null)&&(pcPageID2Entry[hole].page==page))
			{
				BinaryReader b = pcPageID2Entry[hole].br;
				b.BaseStream.Position=0;
				return b;
			}

			br.BaseStream.Position = (page*PageSize);
			byte[] bytes = br.ReadBytes(PageSize);
			pcPageID2Entry[hole] = new PCEntry();
			pcPageID2Entry[hole].br = new BinaryReader( new MemoryStream(bytes,false), System.Text.Encoding.Unicode);
			pcPageID2Entry[hole].page = page;
			return pcPageID2Entry[hole].br;
		}
		void InvalidatePage(int page)
		{
			int hole = page % PageCacheSize;
			pcPageID2Entry[hole]=null;
		}
		#endregion
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
	#region Index
	internal class Index
	{
		public bool bUnique=false;
		private SortedList ht = new SortedList();
		private SortedList reverse = new SortedList();
		public int ReverseCount()
		{
			return reverse.Count;
		}
		public int GetRowCountForKey(object key)
		{
			if(!ExistsKey(key)) return 0;
			Set s = ht[key] as Set;
			return s.Count;
			//return reverse.Count;
		}
		public bool ExistsKey(object key)
		{
			return (ht.ContainsKey(key));
		}
		public bool ExistsRowid(long rowid)
		{
			return (reverse.ContainsKey(rowid));
		}
		public long PeekOne(object key)
		{
			if(!ExistsKey(key)) 
				throw new Exception("Key not found (1).");
			if((ht[key] as Set).Count==0)
				throw new Exception("Key not found (2).");
			long l = (long)(ht[key] as Set).GetOne();
			//reverse.Remove(l);
			if(ht.Count>reverse.Count) throw new Exception("Fatal error on index.");
			return l;
		}
		public void RemoveByKey(object key)
		{
			if(!ExistsKey(key)) 
				throw new Exception("Key not found (3).");
			Set s = ht[key] as Set;
			foreach(long rowid in s)
			{
				reverse.Remove(rowid);
			}
			ht.Remove(key);
			if(ht.Count>reverse.Count) throw new Exception("Fatal error on index.");
		}
//		public void Rem(object key, long row)
//		{
//			if(!ExistsKey(key)) 
//				throw new Exception("Key not found (4).");
//			Set s = ht[key] as Set;
//			s.Remove(row);
//			reverse.Remove(row);
//			if(s.Count==0)
//				ht.Remove(key);
//		}
		public void Add(object key, long row, string FieldName)
		{
			if(!ExistsKey(key))
				ht[key] = new SortedSet();
			Set s = ht[key] as Set;
			s.Add(row);
			try
			{
				//reverse.Add(row,key);
				reverse[row]=key;
			}
			catch(Exception ex)
			{
				throw new  Exception(FieldName,ex);
			}
			if(ht.Count>reverse.Count) throw new  Exception("Fatal error on index.");
		}
		public void RemoveByRowid(long row)
		{
			if(!ExistsRowid(row)) 
				throw new Exception("Row not found (1).");
			object key = reverse[row];
			if(!ExistsKey(key)) 
				throw new Exception("Key not found (3).");
			Set s = ht[key] as Set;
			
			reverse.Remove(row);
			s.Remove(row);
			if(s.Count==0)
				ht.Remove(key);
//			ArrayList keys = new ArrayList( ht.Keys );
//			foreach(object key in keys)
//			{
//				Rem(key,row);
//			}
			if(ht.Count>reverse.Count) throw new Exception("Fatal error on index.");
		}
		public SortedList Row2KeySL()
		{
//				SortedList sl = new SortedList();
//				foreach(object key in ht.Keys)
//						foreach(long rowid in (ht[key] as Set))
//							sl.Add(rowid,key);
//				return sl;
			SortedList sl = reverse.Clone() as SortedList;
			if(ht.Count>reverse.Count) throw new Exception("Fatal error on index.");
			return sl;
		}
		public ICollection Keys
		{
			get
			{
				if(ht.Count>reverse.Count) throw new Exception("Fatal error on index.");
				return ht.Keys;
			}
		}
		public Set GetRowSet(object key)
		{
			if(!ExistsKey(key)) return new SortedSet();
			Set s = (ht[key] as Set).Clone() as Set;
			if(ht.Count>reverse.Count) throw new Exception("Fatal error on index.");
			return s;
		}
	}
	#endregion
	/// <summary>
	/// Indexes and fields
	/// </summary>
	public enum FieldIndexing
	{
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
		internal int tid; // fid of the owner table (NO RW)
		internal int PageOfFieldSef; // page of fielddef (NO RW)
		internal ArrayList DataFID = new ArrayList();
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
		#region ctors
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
		#region -Read: Reads field def
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
		#region -Write: Writes field def
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
		#region -DataSize: Returns a cell data size
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
		#region -WriteDefaultData: Writes a default cell data
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
		#region -WriteData: Writes cell data
		internal void WriteData(BinaryWriter bw, Variant v, bool bCommit)
		{
			const string FuncErrCode=ClassErrCode+".0005";
			try
			{
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
				if(bCommit)
					bw.Flush();
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message+"\n"+FuncErrCode+": Inner exception.");
			}
		}
		#endregion
		#region -ReadData: Reads a value into a datatable
		internal object ReadData(BinaryReader br)
		{
			const string FuncErrCode=ClassErrCode+".0006";
			try
			{
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
		internal void WriteToFieldDef(BinaryWriter bw, bool bCommit)
		{
			const string FuncErrCode=ClassErrCode+".0001";
			try{
				if(bCommit) bw.Flush();
				//BinaryWriter bw = new BinaryWriter(_bw.BaseStream,System.Text.Encoding.Unicode);
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
		/// <param name="br"></param>
		/// <returns></returns>
		internal static Variant ReadFromFieldDef(BinaryReader br)
		{
			Variant v= new Variant();
			const string FuncErrCode=ClassErrCode+".0002";
			try{
				//BinaryReader br = new BinaryReader(_br.BaseStream,System.Text.Encoding.Unicode);
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

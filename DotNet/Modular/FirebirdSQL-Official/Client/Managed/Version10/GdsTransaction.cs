/*
 *	Firebird ADO.NET Data provider for .NET and Mono 
 * 
 *	   The contents of this file are subject to the Initial 
 *	   Developer's Public License Version 1.0 (the "License"); 
 *	   you may not use this file except in compliance with the 
 *	   License. You may obtain a copy of the License at 
 *	   http://www.firebirdsql.org/index.php?op=doc&id=idpl
 *
 *	   Software distributed under the License is distributed on 
 *	   an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either 
 *	   express or implied. See the License for the specific 
 *	   language governing rights and limitations under the License.
 * 
 *	Copyright (c) 2002, 2007 Carlos Guzman Alvarez
 *	All Rights Reserved.
 */

using System;
using System.Data;
using System.IO;

using FirebirdSql.Data.Common;

namespace FirebirdSql.Data.Client.Managed.Version10
{
	internal sealed class GdsTransaction : ITransaction, IDisposable
	{
		#region � Events �

		public event TransactionUpdateEventHandler Update;

		#endregion

		#region � Fields �

		private int					handle;
		private bool				disposed;
		private GdsDatabase			database;
		private TransactionState	state;

		#endregion

		#region � Properties �

		public int Handle
		{
			get { return this.handle; }
		}

		public TransactionState State
		{
			get { return this.state; }
		}

		#endregion

		#region � Constructors �

		public GdsTransaction(IDatabase db)
		{
			if (!(db is GdsDatabase))
			{
				throw new ArgumentException("Specified argument is not of GdsDatabase type.");
			}

			this.database		= (GdsDatabase)db;
			this.state	= TransactionState.NoTransaction;

			GC.SuppressFinalize(this);
		}

		#endregion

		#region � Finalizer �

		~GdsTransaction()
		{
			this.Dispose(false);
		}

		#endregion

		#region � IDisposable methods �

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			lock (this)
			{
				if (!this.disposed)
				{
                    try
                    {
                        // release any unmanaged resources
                        this.Rollback();
                    }
                    catch
                    {
                    }
					finally
					{
                        // release any managed resources
                        if (disposing)
                        {
                            this.database     = null;
                            this.handle = 0;
                            this.state  = TransactionState.NoTransaction;
                        }
                        
                        this.disposed = true;
					}
				}
			}
		}

		#endregion

		#region � Methods �

		public void BeginTransaction(TransactionParameterBuffer tpb)
		{
			if (this.state != TransactionState.NoTransaction)
			{
				throw new IscException(IscCodes.isc_arg_gds, IscCodes.isc_tra_state, this.handle, "no valid");
			}

			lock (this.database.SyncObject)
			{
				try
				{
					this.database.Write(IscCodes.op_transaction);
					this.database.Write(this.database.Handle);
					this.database.WriteBuffer(tpb.ToArray());
					this.database.Flush();

                    GenericResponse response = this.database.ReadGenericResponse();

                    this.handle = response.ObjectHandle;
					this.state  = TransactionState.Active;

					this.database.TransactionCount++;
				}
				catch (IOException)
				{
					throw new IscException(IscCodes.isc_net_read_err);
				}
			}
		}

		public void Commit()
		{
			this.CheckTransactionState();

			lock (this.database.SyncObject)
			{
				try
				{
					this.database.Write(IscCodes.op_commit);
					this.database.Write(this.handle);
					this.database.Flush();

					this.database.ReadResponse();

					this.database.TransactionCount--;

					if (this.Update != null)
					{
						this.Update(this, new EventArgs());
					}

					this.state = TransactionState.NoTransaction;
				}
				catch (IOException)
				{
					throw new IscException(IscCodes.isc_net_read_err);
				}
			}
		}

		public void Rollback()
		{
			this.CheckTransactionState();

			lock (this.database.SyncObject)
			{
				try
				{
					this.database.Write(IscCodes.op_rollback);
					this.database.Write(this.handle);
					this.database.Flush();

					this.database.ReadResponse();

					this.database.TransactionCount--;

					if (this.Update != null)
					{
						this.Update(this, new EventArgs());
					}

					this.state = TransactionState.NoTransaction;
				}
				catch (IOException)
				{
					throw new IscException(IscCodes.isc_net_read_err);
				}
			}
		}

		public void CommitRetaining()
		{
			this.CheckTransactionState();

			lock (this.database.SyncObject)
			{
				try
				{
					this.database.Write(IscCodes.op_commit_retaining);
					this.database.Write(this.handle);
					this.database.Flush();

					this.database.ReadResponse();

					this.state = TransactionState.Active;
				}
				catch (IOException)
				{
					throw new IscException(IscCodes.isc_net_read_err);
				}
			}
		}

		public void RollbackRetaining()
		{
			this.CheckTransactionState();

			lock (this.database.SyncObject)
			{
				try
				{
					this.database.Write(IscCodes.op_rollback_retaining);
					this.database.Write(this.handle);
					this.database.Flush();

					this.database.ReadResponse();

					this.state = TransactionState.Active;
				}
				catch (IOException)
				{
					throw new IscException(IscCodes.isc_net_read_err);
				}
			}
		}

		#endregion

        #region � Two Phase Commit Methods �

        public void Prepare()
        {
            this.CheckTransactionState();

            lock (this.database.SyncObject)
            {
                try
                {
                    this.state = TransactionState.NoTransaction;

                    this.database.Write(IscCodes.op_prepare);
                    this.database.Write(this.handle);
                    this.database.Flush();

                    this.database.ReadResponse();

                    this.state = TransactionState.Prepared;
                }
                catch (IOException)
                {
                    throw new IscException(IscCodes.isc_net_read_err);
                }
            }
        }

        public void Prepare(byte[] buffer)
        {
            this.CheckTransactionState();

            lock (this.database.SyncObject)
            {
                try
                {
                    this.state = TransactionState.NoTransaction;

                    this.database.Write(IscCodes.op_prepare2);
                    this.database.Write(this.handle);
                    this.database.WriteBuffer(buffer, buffer.Length);
                    this.database.Flush();

                    this.database.ReadResponse();

                    this.state = TransactionState.Prepared;
                }
                catch (IOException)
                {
                    throw new IscException(IscCodes.isc_net_read_err);
                }
            }
        }

        #endregion

        #region � Private Methods �

        private void CheckTransactionState()
		{
			if (this.state != TransactionState.Active)
			{
				throw new IscException(IscCodes.isc_arg_gds, IscCodes.isc_tra_state, this.handle, "no valid");
			}
		}

		#endregion
	}
}

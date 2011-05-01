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

using FirebirdSql.Data.Common;
using FirebirdSql.Data.FirebirdClient;

namespace FirebirdSql.Data.Services
{
    public sealed class FbStatistical : FbService
    {
        #region � Fields �

        private FbStatisticalFlags options;

        #endregion

        #region � Properties �

        public FbStatisticalFlags Options
        {
            get { return this.options; }
            set { this.options = value; }
        }

        #endregion

        #region � Constructors �

        public FbStatistical()
            : base()
        { }

        #endregion

        #region � Methods �

        public void Execute()
        {
            try
            {
                this.StartSpb = new ServiceParameterBuffer();

                // Configure Spb
                this.StartSpb.Append(IscCodes.isc_action_svc_db_stats);
                this.StartSpb.Append(IscCodes.isc_spb_dbname, this.Database);
                this.StartSpb.Append(IscCodes.isc_spb_options, (int)this.options);

                // Start execution
                this.StartTask();

                // Process service output
                this.ProcessServiceOutput();
            }
            catch (Exception ex)
            {
                throw new FbException(ex.Message, ex);
            }
            finally
            {
                this.Close();
            }
        }

        #endregion
    }
}

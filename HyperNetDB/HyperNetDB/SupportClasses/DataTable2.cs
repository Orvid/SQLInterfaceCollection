using System;
using System.Data;
using System.Collections;
using System.Globalization;
namespace System.Data
{
    #region DataTable2

    /// <summary>
    /// Fast Datatable for fetching SELECT results
    /// </summary>
    [Serializable]
    public class DataTable2 : ICloneable
    {
        #region SchemaOnly: Schema of table
        /// <summary>
        /// Schema of table
        /// </summary>
        private DataTable m_dtSchemaOnly = new DataTable();
        /// <summary>
        /// Schema of table
        /// </summary>
        public DataTable SchemaOnly
        {
            get { return m_dtSchemaOnly; }
            set { m_dtSchemaOnly = value; }
        } 
        #endregion
        /// <summary>
        /// Array of arrays of objects as rows of table
        /// </summary>
        public object[] Rows = new object[0];
        #region Ctor
        /// <summary>
        /// Constructor
        /// </summary>
        public DataTable2(DataTable SchemaOnly, object[] Rows)
        {
            this.m_dtSchemaOnly = SchemaOnly;
            this.Rows = Rows;
        } 
        #endregion
        #region GetString
        /// <summary>
        /// Gets a string
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnIndex">Column index of result set (beginning at position 0)</param>
        /// <returns>Value at the given position</returns>
        public string GetString(int RowIndex, int ColumnIndex)
        {
            object o = GetValue(RowIndex, ColumnIndex);
            return CastString(o);
        }
        /// <summary>
        /// Gets a string
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnName">Column name</param>
        /// <returns>Value at the given position</returns>
        public string GetString(int RowIndex, string ColumnName)
        {
            object o = GetValue(RowIndex, ColumnName);
            return CastString(o);
        }

        private static string CastString(object o)
        {
            if (o == null)
                return null;
            if (o == DBNull.Value)
                return null;
            return o.ToString();
        } 
        #endregion
        #region GetInt32
        /// <summary>
        /// Gets an integer
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnIndex">Column index of result set (beginning at position 0)</param>
        /// <returns>Value at the given position</returns>
        public int GetInt32(int RowIndex, int ColumnIndex)
        {
            object o = GetValue(RowIndex, ColumnIndex);
            return ForceCastToInt32(o);
        }
        /// <summary>
        /// Gets an integer
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnName">Column name</param>
        /// <returns>Value at the given position</returns>
        public int GetInt32(int RowIndex, string ColumnName)
        {
            object o = GetValue(RowIndex, ColumnName);
            return ForceCastToInt32(o);
        }
        private static int? CastInt32(object o)
        {
            if (o == null)
                return null;
            else if (o == DBNull.Value)
                return null;
            else if (o is string)
                return int.Parse(o.ToString());
            else if (o is int)
                return (int)o;
            else if (o is long)
                return Convert.ToInt32((long)o);
            else
                return null;
        }
        private static int ForceCastToInt32(object o)
        {
            int? l = CastInt32(o);
            if (l == null)
                throw new InvalidCastException();
            else
                return (int)l;
        }
        #endregion
        #region TryGetInt32
        /// <summary>
        /// Gets an integer
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnIndex">Column index of result set (beginning at position 0)</param>
        /// <param name="CellValue">Value at the given position</param>
        /// <returns>True if returning a valid CellValue</returns>
        public bool TryGetInt32(int RowIndex, int ColumnIndex, out int CellValue)
        {
            object o = GetValue(RowIndex, ColumnIndex);
            return TryCastToInt32(o, out CellValue);
        }
        /// <summary>
        /// Gets an integer
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnName">Column name</param>
        /// <param name="CellValue">Value at the given position</param>
        /// <returns>True if returning a valid CellValue</returns>
        public bool TryGetInt32(int RowIndex, string ColumnName, out int CellValue)
        {
            object o = GetValue(RowIndex, ColumnName);
            return TryCastToInt32(o, out CellValue);
        }

        private static bool TryCastToInt32(object o, out int CellValue)
        {
            int? val = CastInt32(o);
            if (val == null)
            {
                CellValue = default(int);
                return false;
            }
            else
            {
                CellValue = (int)val;
                return true;
            }
        } 
        #endregion  
        #region GetInt64
        /// <summary>
        /// Gets a long
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnIndex">Column index of result set (beginning at position 0)</param>
        /// <returns>Value at the given position</returns>
        public long GetInt64(int RowIndex, int ColumnIndex)
        {
            object o = GetValue(RowIndex, ColumnIndex);
            return ForceCastToInt64(o);
        }

        private static long ForceCastToInt64(object o)
        {
            long? l = CastInt64(o);
            if (l == null)
                throw new InvalidCastException();
            else
                return (long)l;
        }
        /// <summary>
        /// Gets a long
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnName">Column name</param>
        /// <returns>Value at the given position</returns>
        public long GetInt64(int RowIndex, string ColumnName)
        {
            object o = GetValue(RowIndex, ColumnName);
            return ForceCastToInt64(o);
        } 
        #endregion
        #region TryGetInt64
        /// <summary>
        /// Gets a long
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnIndex">Column index of result set (beginning at position 0)</param>
        /// <param name="CellValue">Value at the given position</param>
        /// <returns>True if returning a valid CellValue</returns>
        public bool TryGetInt64(int RowIndex, int ColumnIndex, out long CellValue)
        {
            object o = GetValue(RowIndex, ColumnIndex);
            return TryCastToInt64(o, out CellValue);
        }

        private static bool TryCastToInt64(object o, out long CellValue)
        {
            long? l = CastInt64(o);
            if (l == null)
            {
                CellValue = default(long);
                return false;
            }
            else
            {
                CellValue = (long)l;
                return true;
            }
        }
        /// <summary>
        /// Gets a long
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnName">Column name</param>
        /// <param name="CellValue">Value at the given position</param>
        /// <returns>True if returning a valid CellValue</returns>
        public bool TryGetInt64(int RowIndex, string ColumnName, out long CellValue)
        {
            object o = GetValue(RowIndex, ColumnName);
            return TryCastToInt64(o, out CellValue);
        }
        private static long? CastInt64(object o)
        {
            if (o == null)
                return null;
            else if (o is long)
                return (long)o;
            else if (o is int)
                return Convert.ToInt64((int)o);
            else if (o == DBNull.Value)
                return null;
            else if (o is string)
                return long.Parse(o.ToString());
            else
                return null;
        }
        
        #endregion
        #region GetBoolean
        /// <summary>
        /// Gets a boolean
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnName">Column name</param>
        /// <returns>Value at the given position</returns>
        public bool GetBoolean(int RowIndex, string ColumnName)
        {
            object o = GetValue(RowIndex, ColumnName);
            bool? l = CastBoolean(o);
            if (l == null)
                throw new InvalidCastException();
            else
                return (bool)l;
        }
        /// <summary>
        /// Gets a boolean
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnIndex">Column index of result set (beginning at position 0)</param>
        /// <returns>Value at the given position</returns>
        public bool GetBoolean(int RowIndex, int ColumnIndex)
        {
            object o = GetValue(RowIndex, ColumnIndex);
            bool? l = CastBoolean(o);
            if (l == null)
                throw new InvalidCastException();
            else
                return (bool)l;
        } 
        #endregion
        #region TryGetBoolean
        /// <summary>
        /// Gets a boolean
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnName">Column name</param>
        /// <param name="CellValue">Value at the given position</param>
        /// <returns>True if returning a valid CellValue</returns>
        public bool TryGetBoolean(int RowIndex, string ColumnName, out bool CellValue)
        {
            object o = GetValue(RowIndex, ColumnName);
            return TryCastToBoolean(o, out CellValue);
        }
        /// <summary>
        /// Gets a boolean
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnIndex">Column index of result set (beginning at position 0)</param>
        /// <param name="CellValue">Value at the given position</param>
        /// <returns>True if returning a valid CellValue</returns>
        public bool TryGetBoolean(int RowIndex, int ColumnIndex, out bool CellValue)
        {
            object o = GetValue(RowIndex, ColumnIndex);
            return TryCastToBoolean(o, out CellValue);
        }

        private static bool TryCastToBoolean(object o, out bool CellValue)
        {
            bool? l = CastBoolean(o);
            if (l == null)
            {
                CellValue = default(bool);
                return false;
            }
            else
            {
                CellValue = (bool)l;
                return true;
            }
        }
        private static bool? CastBoolean(object o)
        {
            if (o == null)
                return null;
            else if (o is bool)
                return (bool)o;
            else if (o == DBNull.Value)
                return null;
            else if (o is string || o is int || o is long)
                return o.ToString() == "1" || o.ToString().ToLower() == "true";
            else
                return null;
        } 
        #endregion
        #region GetDecimal

        /// <summary>
        /// Gets a decimal
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnName">Column name</param>
        /// <returns>Value at the given position</returns>
        public decimal GetDecimal(int RowIndex, string ColumnName)
        {
            object o = GetValue(RowIndex, ColumnName);
            decimal? l = CastDecimal(o);
            if (l == null)
                throw new InvalidCastException();
            else
                return (decimal)l;
        }
        /// <summary>
        /// Gets a decimal
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnIndex">Column index of result set (beginning at position 0)</param>
        /// <returns>Value at the given position</returns>
        public decimal GetDecimal(int RowIndex, int ColumnIndex)
        {
            object o = GetValue(RowIndex, ColumnIndex);
            decimal? l = CastDecimal(o);
            if (l == null)
                throw new InvalidCastException();
            else
                return (decimal)l;
        } 
        #endregion
        #region TryGetDecimal
        /// <summary>
        /// Gets a decimal
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnName">Column name</param>
        /// <param name="CellValue">Value at the given position</param>
        /// <returns>True if returning a valid CellValue</returns>
        public bool TryGetDecimal(int RowIndex, string ColumnName, out decimal CellValue)
        {
            object o = GetValue(RowIndex, ColumnName);
            return TryCastToDecimal(o, out CellValue);
        }
        
        /// <summary>
        /// Gets a decimal
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnIndex">Column index of result set (beginning at position 0)</param>
        /// <param name="CellValue">Value at the given position</param>
        /// <returns>True if returning a valid CellValue</returns>
        public bool TryGetDecimal(int RowIndex, int ColumnIndex, out decimal CellValue)
        {
            object o = GetValue(RowIndex, ColumnIndex);
            return TryCastToDecimal(o, out CellValue);
        }

        private static bool TryCastToDecimal(object o, out decimal CellValue)
        {
            decimal? l = CastDecimal(o);
            if (l == null)
            {
                CellValue = default(decimal);
                return false;
            }
            else
            {
                CellValue = (decimal)l;
                return true;
            }
        }
        private static decimal? CastDecimal(object o)
        {
            if (o == null)
                return null;
            else if (o is decimal)
                return (decimal)o;
            else if (o == DBNull.Value)
                return null;
            else if (o is string)
                return decimal.Parse(o.ToString().Replace(',', '.'), CultureInfo.InvariantCulture);
            else if (o is long || o is int || o is short)
                return decimal.Parse(o.ToString(), CultureInfo.InvariantCulture);
            else if (o is Double)
                return Convert.ToDecimal((double)o);
            else if (o is float)
                return Convert.ToDecimal((float)o);
            else
                return null;
        }
        

        #endregion
        #region GetTimestamp
        /// <summary>
        /// Gets a datetime
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnName">Column name</param>
        /// <returns>Value at the given position</returns>
        public DateTime GetTimestamp(int RowIndex, string ColumnName)
        {
            object o = GetValue(RowIndex, ColumnName);
            return ForcedCastToTimestamp(o);
        }

        private static DateTime ForcedCastToTimestamp(object o)
        {
            DateTime? l = CastTimestamp(o);
            if (l == null)
                throw new InvalidCastException();
            else
                return (DateTime)l;
        }
        /// <summary>
        /// Gets a datetime
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnIndex">Column index of result set (beginning at position 0)</param>
        /// <returns>Value at the given position</returns>
        public DateTime GetTimestamp(int RowIndex, int ColumnIndex)
        {
            object o = GetValue(RowIndex, ColumnIndex);
            return ForcedCastToTimestamp(o);
        } 
        #endregion
        #region TryGetTimestamp
        /// <summary>
        /// Gets a datetime
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnName">Column name</param>
        /// <param name="CellValue">Value at the given position</param>
        /// <returns>True if returning a valid CellValue</returns>
        public bool TryGetTimestamp(int RowIndex, string ColumnName, out DateTime CellValue)
        {
            object o = GetValue(RowIndex, ColumnName);
            return TryCastToTimestamp(o, out CellValue);
        }

        private static bool TryCastToTimestamp(object o, out DateTime CellValue)
        {
            DateTime? l = CastTimestamp(o);
            if (l == null)
            {
                CellValue = default(DateTime);
                return false;
            }
            else
            {
                CellValue = (DateTime)l;
                return true;
            }
        }
        /// <summary>
        /// Gets a datetime
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnIndex">Column index of result set (beginning at position 0)</param>
        /// <param name="CellValue">Value at the given position</param>
        /// <returns>True if returning a valid CellValue</returns>
        public bool TryGetTimestamp(int RowIndex, int ColumnIndex, out DateTime CellValue)
        {
            object o = GetValue(RowIndex, ColumnIndex);
            return TryCastToTimestamp(o, out CellValue);
        }
        private static DateTime? CastTimestamp(object o)
        {
            if (o == null)
                return null;
            else if (o == DBNull.Value)
                return null;
            else if (o is DateTime)
                return (DateTime)o;
            else if (o is string)
                try
                {
                    if (o.ToString().Contains("T"))
                        return DateTime.ParseExact(o.ToString(), "s", CultureInfo.InvariantCulture);
                    else
                        return DateTime.ParseExact(o.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                }
                catch
                {
                    return null;
                }
            else
                return null;
        } 
        #endregion
        /// <summary>
        /// Value is null?
        /// </summary>
        /// <param name="row"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool IsNull(int row, int index)
        {
            object o = GetValue(row, index);
            if (o == null || o == DBNull.Value)
                return true;
            return false;
        }
        /// <summary>
        /// Value is null?
        /// </summary>
        /// <param name="row"></param>
        /// <param name="fname"></param>
        /// <returns></returns>
        public bool IsNull(int row, string fname)
        {
            object o = GetValue(row, fname);
            if (o == null || o == DBNull.Value)
                return true;
            return false;
        }
        /// <summary>
        /// Get value
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnIndex">Column index of result set (beginning at position 0)</param>
        /// <returns></returns>
        //[Obsolete]
        public object GetValue(int RowIndex, int ColumnIndex)
        {
            if ((RowIndex >= this.Rows.Length) || (RowIndex < 0)) 
                throw new Exception("Row index out of bounds at DataTable2.");
            object[] row = this.Rows[RowIndex] as object[];
            if ((ColumnIndex >= row.Length) || (ColumnIndex < 0)) 
                throw new Exception("Field index out of bounds at DataTable2.");
            return row[ColumnIndex];
        }
        /// <summary>
        /// Sets a value
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="ColumnIndex">Column index of result set (beginning at position 0)</param>
        /// <param name="val"></param>
        [Obsolete]
        public void SetValue(int RowIndex, int ColumnIndex, object val)
        {
            if ((RowIndex >= this.Rows.Length) || (RowIndex < 0)) 
                throw new Exception("Row index out of bounds at DataTable2.");
            object[] row = this.Rows[RowIndex] as object[];
            if ((ColumnIndex >= row.Length) || (ColumnIndex < 0)) 
                throw new Exception("Field index out of bounds at DataTable2.");
            row[ColumnIndex] = val;
        }
        /// <summary>
        /// Get value
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="fieldname"></param>
        /// <returns></returns>
        //[Obsolete]
        public object GetValue(int RowIndex, string fieldname)
        {
            try
            {
                return GetValue(RowIndex, SchemaOnly.Columns.IndexOf(fieldname));
            }
            catch (Exception ex)
            {
                throw new Exception("At field " + fieldname + " and row " + RowIndex.ToString(), ex);
            }
        }
        /// <summary>
        /// Sets a value
        /// </summary>
        /// <param name="RowIndex">Row index of result set (beginning at position 0)</param>
        /// <param name="fieldname"></param>
        /// <param name="val"></param>
        [Obsolete]
        public void SetValue(int RowIndex, string fieldname, object val)
        {
            try
            {
                SetValue(RowIndex, SchemaOnly.Columns.IndexOf(fieldname), val);
            }
            catch (Exception ex)
            {
                throw new Exception("At field " + fieldname + " and row " + RowIndex.ToString(), ex);
            }
        }
        /// <summary>
        /// Sorts by one column
        /// </summary>
        /// <param name="column">Column name</param>
        /// <param name="asc">Ascending if true, Descending if false</param>
        public void Sort(string column, bool asc)
        {
            int ColumnIndex = SchemaOnly.Columns.IndexOf(column);
            if ((ColumnIndex >= this.SchemaOnly.Columns.Count) || (ColumnIndex < 0)) throw new Exception("Field index out of bounds at DataTable2.");
            Array.Sort(Rows, new SortComparer(ColumnIndex, asc));
        }
        private class SortComparer : IComparer
        {
            int ColumnIndex; bool asc;
            public SortComparer(int col, bool asc)
            {
                this.ColumnIndex = col;
                this.asc = asc;
            }
            #region IComparer Members

            public int Compare(object x, object y)
            {
                // TODO:  Add SortComparer.Compare implementation
                IComparable lValue = (IComparable)(((object[])x)[ColumnIndex]);
                IComparable rValue = (IComparable)(((object[])y)[ColumnIndex]);
                int cmp = lValue.CompareTo(rValue);
                if (!asc)
                    cmp = -cmp;
                return cmp;
            }

            #endregion

        }
        /// <summary>
        /// Converts a DataTable2 into a DataTable
        /// </summary>
        /// <returns></returns>
        /// <remarks>Can lead to invalid typed data</remarks>
        //[Obsolete]
        public DataTable ToDataTable()
        {
            DataTable2 dt2 = this;
            DataTable dt = dt2.SchemaOnly.Clone();
            dt.TableName = "";
            dt.BeginLoadData();
            dt.MinimumCapacity = dt2.Rows.Length;
            for (int RowIndex = 0; RowIndex < dt2.Rows.Length; RowIndex++)
            {
                dt.Rows.Add((object[])dt2.Rows[RowIndex]);
            }
            dt.AcceptChanges();
            dt.EndLoadData();
            return dt;
        }
        #region ICloneable Members
        /// <summary>
        /// Internal use
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            DataTable2 dt2 = new DataTable2(null, null);
            dt2.SchemaOnly = SchemaOnly.Clone();
            dt2.Rows = new object[Rows.Length];
            for (int n = 0; n < Rows.Length; n++)
            {
                object[] row = (Rows[n] as Object[]);
                dt2.Rows[n] = new object[row.Length];
                for (int m = 0; m < row.Length; m++)
                {
                    (dt2.Rows[n] as object[])[m] = row[m];
                }
            }
            return dt2;
        }

        #endregion
    }
    #endregion
}

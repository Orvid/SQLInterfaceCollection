using System;
using System.ComponentModel;
using System.Drawing;
using System.Resources;

namespace System.Windows.Forms
{
    /// <summary>
    /// custom class
    /// </summary>
	public class DataGridImageColumn:DataGridColumnStyle 
	{
		/*************************************************************************************/
		/***                                                                               ***/												   
		/***  Function   : public class DataGridImageColumn                                ***/
		/***  Last change: 26.10.2001                                                      ***/
		/***                                                                               ***/
		/***  Remarks    : the constructor of the class                                    ***/
		/***                                                                               ***/
		/*************************************************************************************/
		/// <summary>
		/// Remarks: the constructor of the class
		/// </summary>

		public DataGridImageColumn(/*PropertyDescriptor pcol*/) 
		{
		}

		//------------------------------------------------------------------------------------
		//------------------------------------------------------------------------------------
		//------------------------------------------------------------------------------------
		// ALL THESE METHODS MUST BE OVERRIDDEN FROM 'DataGridColumnStyle'
		//------------------------------------------------------------------------------------
		//------------------------------------------------------------------------------------
		//------------------------------------------------------------------------------------

		/*************************************************************************************/
		/***                                                                               ***/												   
		/***  Function   : protected override void Abort(int RowNum)                       ***/
		/***  Last change: 26.10.2001                                                      ***/
		/***                                                                               ***/
		/***  Remarks    : read-only, so nothing must be aborted                           ***/
		/***                                                                               ***/
		/*************************************************************************************/
		/// <summary>
		/// Remarks: read-only, so nothing must be aborted
		/// </summary>

		protected override void Abort(int RowNum) 
		{
		}

		/*************************************************************************************/
		/***                                                                               ***/												   
		/***  Function   : protected override bool Commit(CurrencyManager DataSource,      ***/
		/***                                              int RowNum)                      ***/
		/***  Last change: 26.10.2001                                                      ***/
		/***                                                                               ***/
		/***  Remarks    : read-only, so nothing must be committed                         ***/
		/***                                                                               ***/
		/*************************************************************************************/
		/// <summary>
		/// Remarks: read-only, so nothing must be committed
		/// </summary>

		protected override bool Commit(CurrencyManager DataSource,int RowNum) 
		{
			return true;
		}

		/*************************************************************************************/
		/***                                                                               ***/												   
		/***  Function   : protected override void Edit(CurrencyManager Source ,int Rownum,***/
		/***                                            Rectangle Bounds, bool ReadOnly,   ***/
		/***                                            string InstantText,                ***/
		/***                                            bool CellIsVisible)                ***/
		/***  Last change: 26.10.2001                                                      ***/
		/***                                                                               ***/
		/***  Remarks    : read-only, so nothing must could be edited                      ***/
		/***                                                                               ***/
		/*************************************************************************************/
		/// <summary>
		/// Remarks: read-only, so nothing must could be edited
		/// </summary>

		protected override void Edit(CurrencyManager Source ,int Rownum,Rectangle Bounds, bool ReadOnly,string InstantText, bool CellIsVisible) 
		{
		}

		/*************************************************************************************/
		/***                                                                               ***/												   
		/***  Function   : protected override int GetMinimumHeight()                       ***/
		/***  Last change: 26.10.2001                                                      ***/
		/***                                                                               ***/
		/***  Remarks    : returns the minimum height of the picture                       ***/
		/***                                                                               ***/
		/*************************************************************************************/
		/// <summary>
		/// Remarks: returns the minimum height of the picture
		/// </summary>

		protected override int GetMinimumHeight() 
		{
			//
			// return here your minimum height
			//
			return 60;
		}

		/*************************************************************************************/
		/***                                                                               ***/												   
		/***  Function   : protected override int GetPreferredHeight(Graphics g,           ***/
		/***                                                         object   Value)       ***/
		/***  Last change: 26.10.2001                                                      ***/
		/***                                                                               ***/
		/***  Remarks    : returns the preferred height of the picture                     ***/
		/***                                                                               ***/
		/*************************************************************************************/
		/// <summary>
		/// Remarks: returns the preferred height of the picture
		/// </summary>
 
		protected override int GetPreferredHeight(Graphics g ,object Value) 
		{
			//
			// return here your preferred height
			//
			return 16;
		}

		/*************************************************************************************/
		/***                                                                               ***/												   
		/***  Function   : protected override Size GetPreferredSize(Graphics g,            ***/
		/***                                                        object   Value)        ***/
		/***  Last change: 26.10.2001                                                      ***/
		/***                                                                               ***/
		/***  Remarks    : returns the preferred size of the picture                       ***/
		/***                                                                               ***/
		/*************************************************************************************/
		/// <summary>
		/// Remarks: returns the preferred size of the picture
		/// </summary>
 
		protected override Size GetPreferredSize(Graphics g, object Value) 
		{
			//
			// return here your preferred size
			//
			Size PicSize = new Size(16,16);
			return PicSize;
		}

		/*************************************************************************************/
		/***                                                                               ***/												   
		/***  Function   : protected override void Paint( ... )                            ***/
		/***  Last change: 26.10.2001                                                      ***/
		/***                                                                               ***/
		/***  Remarks    : the paint method of the 'DataGridColumnStyle' class do the      ***/
		/***               work. There exist three overloaded versions of this method      ***/
		/***                                                                               ***/
		/*************************************************************************************/
		/// <summary>
		/// Remarks: the paint method of the 'DataGridColumnStyle' class do the work. There 
		///          exist three overloaded versions of this method
		/// </summary>
 
		protected override void Paint(Graphics g,Rectangle Bounds,CurrencyManager Source,int RowNum) 
		{
			SolidBrush BackBrush = new SolidBrush(Color.White);

			Image ImagePic = (Image) GetColumnValueAtRow(Source, RowNum);
			g.FillRectangle(BackBrush, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
			g.DrawImage((Image) ImagePic, Bounds.X + ((Bounds.Width - ImagePic.Width)>>1), Bounds.Y, ImagePic.Width, ImagePic.Height);
		}
        /// <summary>
        /// [INTERNAL]
        /// </summary>
        /// <param name="g"></param>
        /// <param name="Bounds"></param>
        /// <param name="Source"></param>
        /// <param name="RowNum"></param>
        /// <param name="AlignToRight"></param>
		protected override void Paint(Graphics g,Rectangle Bounds,CurrencyManager Source,int RowNum,bool AlignToRight) 
		{
			SolidBrush BackBrush = new SolidBrush(Color.White);

			Image ImagePic = (Image) GetColumnValueAtRow(Source, RowNum);
			g.FillRectangle(BackBrush, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
			g.DrawImage((Image) ImagePic, Bounds.X + ((Bounds.Width - ImagePic.Width)>>1), Bounds.Y, ImagePic.Width, ImagePic.Height);
		}
        /// <summary>
        /// [INTERNAL]
        /// </summary>
        /// <param name="g"></param>
        /// <param name="Bounds"></param>
        /// <param name="Source"></param>
        /// <param name="RowNum"></param>
        /// <param name="BackBrush"></param>
        /// <param name="ForeBrush"></param>
        /// <param name="AlignToRight"></param>
		protected override void Paint(Graphics g,Rectangle Bounds,CurrencyManager Source,int RowNum, Brush BackBrush ,Brush ForeBrush ,bool AlignToRight) 
		{
			Image ImagePic = (Image) GetColumnValueAtRow(Source, RowNum);
			g.FillRectangle(BackBrush, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
			g.DrawImage((Image) ImagePic, Bounds.X + ((Bounds.Width - ImagePic.Width)>>1), Bounds.Y, ImagePic.Width, ImagePic.Height);
		}
//
//		/*************************************************************************************/
//		/***                                                                               ***/												   
//		/***  Function   : private Bitmap ExtractImageFromOleContainer(byte[] Data)        ***/
//		/***  Last change: 26.10.2001                                                      ***/
//		/***                                                                               ***/
//		/***  Remarks    : extract bitmap from OLE container                               ***/
//		/***                                                                               ***/
//		/*************************************************************************************/
//		/// <summary>
//		/// Remarks: extract bitmap from OLE container
//		/// </summary>
//		private Bitmap ExtractImageFromOleContainer(byte[] Data) 
//		{
//			ResourceManager         rm         = new ResourceManager("DataGridWithImages.Resource",this.GetType().Assembly);
//			byte[]                  ImageArray = new byte[8000];
//			System.IO.MemoryStream  stream     = null;
//			Bitmap                  Picture;
//
//			// 0x1C15 = OLEContainer signature
//			if(Data[0] == 0x15 && Data[1] == 0x1C) 
//			{
//				// extract bitmap data from container
//				try 
//				{
//					System.Buffer.BlockCopy(Data, 0x48, ImageArray, 0, Data.Length - 0x48);
//					stream = new System.IO.MemoryStream(ImageArray); 
//					Picture = new Bitmap(stream);
//				}
//				catch 
//				{
//					Picture = (Bitmap) rm.GetObject("Error");
//				}
//				return Picture;
//			}
//			else 
//			{
//				// if header incorrect, return default bitmap
//				return (Bitmap) rm.GetObject("Error");
//			}
//		}
	}
}

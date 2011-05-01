/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BerkeleyDb
{
    /*
    * XA Switch Data Structure
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize), CLSCompliant(false)]
    public unsafe struct XA_SWITCH
    {
      /* length of resource manager name, including the null terminator */
      public const int RMNAMESZ = 32;
      /* maximum size in bytes of xa_info strings, including the null terminator */
      public const int MAXINFOSIZE = 256;

	    public fixed byte name[RMNAMESZ];		/* name of resource manager */
      public readonly uint flags;			    /* resource manager specific options */
      public readonly uint version;			  /* must be 0 */

#if false
	int (*xa_open_entry)		/* xa_open function pointer */
	    __P((char *, int, long));
	int (*xa_close_entry)		/* xa_close function pointer */
	    __P((char *, int, long));
	int (*xa_start_entry)		/* xa_start function pointer */
	    __P((XID *, int, long));
	int (*xa_end_entry)		/* xa_end function pointer */
	    __P((XID *, int, long));
	int (*xa_rollback_entry)	/* xa_rollback function pointer */
	    __P((XID *, int, long));
	int (*xa_prepare_entry)		/* xa_prepare function pointer */
	    __P((XID *, int, long));
	int (*xa_commit_entry)		/* xa_commit function pointer */
	    __P((XID *, int, long));
	int (*xa_recover_entry)		/* xa_recover function pointer */
	    __P((XID *, long, int, long));
	int (*xa_forget_entry)		/* xa_forget function pointer */
	    __P((XID *, int, long));
	int (*xa_complete_entry)	/* xa_complete function pointer */
	    __P((int *, int *, int, long));
#endif
  }
}

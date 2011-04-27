/**
 * com.mckoi.database.TBinaryType  31 Jul 2002
 *
 * Mckoi SQL Database ( http://www.mckoi.com/database )
 * Copyright (C) 2000, 2001, 2002  Diehl and Associates, Inc.
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * Version 2 as published by the Free Software Foundation.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License Version 2 for more details.
 *
 * You should have received a copy of the GNU General Public License
 * Version 2 along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 * Change Log:
 * 
 * 
 */

package com.mckoi.database;

import java.io.InputStream;
import java.io.BufferedInputStream;
import java.io.IOException;

import com.mckoi.database.global.SQLTypes;
import com.mckoi.database.global.ByteLongObject;
import com.mckoi.database.global.BlobRef;
import com.mckoi.database.global.BlobAccessor;

/**
 * An implementation of TType for a binary block of data.
 *
 * @author Tobias Downer
 */

public class TBinaryType extends TType {

  static final long serialVersionUID = 5141996433600529406L;

  /**
   * This constrained size of the binary block of data or -1 if there is no
   * size limit.
   */
  private int max_size;

  /**
   * Constructs the type.
   */
  public TBinaryType(int sql_type, int max_size) {
    super(sql_type);
    this.max_size = max_size;
  }

  /**
   * Returns the maximum size of this binary type.
   */
  public int getMaximumSize() {
    return max_size;
  }

  // ---------- Static utility method for comparing blobs ----------
  
  /**
   * Utility method for comparing one blob with another.  Uses the
   * BlobAccessor interface to compare the blobs.  This will collate larger
   * blobs higher than smaller blobs.
   */
  static int compareBlobs(BlobAccessor blob1, BlobAccessor blob2) {
    // We compare smaller sized blobs before larger sized blobs
    int c = blob1.length() - blob2.length();
    if (c != 0) {
      return c;
    }
    else {
      // Size of the blobs are the same, so find the first non equal byte in
      // the byte array and return the difference between the two.  eg.
      // compareTo({ 0, 0, 0, 1 }, { 0, 0, 0, 3 }) == -3

      int len = blob1.length();

      InputStream b1 = blob1.getInputStream();
      InputStream b2 = blob2.getInputStream();
      try {
        BufferedInputStream bin1 = new BufferedInputStream(b1);
        BufferedInputStream bin2 = new BufferedInputStream(b2);
        while (len > 0) {
          c = bin1.read() - bin2.read();
          if (c != 0) {
            return c;
          }
          --len;
        }
      
        return 0;
      }
      catch (IOException e) {
        throw new RuntimeException("IO Error when comparing blobs: " +
                                   e.getMessage());
      }
    }
  }
  
  // ---------- Implemented from TType ----------

  public boolean comparableTypes(TType type) {
    return (type instanceof BlobAccessor);
  }
  
  public int compareObs(Object ob1, Object ob2) {
    if (ob1 == ob2) {
      return 0;
    }

    BlobAccessor blob1 = (BlobAccessor) ob1;
    BlobAccessor blob2 = (BlobAccessor) ob2;
    
    return compareBlobs(blob1, blob2);
  }
  
  public int calculateApproximateMemoryUse(Object ob) {
    if (ob != null) {
      if (ob instanceof BlobRef) {
        return 256;
      }
      else {
        return ((ByteLongObject) ob).length() + 24;
      }
    }
    else {
      return 32;
    }
  }
  
  public Class javaClass() {
    return BlobAccessor.class;
  }

}

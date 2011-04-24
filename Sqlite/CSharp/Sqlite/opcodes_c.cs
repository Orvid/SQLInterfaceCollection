/*
*************************************************************************
**  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
**  C#-SQLite is an independent reimplementation of the SQLite software library
**
**  SQLITE_SOURCE_ID: 2009-12-07 16:39:13 1ed88e9d01e9eda5cbc622e7614277f29bcc551c
**
**  $Header: Community.CsharpSqlite/src/opcodes_c.cs,v bcbd36f24b23 2010/02/18 17:35:24 Noah $
*************************************************************************
*/
namespace Community.CsharpSqlite
{
  public partial class Sqlite3
  {
    /* Automatically generated.  Do not edit */
    /* See the mkopcodec.awk script for details. */
#if !SQLITE_OMIT_EXPLAIN || !NDEBUG || VDBE_PROFILE || SQLITE_DEBUG
    static string sqlite3OpcodeName( int i )
    {
      string[] azName =  { "?",
     /*   1 */ "Goto",
     /*   2 */ "Gosub",
     /*   3 */ "Return",
     /*   4 */ "Yield",
     /*   5 */ "HaltIfNull",
     /*   6 */ "Halt",
     /*   7 */ "Integer",
     /*   8 */ "Int64",
     /*   9 */ "String",
     /*  10 */ "Null",
     /*  11 */ "Blob",
     /*  12 */ "Variable",
     /*  13 */ "Move",
     /*  14 */ "Copy",
     /*  15 */ "SCopy",
     /*  16 */ "ResultRow",
     /*  17 */ "CollSeq",
     /*  18 */ "Function",
     /*  19 */ "Not",
     /*  20 */ "AddImm",
     /*  21 */ "MustBeInt",
     /*  22 */ "RealAffinity",
     /*  23 */ "Permutation",
     /*  24 */ "Compare",
     /*  25 */ "Jump",
     /*  26 */ "If",
     /*  27 */ "IfNot",
     /*  28 */ "Column",
     /*  29 */ "Affinity",
     /*  30 */ "MakeRecord",
     /*  31 */ "Count",
     /*  32 */ "Savepoint",
     /*  33 */ "AutoCommit",
     /*  34 */ "Transaction",
     /*  35 */ "ReadCookie",
     /*  36 */ "SetCookie",
     /*  37 */ "VerifyCookie",
     /*  38 */ "OpenRead",
     /*  39 */ "OpenWrite",
     /*  40 */ "OpenEphemeral",
     /*  41 */ "OpenPseudo",
     /*  42 */ "Close",
     /*  43 */ "SeekLt",
     /*  44 */ "SeekLe",
     /*  45 */ "SeekGe",
     /*  46 */ "SeekGt",
     /*  47 */ "Seek",
     /*  48 */ "NotFound",
     /*  49 */ "Found",
     /*  50 */ "IsUnique",
     /*  51 */ "NotExists",
     /*  52 */ "Sequence",
     /*  53 */ "NewRowid",
     /*  54 */ "Insert",
     /*  55 */ "InsertInt",
     /*  56 */ "Delete",
     /*  57 */ "ResetCount",
     /*  58 */ "RowKey",
     /*  59 */ "RowData",
     /*  60 */ "Rowid",
     /*  61 */ "NullRow",
     /*  62 */ "Last",
     /*  63 */ "Sort",
     /*  64 */ "Rewind",
     /*  65 */ "Prev",
     /*  66 */ "Next",
     /*  67 */ "IdxInsert",
     /*  68 */ "Or",
     /*  69 */ "And",
     /*  70 */ "IdxDelete",
     /*  71 */ "IdxRowid",
     /*  72 */ "IdxLT",
     /*  73 */ "IsNull",
     /*  74 */ "NotNull",
     /*  75 */ "Ne",
     /*  76 */ "Eq",
     /*  77 */ "Gt",
     /*  78 */ "Le",
     /*  79 */ "Lt",
     /*  80 */ "Ge",
     /*  81 */ "IdxGE",
     /*  82 */ "BitAnd",
     /*  83 */ "BitOr",
     /*  84 */ "ShiftLeft",
     /*  85 */ "ShiftRight",
     /*  86 */ "Add",
     /*  87 */ "Subtract",
     /*  88 */ "Multiply",
     /*  89 */ "Divide",
     /*  90 */ "Remainder",
     /*  91 */ "Concat",
     /*  92 */ "Destroy",
     /*  93 */ "BitNot",
     /*  94 */ "String8",
     /*  95 */ "Clear",
     /*  96 */ "CreateIndex",
     /*  97 */ "CreateTable",
     /*  98 */ "ParseSchema",
     /*  99 */ "LoadAnalysis",
     /* 100 */ "DropTable",
     /* 101 */ "DropIndex",
     /* 102 */ "DropTrigger",
     /* 103 */ "IntegrityCk",
     /* 104 */ "RowSetAdd",
     /* 105 */ "RowSetRead",
     /* 106 */ "RowSetTest",
     /* 107 */ "Program",
     /* 108 */ "Param",
     /* 109 */ "FkCounter",
     /* 110 */ "FkIfZero",
     /* 111 */ "MemMax",
     /* 112 */ "IfPos",
     /* 113 */ "IfNeg",
     /* 114 */ "IfZero",
     /* 115 */ "AggStep",
     /* 116 */ "AggFinal",
     /* 117 */ "Vacuum",
     /* 118 */ "IncrVacuum",
     /* 119 */ "Expire",
     /* 120 */ "TableLock",
     /* 121 */ "VBegin",
     /* 122 */ "VCreate",
     /* 123 */ "VDestroy",
     /* 124 */ "VOpen",
     /* 125 */ "VFilter",
     /* 126 */ "VColumn",
     /* 127 */ "VNext",
     /* 128 */ "VRename",
     /* 129 */ "VUpdate",
     /* 130 */ "Real",
     /* 131 */ "Pagecount",
     /* 132 */ "Trace",
     /* 133 */ "Noop",
     /* 134 */ "Explain",
     /* 135 */ "NotUsed_135",
     /* 136 */ "NotUsed_136",
     /* 137 */ "NotUsed_137",
     /* 138 */ "NotUsed_138",
     /* 139 */ "NotUsed_139",
     /* 140 */ "NotUsed_140",
     /* 141 */ "ToText",
     /* 142 */ "ToBlob",
     /* 143 */ "ToNumeric",
     /* 144 */ "ToInt",
     /* 145 */ "ToReal",
};
      return azName[i];
    }
#endif
  }
}

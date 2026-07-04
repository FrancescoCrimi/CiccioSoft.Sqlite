// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite.Interop.Com;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VTable
{
    public uint Version;
    public SqliteVTable Db;
    public StmtVTable Stmt;
    public BackupVTable Backup;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct BackupVTable
{
    public delegate* unmanaged[Cdecl]<nint, byte*, nint, byte*, nint> backup_init;
    public delegate* unmanaged[Cdecl]<nint, int, int> backup_step;
    public delegate* unmanaged[Cdecl]<nint, int> backup_finish;
    public delegate* unmanaged[Cdecl]<nint, int> backup_remaining;
    public delegate* unmanaged[Cdecl]<nint, int> backup_pagecount;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct SqliteVTable
{
    public delegate* unmanaged[Cdecl]<byte*> libversion;
    public delegate* unmanaged[Cdecl]<int> libversion_number;
    public delegate* unmanaged[Cdecl]<nint, int> close;
    public delegate* unmanaged[Cdecl]<nint, byte*, void*, byte**, int> exec;
    public delegate* unmanaged[Cdecl]<nint, int, int> extended_result_codes;
    public delegate* unmanaged[Cdecl]<nint, long> last_insert_rowid;
    public delegate* unmanaged[Cdecl]<nint, long> changes;
    public delegate* unmanaged[Cdecl]<nint, long> total_changes;
    public delegate* unmanaged[Cdecl]<nint, void> interrupt;
    public delegate* unmanaged[Cdecl]<nint, int, int> busy_timeout;
    public delegate* unmanaged[Cdecl]<void*, void> free;
    public delegate* unmanaged[Cdecl]<byte*, nint*, int, byte*, int> open;
    public delegate* unmanaged[Cdecl, SuppressGCTransition]<nint, int> errcode;
    public delegate* unmanaged[Cdecl]<nint, int> extended_errcode;
    public delegate* unmanaged[Cdecl, SuppressGCTransition]<nint, byte*> errmsg;
    public delegate* unmanaged[Cdecl]<nint, int> error_offset;
    public delegate* unmanaged[Cdecl]<nint, int, int, int> limit;
    public delegate* unmanaged[Cdecl]<nint, byte*, int, uint, nint*, byte**, int> prepare;
    public delegate* unmanaged[Cdecl]<nint, int> get_autocommit;
    public delegate* unmanaged[Cdecl]<nint, byte*, int> txn_state;
    public delegate* unmanaged[Cdecl]<nint, byte*, int> db_readonly;
    public delegate* unmanaged[Cdecl]<nint, byte*, byte*, byte*, byte**, byte**, int*, int*, int*, int> table_column_metadata;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct StmtVTable
{
    public delegate* unmanaged[Cdecl]<nint, byte*> sql;
    public delegate* unmanaged[Cdecl]<nint, byte*> expanded_sql;
    public delegate* unmanaged[Cdecl]<nint, int> stmt_readonly;
    public delegate* unmanaged[Cdecl]<nint, int> stmt_busy;
    public delegate* unmanaged[Cdecl]<nint, int, byte*, int, int> bind_blob;
    public delegate* unmanaged[Cdecl, SuppressGCTransition]<nint, int, double, int> bind_double;
    public delegate* unmanaged[Cdecl, SuppressGCTransition]<nint, int, int, int> bind_int;
    public delegate* unmanaged[Cdecl, SuppressGCTransition]<nint, int, long, int> bind_int64;
    public delegate* unmanaged[Cdecl, SuppressGCTransition]<nint, int, int> bind_null;
    public delegate* unmanaged[Cdecl]<nint, int, byte*, int, int> bind_text;
    public delegate* unmanaged[Cdecl]<nint, int> bind_parameter_count;
    public delegate* unmanaged[Cdecl]<nint, int, byte*> bind_parameter_name;
    public delegate* unmanaged[Cdecl]<nint, byte*, int> bind_parameter_index;
    public delegate* unmanaged[Cdecl]<nint, int> clear_bindings;
    public delegate* unmanaged[Cdecl]<nint, int> column_count;
    public delegate* unmanaged[Cdecl]<nint, int, byte*> column_name;
    public delegate* unmanaged[Cdecl]<nint, int, byte*> column_database_name;
    public delegate* unmanaged[Cdecl]<nint, int, byte*> column_table_name;
    public delegate* unmanaged[Cdecl]<nint, int, byte*> column_origin_name;
    public delegate* unmanaged[Cdecl]<nint, int, byte*> column_decltype;
    public delegate* unmanaged[Cdecl]<nint, int> step;
    public delegate* unmanaged[Cdecl]<nint, int, void*> column_blob;
    public delegate* unmanaged[Cdecl]<nint, int, double> column_double;
    public delegate* unmanaged[Cdecl]<nint, int, int> column_int;
    public delegate* unmanaged[Cdecl]<nint, int, long> column_int64;
    public delegate* unmanaged[Cdecl]<nint, int, byte*> column_text;
    public delegate* unmanaged[Cdecl]<nint, int, void*> column_value;
    public delegate* unmanaged[Cdecl]<nint, int, int> column_bytes;
    public delegate* unmanaged[Cdecl]<nint, int, int> column_type;
    public delegate* unmanaged[Cdecl]<nint, int> finalize;
    public delegate* unmanaged[Cdecl]<nint, int> reset;
}

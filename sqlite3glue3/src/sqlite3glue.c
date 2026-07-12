// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

#include <sqlite3.h>
#include "sqlite3glue.h"

void get_vtable(vtable* table) {
	*table = (vtable) {
		.version	= 1u,

		.sqlite.libversion = sqlite3_libversion,
		.sqlite.libversion_number = sqlite3_libversion_number,
		.sqlite.close = sqlite3_close_v2,
		.sqlite.exec = sqlite3_exec,
		.sqlite.extended_result_codes = sqlite3_extended_result_codes,
		.sqlite.last_insert_rowid = sqlite3_last_insert_rowid,
		.sqlite.changes = sqlite3_changes,
		.sqlite.changes64 = sqlite3_changes64,
		.sqlite.total_changes = sqlite3_total_changes,
		.sqlite.total_changes64 = sqlite3_total_changes64,
		.sqlite.interrupt = sqlite3_interrupt,
		.sqlite.is_interrupted = sqlite3_is_interrupted,
		.sqlite.busy_timeout = sqlite3_busy_timeout,
		.sqlite.free = sqlite3_free,
		.sqlite.open = sqlite3_open_v2,
		.sqlite.errcode = sqlite3_errcode,
		.sqlite.extended_errcode = sqlite3_extended_errcode,
		.sqlite.errmsg = sqlite3_errmsg,
		.sqlite.errstr = sqlite3_errstr,
		.sqlite.error_offset = sqlite3_error_offset,
		.sqlite.limit = sqlite3_limit,
		.sqlite.prepare = sqlite3_prepare_v3,
		.sqlite.get_autocommit = sqlite3_get_autocommit,
		.sqlite.db_readonly = sqlite3_db_readonly,
		.sqlite.txn_state = sqlite3_txn_state,
		.sqlite.table_column_metadata = sqlite3_table_column_metadata,

		.stmt.sql = sqlite3_sql,
		.stmt.expanded_sql = sqlite3_expanded_sql,
		.stmt.stmt_readonly = sqlite3_stmt_readonly,
		.stmt.stmt_busy = sqlite3_stmt_busy,
		.stmt.bind_blob = sqlite3_bind_blob,
		.stmt.bind_double = sqlite3_bind_double,
		.stmt.bind_int = sqlite3_bind_int,
		.stmt.bind_int64 = sqlite3_bind_int64,
		.stmt.bind_null = sqlite3_bind_null,
		.stmt.bind_text = sqlite3_bind_text,
		.stmt.bind_value = sqlite3_bind_value,
		.stmt.bind_parameter_count = sqlite3_bind_parameter_count,
		.stmt.bind_parameter_name = sqlite3_bind_parameter_name,
		.stmt.bind_parameter_index = sqlite3_bind_parameter_index,
		.stmt.clear_bindings = sqlite3_clear_bindings,
		.stmt.column_count = sqlite3_column_count,
		.stmt.column_name = sqlite3_column_name,
		.stmt.column_database_name = sqlite3_column_database_name,
		.stmt.column_table_name = sqlite3_column_table_name,
		.stmt.column_origin_name = sqlite3_column_origin_name,
		.stmt.column_decltype = sqlite3_column_decltype,
		.stmt.step = sqlite3_step,
		.stmt.column_blob = sqlite3_column_blob,
		.stmt.column_double = sqlite3_column_double,
		.stmt.column_int = sqlite3_column_int,
		.stmt.column_int64 = sqlite3_column_int64,
		.stmt.column_text = sqlite3_column_text,
		.stmt.column_value = sqlite3_column_value,
		.stmt.column_bytes = sqlite3_column_bytes,
		.stmt.column_type = sqlite3_column_type,
		.stmt.finalize = sqlite3_finalize,
		.stmt.reset = sqlite3_reset,

		.backup.backup_init = sqlite3_backup_init,
		.backup.backup_step = sqlite3_backup_step,
		.backup.backup_finish = sqlite3_backup_finish,
		.backup.backup_remaining = sqlite3_backup_remaining,
		.backup.backup_pagecount = sqlite3_backup_pagecount
	};
}

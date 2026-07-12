// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

#pragma once
#include <stdint.h>

typedef struct my_sqlite3 my_sqlite3;
typedef struct my_sqlite3_stmt my_sqlite3_stmt;
typedef struct my_sqlite3_value my_sqlite3_value;
typedef struct my_sqlite3_backup my_sqlite3_backup;

const char *my_libversion(void);

int32_t my_libversion_number(void);

// wrap sqlite3_close_v2 
int32_t my_close(my_sqlite3*);

int32_t my_exec(
	my_sqlite3*,																					/* An open database */
	const char *sql,																			/* SQL to be evaluated */
	int32_t (*callback)(void*, int32_t, char**, char**),	/* Callback function */
	void *,																								/* 1st argument to callback */
	char **errmsg																					/* Error msg written here */
);

int32_t my_extended_result_codes(my_sqlite3*, int32_t onoff);

int64_t my_last_insert_rowid(my_sqlite3*);

int32_t my_changes(my_sqlite3*);
int64_t my_changes64(my_sqlite3*);

int32_t my_total_changes(my_sqlite3*);
int64_t my_total_changes64(my_sqlite3*);

void my_interrupt(my_sqlite3*);

int32_t my_is_interrupted(my_sqlite3*);

int32_t my_busy_timeout(my_sqlite3*, int32_t ms);

void my_free(void*);

// wrap sqlite3_open_v2
int32_t my_open(
  const char *filename,			/* Database filename (UTF-8) */
  my_sqlite3 **ppDb,				/* OUT: SQLite db handle */
  int32_t flags,						/* Flags */
  const char *zVfs					/* Name of VFS module to use */
);

int32_t my_errcode(my_sqlite3 *db);

int32_t my_extended_errcode(my_sqlite3 *db);

const char *my_errmsg(my_sqlite3*);

const char *my_errstr(int32_t);

int32_t my_error_offset(my_sqlite3 *db);

int32_t my_limit(my_sqlite3*, int32_t id, int32_t newVal);

// sqlite3_prepare_v3
int32_t my_prepare(
	my_sqlite3 *db,						/* Database handle */
	const char *zSql,					/* SQL statement, UTF-8 encoded */
	int32_t nByte,						/* Maximum length of zSql in bytes. */
	uint32_t prepFlags,					/* Zero or more SQLITE_PREPARE_ flags */
	my_sqlite3_stmt **ppStmt,			/* OUT: Statement handle */
	const char **pzTail					/* OUT: Pointer to unused portion of zSql */
);

const char *my_sql(my_sqlite3_stmt *pStmt);

char *my_expanded_sql(my_sqlite3_stmt *pStmt);

int32_t my_stmt_readonly(my_sqlite3_stmt *pStmt);

int32_t my_stmt_busy(my_sqlite3_stmt*);

int32_t my_bind_blob(my_sqlite3_stmt*, int32_t, const uint8_t*, int32_t n, void(*)(void*));

int32_t my_bind_double(my_sqlite3_stmt*, int32_t, double);

int32_t my_bind_int(my_sqlite3_stmt*, int32_t, int32_t);

int32_t my_bind_int64(my_sqlite3_stmt*, int32_t, int64_t);

int32_t my_bind_null(my_sqlite3_stmt*, int32_t);

int32_t my_bind_text(my_sqlite3_stmt*, int32_t, const char*, int32_t, void(*)(void*));

int32_t my_bind_value(my_sqlite3_stmt*, int32_t, const my_sqlite3_value*);

int32_t my_bind_parameter_count(my_sqlite3_stmt*);

const char *my_bind_parameter_name(my_sqlite3_stmt*, int32_t);

int32_t my_bind_parameter_index(my_sqlite3_stmt*, const char *zName);

int32_t my_clear_bindings(my_sqlite3_stmt*);

int32_t my_column_count(my_sqlite3_stmt *pStmt);

const char *my_column_name(my_sqlite3_stmt*, int32_t N);

const char *my_column_database_name(my_sqlite3_stmt*,int32_t);

const char *my_column_table_name(my_sqlite3_stmt*,int32_t);

const char *my_column_origin_name(my_sqlite3_stmt*,int32_t);

const char *my_column_decltype(my_sqlite3_stmt*,int32_t);

int32_t my_step(my_sqlite3_stmt*);

const uint8_t *my_column_blob(my_sqlite3_stmt*, int32_t iCol);

double my_column_double(my_sqlite3_stmt*, int32_t iCol);

int32_t my_column_int(my_sqlite3_stmt*, int32_t iCol);

int64_t my_column_int64(my_sqlite3_stmt*, int32_t iCol);

const unsigned char *my_column_text(my_sqlite3_stmt*, int32_t iCol);

my_sqlite3_value *my_column_value(my_sqlite3_stmt*, int32_t iCol);

int32_t my_column_bytes(my_sqlite3_stmt*, int32_t iCol);

int32_t my_column_type(my_sqlite3_stmt*, int32_t iCol);

int32_t my_finalize(my_sqlite3_stmt *pStmt);

int32_t my_reset(my_sqlite3_stmt *pStmt);

int32_t my_get_autocommit(my_sqlite3*);

int32_t my_db_readonly(my_sqlite3 *db, const char *zDbName);

int32_t my_txn_state(my_sqlite3*,const char *zSchema);

int32_t my_table_column_metadata(
	my_sqlite3 *db,								/* Connection handle */
	const char *zDbName,					/* Database name or NULL */
	const char *zTableName,				/* Table name */
	const char *zColumnName,			/* Column name */
	char const **pzDataType,			/* OUTPUT: Declared data type */
	char const **pzCollSeq,				/* OUTPUT: Collation sequence name */
	int32_t *pNotNull,						/* OUTPUT: True if NOT NULL constraint exists */
	int32_t *pPrimaryKey,					/* OUTPUT: True if column part of PK */
	int32_t *pAutoinc							/* OUTPUT: True if column is auto-increment */
);

my_sqlite3_backup *my_backup_init(
	my_sqlite3 *pDest,					/* Destination database handle */
	const char *zDestName,			/* Destination database name */
	my_sqlite3 *pSource,				/* Source database handle */
	const char *zSourceName			/* Source database name */
);

int32_t my_backup_step(my_sqlite3_backup *p, int32_t nPage);

int32_t my_backup_finish(my_sqlite3_backup *p);

int32_t my_backup_remaining(my_sqlite3_backup *p);

int32_t my_backup_pagecount(my_sqlite3_backup *p);

// Funzione di accesso entry-point
// void get_vtable(vtable* table);

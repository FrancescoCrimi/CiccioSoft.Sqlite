// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

#pragma once
#include <sqlite3.h>

typedef struct sqlite_vtable {

	const char *(*libversion)(void);

	int (*libversion_number)(void);

	int (*close)(sqlite3*);

	int (*exec)(
		sqlite3*,                                  /* An open database */
		const char *sql,                           /* SQL to be evaluated */
		int (*callback)(void*,int,char**,char**),  /* Callback function */
		void *,                                    /* 1st argument to callback */
		char **errmsg                              /* Error msg written here */
	);

	int (*extended_result_codes)(sqlite3*, int onoff);

	sqlite3_int64 (*last_insert_rowid)(sqlite3*);

	int (*changes)(sqlite3*);

	sqlite3_int64 (*changes64)(sqlite3*);

	int (*total_changes)(sqlite3*);

	sqlite3_int64 (*total_changes64)(sqlite3*);

	void (*interrupt)(sqlite3*);

	int (*is_interrupted)(sqlite3*);

	int (*busy_timeout)(sqlite3*, int ms);

	void (*free)(void*);

	int (*open)(
		const char *filename,   /* Database filename (UTF-8) */
		sqlite3 **ppDb,         /* OUT: SQLite db handle */
		int flags,              /* Flags */
		const char *zVfs        /* Name of VFS module to use */
	);

	int (*errcode)(sqlite3 *db);

	int (*extended_errcode)(sqlite3 *db);

	const char *(*errmsg)(sqlite3*);

	const char *(*errstr)(int);

	int (*error_offset)(sqlite3 *db);

	int (*limit)(sqlite3*, int id, int newVal);

	int (*prepare)(
		sqlite3 *db,            /* Database handle */
		const char *zSql,       /* SQL statement, UTF-8 encoded */
		int nByte,              /* Maximum length of zSql in bytes. */
		unsigned int prepFlags, /* Zero or more SQLITE_PREPARE_ flags */
		sqlite3_stmt **ppStmt,  /* OUT: Statement handle */
		const char **pzTail     /* OUT: Pointer to unused portion of zSql */
	);

	int (*get_autocommit)(sqlite3*);

	int (*db_readonly)(sqlite3 *db, const char *zDbName);

	int (*txn_state)(sqlite3*,const char *zSchema);

	int (*table_column_metadata)(
		sqlite3 *db,                /* Connection handle */
		const char *zDbName,        /* Database name or NULL */
		const char *zTableName,     /* Table name */
		const char *zColumnName,    /* Column name */
		char const **pzDataType,    /* OUTPUT: Declared data type */
		char const **pzCollSeq,     /* OUTPUT: Collation sequence name */
		int *pNotNull,              /* OUTPUT: True if NOT NULL constraint exists */
		int *pPrimaryKey,           /* OUTPUT: True if column part of PK */
		int *pAutoinc               /* OUTPUT: True if column is auto-increment */
	);

} sqlite_vtable;

typedef struct sqlite_stmt_vtable {

	const char *(*sql)(sqlite3_stmt *pStmt);

	char *(*expanded_sql)(sqlite3_stmt *pStmt);

	int (*stmt_readonly)(sqlite3_stmt *pStmt);

	int (*stmt_busy)(sqlite3_stmt*);

	int (*bind_blob)(sqlite3_stmt*, int, const void*, int n, void(*)(void*));

	int (*bind_double)(sqlite3_stmt*, int, double);

	int (*bind_int)(sqlite3_stmt*, int, int);

	int (*bind_int64)(sqlite3_stmt*, int, sqlite3_int64);

	int (*bind_null)(sqlite3_stmt*, int);

	int (*bind_text)(sqlite3_stmt*, int, const char*, int, void(*)(void*));

	int (*bind_value)(sqlite3_stmt*, int, const sqlite3_value*);

	int (*bind_parameter_count)(sqlite3_stmt*);

	const char *(*bind_parameter_name)(sqlite3_stmt*, int);

	int (*bind_parameter_index)(sqlite3_stmt*, const char *zName);

	int (*clear_bindings)(sqlite3_stmt*);

	int (*column_count)(sqlite3_stmt *pStmt);

	const char *(*column_name)(sqlite3_stmt*, int N);

	const char *(*column_database_name)(sqlite3_stmt*,int);

	const char *(*column_table_name)(sqlite3_stmt*,int);

	const char *(*column_origin_name)(sqlite3_stmt*,int);

	const char *(*column_decltype)(sqlite3_stmt*,int);

	int (*step)(sqlite3_stmt*);

	const void *(*column_blob)(sqlite3_stmt*, int iCol);

	double (*column_double)(sqlite3_stmt*, int iCol);

	int (*column_int)(sqlite3_stmt*, int iCol);

	sqlite3_int64 (*column_int64)(sqlite3_stmt*, int iCol);

	const unsigned char *(*column_text)(sqlite3_stmt*, int iCol);

	sqlite3_value *(*column_value)(sqlite3_stmt*, int iCol);

	int (*column_bytes)(sqlite3_stmt*, int iCol);

	int (*column_type)(sqlite3_stmt*, int iCol);

	int (*finalize)(sqlite3_stmt *pStmt);

	int (*reset)(sqlite3_stmt *pStmt);

} sqlite_stmt_vtable;

typedef struct sqlite_backup_vtable {

	sqlite3_backup *(*backup_init)(
		sqlite3 *pDest,                        /* Destination database handle */
		const char *zDestName,                 /* Destination database name */
		sqlite3 *pSource,                      /* Source database handle */
		const char *zSourceName                /* Source database name */
	);

	int (*backup_step)(sqlite3_backup *p, int nPage);

	int (*backup_finish)(sqlite3_backup *p);

	int (*backup_remaining)(sqlite3_backup *p);

	int (*backup_pagecount)(sqlite3_backup *p);

} sqlite_backup_vtable;

typedef struct vtable {
	int version;     				/* ABI version = 1 */
	struct sqlite_vtable sqlite;
	struct sqlite_stmt_vtable stmt;
	struct sqlite_backup_vtable backup;
} vtable;

// Funzione di accesso entry-point
void get_vtable(vtable* table);

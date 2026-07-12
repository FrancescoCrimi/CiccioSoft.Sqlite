// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

#include "SQLiteGlue.h"
#include <sqlite3.h>

static const char *my_libversion(void) {
	return sqlite3_libversion();
}

static int32_t my_close(void* pSqlite) {
	return sqlite3_close_v2((sqlite3*)pSqlite);
}

static int32_t my_exec(void* pSqlite,								/* An open database */
					   const char *sql,								/* SQL to be evaluated */
					   void *arg,									/* 1st argument to callback */
					   char **errmsg)								/* Error msg written here */
{
	return sqlite3_exec((sqlite3*)pSqlite, sql, NULL, arg, errmsg);
}

static int64_t my_last_insert_rowid(void* pSqlite) {
	return (int64_t)sqlite3_last_insert_rowid((sqlite3*)pSqlite);
}

static int64_t my_changes(void* pSqlite) {
	return (int64_t)sqlite3_changes64((sqlite3*)pSqlite);
}

static int64_t my_total_changes(void* pSqlite) {
	return (int64_t)sqlite3_total_changes64((sqlite3*)pSqlite);
}

static void my_interrupt(void* pSqlite) {
	sqlite3_interrupt((sqlite3*)pSqlite);
}

static int32_t my_open(const char *filename,	/* Database filename (UTF-8) */
					   void **ppSqlite,			/* OUT: SQLite db handle */
					   int flags,				/* Flags */
					   const char *zVfs)		/* Name of VFS module to use */
{
	return sqlite3_open_v2(filename, (sqlite3**)ppSqlite, flags, zVfs);
}

static int32_t my_errcode(void* pSqlite) {
	return sqlite3_errcode((sqlite3*)pSqlite);
}

static int32_t my_extended_errcode(void* pSqlite) {
	return sqlite3_extended_errcode((sqlite3*)pSqlite);
}

static const char* my_errmsg(void* pSqlite) {
	return sqlite3_errmsg((sqlite3*)pSqlite);
}

static int32_t my_prepare(void *pSqlite,			/* Database handle */
						  const char *zSql,			/* SQL statement, UTF-8 encoded */
						  int nByte,				/* Maximum length of zSql in bytes. */
						  unsigned int prepFlags,	/* Zero or more SQLITE_PREPARE_ flags */
						  void **ppStmt,			/* OUT: Statement handle */
						  const char **pzTail)		/* OUT: Pointer to unused portion of zSql */
{
	return sqlite3_prepare_v3((sqlite3*)pSqlite, zSql, nByte, prepFlags, (sqlite3_stmt**)ppStmt, pzTail);
}




static int32_t my_bind_blob (void *pStmt, int32_t i, const uint8_t *d, int32_t n) { 
	return sqlite3_bind_blob(pStmt, i, d, n, SQLITE_TRANSIENT); 
}

static int32_t my_bind_double(void *pStmt, int32_t i, double v) { 
	return sqlite3_bind_double(pStmt, i, v); 
}

static int32_t my_bind_int (void *pStmt, int32_t i, int32_t v) { 
	return sqlite3_bind_int(pStmt, i, v); 
}

static int32_t my_bind_int64 (void *pStmt, int32_t i, int64_t v) { 
	return sqlite3_bind_int64(pStmt, i, v); 
}

static int32_t my_bind_null  (void *pStmt, int32_t i) { 
	return sqlite3_bind_null(pStmt, i); 
}

static int32_t my_bind_text (void *pStmt, int32_t i, const char *utf8, int32_t bytes) {
	return sqlite3_bind_text(pStmt, i, utf8, bytes, SQLITE_TRANSIENT);
}

static int32_t my_step(void *pStmt)	{ 
	return sqlite3_step((sqlite3_stmt*)pStmt);
}




static const void *my_column_blob(void *pStmt, int32_t iCol) {
	return sqlite3_column_blob(pStmt, iCol);
}

static double my_column_double(void *pStmt, int32_t iCol) {
	return sqlite3_column_double(pStmt, iCol);
}

static int32_t my_column_int(void *pStmt, int32_t iCol) {
	return sqlite3_column_int(pStmt, iCol);
}

static int64_t my_column_int64(void *pStmt, int32_t iCol) {
	return sqlite3_column_int64(pStmt, iCol);
}
static const unsigned char *my_column_text(void *pStmt, int32_t iCol) {
	return sqlite3_column_text(pStmt, iCol);
}

// static sqlite3_value *sqlite3_column_value(sqlite3_stmt *pStmt, int32_t iCol) {}

static int32_t my_column_bytes(void *pStmt, int32_t iCol) {
	return sqlite3_column_bytes(pStmt, iCol);
}

static int32_t my_column_type(void *pStmt, int32_t iCol) {
	return sqlite3_column_type(pStmt, iCol);
}




static int32_t my_finalize(void *pStmt)	{ 
	return sqlite3_finalize((sqlite3_stmt*)pStmt); 
}

static int32_t my_reset(void *pStmt) { 
	return sqlite3_reset((sqlite3_stmt*)pStmt); 
}

void get_vtable(vtable* table) {
	*table = (vtable) {
		.version	= 1u,

		.sqlite.libversion = my_libversion,
		.sqlite.close = my_close,
		.sqlite.exec = my_exec,
		.sqlite.last_insert_rowid = my_last_insert_rowid,
		.sqlite.changes = my_changes,
		.sqlite.total_changes = my_total_changes,
		.sqlite.interrupt = my_interrupt,
		.sqlite.open = my_open,
		.sqlite.errcode = my_errcode,
		.sqlite.extended_errcode = my_extended_errcode,
		.sqlite.errmsg = my_errmsg,
		.sqlite.prepare = my_prepare,

		.stmt.bind_blob = my_bind_blob,
		.stmt.bind_double = my_bind_double,
		.stmt.bind_int = my_bind_int,
		.stmt.bind_int64 = my_bind_int64,
		.stmt.bind_null = my_bind_null,
		.stmt.bind_text = my_bind_text,
		.stmt.step = my_step,
		.stmt.column_blob = my_column_blob,
		.stmt.column_double = my_column_double,
		.stmt.column_int = my_column_int,
		.stmt.column_int64 = my_column_int64,
		.stmt.column_text = my_column_text,
		.stmt.column_bytes = my_column_bytes,
		.stmt.column_type = my_column_type,
		.stmt.finalize = my_finalize,
		.stmt.reset = my_reset
	};
}

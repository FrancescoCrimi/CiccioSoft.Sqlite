// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

#include "SQLiteGlue.h"
#include <sqlite3.h>

const char *my_libversion(void) {
	return sqlite3_libversion();
}

int32_t my_libversion_number(void) {
	return sqlite3_libversion_number();
}

int32_t my_close(my_sqlite3 *pSqlite) {
	return sqlite3_close_v2((sqlite3*)pSqlite);
}

int32_t my_exec(
	my_sqlite3 *pSqlite,									/* An open database */
	const char *sql,										/* SQL to be evaluated */
	int32_t (*callback)(void*, int32_t, char**, char**),	/* Callback function */
	void *arg,												/* 1st argument to callback */
	char **errmsg)											/* Error msg written here */
{
	return sqlite3_exec((sqlite3*)pSqlite, sql, callback, arg, errmsg);
}

int32_t my_extended_result_codes(my_sqlite3 *pSqlite, int32_t onoff) {
	return sqlite3_extended_result_codes((sqlite3*)pSqlite, onoff);
}

int64_t my_last_insert_rowid(my_sqlite3 *pSqlite) {
	return (int64_t)sqlite3_last_insert_rowid((sqlite3*)pSqlite);
}

int32_t my_changes(my_sqlite3 *pSqlite) {
	return sqlite3_changes64((sqlite3*)pSqlite);
}

int64_t my_changes64(my_sqlite3 *pSqlite) {
	return sqlite3_changes64((sqlite3*)pSqlite);
}

int32_t my_total_changes(my_sqlite3 *pSqlite) {
	return sqlite3_total_changes64((sqlite3*)pSqlite);
}

int64_t my_total_changes64(my_sqlite3 *pSqlite) {
	return sqlite3_total_changes64((sqlite3*)pSqlite);
}

void my_interrupt(my_sqlite3 *pSqlite) {
	sqlite3_interrupt((sqlite3*)pSqlite);
}

int32_t my_is_interrupted(my_sqlite3 *pSqlite) {
	return sqlite3_is_interrupted((sqlite3*)pSqlite);
}

int32_t my_busy_timeout(my_sqlite3 *pSqlite, int32_t ms) {
	return sqlite3_busy_timeout((sqlite3*)pSqlite, ms);
}

void my_free(void *arg) {
	return sqlite3_free(arg);
}

int32_t my_open(
	const char *filename,			/* Database filename (UTF-8) */
	my_sqlite3 **ppSqlite,			/* OUT: SQLite db handle */
	int flags,						/* Flags */
	const char *zVfs)				/* Name of VFS module to use */
{
	return sqlite3_open_v2(filename, (sqlite3**)ppSqlite, flags, zVfs);
}

int32_t my_errcode(my_sqlite3 *pSqlite) {
	return sqlite3_errcode((sqlite3*)pSqlite);
}

int32_t my_extended_errcode(my_sqlite3 *pSqlite) {
	return sqlite3_extended_errcode((sqlite3*)pSqlite);
}

const char* my_errmsg(my_sqlite3 *pSqlite) {
	return sqlite3_errmsg((sqlite3*)pSqlite);
}

const char *my_errstr(int32_t arg) {
	return sqlite3_errstr(arg);
}

int32_t my_error_offset(my_sqlite3 *pSqlite) {
	return sqlite3_error_offset((sqlite3*)pSqlite);
}

int32_t my_limit(my_sqlite3 *pSqlite, int32_t id, int32_t newVal) {
	return sqlite3_limit((sqlite3*)pSqlite, id, newVal);
}

int32_t my_prepare(
	my_sqlite3 *pSqlite,			/* Database handle */
	const char *zSql,				/* SQL statement, UTF-8 encoded */
	int nByte,						/* Maximum length of zSql in bytes. */
	unsigned int prepFlags,			/* Zero or more SQLITE_PREPARE_ flags */
	my_sqlite3_stmt **ppStmt,		/* OUT: Statement handle */
	const char **pzTail)			/* OUT: Pointer to unused portion of zSql */
{
	return sqlite3_prepare_v3((sqlite3*)pSqlite, zSql, nByte, prepFlags, (sqlite3_stmt**)ppStmt, pzTail);
}

const char *my_sql(my_sqlite3_stmt *pStmt) {
	return sqlite3_sql((sqlite3_stmt*)pStmt);
}

char *my_expanded_sql(my_sqlite3_stmt *pStmt) {
	return sqlite3_expanded_sql((sqlite3_stmt*)pStmt);
}

int32_t my_stmt_readonly(my_sqlite3_stmt *pStmt) {
	return sqlite3_stmt_readonly((sqlite3_stmt*)pStmt);
}

int32_t my_stmt_busy(my_sqlite3_stmt *pStmt) {
	return sqlite3_stmt_busy((sqlite3_stmt*)pStmt);
}

int32_t my_bind_blob(my_sqlite3_stmt *pStmt, int32_t i, const uint8_t *data, int32_t bytes, void(*destructor)(void*)) {
	return sqlite3_bind_blob((sqlite3_stmt*)pStmt, i, data, bytes, destructor); 
}

int32_t my_bind_double(my_sqlite3_stmt *pStmt, int32_t i, double v) {
	return sqlite3_bind_double((sqlite3_stmt*)pStmt, i, v); 
}

int32_t my_bind_int(my_sqlite3_stmt *pStmt, int32_t i, int32_t v) { 
	return sqlite3_bind_int((sqlite3_stmt*)pStmt, i, v); 
}

int32_t my_bind_int64(my_sqlite3_stmt *pStmt, int32_t i, int64_t v) { 
	return sqlite3_bind_int64((sqlite3_stmt*)pStmt, i, v); 
}

int32_t my_bind_null(my_sqlite3_stmt *pStmt, int32_t i) { 
	return sqlite3_bind_null((sqlite3_stmt*)pStmt, i); 
}

int32_t my_bind_text(my_sqlite3_stmt *pStmt, int32_t i, const char *utf8, int32_t bytes, void(*destructor)(void*)) {
	return sqlite3_bind_text((sqlite3_stmt*)pStmt, i, utf8, bytes, destructor);
}

int32_t my_bind_value(my_sqlite3_stmt *pStmt, int32_t i, const my_sqlite3_value *value) {
	return sqlite3_bind_value((sqlite3_stmt*)pStmt, i, (sqlite3_value*)value);
}

int32_t my_bind_parameter_count(my_sqlite3_stmt *pStmt) {
	return sqlite3_bind_parameter_count((sqlite3_stmt*)pStmt);
}

const char *my_bind_parameter_name(my_sqlite3_stmt *pStmt, int32_t i) {
	return sqlite3_bind_parameter_name((sqlite3_stmt*)pStmt, i);
}

int32_t my_bind_parameter_index(my_sqlite3_stmt *pStmt, const char *zName) {
	return sqlite3_bind_parameter_index((sqlite3_stmt*)pStmt, zName);
}

int32_t my_clear_bindings(my_sqlite3_stmt *pStmt) {
	return sqlite3_clear_bindings((sqlite3_stmt*)pStmt);
}

int32_t my_column_count(my_sqlite3_stmt *pStmt) {
	return sqlite3_column_count((sqlite3_stmt*)pStmt);
}

const char *my_column_name(my_sqlite3_stmt *pStmt, int32_t N) {
	return sqlite3_column_name((sqlite3_stmt*)pStmt, N);
}

const char *my_column_database_name(my_sqlite3_stmt *pStmt, int32_t i) {
	return sqlite3_column_database_name((sqlite3_stmt*)pStmt, i);
}

const char *my_column_table_name(my_sqlite3_stmt *pStmt, int32_t i) {
	return sqlite3_column_table_name((sqlite3_stmt*)pStmt, i);
}

const char *my_column_origin_name(my_sqlite3_stmt *pStmt, int32_t i) {
	return sqlite3_column_origin_name((sqlite3_stmt*)pStmt, i);
}

const char *my_column_decltype(my_sqlite3_stmt *pStmt, int32_t i) {
	return sqlite3_column_decltype((sqlite3_stmt*)pStmt, i);
}

int32_t my_step(my_sqlite3_stmt *pStmt)	{ 
	return sqlite3_step((sqlite3_stmt*)pStmt);
}

const uint8_t *my_column_blob(my_sqlite3_stmt *pStmt, int32_t iCol) {
	return sqlite3_column_blob((sqlite3_stmt*)pStmt, iCol);
}

double my_column_double(my_sqlite3_stmt *pStmt, int32_t iCol) {
	return sqlite3_column_double((sqlite3_stmt*)pStmt, iCol);
}

int32_t my_column_int(my_sqlite3_stmt *pStmt, int32_t iCol) {
	return sqlite3_column_int((sqlite3_stmt*)pStmt, iCol);
}

int64_t my_column_int64(my_sqlite3_stmt *pStmt, int32_t iCol) {
	return sqlite3_column_int64((sqlite3_stmt*)pStmt, iCol);
}
const unsigned char *my_column_text(my_sqlite3_stmt *pStmt, int32_t iCol) {
	return sqlite3_column_text((sqlite3_stmt*)pStmt, iCol);
}

my_sqlite3_value *my_column_value(my_sqlite3_stmt *pStmt, int32_t iCol) {
	return (my_sqlite3_value*)sqlite3_column_value((sqlite3_stmt*)pStmt, iCol);
}

int32_t my_column_bytes(my_sqlite3_stmt *pStmt, int32_t iCol) {
	return sqlite3_column_bytes((sqlite3_stmt*)pStmt, iCol);
}

int32_t my_column_type(my_sqlite3_stmt *pStmt, int32_t iCol) {
	return sqlite3_column_type((sqlite3_stmt*)pStmt, iCol);
}

int32_t my_finalize(my_sqlite3_stmt *pStmt)	{ 
	return sqlite3_finalize((sqlite3_stmt*)pStmt); 
}

int32_t my_reset(my_sqlite3_stmt *pStmt) { 
	return sqlite3_reset((sqlite3_stmt*)pStmt); 
}

int32_t my_get_autocommit(my_sqlite3 *pSqlite) {
	return sqlite3_get_autocommit((sqlite3*)pSqlite);
}

int32_t my_db_readonly(my_sqlite3 *pSqlite, const char *zDbName) {
	return sqlite3_db_readonly((sqlite3*)pSqlite, zDbName);
}

int32_t my_txn_state(my_sqlite3 *pSqlite, const char *zSchema) {
	return sqlite3_txn_state((sqlite3*)pSqlite, zSchema);
}

int32_t my_table_column_metadata(
	my_sqlite3 *pSqlite,			/* Connection handle */
	const char *zDbName,			/* Database name or NULL */
	const char *zTableName,			/* Table name */
	const char *zColumnName,		/* Column name */
	char const **pzDataType,		/* OUTPUT: Declared data type */
	char const **pzCollSeq,			/* OUTPUT: Collation sequence name */
	int32_t *pNotNull,				/* OUTPUT: True if NOT NULL constraint exists */
	int32_t *pPrimaryKey,			/* OUTPUT: True if column part of PK */
	int32_t *pAutoinc)				/* OUTPUT: True if column is auto-increment */
{
	return sqlite3_table_column_metadata((sqlite3*)pSqlite, zDbName, zTableName, zColumnName, pzDataType, pzCollSeq, pNotNull, pPrimaryKey, pAutoinc);
}

my_sqlite3_backup *my_backup_init(
	my_sqlite3 *pDest,				/* Destination database handle */
	const char *zDestName,			/* Destination database name */
	my_sqlite3 *pSource,			/* Source database handle */
	const char *zSourceName)		/* Source database name */
{
	return (my_sqlite3_backup*)sqlite3_backup_init((sqlite3*)pDest, zDestName, (sqlite3*)pSource, zSourceName);
}

int32_t my_backup_step(my_sqlite3_backup *p, int32_t nPage) {
	return sqlite3_backup_step((sqlite3_backup*)p, nPage);
}

int32_t my_backup_finish(my_sqlite3_backup *p) {
	return sqlite3_backup_finish((sqlite3_backup*)p);
}

int32_t my_backup_remaining(my_sqlite3_backup *p) {
	return sqlite3_backup_remaining((sqlite3_backup*)p);
}

int32_t my_backup_pagecount(my_sqlite3_backup *p) {
	return sqlite3_backup_pagecount((sqlite3_backup*)p);
}


// void get_vtable(vtable* table) {
// 	*table = (vtable) {
// 		.version	= 1u,

// 		.sqlite.libversion = my_libversion,
// 		.sqlite.close = my_close,
// 		.sqlite.exec = my_exec,
// 		.sqlite.last_insert_rowid = my_last_insert_rowid,
// 		.sqlite.changes = my_changes,
// 		.sqlite.total_changes = my_total_changes,
// 		.sqlite.interrupt = my_interrupt,
// 		.sqlite.open = my_open,
// 		.sqlite.errcode = my_errcode,
// 		.sqlite.extended_errcode = my_extended_errcode,
// 		.sqlite.errmsg = my_errmsg,
// 		.sqlite.prepare = my_prepare,

// 		.stmt.bind_blob = my_bind_blob,
// 		.stmt.bind_double = my_bind_double,
// 		.stmt.bind_int = my_bind_int,
// 		.stmt.bind_int64 = my_bind_int64,
// 		.stmt.bind_null = my_bind_null,
// 		.stmt.bind_text = my_bind_text,
// 		.stmt.step = my_step,
// 		.stmt.column_blob = my_column_blob,
// 		.stmt.column_double = my_column_double,
// 		.stmt.column_int = my_column_int,
// 		.stmt.column_int64 = my_column_int64,
// 		.stmt.column_text = my_column_text,
// 		.stmt.column_bytes = my_column_bytes,
// 		.stmt.column_type = my_column_type,
// 		.stmt.finalize = my_finalize,
// 		.stmt.reset = my_reset
// 	};
// }

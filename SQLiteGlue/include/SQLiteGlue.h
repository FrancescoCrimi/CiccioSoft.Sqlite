// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

#pragma once
#include <stdint.h>

typedef struct sqlite_vtable {
	const char *(*libversion)(void);														//  186
	int32_t (*libversion_number)(void);														//  188
	int32_t (*close)(void *pSqlite);														//  354
	int32_t (*exec)(void *pSqlite, const char *sql, void *arg, char **errmsg);				//  427
	int32_t (*extended_result_codes)(void *pSqlite, int onoff);								// 2681
	int64_t (*last_insert_rowid)(void* pSqlite);											// 2743
	int64_t (*changes)(void *pSqlite);														// 2818
	int64_t (*total_changes)(void* pSqlite);												// 2861
	void (*interrupt)(void* pSqlite);														// 2902
	int32_t (*busy_timeout)(void *pSqlite, int ms);											// 3023
	void (*free)(void*);; 																	// 3270
	int32_t (*open)(const char *filename, void **ppSqlite, int flags, const char *zVfs);	// 3946
	int32_t (*errcode)(void* pSqlite);														// 4191
	int32_t (*extended_errcode)(void* pSqlite);												// 4192
	const char *(*errmsg)(void *pSqlite);													// 4193
	int32_t (*error_offset)(void *pSqlite);													// 4196
	int32_t (*limit)(void *pSqlite, int id, int newVal);									// 4264
	int32_t (*prepare)(
		void *pSqlite,          /* Database handle */
		const char *zSql,       /* SQL statement, UTF-8 encoded */
		int nByte,              /* Maximum length of zSql in bytes. */
		unsigned int prepFlags, /* Zero or more SQLITE_PREPARE_ flags */
		void **ppStmt,  /* OUT: Statement handle */
		const char **pzTail     /* OUT: Pointer to unused portion of zSql */
	);																						// 4503
	int32_t (*get_autocommit)(void *pSqlite);												// 6799
	int32_t (*txn_state)(void *pSqlite,const char *zSchema); 								// 6894
	int32_t (*db_readonly)(void *pSqlite, const char *zDbName);								// 6876
	int32_t (*table_column_metadata)(
  		void *pSqlite,                /* Connection handle */
  		const char *zDbName,        /* Database name or NULL */
  		const char *zTableName,     /* Table name */
  		const char *zColumnName,    /* Column name */
  		char const **pzDataType,    /* OUTPUT: Declared data type */
  		char const **pzCollSeq,     /* OUTPUT: Collation sequence name */
  		int *pNotNull,              /* OUTPUT: True if NOT NULL constraint exists */
  		int *pPrimaryKey,           /* OUTPUT: True if column part of PK */
  		int *pAutoinc               /* OUTPUT: True if column is auto-increment */
	);																						// 7348

} sqlite_vtable;

typedef struct sqlite_stmt_vtable {
	const char *(*sql)(void *pStmt);															// 4575
	char *(*expanded_sql)(void *pStmt);															// 4576
	int32_t (*stmt_readonly)(void *pStmt);														// 4628
	int32_t (*stmt_busy)(void *pStmt);															// 4696
	int32_t (*bind_blob)(void *pStmt, int32_t i, const uint8_t *data, int32_t bytes);			// 4896
	int32_t (*bind_double)(void *pStmt, int32_t i, double  v);									// 4899
	int32_t (*bind_int)(void *pStmt, int32_t i, int32_t v);										// 4900
	int32_t (*bind_int64)(void *pStmt, int32_t i, int64_t v);									// 4901
	int32_t (*bind_null)(void *pStmt, int32_t i);												// 4902
	int32_t (*bind_text)(void *pStmt, int32_t i, const char *utf8, int32_t bytes);				// 4903
	int32_t (*bind_parameter_count)(void *pStmt);												// 4931
	const char *(*bind_parameter_name)(void *pStmt, int32_t i); 								// 4959
	int32_t (*bind_parameter_index)(void *pStmt, const char *zName);							// 4977
	int32_t (*clear_bindings)(void *pStmt);														// 4987
	int32_t (*column_count)(void *pStmt); 														// 5003
	const char *(*column_name)(void *pStmt, int32_t N);											// 5032
	const char *(*column_database_name)(void *pStmt, int32_t iCol);								// 5077
	const char *(*column_table_name)(void *pStmt, int32_t iCol);								// 5079
	const char *(*column_origin_name)(void *pStmt, int32_t iCol);								// 5081
	const char *(*column_decltype)(void *pStmt, int32_t iCol);									// 5114			
	int32_t (*step)(void *pStmt);  																// 5199 /* CS_OK=riga, CS_DONE=finito */
	const void *(*column_blob)(void *pStmt, int32_t iCol);										// 5467
	double (*column_double)(void *pStmt, int32_t iCol);											// 5468
	int32_t (*column_int)(void *pStmt, int32_t iCol);											// 5469
	int64_t (*column_int64)(void *pStmt, int32_t iCol);											// 5470
	const unsigned char* (*column_text)(void *pStmt, int32_t iCol);								// 5471
	void *(*column_value)(void *pStmt, int iCol);												// 5473
	int32_t (*column_bytes)(void *pStmt, int iCol);												// 5474
	int32_t (*column_type)(void *pStmt, int32_t iCol);											// 5476
	int32_t (*finalize)(void *pStmt);															// 5504
	int32_t (*reset)(void *pStmt);																// 5543

    /* Colonne  –  indici 0-based (convenzione SQLite) */
	// CsType     (*column_type  )(void *pStmt, int32_t c);
    // CsByteSpan (*column_blob  )(void *pStmt, int32_t c);
} sqlite_stmt_vtable;

typedef struct sqlite_backup_vtable {
	void *(*backup_init)(
		void *pSqliteDest,                     /* Destination database handle */
		const char *zDestName,                 /* Destination database name */
		void *pSqliteSource,                   /* Source database handle */
		const char *zSourceName                /* Source database name */
	);																						// 9553
	int32_t (*backup_step)(void *pBackup, int32_t nPage);									// 9559
	int32_t (*backup_finish)(void *pBackup);												// 9560
	int32_t (*backup_remaining)(void *pBackup);												// 9561
	int32_t (*backup_pagecount)(void *pBackup);												// 9562
} sqlite_backup_vtable;


typedef struct vtable {
	uint32_t version;     				/* ABI version = 1 */
	struct sqlite_vtable sqlite;
	struct sqlite_stmt_vtable stmt;
} vtable;

// Funzione di accesso entry-point
void get_vtable(vtable* table);

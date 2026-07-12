cmake_minimum_required(VERSION 3.28)

project(sqlite3glue
    VERSION		1.0.0
    DESCRIPTION "C23 SQLite VTable wrapper – consumabile da C# via delegate* unmanaged"
	LANGUAGES	C
)

# ── Shared library ───────────────────────────────────────────────────────── #
add_library(sqlite3glue SHARED
    src/sqlite3glue.c
)

# ── Proprietà C23 ────────────────────────────────────────────────────────── #
set_target_properties(sqlite3glue PROPERTIES
    C_STANDARD             23
    C_STANDARD_REQUIRED    ON
    C_EXTENSIONS           OFF
    VERSION                ${PROJECT_VERSION}
    PREFIX                 ""
)

# ── SQLite3 ──────────────────────────────────────────────────────────────── #
find_package(SQLite3 REQUIRED)

# ── Include / link ───────────────────────────────────────────────────────── #
target_include_directories(sqlite3glue PUBLIC include)
target_link_libraries(sqlite3glue PRIVATE SQLite::SQLite3)

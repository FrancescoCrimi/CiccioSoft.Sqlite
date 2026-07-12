cmake_minimum_required(VERSION 3.28)

project(SQLiteGlue
    VERSION		1.0.0
    DESCRIPTION "C23 SQLite VTable wrapper – consumabile da C# via delegate* unmanaged"
	LANGUAGES	C
)

# ── Shared library ───────────────────────────────────────────────────────── #
add_library(SQLiteGlue SHARED
    src/SQLiteGlue.c
)

# ── Proprietà C23 ────────────────────────────────────────────────────────── #
set_target_properties(SQLiteGlue PROPERTIES
    C_STANDARD             23
    C_STANDARD_REQUIRED    ON
    C_EXTENSIONS           OFF
    VERSION                ${PROJECT_VERSION}
)

# ── SQLite3 ──────────────────────────────────────────────────────────────── #
find_package(SQLite3 REQUIRED)

# ── Include / link ───────────────────────────────────────────────────────── #
target_include_directories(SQLiteGlue PUBLIC include)
target_link_libraries(SQLiteGlue PRIVATE SQLite::SQLite3)

cmake_minimum_required(VERSION 3.28)
project(AnimalBattle LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

set(DXLIB_DIR "" CACHE PATH "Path to DxLib")
if (NOT EXISTS "${DXLIB_DIR}")
    message(WARNING "DXLIB_DIR is not set or does not exist. Please configure it via CMakeUserPresets.json or provide it as a CMake parameter.")
endif ()

if (MSVC)
    add_compile_options("/utf-8")
    add_compile_definitions(WIN32 _WINDOWS UNICODE _UNICODE)
endif ()

add_subdirectory(AnimalBattleCore)

# Precompile DxLib.h for faster compilation
target_precompile_headers(AnimalBattleCore PUBLIC
        <DxLib.h>
)

find_program(CCACHE_PROGRAM ccache)
if (CCACHE_PROGRAM)
    set(CMAKE_CXX_COMPILER_LAUNCHER "${CCACHE_PROGRAM}")
endif ()

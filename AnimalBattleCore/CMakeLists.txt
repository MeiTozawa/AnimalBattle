file(GLOB_RECURSE ANIMALBATTLE_SOURCES CONFIGURE_DEPENDS
        "src/*.cpp"
        "AnimalBattleCore.cpp"
)
file(GLOB_RECURSE ANIMALBATTLE_MODULES CONFIGURE_DEPENDS
        "src/*.ixx"
)

add_library(AnimalBattleCore SHARED
    ${ANIMALBATTLE_SOURCES}
)

# Prevent min/max macro issues and winsock conflicts with DxLib/windows.h
target_compile_definitions(AnimalBattleCore PRIVATE NOMINMAX WIN32_LEAN_AND_MEAN)

# Ensure output DLL name is AnimalBattleCore.dll without 'lib' prefix on MinGW
set_target_properties(AnimalBattleCore PROPERTIES PREFIX "")

# Enable Unity Build for MSVC only (G++ module support requires separate compilation units)
if (MSVC)
    set_target_properties(AnimalBattleCore PROPERTIES UNITY_BUILD ON)
else ()
    set_target_properties(AnimalBattleCore PROPERTIES UNITY_BUILD OFF)
endif ()

# Configure C++ Modules
set_source_files_properties(${ANIMALBATTLE_MODULES} PROPERTIES SKIP_PRECOMPILE_HEADERS ON)
target_sources(AnimalBattleCore
        PUBLIC
        FILE_SET cxx_modules TYPE CXX_MODULES FILES
        ${ANIMALBATTLE_MODULES}
)

target_include_directories(AnimalBattleCore PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/src
    ${DXLIB_DIR}
)

target_link_directories(AnimalBattleCore PUBLIC
    ${DXLIB_DIR}
)

if (MSVC)
    target_link_libraries(AnimalBattleCore PUBLIC
        DxLibW
    )
elseif (CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
    target_link_options(AnimalBattleCore PRIVATE -static-libgcc -static-libstdc++)
    target_link_libraries(AnimalBattleCore PUBLIC
        DxLibW
        DxUseCLibW
        DxDrawFunc
        bulletdynamics
        bulletcollision
        bulletmath
        opusfile
        opus
        vorbisfile_static
        vorbis_static
        ogg_static
        theora_static
        jpeg
        png
        zlib
        tiff
        celt
        silk_common
        gdi32
        user32
        imm32
        winmm
        ole32
        oleaut32
        shell32
        ws2_32
        gdiplus
        d3d11
        d3dcompiler
        dxgi
        dxguid
    )
endif ()

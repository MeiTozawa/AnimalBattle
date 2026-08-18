file(GLOB_RECURSE ANIMALBATTLE_EXT_SOURCES CONFIGURE_DEPENDS
    "src/*.cpp"
    "AnimalBattleExtension.cpp"
)
file(GLOB_RECURSE ANIMALBATTLE_EXT_MODULES CONFIGURE_DEPENDS
    "src/*.ixx"
)

add_library(AnimalBattleExtension SHARED
    ${ANIMALBATTLE_EXT_SOURCES}
)

# Configure C++ Modules
target_sources(AnimalBattleExtension
    PUBLIC
    FILE_SET cxx_modules TYPE CXX_MODULES FILES
    ${ANIMALBATTLE_EXT_MODULES}
)

target_compile_definitions(AnimalBattleExtension PRIVATE NOMINMAX WIN32_LEAN_AND_MEAN)
set_target_properties(AnimalBattleExtension PROPERTIES PREFIX "")

target_include_directories(AnimalBattleExtension PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/src
    ${CMAKE_SOURCE_DIR}/AnimalBattleCore
    ${CMAKE_SOURCE_DIR}/AnimalBattleCore/src
)

target_link_libraries(AnimalBattleExtension PUBLIC
    AnimalBattleCore
)

if (MSVC)
    target_compile_options(AnimalBattleExtension PRIVATE /wd4828 /MP /permissive- /Zc:preprocessor)
elseif (CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
    target_compile_options(AnimalBattleExtension PRIVATE -fopenmp -w)
endif ()

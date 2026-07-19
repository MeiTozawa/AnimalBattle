file(GLOB_RECURSE ANIMALBATTLE_SOURCES CONFIGURE_DEPENDS
        "program/*.cpp"
        "AnimalBattleCore.cpp"
)
file(GLOB_RECURSE ANIMALBATTLE_MODULES CONFIGURE_DEPENDS
        "program/*.ixx"
)

add_library(AnimalBattleCore SHARED
    ${ANIMALBATTLE_SOURCES}
)

# Prevent min/max macro issues with DxLib/windows.h
target_compile_definitions(AnimalBattleCore PRIVATE NOMINMAX)

# Enable Unity Build for faster compilation
set_target_properties(AnimalBattleCore PROPERTIES UNITY_BUILD ON)

# Configure C++ Modules
target_sources(AnimalBattleCore
        PUBLIC
        FILE_SET cxx_modules TYPE CXX_MODULES FILES
        ${ANIMALBATTLE_MODULES}
)

target_include_directories(AnimalBattleCore PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/program
    ${DXLIB_DIR}
)

target_link_directories(AnimalBattleCore PUBLIC
    ${DXLIB_DIR}
)

target_link_libraries(AnimalBattleCore PUBLIC
)

@chcp 65001 >nul
@echo off
setlocal enabledelayedexpansion

REM Ensure current working directory is the repository root
cd /d "%~dp0"

echo ===============================================================================
echo                      AnimalBattle - One-Click Build Script
echo ===============================================================================
echo.

REM -----------------------------------------------------------------------------
REM Step 1: Resolve DxLib directory
REM -----------------------------------------------------------------------------
set "DXLIB_PATH=%~1"

if not "!DXLIB_PATH!"=="" goto :dxlib_normalize

if defined DXLIB_DIR (
    set "DXLIB_PATH=!DXLIB_DIR!"
    echo [INFO] Using DXLIB_DIR environment variable: !DXLIB_PATH!
    goto :dxlib_normalize
)

REM Standard DxLib VC package default folder is "プロジェクトに追加すべきファイル_VC用"
if exist "C:\DxLib_VC\プロジェクトに追加すべきファイル_VC用\DxLib.h" (
    set "DXLIB_PATH=C:\DxLib_VC\プロジェクトに追加すべきファイル_VC用"
    echo [INFO] Detected DxLib at default location: !DXLIB_PATH!
    goto :dxlib_normalize
)

echo Please enter the path to your DxLib directory.
echo Standard path: C:\DxLib_VC\プロジェクトに追加すべきファイル_VC用
set /p "DXLIB_PATH=DxLib Path: "

:dxlib_normalize
REM Strip quotes
set "DXLIB_PATH=!DXLIB_PATH:"=!"

REM Trim trailing backslash or forward slash
if "!DXLIB_PATH:~-1!"=="\" set "DXLIB_PATH=!DXLIB_PATH:~0,-1!"
if "!DXLIB_PATH:~-1!"=="/" set "DXLIB_PATH=!DXLIB_PATH:~0,-1!"

REM If user provided parent folder, check standard subfolders
if exist "!DXLIB_PATH!\プロジェクトに追加すべきファイル_VC用\DxLib.h" (
    set "DXLIB_PATH=!DXLIB_PATH!\プロジェクトに追加すべきファイル_VC用"
) 

REM Validate DxLib.h
if not exist "!DXLIB_PATH!\DxLib.h" goto :dxlib_not_found

echo [OK] DxLib path validated: !DXLIB_PATH!
echo.

REM -----------------------------------------------------------------------------
REM Step 2: Set up Visual Studio / MSVC build environment
REM -----------------------------------------------------------------------------
echo [INFO] Checking Visual Studio C++ build environment...

where cl.exe >nul 2>&1
if !ERRORLEVEL! EQU 0 (
    echo [OK] cl.exe compiler already available in PATH.
    goto :compiler_ready
)

set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%VSWHERE%" goto :find_vs
set "VSWHERE=%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%VSWHERE%" goto :find_vs
goto :vs_not_found

:find_vs
for /f "usebackq tokens=*" %%i in (`"!VSWHERE!" -latest -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do (
    set "VS_PATH=%%i"
)

if not defined VS_PATH goto :vs_not_found

echo [INFO] Found Visual Studio at: !VS_PATH!

if exist "!VS_PATH!\VC\Auxiliary\Build\vcvars64.bat" (
    call "!VS_PATH!\VC\Auxiliary\Build\vcvars64.bat" >nul
    goto :vcvars_done
)
if exist "!VS_PATH!\VC\Auxiliary\Build\vcvarsall.bat" (
    call "!VS_PATH!\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul
    goto :vcvars_done
)
goto :vs_not_found

:vcvars_done
where cl.exe >nul 2>&1
if !ERRORLEVEL! NEQ 0 goto :vs_not_found
echo [OK] MSVC x64 environment initialized successfully.

:compiler_ready
echo.

REM -----------------------------------------------------------------------------
REM Step 3: Check CMake, Ninja, and .NET SDK
REM -----------------------------------------------------------------------------
where cmake.exe >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    if defined VS_PATH (
        if exist "!VS_PATH!\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe" (
            set "PATH=!VS_PATH!\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin;!PATH!"
        )
    )
)

where cmake.exe >nul 2>&1
if !ERRORLEVEL! NEQ 0 goto :cmake_not_found

where ninja.exe >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    if defined VS_PATH (
        if exist "!VS_PATH!\Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja\ninja.exe" (
            set "PATH=!VS_PATH!\Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja;!PATH!"
        )
    )
)

where ninja.exe >nul 2>&1
if !ERRORLEVEL! NEQ 0 goto :ninja_not_found

where dotnet.exe >nul 2>&1
if !ERRORLEVEL! NEQ 0 goto :dotnet_not_found

echo [OK] Build tools verified: MSVC, CMake, Ninja, .NET SDK.
echo.

REM -----------------------------------------------------------------------------
REM Step 4: Configure and build AnimalBattleCore [C++23]
REM -----------------------------------------------------------------------------
echo ===============================================================================
echo [Phase 1/2] Building AnimalBattleCore [C++23 Shared Library]...
echo ===============================================================================

REM Clean stale/conflicting MinGW or incompatible cache if present
if exist "cmake-build-debug\CMakeCache.txt" (
    findstr /i "mingw gcc ld.exe" "cmake-build-debug\CMakeCache.txt" >nul 2>&1
    if !ERRORLEVEL! EQU 0 (
        echo [INFO] Cleaning incompatible compiler cache from cmake-build-debug...
        del /f /q "cmake-build-debug\CMakeCache.txt" >nul 2>&1
        if exist "cmake-build-debug\CMakeFiles" rmdir /s /q "cmake-build-debug\CMakeFiles" >nul 2>&1
    )
)

REM Convert backslashes to forward slashes for CMake path safety
set "DXLIB_DIR_CMAKE=!DXLIB_PATH:\=/!"

echo [INFO] Running CMake configure...
cmake -B cmake-build-debug -G Ninja -DCMAKE_BUILD_TYPE=Debug -DDXLIB_DIR="!DXLIB_DIR_CMAKE!"
if !ERRORLEVEL! NEQ 0 (
    echo [WARN] CMake configure failed. Retrying with a clean cache...
    if exist "cmake-build-debug\CMakeCache.txt" del /f /q "cmake-build-debug\CMakeCache.txt" >nul 2>&1
    if exist "cmake-build-debug\CMakeFiles" rmdir /s /q "cmake-build-debug\CMakeFiles" >nul 2>&1
    cmake -B cmake-build-debug -G Ninja -DCMAKE_BUILD_TYPE=Debug -DDXLIB_DIR="!DXLIB_DIR_CMAKE!"
    if !ERRORLEVEL! NEQ 0 goto :cmake_config_failed
)

echo [INFO] Building AnimalBattleCore with Ninja...
cmake --build cmake-build-debug --config Debug
if !ERRORLEVEL! NEQ 0 goto :cmake_build_failed

if not exist "cmake-build-debug\AnimalBattleCore\AnimalBattleCore.dll" goto :dll_missing

echo [OK] AnimalBattleCore.dll built successfully.
echo.

REM -----------------------------------------------------------------------------
REM Step 5: Build AnimalBattle [C# .NET 10]
REM -----------------------------------------------------------------------------
echo ===============================================================================
echo [Phase 2/2] Building AnimalBattle [C# .NET 10 Gameplay Layer]...
echo ===============================================================================

dotnet build AnimalBattle\AnimalBattle.csproj -c Debug
if !ERRORLEVEL! NEQ 0 goto :csharp_build_failed

REM Ensure DLL is present in target binary folder
if not exist "AnimalBattle\bin\Debug\net10.0\AnimalBattleCore.dll" (
    copy /Y "cmake-build-debug\AnimalBattleCore\AnimalBattleCore.dll" "AnimalBattle\bin\Debug\net10.0\AnimalBattleCore.dll" >nul
)

echo.
echo ===============================================================================
echo                             BUILD COMPLETED!
echo ===============================================================================
echo.
echo Build Artifacts:
echo   [Core DLL]    cmake-build-debug\AnimalBattleCore\AnimalBattleCore.dll
echo   [Game Exec]   AnimalBattle\bin\Debug\net10.0\AnimalBattle.exe
echo.
echo How to run:
echo   1. Via .NET CLI : dotnet run --project AnimalBattle\AnimalBattle.csproj
echo   2. Direct Exec  : AnimalBattle\bin\Debug\net10.0\AnimalBattle.exe
echo.

goto :success_exit

REM -----------------------------------------------------------------------------
REM Error Handlers (Always pause to prevent sudden window closing)
REM -----------------------------------------------------------------------------

:dxlib_not_found
echo.
echo ===============================================================================
echo [ERROR] DxLib.h was not found at: "!DXLIB_PATH!"
echo.
echo Please specify the folder containing DxLib.h and library files.
echo Standard path:
echo   C:\DxLib_VC\プロジェクトに追加すべきファイル_VC用
echo ===============================================================================
goto :fail_exit

:vs_not_found
echo.
echo ===============================================================================
echo [ERROR] Visual Studio C++ build environment [MSVC x64] was not found!
echo Please ensure Visual Studio 2022 or 2026 is installed with the
echo "Desktop development with C++" workload.
echo ===============================================================================
goto :fail_exit

:cmake_not_found
echo.
echo ===============================================================================
echo [ERROR] cmake.exe was not found in PATH or Visual Studio installation.
echo Please install CMake 3.28+ or enable "C++ CMake tools for Windows" in Visual Studio.
echo ===============================================================================
goto :fail_exit

:ninja_not_found
echo.
echo ===============================================================================
echo [ERROR] ninja.exe was not found in PATH or Visual Studio installation.
echo Please install Ninja or enable "C++ CMake tools for Windows" in Visual Studio.
echo ===============================================================================
goto :fail_exit

:dotnet_not_found
echo.
echo ===============================================================================
echo [ERROR] dotnet CLI not found. Please install .NET 10 SDK.
echo ===============================================================================
goto :fail_exit

:cmake_config_failed
echo.
echo ===============================================================================
echo [ERROR] Phase 1: CMake configuration failed!
echo Check the error output above.
echo Common causes:
echo   1. Conflicting cache in cmake-build-debug directory.
echo      Fix: Delete the folder 'cmake-build-debug' and run build.bat again.
echo   2. Invalid DxLib path or missing DxLibW.lib.
echo ===============================================================================
goto :fail_exit

:cmake_build_failed
echo.
echo ===============================================================================
echo [ERROR] Phase 1: AnimalBattleCore compilation failed!
echo Check the compiler errors output above.
echo Common causes:
echo   1. AnimalBattleCore.dll is locked by a running game instance or IDE debugger.
echo      Fix: Close any running AnimalBattle processes or IDE debuggers.
echo   2. Missing DxLib library files in your DxLib directory.
echo ===============================================================================
goto :fail_exit

:dll_missing
echo.
echo ===============================================================================
echo [ERROR] AnimalBattleCore.dll was not generated in cmake-build-debug\AnimalBattleCore!
echo ===============================================================================
goto :fail_exit

:csharp_build_failed
echo.
echo ===============================================================================
echo [ERROR] Phase 2: AnimalBattle C# build failed!
echo Check the dotnet build errors printed above.
echo ===============================================================================
goto :fail_exit

:fail_exit
echo.
echo Build process stopped with errors.
echo Press any key to close this window...
pause >nul
exit /b 1

:success_exit
echo.
echo Press any key to close this window...
pause >nul
exit /b 0

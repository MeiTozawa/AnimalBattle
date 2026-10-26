@echo off
setlocal enabledelayedexpansion

echo ===============================================================================
echo                      AnimalBattle - One-Click Build Script
echo ===============================================================================
echo.

:: -----------------------------------------------------------------------------
:: Step 1: Resolve DxLib directory
:: -----------------------------------------------------------------------------
set "DXLIB_PATH=%~1"

if not "!DXLIB_PATH!"=="" goto :dxlib_resolved

if defined DXLIB_DIR (
    set "DXLIB_PATH=!DXLIB_DIR!"
    echo [INFO] Using DXLIB_DIR environment variable: !DXLIB_PATH!
    goto :dxlib_resolved
)

if exist "C:\DxLib_VC\プロジェクトに追加すべきファイル_VC用\DxLib.h" (
    set "DXLIB_PATH=C:\DxLib_VC\プロジェクトに追加すべきファイル_VC用"
    echo [INFO] Detected DxLib at default location: !DXLIB_PATH!
    goto :dxlib_resolved
)

echo Please enter the path to your DxLib directory.
echo (This should be the folder containing DxLib.h, e.g. C:\DxLib_VC\プロジェクトに追加すべきファイル_VC用)
set /p "DXLIB_PATH=DxLib Path: "

:dxlib_resolved
:: Trim quotes
set "DXLIB_PATH=!DXLIB_PATH:"=!"

:: Trim trailing backslash or forward slash
if "!DXLIB_PATH:~-1!"=="\" set "DXLIB_PATH=!DXLIB_PATH:~0,-1!"
if "!DXLIB_PATH:~-1!"=="/" set "DXLIB_PATH=!DXLIB_PATH:~0,-1!"

:: If user pointed to the root DxLib_VC folder, adjust to VC subfolder
if exist "!DXLIB_PATH!\プロジェクトに追加すべきファイル_VC用\DxLib.h" (
    set "DXLIB_PATH=!DXLIB_PATH!\プロジェクトに追加すべきファイル_VC用"
)

:: Validate DxLib.h exists
if not exist "!DXLIB_PATH!\DxLib.h" (
    echo.
    echo [ERROR] DxLib.h was not found at: "!DXLIB_PATH!"
    echo Please ensure the specified directory contains DxLib.h and library files.
    echo Example valid path: C:\DxLib_VC\プロジェクトに追加すべきファイル_VC用
    echo.
    exit /b 1
)

echo [OK] DxLib path validated: !DXLIB_PATH!
echo.

:: -----------------------------------------------------------------------------
:: Step 2: Set up Visual Studio / MSVC build environment
:: -----------------------------------------------------------------------------
echo [INFO] Checking Visual Studio C++ build environment...

where cl.exe >nul 2>&1
if !ERRORLEVEL! EQU 0 (
    echo [OK] cl.exe compiler already available in PATH.
    goto :compiler_ready
)

set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "!VSWHERE!" (
    set "VSWHERE=%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe"
)

if not exist "!VSWHERE!" (
    echo [ERROR] vswhere.exe not found. Visual Studio installation could not be detected.
    echo Please install Visual Studio 2022 / 2026 with "Desktop development with C++".
    exit /b 1
)

for /f "usebackq tokens=*" %%i in (`"!VSWHERE!" -latest -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do (
    set "VS_PATH=%%i"
)

if not defined VS_PATH (
    echo [ERROR] Visual Studio C++ toolset not found.
    echo Please install the "Desktop development with C++" workload via the Visual Studio Installer.
    exit /b 1
)

echo [INFO] Found Visual Studio at: !VS_PATH!

if exist "!VS_PATH!\VC\Auxiliary\Build\vcvars64.bat" (
    call "!VS_PATH!\VC\Auxiliary\Build\vcvars64.bat" >nul
    goto :vcvars_done
)
if exist "!VS_PATH!\VC\Auxiliary\Build\vcvarsall.bat" (
    call "!VS_PATH!\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul
    goto :vcvars_done
)

echo [ERROR] Unable to locate vcvars64.bat or vcvarsall.bat in Visual Studio.
exit /b 1

:vcvars_done
where cl.exe >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    echo [ERROR] Failed to initialize MSVC x64 compiler environment.
    exit /b 1
)
echo [OK] MSVC x64 environment initialized successfully.

:compiler_ready
echo.

:: -----------------------------------------------------------------------------
:: Step 3: Check CMake, Ninja, and .NET SDK
:: -----------------------------------------------------------------------------
where cmake.exe >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    if defined VS_PATH (
        if exist "!VS_PATH!\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe" (
            set "PATH=!VS_PATH!\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin;!PATH!"
        )
    )
)

where cmake.exe >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    echo [ERROR] cmake.exe not found in PATH or Visual Studio installation.
    echo Please install CMake 3.28+ or enable "C++ CMake tools for Windows" in Visual Studio.
    exit /b 1
)

where ninja.exe >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    if defined VS_PATH (
        if exist "!VS_PATH!\Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja\ninja.exe" (
            set "PATH=!VS_PATH!\Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja;!PATH!"
        )
    )
)

where ninja.exe >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    echo [ERROR] ninja.exe not found in PATH or Visual Studio installation.
    echo Please install Ninja or enable "C++ CMake tools for Windows" in Visual Studio.
    exit /b 1
)

where dotnet.exe >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    echo [ERROR] dotnet CLI not found. Please install .NET 10 SDK.
    exit /b 1
)

echo [OK] Build tools verified: MSVC, CMake, Ninja, .NET SDK.
echo.

:: -----------------------------------------------------------------------------
:: Step 4: Configure and build AnimalBattleCore (C++23)
:: -----------------------------------------------------------------------------
echo ===============================================================================
echo [Phase 1/2] Building AnimalBattleCore (C++23 Shared Library)...
echo ===============================================================================

:: Format DXLIB_PATH with forward slashes for CMake compatibility
set "DXLIB_DIR_CMAKE=!DXLIB_PATH:\=/!"

echo [INFO] Running CMake configure...
cmake -B cmake-build-debug -G Ninja -DCMAKE_BUILD_TYPE=Debug -DDXLIB_DIR="!DXLIB_DIR_CMAKE!"
if !ERRORLEVEL! NEQ 0 (
    echo.
    echo [ERROR] CMake configuration failed.
    exit /b !ERRORLEVEL!
)

echo [INFO] Building AnimalBattleCore with Ninja...
cmake --build cmake-build-debug --config Debug
if !ERRORLEVEL! NEQ 0 (
    echo.
    echo [ERROR] AnimalBattleCore compilation failed.
    exit /b !ERRORLEVEL!
)

if not exist "cmake-build-debug\AnimalBattleCore\AnimalBattleCore.dll" (
    echo.
    echo [ERROR] AnimalBattleCore.dll was not generated.
    exit /b 1
)

echo [OK] AnimalBattleCore.dll built successfully.
echo.

:: -----------------------------------------------------------------------------
:: Step 5: Build AnimalBattle (C# .NET 10)
:: -----------------------------------------------------------------------------
echo ===============================================================================
echo [Phase 2/2] Building AnimalBattle (C# .NET 10 Gameplay Layer)...
echo ===============================================================================

dotnet build AnimalBattle\AnimalBattle.csproj -c Debug
if !ERRORLEVEL! NEQ 0 (
    echo.
    echo [ERROR] AnimalBattle build failed.
    exit /b !ERRORLEVEL!
)

:: Ensure DLL is present in target binary folder
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
exit /b 0

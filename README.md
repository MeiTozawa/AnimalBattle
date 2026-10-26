# AnimalBattle

A high-performance 2D action boss-rush game built with a dual-engine architecture: a **C++23 native core** powering graphics and platform services via **DxLib** / DirectX, and a **C# .NET 10 pure ECS gameplay layer** powered by **Flecs.NET**.

---

## Architecture Overview

```
+-------------------------------------------------------------------------+
|                       AnimalBattle (C# .NET 10)                         |
|                                                                         |
|  - Flecs.NET Pure Data-Driven ECS (Zero-GC hot paths)                   |
|  - Feature Slices: Player, Bosses, Combat, Arena, Presentation          |
|  - Action Gameplay Ability System (GAS) & 64-bit GameplayTags           |
|  - RCU Lock-Free TOML Configuration with Runtime Hot-Reloading          |
+-------------------------------------------------------------------------+
                                    |
                    P/Invoke (SuppressGCTransition)
                                    v
+-------------------------------------------------------------------------+
|                    AnimalBattleCore.dll (C++23 Native)                  |
|                                                                         |
|  - Win32 Window Management & Message Loop                               |
|  - High-precision Geometric Sweep Tests (Platform Ground Contact)      |
|  - DxLib / DirectX 9 Rendering & Audio Services                         |
|  - C++20/23 Modules (`.ixx`) and C-compatible ABI exports               |
+-------------------------------------------------------------------------+
```

---

## Prerequisites & Requirements

Before building the project, ensure the following tools and dependencies are installed on your machine:

| Requirement | Recommended Version | Description |
| :--- | :--- | :--- |
| **Operating System** | Windows 10 / 11 (64-bit) | Windows x64 environment |
| **Visual Studio** | 2022 (v17.4+) or 2026 | Must have **"Desktop development with C++"** installed with MSVC x64 toolset and C++ CMake tools |
| **.NET SDK** | .NET 10.0+ | Required to build the C# gameplay layer (`net10.0`) |
| **CMake** | 3.28 or later | Required for C++20/C++23 `FILE_SET cxx_modules` support |
| **Ninja** | 1.11 or later | Fast build generator used with MSVC (bundled with Visual Studio C++ CMake tools) |
| **DxLib (VC package)** | Latest (3.24+) | Windows Visual Studio C++ edition of DxLib ([DxLib Official Website](https://dxlib.xsrv.jp/)) |
| **Python** *(Optional)* | 3.10+ | Used by pre-build target to regenerate typed asset constants (`scripts/generate_resource_paths.py`) |

> [!IMPORTANT]
> **DxLib Directory Structure**:
> Download the **DxLib for Visual Studio / C++** package (`DxLib_VC.zip`). When unzipped, it contains a folder named `プロジェクトに追加すべきファイル_VC用`, which directly houses `DxLib.h`, `DxCompileConfig.h`, and `DxLibW.lib`. You will need to provide the path to this folder (e.g. `C:\DxLib_VC\プロジェクトに追加すべきファイル_VC用`).

---

## One-Click Build (`build.bat`)

The easiest way to configure and build the entire solution is via the bundled [`build.bat`](build.bat) script.

### Usage Options

#### Option 1: Provide DxLib path as an argument
```cmd
build.bat "C:\DxLib_VC\プロジェクトに追加すべきファイル_VC用"
```
*(If you pass `C:\DxLib_VC`, the script will automatically detect and use the `プロジェクトに追加すべきファイル_VC用` or `VC` subfolder if present).*

#### Option 2: Set the `DXLIB_DIR` environment variable
```cmd
set DXLIB_DIR=C:\DxLib_VC\プロジェクトに追加すべきファイル_VC用
build.bat
```

#### Option 3: Interactive Prompt / Auto-Detection
Simply double-click [`build.bat`](build.bat) or run it in your terminal without arguments:
```cmd
build.bat
```
* If DxLib is located at `C:\DxLib_VC\VC` (the default installation path), it will be detected automatically.
* Otherwise, the script will prompt you to enter the path.

### What `build.bat` Does Automatically
1. Locates and validates `DxLib.h` in your specified directory.
2. Finds your Visual Studio MSVC installation via `vswhere` and initializes the 64-bit compiler environment (`vcvars64.bat`).
3. Verifies `cmake`, `ninja`, and `dotnet`.
4. Runs CMake to configure and build `AnimalBattleCore.dll` (C++23) into `cmake-build-debug/AnimalBattleCore/`.
5. Compiles the C# project `AnimalBattle` (.NET 10) and copies the native DLL to the output directory.

---

## Manual Step-by-Step Build Guide

If you prefer building manually or want to configure your IDE (CLion, Visual Studio, Visual Studio Code), follow these steps.

### Step 1: Build `AnimalBattleCore` (C++23 Native Core)

1. Open the **x64 Native Tools Command Prompt for VS 2022 / 2026** (or run `vcvars64.bat` in your terminal).
2. Navigate to the project root directory:
   ```cmd
   cd /d "C:\Projects\Dxlib\AnimalBattle"
   ```
3. Configure CMake with Ninja and specify your DxLib directory:
   ```cmd
   cmake -B cmake-build-debug -G Ninja -DCMAKE_BUILD_TYPE=Debug -DDXLIB_DIR="C:/DxLib_VC/プロジェクトに追加すべきファイル_VC用"
   ```
   *Note: Use forward slashes `/` in the `DXLIB_DIR` CMake parameter.*
4. Compile the native DLL:
   ```cmd
   cmake --build cmake-build-debug --config Debug
   ```
5. Verify that the output binary exists:
   ```
   cmake-build-debug/AnimalBattleCore/AnimalBattleCore.dll
   ```

#### Configuring in IDEs (CLion / VS Code / Visual Studio)
* Set the CMake generator to **Ninja**.
* Set the C++ toolchain to **MSVC (Visual Studio x64)** (MSVC is required for full C++23 Module support in this project).
* Add `-DDXLIB_DIR=C:/DxLib_VC/プロジェクトに追加すべきファイル_VC用` to your CMake options or inside `CMakeUserPresets.json`.

---

### Step 2: Build `AnimalBattle` (C# .NET 10 Gameplay Layer)

Once `AnimalBattleCore.dll` is compiled in `cmake-build-debug/AnimalBattleCore/`, the C# project will automatically link and copy it to its binary folder.

1. Build the Debug configuration using the .NET CLI:
   ```cmd
   dotnet build AnimalBattle/AnimalBattle.csproj -c Debug
   ```
2. *(Optional)* Publish a high-performance Native AOT / Release build:
   ```cmd
   dotnet publish AnimalBattle/AnimalBattle.csproj -c Release -r win-x64
   ```

---

## Running the Game

You can run the game using either of the following methods:

### Method 1: Using the .NET CLI
```cmd
dotnet run --project AnimalBattle/AnimalBattle.csproj
```

### Method 2: Launching the Executable Directly
Run the compiled binary:
```cmd
AnimalBattle\bin\Debug\net10.0\AnimalBattle.exe
```

> [!NOTE]
> **Asset & Config Discovery**:
> `AnimalBattle` features dynamic asset and configuration resolvers ([`AssetPathResolver.cs`](AnimalBattle/Engine/Asset/AssetPathResolver.cs) and [`ConfigPathResolver.cs`](AnimalBattle/Engine/ConfigSystem/ConfigPathResolver.cs)). They automatically search the working directory and recursively traverse ancestor directories to discover `Resource/` and `Config/`. You can safely launch the game from either the repository root or the executable's directory.

---

## Repository Directory Structure

```
AnimalBattle/
├── AnimalBattle/                     # C# .NET 10 Gameplay Layer (Flecs.NET ECS)
│   ├── Engine/                       # Core engine services (Asset, Bindings, Config, Audio, Math)
│   ├── Features/                     # Feature Slices (Player, Bosses, Combat, Arena, Presentation)
│   ├── Framework/                    # Zero-GC infrastructure (Ability System / GAS, Object Pools)
│   ├── Scenes/                       # Scene flow & Composition Root (GameFlow, StartScene, BattleScene)
│   ├── AnimalBattle.csproj           # C# project definition
│   ├── GameManager.cs                # Deterministic main loop orchestrator
│   └── Program.cs                    # Application entry point
│
├── AnimalBattleCore/                 # C++23 Native Core (DxLib & Win32)
│   ├── src/                          # C++20/23 Modules (.ixx) & implementations
│   │   ├── Core/                     # Engine core definitions and C export interfaces
│   │   ├── Input/                    # DxLib input service module
│   │   ├── Physics/                  # Geometric sweep test platform collision module
│   │   ├── Render/                   # DxLib rendering service module
│   │   └── Sound/                    # DxLib audio service module
│   ├── AnimalBattleCore.cpp          # P/Invoke C ABI bridge implementation
│   └── CMakeLists.txt                # C++ CMake configuration
│
├── Config/                           # TOML game configuration files (Hot-reload supported)
│   ├── Arena/                        # Arena dimensions and layout config
│   ├── Audio/                        # BGM & Sound effect settings
│   ├── Boss/                         # Boss phase, behavior, and telegraph configs
│   ├── Combat/                       # Bullet, weapon, and damage attributes
│   └── Player/                       # Player movement, speed, and dash parameters
│
├── Resource/                         # Game assets (sprites, textures, audio, fonts)
├── scripts/                          # Build automation and code-generation utilities
│   ├── generate_resource_paths.py    # Generates typed C# asset paths (ResourcePath.g.cs)
│   └── verify_comments.py            # Code comment integrity verification
│
├── CMakeLists.txt                    # Top-level CMake project configuration
├── build.bat                         # One-click automated build script
└── README.md                         # Project documentation (this file)
```

---

## Troubleshooting & FAQ

### 1. `fatal error: DxLib.h: No such file or directory`
* **Cause**: `DXLIB_DIR` was not specified or points to the wrong folder.
* **Fix**: Ensure `DXLIB_DIR` points to the folder containing `DxLib.h` (e.g., `C:\DxLib_VC\プロジェクトに追加すべきファイル_VC用`). If your DxLib root is `C:\DxLib_VC`, pass `C:\DxLib_VC\プロジェクトに追加すべきファイル_VC用`.

### 2. Ambiguous symbol or module errors with GCC / MinGW
* **Cause**: `AnimalBattleCore` utilizes C++23 Modules (`FILE_SET cxx_modules`). Current versions of MinGW GCC have known limitations with module export resolution for this codebase.
* **Fix**: Always build using the **MSVC toolset** (Visual Studio 2022 v17.4+ or 2026) alongside **Ninja**.

### 3. `DllNotFoundException: Unable to load DLL 'AnimalBattleCore.dll'`
* **Cause**: `AnimalBattleCore.dll` has not been compiled yet, or was not copied to the executable directory.
* **Fix**: Run `build.bat` or manually compile `AnimalBattleCore` first via CMake into `cmake-build-debug/AnimalBattleCore/`. `AnimalBattle.csproj` will automatically copy it on build.

### 4. Missing .NET 10 SDK
* **Cause**: The project targets `net10.0`.
* **Fix**: Install the .NET 10 SDK from the [.NET Official Download Page](https://dotnet.microsoft.com/download).

---

## License & Credits

* **DxLib**: Copyright (C) Takumi Yamada ([DxLib Official](https://dxlib.xsrv.jp/))
* **Flecs.NET**: Flecs C# bindings ([Flecs.NET GitHub](https://github.com/BeanCheeseBurrito/Flecs.NET))

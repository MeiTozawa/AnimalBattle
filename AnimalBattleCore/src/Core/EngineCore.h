#ifndef ANIMALBATTLE_CORE_ENGINE_CORE_H_
#define ANIMALBATTLE_CORE_ENGINE_CORE_H_

#ifdef _WIN32
  #ifdef ANIMALBATTLE_CORE_BUILD_DLL
    #define ANIMALBATTLE_CORE_API __declspec(dllexport)
  #else
    #define ANIMALBATTLE_CORE_API __declspec(dllimport)
  #endif
#else
  #define ANIMALBATTLE_CORE_API
#endif

namespace ab {

class IRenderService;
class IInputService;
class IPhysicsService;

/// @brief アクティブなレンダリングサービスを設定する
ANIMALBATTLE_CORE_API void SetRenderService(IRenderService* service) noexcept;

/// @brief 現在アクティブなレンダリングサービスを取得する
ANIMALBATTLE_CORE_API IRenderService* GetRenderService() noexcept;

/// @brief アクティブな入力サービスを設定する
ANIMALBATTLE_CORE_API void SetInputService(IInputService* service) noexcept;

/// @brief 現在アクティブな入力サービスを取得する
ANIMALBATTLE_CORE_API IInputService* GetInputService() noexcept;

/// @brief アクティブな物理サービスを設定する
ANIMALBATTLE_CORE_API void SetPhysicsService(IPhysicsService* service) noexcept;

/// @brief 現在アクティブな物理サービスを取得する
ANIMALBATTLE_CORE_API IPhysicsService* GetPhysicsService() noexcept;

class ISoundService;

/// @brief アクティブなサウンドサービスを設定する
ANIMALBATTLE_CORE_API void SetSoundService(ISoundService* service) noexcept;

/// @brief 現在アクティブなサウンドサービスを取得する
ANIMALBATTLE_CORE_API ISoundService* GetSoundService() noexcept;

/// @brief DxLib バックエンドを初期化してサービスを登録する
ANIMALBATTLE_CORE_API bool SetupDxLibBackend(void* hwnd, int width, int height) noexcept;

/// @brief DxLib バックエンドをシャットダウンする
ANIMALBATTLE_CORE_API void ShutdownDxLibBackend() noexcept;

/// @brief DxLib のウィンドウメッセージを処理する
ANIMALBATTLE_CORE_API bool ProcessDxLibMessages() noexcept;

/// @brief マウスカーソルの表示／非表示を設定する
ANIMALBATTLE_CORE_API void SetMouseDispFlag(bool visible) noexcept;

}  // namespace ab

#endif  // ANIMALBATTLE_CORE_ENGINE_CORE_H_



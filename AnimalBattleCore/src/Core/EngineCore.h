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

/// @brief アクティブなレンダリングサービスを設定します
/// @param service 登録対象のレンダリングサービス
ANIMALBATTLE_CORE_API void SetRenderService(IRenderService* service) noexcept;

/// @brief 現在アクティブなレンダリングサービスを取得します
/// @return 登録済みのレンダリングサービスへのポインタ
ANIMALBATTLE_CORE_API IRenderService* GetRenderService() noexcept;

/// @brief アクティブな入力サービスを設定します
/// @param service 登録対象の入力サービス
ANIMALBATTLE_CORE_API void SetInputService(IInputService* service) noexcept;

/// @brief 現在アクティブな入力サービスを取得します
/// @return 登録済みの入力サービスへのポインタ
ANIMALBATTLE_CORE_API IInputService* GetInputService() noexcept;

/// @brief アクティブな物理サービスを設定します
/// @param service 登録対象の物理サービス
ANIMALBATTLE_CORE_API void SetPhysicsService(IPhysicsService* service) noexcept;

/// @brief 現在アクティブな物理サービスを取得します
/// @return 登録済みの物理サービスへのポインタ
ANIMALBATTLE_CORE_API IPhysicsService* GetPhysicsService() noexcept;

class ISoundService;

/// @brief アクティブなサウンドサービスを設定します
/// @param service 登録対象のサウンドサービス
ANIMALBATTLE_CORE_API void SetSoundService(ISoundService* service) noexcept;

/// @brief 現在アクティブなサウンドサービスを取得します
/// @return 登録済みのサウンドサービスへのポインタ
ANIMALBATTLE_CORE_API ISoundService* GetSoundService() noexcept;

/// @brief DxLib描画バックエンドを初期化し各種具象サービスを登録します
/// @param hwnd メインウィンドウのネイティブハンドル
/// @param width バックバッファの初期水平解像度
/// @param height バックバッファの初期垂直解像度
/// @return 初期化に成功した場合はtrue、失敗時はfalse
/// @note Windowsメッセージポンプと同一のメインスレッドから呼び出す必要があります
ANIMALBATTLE_CORE_API bool SetupDxLibBackend(void* hwnd, int width, int height) noexcept;

/// @brief DxLib描画バックエンドを安全に終了処理します
/// @note 確保済みテクスチャおよび描画サーフェスを解放します
ANIMALBATTLE_CORE_API void ShutdownDxLibBackend() noexcept;

/// @brief DxLibのWindowsウィンドウメッセージを処理します
/// @return アプリケーション継続時はtrue、終了要求受信時はfalse
/// @note メインループの先頭で毎フレーム呼び出す必要があります
ANIMALBATTLE_CORE_API bool ProcessDxLibMessages() noexcept;

/// @brief マウスカーソルの表示状態を切り替えます
/// @param visible カーソルを表示する場合はtrue、非表示時はfalse
ANIMALBATTLE_CORE_API void SetMouseDispFlag(bool visible) noexcept;

}  // namespace ab

#endif  // ANIMALBATTLE_CORE_ENGINE_CORE_H_



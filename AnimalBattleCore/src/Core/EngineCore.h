#ifndef ANIMALBATTLE_CORE_ENGINE_CORE_H_
#define ANIMALBATTLE_CORE_ENGINE_CORE_H_

namespace ab {

class IRenderService;
class IInputService;
class IPhysicsService;

/// @brief アクティブなレンダリングサービスを設定する
void SetRenderService(IRenderService* service) noexcept;

/// @brief 現在アクティブなレンダリングサービスを取得する
IRenderService* GetRenderService() noexcept;

/// @brief アクティブな入力サービスを設定する
void SetInputService(IInputService* service) noexcept;

/// @brief 現在アクティブな入力サービスを取得する
IInputService* GetInputService() noexcept;

/// @brief アクティブな物理サービスを設定する
void SetPhysicsService(IPhysicsService* service) noexcept;

/// @brief 現在アクティブな物理サービスを取得する
IPhysicsService* GetPhysicsService() noexcept;

/// @brief DxLib バックエンドを初期化してサービスを登録する
bool SetupDxLibBackend(void* hwnd, int width, int height) noexcept;

/// @brief DxLib バックエンドをシャットダウンする
void ShutdownDxLibBackend() noexcept;

/// @brief DxLib のウィンドウメッセージを処理する
bool ProcessDxLibMessages() noexcept;

}  // namespace ab

#endif  // ANIMALBATTLE_CORE_ENGINE_CORE_H_

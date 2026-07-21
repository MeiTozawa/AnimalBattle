module;

#include <unordered_map>
#include <typeindex>
#include <functional>
#include <vector>
#include <algorithm>
#include <concepts>
#include <ranges>
#include <shared_mutex>

export module EventBus;

namespace ab {
    /// @brief ゲーム内イベントクラスの基底インターフェース
    export struct IEvent {
        virtual ~IEvent() = default;
    };

    export using EventHandle = uint64_t;

    /// @brief イベントの購読（Subscribe）、解除（Unsubscribe）、および発火（Publish）を仲介する静的イベントバス
    export class EventBus {
    public:
        /// @brief 特定のイベントに対するコールバックを登録する
        /// @tparam TEvent 購読対象のイベントクラス
        /// @param callback イベント発生時に呼び出される関数
        /// @return 登録の解除に使用するEventHandle
        template <std::derived_from<IEvent> TEvent>
        static EventHandle Subscribe(std::function<void(const TEvent&)> callback) {
            std::unique_lock lock(GetMutex());
            EventHandle handle = GetNextHandle()++;
            auto& listeners_map = GetListeners();
            auto type_id = std::type_index(typeid(TEvent));

            auto old_vec_ptr = listeners_map[type_id];
            auto new_vec_ptr = std::make_shared<std::vector<CallbackWrapper>>();
            if (old_vec_ptr) {
                *new_vec_ptr = *old_vec_ptr;
            }

            new_vec_ptr->push_back({
                handle,
                [callback](const IEvent& e) {
                    callback(static_cast<const TEvent&>(e));
                }
            });

            listeners_map[type_id] = new_vec_ptr;
            GetHandleToTypeMap().insert_or_assign(handle, type_id);

            return handle;
        }

        /// @brief 指定したハンドルを用いてイベント購読を解除する
        /// @param handle 解除対象のEventHandle
        static void Unsubscribe(EventHandle handle) {
            std::unique_lock lock(GetMutex());
            auto& map = GetHandleToTypeMap();
            auto it_map = map.find(handle);
            if (it_map == map.end()) return;

            std::type_index type_id = it_map->second;
            auto& listeners_map = GetListeners();
            auto old_vec_ptr = listeners_map[type_id];
            if (!old_vec_ptr) return;

            auto new_vec_ptr = std::make_shared<std::vector<CallbackWrapper>>(*old_vec_ptr);

            std::erase_if(*new_vec_ptr, [handle](const CallbackWrapper& w) {
                return w.handle == handle;
            });

            listeners_map[type_id] = new_vec_ptr;
            map.erase(it_map);
        }

        /// @brief 指定したイベントを発行し、購読しているすべてのコールバックを実行する
        /// @tparam TEvent 発行するイベントクラス
        /// @param event 発行するイベントインスタンス
        template <std::derived_from<IEvent> TEvent>
        static void Publish(const TEvent& event) {
            std::shared_ptr<std::vector<CallbackWrapper>> callbacks;
            {
                std::shared_lock lock(GetMutex());
                auto& listeners = GetListeners();
                auto it = listeners.find(std::type_index(typeid(TEvent)));
                if (it != listeners.end()) {
                    callbacks = it->second;
                }
            }
            if (callbacks) {
                for (const auto& wrapper : *callbacks) {
                    wrapper.callback(event);
                }
            }
        }

        static void Clear() {
            std::unique_lock lock(GetMutex());
            GetListeners().clear();
            GetHandleToTypeMap().clear();
        }

    private:
        struct CallbackWrapper {
            EventHandle handle;
            std::function<void(const IEvent&)> callback;
        };

        static std::unordered_map<std::type_index, std::shared_ptr<std::vector<CallbackWrapper>>>& GetListeners() {
            static std::unordered_map<std::type_index, std::shared_ptr<std::vector<CallbackWrapper>>> listeners;
            return listeners;
        }

        static std::unordered_map<EventHandle, std::type_index>& GetHandleToTypeMap() {
            static std::unordered_map<EventHandle, std::type_index> map;
            return map;
        }

        static std::shared_mutex& GetMutex() {
            static std::shared_mutex mtx;
            return mtx;
        }

        static EventHandle& GetNextHandle() {
            static EventHandle next_handle = 0;
            return next_handle;
        }
    };
} // namespace ab

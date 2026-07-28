module;

#include <cstdint>

export module CollisionTypes;

namespace ab {
    /// @brief 衝突判定のセマンティックカテゴリ（機能レイヤー）
    export enum class CollisionCategory : uint32_t {
        None = 0,
        Environment = 1 << 0,   // 地形・プラットフォーム・バーストゾーン
        CharacterBody = 1 << 1, // プレイヤー/エネミーの食らい判定（Hurtbox）
        AttackHitbox = 1 << 2,  // 攻撃判定（Hitbox）
        Projectile = 1 << 3,    // 飛び道具・アイテム
        TriggerZone = 1 << 4,   // トリガー領域
        All = 0xFFFFFFFF
    };

    export constexpr CollisionCategory operator|(CollisionCategory a, CollisionCategory b) noexcept {
        return static_cast<CollisionCategory>(static_cast<uint32_t>(a) | static_cast<uint32_t>(b));
    }

    export constexpr CollisionCategory operator&(CollisionCategory a, CollisionCategory b) noexcept {
        return static_cast<CollisionCategory>(static_cast<uint32_t>(a) & static_cast<uint32_t>(b));
    }

    export constexpr CollisionCategory& operator|=(CollisionCategory& a, CollisionCategory b) noexcept {
        a = a | b;
        return a;
    }

    export constexpr CollisionCategory& operator&=(CollisionCategory& a, CollisionCategory b) noexcept {
        a = a & b;
        return a;
    }

    export constexpr CollisionCategory operator~(CollisionCategory a) noexcept {
        return static_cast<CollisionCategory>(~static_cast<uint32_t>(a));
    }

    /// @brief 衝突フィルター（レイヤー・マスク・所有者ID・チームIDによる判定規則）
    export struct CollisionFilter {
        CollisionCategory category = CollisionCategory::None;
        CollisionCategory mask = CollisionCategory::All;
        int owner_id = -1;
        int team_id = -1;

        /// @brief 指定したターゲットとの衝突・攻撃命中が可能か判定します
        /// @param target 判定対象の CollisionFilter
        /// @param friendly_fire フレンドリーファイア（味方への命中）を許可するか
        /// @return 命中・衝突が有効な場合は true
        constexpr bool CanHit(const CollisionFilter& target, bool friendly_fire = false) const noexcept {
            // 1. カテゴリとマスクの論理積チェック
            if ((static_cast<uint32_t>(category) & static_cast<uint32_t>(target.mask)) == 0) {
                return false;
            }

            // 2. 所有者チェック（自分自身へのヒット防止）
            if (owner_id >= 0 && owner_id == target.owner_id) {
                return false;
            }

            // 3. チーム判定（フレンドリーファイア非許可かつ同一チームの場合はヒット防止）
            if (!friendly_fire && team_id >= 0 && target.team_id >= 0 && team_id == target.team_id) {
                return false;
            }

            return true;
        }
    };
} // namespace ab

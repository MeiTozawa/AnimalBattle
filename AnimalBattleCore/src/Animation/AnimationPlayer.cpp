module;

#include <cstdint>
#include <string>
#include <vector>
#include <unordered_map>

module AnimationPlayer;

namespace ab {
    void AnimationPlayer::AddClip(AnimationClip clip) {
        clips_[clip.name] = std::move(clip);
    }

    void AnimationPlayer::AddGridClip(const std::wstring& name, int row, int frame_width, int frame_height,
                                      int frame_count, float frame_duration, bool is_looping) {
        AnimationClip clip;
        clip.name = name;
        clip.frame_duration = frame_duration;
        clip.is_looping = is_looping;
        clip.frames.reserve(frame_count);

        for (int i = 0; i < frame_count; ++i) {
            int x1 = i * frame_width;
            int y1 = row * frame_height;
            int x2 = x1 + frame_width;
            int y2 = y1 + frame_height;
            clip.frames.push_back(AnimationFrame{ Rect<int>{ x1, y1, x2, y2 } });
        }

        clips_[name] = std::move(clip);
    }

    bool AnimationPlayer::Play(const std::wstring& name, bool restart_if_same) {
        auto it = clips_.find(name);
        if (it == clips_.end()) {
            return false;
        }

        if (current_clip_ == &it->second && !restart_if_same) {
            return true;
        }

        current_clip_ = &it->second;
        current_frame_index_ = 0;
        frame_timer_ = 0.0f;
        is_playing_ = true;
        is_finished_ = false;
        return true;
    }

    void AnimationPlayer::Pause() {
        is_playing_ = false;
    }

    void AnimationPlayer::Resume() {
        if (current_clip_ != nullptr && !is_finished_) {
            is_playing_ = true;
        }
    }

    void AnimationPlayer::Stop() {
        is_playing_ = false;
        current_frame_index_ = 0;
        frame_timer_ = 0.0f;
        is_finished_ = false;
    }

    void AnimationPlayer::Update(float delta_time) {
        if (!is_playing_ || current_clip_ == nullptr || current_clip_->frames.empty()) {
            return;
        }

        frame_timer_ += delta_time;
        float duration = current_clip_->frame_duration;
        if (duration <= 0.0001f) {
            duration = 0.1f;
        }

        while (frame_timer_ >= duration) {
            frame_timer_ -= duration;
            current_frame_index_++;

            if (current_frame_index_ >= static_cast<int>(current_clip_->frames.size())) {
                if (current_clip_->is_looping) {
                    current_frame_index_ = 0;
                } else {
                    current_frame_index_ = static_cast<int>(current_clip_->frames.size()) - 1;
                    is_finished_ = true;
                    is_playing_ = false;
                    break;
                }
            }
        }
    }

    void AnimationPlayer::Draw(RenderService<DxLibRenderBackend>* render_service, Point<int> position,
                              int graph_handle, bool flip_x, float scale, double angle) {
        if (render_service == nullptr || current_clip_ == nullptr || graph_handle < 0) {
            return;
        }

        if (current_frame_index_ < 0 || current_frame_index_ >= static_cast<int>(current_clip_->frames.size())) {
            return;
        }

        const auto& frame = current_clip_->frames[current_frame_index_];
        render_service->DrawRectRotaGraph(position, frame.src_rect, scale, angle, graph_handle, true, flip_x);
    }
} // namespace ab

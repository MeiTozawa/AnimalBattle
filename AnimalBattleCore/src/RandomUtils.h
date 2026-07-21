#ifndef ANIMALBATTLE_CORE_SRC_RANDOM_UTILS_H_
#define ANIMALBATTLE_CORE_SRC_RANDOM_UTILS_H_

#include <cassert>
#include <cstdint>
#include <random>
#include <ranges>

namespace ab {

class Random {
public:
    Random() = delete;

    static void Seed(unsigned int seed) {
        GetEngine().seed(seed);
    }

    template <typename... T>
    static int RandomSelection(T... list) {
        std::discrete_distribution<int> dist({static_cast<double>(list)...});
        return dist(GetEngine());
    }

    static int RangeInt(int min_val = INT32_MIN, int max_val = INT32_MAX) {
        std::uniform_int_distribution dist(min_val, max_val);
        return dist(GetEngine());
    }

    template <std::ranges::random_access_range R>
    static auto& Choice(R& container) {
        assert(!std::ranges::empty(container));
        std::uniform_int_distribution<size_t> dist(0, std::ranges::size(container) - 1);
        return container[dist(GetEngine())];
    }

    template <std::ranges::random_access_range R>
    static void Shuffle(R& container) {
        std::shuffle(std::ranges::begin(container), std::ranges::end(container), GetEngine());
    }

private:
    static std::mt19937& GetEngine() {
        static std::mt19937 gen;
        return gen;
    }
};

}  // namespace ab

#endif  // ANIMALBATTLE_CORE_SRC_RANDOM_UTILS_H_

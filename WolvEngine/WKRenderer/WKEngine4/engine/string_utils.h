#include <string>
#include <string_view>
#include <cstdint>

namespace StringUtils {

	// FNV-1a 32bit hashing algorithm.
	constexpr uint32_t fnv1a_32(char const* s, std::size_t count)
	{
		return ((count ? fnv1a_32(s, count - 1) : 2166136261u) ^ s[count]) * 16777619u;
	}

    // FNV-1a 64bit hashing algorithm.
    constexpr uint64_t fnv1a_64(char const* s, std::size_t count)
    {
        auto hash = 0xCBF29CE484222325ul;
        for (auto i = 0; i < count; i++) {
            hash = (hash ^ s[i]) * 0x00000100000001B3ul;
        }
        return hash;
    }

	constexpr size_t const_strlen(const char* s)
	{
		size_t size = 0;
		while (s[size]) { size++; };
		return size;
	}

	struct StringHash
	{
		uint32_t computedHash;

		constexpr StringHash(uint32_t hash) noexcept : computedHash(hash) {}

		constexpr StringHash(const char* s) noexcept : computedHash(0)
		{
			computedHash = fnv1a_32(s, const_strlen(s));
		}
		constexpr StringHash(const char* s, std::size_t count)noexcept : computedHash(0)
		{
			computedHash = fnv1a_32(s, count);
		}
		constexpr StringHash(std::string_view s)noexcept : computedHash(0)
		{
			computedHash = fnv1a_32(s.data(), s.size());
		}
		StringHash(const StringHash& other) = default;

		constexpr operator uint32_t()noexcept { return computedHash; }
	};

}

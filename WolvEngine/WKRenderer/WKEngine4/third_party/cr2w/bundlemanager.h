#pragma once

#include <unordered_map>
#include "bundle.h"
#include "file.h"

namespace WolvenEngine
{
    class BundleManager
    {
    public:
        static BundleManager* instance();
        bool open(const char* filename, File& fp);

        void clear() { _bundles.clear(); }

        //bool openHash(const std::string& hash);
        //bool open(uint64_t hash);

    protected:
        BundleManager();
        ~BundleManager();

        std::unordered_map<uint16_t, Bundle> _bundles;
        static BundleManager* s_instance;
    };

    class CP77BundleManager : BundleManager
    {
    public:
        static CP77BundleManager* instance();
        bool open(const char* filename, File& fp);

        void clear() { _bundles.clear(); }

        //bool openHash(const std::string& hash);
        //bool open(uint64_t hash);

    private:
        CP77BundleManager();
        ~CP77BundleManager();

        std::unordered_map<uint16_t, Bundle> _bundles;
        static CP77BundleManager* s_instance;
    };
}

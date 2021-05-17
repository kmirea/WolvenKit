#pragma once

#include <vector>
#include <string>
#include <filesystem>
#include <cr2w/bundledictionary.h>

int extract_radr_archive(
    std::filesystem::path filepath,
    std::filesystem::path dump_path,
    bool is_dump_path,
    std::vector<std::string>& CP77BundleNames,
    WolvenEngine::BundleDictionary& CP77RawPathHashes,
    WolvenEngine::LookupDictionary& CP77Lookup);
int extract_radr_archive(
    std::filesystem::path filepath,
    std::vector<std::string>& CP77BundleNames,
    WolvenEngine::BundleDictionary& CP77RawPathHashes,
    WolvenEngine::LookupDictionary& CP77Lookup);

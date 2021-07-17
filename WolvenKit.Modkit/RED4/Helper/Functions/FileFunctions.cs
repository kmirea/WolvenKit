using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WolvenKit.Common;
using WolvenKit.Common.Model.Arguments;
using WolvenKit.Common.Services;
using WolvenKit.Modkit.RED4.GeneralStructs;
using WolvenKit.RED4.CR2W.Types;
using WolvenKit.RED4.CR2W;
using CP77.CR2W;
using WolvenKit.Common.FNV1A;
using WolvenKit.Modkit.RED4;
using WolvenKit.RED4.CR2W.Archive;


using WolvenKit.Common.DDS;


using System.Collections.Concurrent;

using System.IO.MemoryMappedFiles;

using System.Threading.Tasks;

using WolvenKit.Common.Oodle;
using WolvenKit.Common.Extensions;
using Newtonsoft.Json;
using WolvenKit.Core.Services;


namespace WolvenKit.Modkit.RED4
{
    using Vec4 = System.Numerics.Vector4;
    using Vec2 = System.Numerics.Vector2;
    using Vec3 = System.Numerics.Vector3;
    public class MemTools
    {
        private readonly ModTools _modTools;
        private readonly Red4ParserService _fileService;
        private readonly ILoggerService _loggerService;
        private readonly IHashService _hashService;
        private Dictionary<ulong, int> _archiveHashes = new();
        private ArchiveManager archiveMgr;
        //private Dictionary<ulong, int> _archiveHashes = new();
        private List<FileInfo> _archiveList = new List<FileInfo>();

        public MemTools(ModTools modTools,Red4ParserService fileService, ILoggerService loggerService, IHashService hashService)
        {
            _modTools = modTools;
            _fileService = fileService;
            _loggerService = loggerService;
            _hashService = hashService;
            archiveMgr = new ArchiveManager(_hashService);
        }
        public List<ulong> FindResource(string pattern)
        {
            var matches = archiveMgr.FileList.Cast<FileEntry>().MatchesWildcard(f => f.FileName, pattern).ToList();
            return matches.Select(_ => _.NameHash64).ToList();
        }
        public bool LoadArchives(string path)
        {
            _archiveList = new List<FileInfo>();
            #region checks
            if (string.IsNullOrEmpty(path))
            {
                _loggerService.Warning("Please fill in an input path.");
                return false;
            }

            var inputFileInfo = new FileInfo(path);
            var inputDirInfo = new DirectoryInfo(path);

            if (!inputFileInfo.Exists && !inputDirInfo.Exists)
            {
                _loggerService.Warning("Input path does not exist.");
                return false;
            }

            if (inputFileInfo.Exists && inputFileInfo.Extension != ".archive")
            {
                _loggerService.Warning("Input file is not an .archive.");
                return false;
            }
            else if (inputDirInfo.Exists && inputDirInfo.GetFiles().All(_ => _.Extension != ".archive"))
            {
                _loggerService.Warning("No .archive file to process in the input directory");
                return false;
            }
            var isDirectory = !inputFileInfo.Exists;
            var basedir = inputFileInfo.Exists ? new FileInfo(path).Directory : inputDirInfo;

            #endregion
            archiveMgr = new ArchiveManager(_hashService);
            archiveMgr.LoadFromFolder(basedir);
            /*if (isDirectory)
            {
                var archiveManager = new ArchiveManager(_hashService);
                archiveManager.LoadFromFolder(basedir);
                _archiveList = archiveManager.Archives.Select(_ => new FileInfo(_.Value.ArchiveAbsolutePath)).ToList();
                //archiveManager.Items.FileList.Where(_ => _.Key == 1234).FirstOrDefault()
            }
            else
            {
                _archiveList = new List<FileInfo> { inputFileInfo };
            }
            for (int i = 0; i < _archiveList.Count; i++)
            {
                var ar = Red4ParserServiceExtensions.ReadArchive(_archiveList[i].FullName, _hashService);
                var hsh = ar.Files.Values.Cast<FileEntry>().Select(_ => _.NameHash64).ToList();
                foreach (var h in hsh)
                {
                    if (!_archiveHashes.ContainsKey(h))
                    {
                        _archiveHashes[h] = i;
                    }
                }
            }*/
            return true;
        }
        /// <summary>
        /// Load CR2W + Buffers from fixed depot path
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="getBuffers"></param>
        /// <returns></returns>
        public CR2W_Wrapper LoadResource(string pattern, bool getBuffers = true)
        {
            var fhash = FNV1A64HashAlgorithm.HashString(pattern);
            if (!archiveMgr.Items.ContainsKey(fhash) || pattern.Contains("*"))
            {
                fhash = FindResource(pattern)[0];
            }
            return LoadResource(fhash, getBuffers);
        }
        /// <summary>
        /// Load CR2W + Buffers from hash
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="getBuffers"></param>
        /// <returns></returns>
        public CR2W_Wrapper LoadResource(ulong fhash, bool getBuffers = true)
        {
            CR2W_Wrapper cw = new CR2W_Wrapper();
            if (!archiveMgr.Items.ContainsKey(fhash))
            {
                return cw;
            }
            var fileEntry = archiveMgr.Items[fhash][0] as FileEntry;
            
            
            var cr2WStream = new MemoryStream();
            ModTools.ExtractSingleToStream(fileEntry.Archive as Archive, fhash, cr2WStream);
            var cr2w = _fileService.TryReadRED4File(cr2WStream);
            if (cr2w == null)
            {
                return null;
            }
            cw = new CR2W_Wrapper()
            {
                archive_path = fileEntry.Archive.Name,
                buffers = new List<byte[]>(),
                depot_path = fileEntry.Name,
                cr2w = cr2w,
                cr2wstream = cr2WStream
            };
            var hasBuffers = (fileEntry.SegmentsEnd - fileEntry.SegmentsStart) > 1;
            if (hasBuffers && getBuffers)
            {
                cw.buffers = GenerateMemBuffers(cr2WStream);
            }
            return cw;
        }
        public Dictionary<int, List<ulong>> FindFiles(string path, string pattern, List<FileInfo> archiveFileInfos)
        {
            Dictionary<ulong, string> _userHashes = new();

            //_wolvenkitFileService.TryReadCr2WFile
            Dictionary<int, List<ulong>> fileList = new Dictionary<int, List<ulong>>();
            int found = 0;
            if (archiveFileInfos == null || archiveFileInfos.Count < 1)
            {
                return fileList;
            }
            foreach (var arch in archiveFileInfos)
            {
                var arIdx = archiveFileInfos.IndexOf(arch);
                // read archive
                var ar = Red4ParserServiceExtensions.ReadArchive(arch.FullName, _hashService);
                // check search pattern
                var finalmatches = ar.Files.Values.Cast<FileEntry>();
                finalmatches = ar.Files.Values.Cast<FileEntry>().MatchesWildcard(item => item.FileName, pattern);
                var finalMatchesList = finalmatches.ToList();
                found += finalMatchesList.Count;
                if (finalMatchesList.Count > 0)
                {
                    fileList[arIdx] = new List<ulong>();
                }
                foreach (var m in finalMatchesList)
                {
                    fileList[arIdx].Add(m.NameHash64);
                }
            }
            return fileList;
        }
        public CR2W_Wrapper LoadSingleFile(string path, string pattern,bool getBuffers = true, ulong hash = 0)
        {
            
            CR2W_Wrapper cw = new CR2W_Wrapper();
            if (String.IsNullOrEmpty(path) || String.IsNullOrEmpty(pattern))
                return null;
            List<FileInfo> archiveFileInfos = Get_archives(path);
            var foundList = FindFiles(path, pattern, archiveFileInfos);
            if (foundList.Count < 1)
                return null;

            var arIdx = foundList.First().Key;
            hash = foundList[arIdx][0];
            // read archive
            var ar = Red4ParserServiceExtensions.ReadArchive(archiveFileInfos[arIdx].FullName, _hashService);
            if (!ar.Files.ContainsKey(hash))
            {
                return null;
            }

            var cr2WStream = new MemoryStream();
            ModTools.ExtractSingleToStream(ar, hash, cr2WStream);
            var cr2w = _fileService.TryReadRED4File(cr2WStream);
            if (cr2w == null)
            {
                return null;
            }
            cw = new CR2W_Wrapper()
            {
                archive_path = archiveFileInfos[arIdx].Name,
                buffers = new List<byte[]>(),
                depot_path = path,
                cr2w = cr2w,
                cr2wstream = cr2WStream
            };

            if (ar.Files[hash] is FileEntry entry)
            {
                cw = new CR2W_Wrapper() {
                    archive_path = archiveFileInfos[arIdx].Name,
                    buffers = new List<byte[]>(),
                    cr2w = cr2w,
                    cr2wstream = cr2WStream,
                    depot_path = entry.Name
                };

                var hasBuffers = (entry.SegmentsEnd - entry.SegmentsStart) > 1;
                if (hasBuffers && getBuffers)
                {
                    cw.buffers = GenerateMemBuffers(cr2WStream);
                }
            }
            return cw;
           

            /*var inputFileInfo = new FileInfo(path);
            var inputDirInfo = new DirectoryInfo(path);
            var basedir = inputFileInfo.Exists ? new FileInfo(path).Directory : inputDirInfo;
            DirectoryInfo outDir = new DirectoryInfo(outpath);
            var extractedList = new ConcurrentBag<string>();
            var failedList = new ConcurrentBag<string>();
            //_loggerService.Info($"Found {finalMatchesList.Count} bundle entries to extract.");   
            // var progress = 0;
            foreach (var processedarchive in archiveFileInfos)
            {

                if (string.IsNullOrEmpty(outpath))
                {
                    outDir = Directory.CreateDirectory(Path.Combine(
                        basedir.FullName,
                        processedarchive.Name.Replace(".archive", "")));
                }
                else
                {
                    outDir = new DirectoryInfo(outpath);
                    if (!outDir.Exists)
                    {
                        outDir = Directory.CreateDirectory(outpath);
                    }
                    if (inputDirInfo.Exists)
                    {
                        outDir = Directory.CreateDirectory(Path.Combine(
                            outDir.FullName,
                            processedarchive.Name.Replace(".archive", "")));
                    }
                }
            }
           
            int found = 0;
            foreach (var arIdx in foundList.Keys)
            {
                found += foundList[arIdx].Count;
            }
            foreach (var arIdx in foundList.Keys)
            {
                if (foundList[arIdx].Count > 0)
                {
                    // read archive
                    var ar = Red4ParserServiceExtensions.ReadArchive(archiveFileInfos[arIdx].FullName, _hashService);
                    using var fs = new FileStream(ar.ArchiveAbsolutePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    using var mmf = MemoryMappedFile.CreateFromFile(fs, null, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
                    foreach (var fhash in foundList[arIdx])
                    {
                       
                        // get filename
                        var entry = ar.Files[fhash] as FileEntry;
                        var name = entry.FileName;
                        var entryName = ar.Files[fhash].Name;
                        if (string.IsNullOrEmpty(outpath))
                        {
                            outDir = Directory.CreateDirectory(Path.Combine(basedir.FullName, ar.Name.Replace(".archive", "")));
                        }
                        else
                        {
                            outDir = new DirectoryInfo(outpath);
                            if (!outDir.Exists)
                            {
                                outDir = Directory.CreateDirectory(outpath);
                            }
                            if (inputDirInfo.Exists)
                            {
                                outDir = Directory.CreateDirectory(Path.Combine(outDir.FullName, ar.Name.Replace(".archive", "")));
                            }
                        }
                        //output anims to json
                        var outfile = new FileInfo(Path.Combine(outDir.FullName, $"{name}" + ".json"));
                       
                        // extract file to memorystream
                        var ms = new MemoryStream();

                        ar.CopyFileToStream(ms, fhash, false, mmf);

                        var cr2w = _modTools.TryReadCr2WFile(ms);
                        if (cr2w == null)
                        {
                            failedList.Add(entryName);
                        }
                        else
                        {
                            extractedList.Add(entryName);
                            var buffers = GenerateMemBuffers(ms);
                            ///HANDLE CR2W & BUFFERS HERE
                        }
                    }
                }
            }*/
        }
        public List<byte[]> GenerateMemBuffers(Stream cr2wStream)
        {
            uint KARK = 1263681867;
            List<byte[]> bufferList = new List<byte[]>();
            cr2wStream.Seek(0, SeekOrigin.Begin);
            // read the cr2wfile
            var cr2w = _fileService.TryReadRED4FileHeaders(cr2wStream);
            if (cr2w == null)
            {
                _loggerService.Error($"Failed to read cr2w buffers");
                return bufferList;
            }
            // decompress buffers
            var buffers = cr2w.Buffers;
            foreach (var b in buffers)
            {
                using var ms = new MemoryStream();
                cr2wStream.Seek(b.Offset, SeekOrigin.Begin);
                var realSize = b.MemSize;
                var oodleCompression = cr2wStream.ReadStruct<uint>();
                if (oodleCompression == KARK)
                {
                    var headerSize = cr2wStream.ReadStruct<uint>();
                    if (b.MemSize == b.DiskSize)
                    {
                        realSize = headerSize;
                    }
                    cr2wStream.Seek(b.Offset, SeekOrigin.Begin);
                }
                cr2wStream.DecompressAndCopySegment(ms, b.DiskSize, realSize);
                var bufferData = ms.ToArray();
                ms.Seek(0, SeekOrigin.Begin);
                oodleCompression = ms.ReadStruct<uint>();
                if (oodleCompression == KARK)
                {
                    using var ms2 = new MemoryStream();
                    realSize = ms.ReadStruct<uint>();
                    var innerSize = (uint)bufferData.Count();
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.DecompressAndCopySegment(ms2, innerSize, realSize);
                    bufferData = ms2.ToArray();
                }
                bufferList.Add(bufferData);
            }
            return bufferList;
        }
        /// <summary>
        /// Get Archives List
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public List<FileInfo> Get_archives(string path)
        {
            List<FileInfo> archiveFileInfos = new List<FileInfo>();

            if (string.IsNullOrEmpty(path))
            {
                _loggerService.Warning("Please fill in an input path.");
                return archiveFileInfos;
            }

            var inputFileInfo = new FileInfo(path);
            var inputDirInfo = new DirectoryInfo(path);

            if (!inputFileInfo.Exists && !inputDirInfo.Exists)
            {
                _loggerService.Warning("Input path does not exist.");
                return archiveFileInfos;
            }

            if (inputFileInfo.Exists && inputFileInfo.Extension != ".archive")
            {
                _loggerService.Warning("Input file is not an .archive.");
                return archiveFileInfos;
            }
            else if (inputDirInfo.Exists && inputDirInfo.GetFiles().All(_ => _.Extension != ".archive"))
            {
                _loggerService.Warning("No .archive file to process in the input directory");
                return archiveFileInfos;
            }

            var isDirectory = !inputFileInfo.Exists;
            var basedir = inputFileInfo.Exists ? new FileInfo(path).Directory : inputDirInfo;

            if (isDirectory)
            {
                var archiveManager = new ArchiveManager(_hashService);
                archiveManager.LoadFromFolder(basedir);
                archiveFileInfos = archiveManager.Archives.Select(_ => new FileInfo(_.Value.ArchiveAbsolutePath)).ToList();
            }
            else
            {
                archiveFileInfos = new List<FileInfo> { inputFileInfo };
            }
            return archiveFileInfos;

        }
        /// <summary>
        /// Get File Hashes for given search pattern
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public Dictionary<int,List<ulong>> FindFiles5(string path, string pattern, List<FileInfo> archiveFileInfos)
        {
            Dictionary<ulong, string> _userHashes = new();

            //_wolvenkitFileService.TryReadCr2WFile
            Dictionary<int, List<ulong>> fileList = new Dictionary<int, List<ulong>>();
            int found = 0;
            if (archiveFileInfos == null ||  archiveFileInfos.Count < 1)
            {
                return fileList;
            }
            foreach (var arch in archiveFileInfos)
            {
                var arIdx = archiveFileInfos.IndexOf(arch);
                // read archive
                var ar = Red4ParserServiceExtensions.ReadArchive(arch.FullName, _hashService);
                // check search pattern
                var finalmatches = ar.Files.Values.Cast<FileEntry>();
                finalmatches = ar.Files.Values.Cast<FileEntry>().MatchesWildcard(item => item.FileName, pattern);
                var finalMatchesList = finalmatches.ToList();
                found += finalMatchesList.Count;
                if(finalMatchesList.Count > 0)
                {
                    fileList[arIdx] = new List<ulong>();
                }
                foreach (var m in finalMatchesList)
                {
                    fileList[arIdx].Add(m.NameHash64);
                }
            }
            return fileList;

        }
        public void readApp(string path, string pattern)
        {
            string baseType = "";
            List<string> appearances = new List<string>();
            List<string> meshFiles = new List<string>();
            List<CR2W_Wrapper> rigStreams = new List<CR2W_Wrapper>();
            List<CR2W_Wrapper> meshStreams = new List<CR2W_Wrapper>();
            List<Stream> mStreams = new List<Stream>();


            Dictionary<ulong, string> meshFileHashes = new Dictionary<ulong, string>();
            Dictionary<ulong, int> meshCookedAppHashes = new Dictionary<ulong, int>();
            Dictionary<string, int> cookedAppMeshLink = new Dictionary<string, int>();
            List<Archive> archives = new List<Archive>();
            string matRepot = @"C:\dev\mats";
            var ss = "body_types_map.csv";
            if (ss == "")
            { }

            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                LoadArchives(path);
            }

            Dictionary<EBaseEntityType, string> entityTypes = new Dictionary<EBaseEntityType, string>()
            {
                { EBaseEntityType.ManAverage, "man_base"},              //base\characters\base_entities\man_base\man_base.ent, ma
                { EBaseEntityType.ManBig, "man_big"},                   //base\characters\base_entities\man_big\man_big.ent, mb
                { EBaseEntityType.ManMassive, "man_massive"},           //base\characters\base_entities\man_massive\man_massive.ent, mm
                { EBaseEntityType.ManFat, "man_fat"},                   //base\characters\base_entities\man_fat\man_fat.ent, mf
                { EBaseEntityType.ManOld, "man_small"},                 //base\characters\base_entities\man_small\man_small.ent,
                { EBaseEntityType.ManSmall, "man_small"},               //base\characters\base_entities\man_small\man_small.ent,
                { EBaseEntityType.ManChild, "man_child"},               //base\characters\base_entities\man_child\man_child.ent, mc
                { EBaseEntityType.WomanAverage, "woman_base"},          //base\characters\base_entities\woman_base\woman_base.ent, wa
                { EBaseEntityType.WomanBig, "woman_big"},               //base\characters\base_entities\woman_big\woman_big.ent, wf
                { EBaseEntityType.WomanFat, "man_fat"},                 //base\characters\base_entities\man_fat\man_fat.ent,
                { EBaseEntityType.WomanOld, "woman_small"},             //base\characters\base_entities\woman_small\woman_small.ent,
                { EBaseEntityType.WomanSmall, "woman_small"},           //base\characters\base_entities\woman_small\woman_small.ent,
                { EBaseEntityType.WomanChild, "man_child"}              //base\characters\base_entities\man_child\man_child.ent, wc
            };
            Dictionary<string, string> vehicleType = new Dictionary<string, string>()
            {
                {"Arch_Nemesis"             , @"base\vehicles\sportbike\v_sportbike2_arch_nemesis_basic_01.ent" },
                {"Archer_Hella"             , @"base\vehicles\standard\v_standard2_archer_hella__basic_01.ent" },
                {"Archer_Quartz"        , @"base\vehicles\standard\v_standard2_archer_quartz__basic_01.ent" },
                {"Archer_Quartz_Nomad"  , @"base\vehicles\standard\v_standard2_archer_quartz_nomad__01.ent" },
                {"Brennan_Apollo"       , @"base\vehicles\sportbike\v_sportbike3_brennan_apollo_basic_01.ent" },
                {"Chevalier_Emperor"    , @"base\vehicles\standard\v_standard3_chevalier_emperor_01__basic_01.ent" },
                {"Chevalier_Thrax"      , @"base\vehicles\standard\v_standard2_chevalier_thrax__basic_01.ent" },
                {"Chevalier_Thrax_Dex"  , @"base\vehicles\standard\v_standard2_chevalier_thrax__dex.ent" },
                {"Herrera_Outlaw"       , @"base\vehicles\sport\v_sport1_herrera_outlaw_basic_01.ent" },
                {"Kaukaz_Bratsk"        , @"base\vehicles\utility\v_utility4_kaukaz_bratsk__basic_01.ent" },
                {"Kaukaz_Bratsk_Extended" , @"base\vehicles\utility\v_utility4_kaukaz_bratsk_extended__basic_01.ent" },
                {"Kaukaz_Z71_Aras"      , @"base\vehicles\special\v_kaukaz_z71_aras__basic_01.ent" },
                {"Kaukaz_Zeya"          , @"base\vehicles\utility\v_utility4_kaukaz_zeya__basic_01.ent" },
                {"Mahir_Supron"             , @"base\vehicles\standard\v_standard25_mahir_supron_01__basic_01.ent" },
                {"Mahir_MT28_Coach"         , @"base\vehicles\special\v_mahir_mt28_coach_basic_01.ent" },
                {"Makigai_MaiMai"       , @"base\vehicles\standard\v_standard2_makigai_maimai_01_basic_01.ent" },
                {"Militech_Basilisk"    , @"base\vehicles\special\v_militech_basilisk_01__basic_01.ent" },
                {"Militech_Behemoth"    , @"base\vehicles\utility\v_utility4_militech_behemoth_basic_01.ent" },
                {"Militech_Griffin"         , @"base\vehicles\special\av_militech_griffin__basic_01.ent" },
                {"Militech_Manticore"   , @"base\vehicles\special\av_militech_manticore_basic_01.ent" },
                {"Militech_Wyvern"      , @"base\vehicles\special\av_militech_wyvern__basic_01.ent" },
                {"Mizutani_Shion"       , @"base\vehicles\sport\v_sport2_mizutani_shion__basic_01.ent" },
                {"Mizutani_Shion_Nomad"     , @"base\vehicles\sport\v_sport2_mizutani_shion_nomad__basic_01.ent" },
                {"Porsche_911turbo"         , @"base\vehicles\sport\v_sport2_porsche_911turbo__basic_01.ent" },
                {"Quadra_Type66"        , @"base\vehicles\sport\v_sport2_quadra_type66__basic_01.ent" },
                {"Quadra_Type66_Nomad"  , @"base\vehicles\sport\v_sport2_quadra_type66_nomad__basic_01.ent" },
                {"Quadra_Turbo"             , @"base\vehicles\sport\v_sport1_quadra_turbo__basic_01.ent" },
                {"Rayfield_Aerondight"  , @"base\vehicles\sport\v_sport1_rayfield_aerondight__basic_01.ent" },
                {"Rayfield_Calibrun"    , @"base\vehicles\sport\v_sport1_rayfield_caliburn__basic_01.ent" },
                {"Rayfield_Excalibur"   , @"base\vehicles\special\av_rayfield_excalibur__basic_01.ent" },
                {"Thorton_Colby"        , @"base\vehicles\standard\v_standard2_thorton_colby__basic_01.ent" },
                {"Thorton_Colby_Pickup"     , @"base\vehicles\standard\v_standard25_thorton_colby_pickup__basic_01.ent" },
                {"Thorton_Colby_Pickup_Nomad" , @"base\vehicles\standard\v_standard25_thorton_colby_pickup_nomad__basic_01.ent" },
                {"Thorton_Galena"       , @"base\vehicles\standard\v_standard2_thorton_galena_01__basic_01.ent" },
                {"Thorton_Galena_Nomad"     , @"base\vehicles\standard\v_standard2_thorton_galena_nomad__01.ent" },
                {"Thorton_Mackinaw_Larimore" , @"base\vehicles\standard\v_standard3_thorton_mackinaw_larimore_01.ent" },
                {"Thorton_Mackinaw"         , @"base\vehicles\standard\v_standard3_thorton_mackinaw_01__basic_01.ent" },
                {"Thorton_Mackinaw_Nomad" , @"base\vehicles\standard\v_standard3_thorton_mackinaw_nomad_01__basic_01.ent" },
                {"Villefort_Alvarado"   , @"base\vehicles\sport\v_sport2_villefort_alvarado__basic_01.ent" },
                {"Villefort_Columbus"   , @"base\vehicles\standard\v_standard25_villefort_columbus_01__basic_01.ent" },
                {"Villefort_Cortes"         , @"base\vehicles\standard\v_standard2_villefort_cortes_01__basic_01.ent" },
                {"Yaiba_Kusanagi"       , @"base\vehicles\sportbike\v_sportbike1_yaiba_kusanagi_basic_01.ent" },
                {"Zetatech_Atlus"       , @"base\vehicles\special\av_zetatech_atlus_basic_01.ent" },
                {"Zetatech_Bombus"      , @"base\vehicles\special\av_zetatech_bombus__basic.ent" },
                {"Zetatech_Canopy"      , @"base\vehicles\special\av_zetatech_canopy__basic_01.ent" },
                {"Zetatech_Octant"      , @"base\vehicles\special\av_zetatech_octant.ent" },
                {"Zetatech_Surveyor"    , @"base\vehicles\special\av_zetatech_surveyor_basic_01.ent" },
                {"Zetatech_Valgus"      , @"base\vehicles\special\av_zetatech_valgus_basic_01.ent" },
                {"Decoration"           , @"base\vehicles\common\templates\vehicle_decoration_base.ent" },
                {"Disposal_Alvarado"    , @"base\vehicles\decoration\entities\v_disposal_alvarado_base.ent" },
                {"Disposal_Galena"      , @"base\vehicles\decoration\entities\v_disposal_galena_base.ent"}
            };

            var cw = LoadResource(pattern,false);
            if (cw.cr2w != null)
            {
                for (int i = 0; i < cw.cr2w.Chunks.Count; i++)
                {
                    if (cw.cr2w.Chunks[i].REDType == "appearanceAppearanceResource")
                    {
                        var appres = cw.cr2w.Chunks[i].Data as appearanceAppearanceResource;
                        baseType = appres.BaseEntityType != null ? appres.BaseEntityType.Value : "";
                        //LoadRigs for EntityType
                        #region GetRigs
                        if (Enum.TryParse(baseType, true, out EBaseEntityType entType))
                        {
                            var bodyType = entityTypes[entType];
                            var baseRig = @"base\characters\base_entities\" + bodyType + @"\"+ bodyType + @".rig";
                            var deformRig = @"base\characters\base_entities\" + bodyType + @"\deformations_rigs\" + bodyType + @"_deformations.rig";
                            var deformRig2 = @"base\characters\base_entities\" + bodyType + @"\deformations_rig\" + bodyType + @"_deformations.rig";
                            var brig = LoadResource( baseRig, false);
                            
                            var drig = LoadResource( deformRig, false);
                            if(drig == null)
                            {
                                drig = LoadResource( deformRig2, false);
                            }
                            if (drig.cr2w != null)
                            {
                                rigStreams.Add(drig);
                            }
                            if (brig.cr2w != null)
                            {
                                rigStreams.Add(brig);
                            }
                        }
                        else
                        {
                            //C:\dev\wkitproj\jj\hjh\files\Mod\base\vehicles\sport\v_sport1_rayfield_caliburn\rig\v_sport1_rayfield_caliburn_01__basic_01.rig
                            if (vehicleType.ContainsKey(baseType))
                            {
                                var entFile = new FileInfo(vehicleType[baseType]);
                                var vname = entFile.Name.Split("__")[0];
                                var vrigFile = Path.Combine(vehicleType[baseType].Split("__")[0], "rig", entFile.Name.Replace(".ent", ".rig"));
                                vrigFile = @"base\vehicles\sport\v_sport1_rayfield_caliburn\rig\v_sport1_rayfield_caliburn_01__basic_01.rig";
                                var vrig = LoadResource(vrigFile, false);
                                
                                if (vrig.cr2w != null)
                                {
                                    rigStreams.Add(vrig);
                                }
                            }
                            switch (baseType)
                            {
                                case "":
                                //v_sport1_rayfield_caliburn_01__basic_01.rig
                                default:
                                    break;
                            }
                        }
                        #endregion
                        if (appres.CommonCookData != null && appres.CommonCookData.DepotPath != null)
                        {
                            var cookedApp = LoadResource( appres.CommonCookData.DepotPath,false);
                            if (cookedApp.cr2w != null)
                            {
                                for (int j = 0; j < cookedApp.cr2w.Chunks.Count; j++)
                                {
                                    if (cookedApp.cr2w.Chunks[j].REDType == "CMesh")
                                    {
                                        var msh = cookedApp.cr2w.Chunks[j].Data as CMesh;
                                        var mhash = msh.GeometryHash != null ? msh.GeometryHash.Value : 0;
                                        meshCookedAppHashes[mhash] = j;
                                    }
                                }
                                for (int j = 0; j < cookedApp.cr2w.Chunks.Count; j++)
                                {
                                    if (cookedApp.cr2w.Chunks[j].REDType == "appearanceCookedAppearanceData")
                                    {
                                        var ck = cookedApp.cr2w.Chunks[j].Data as appearanceCookedAppearanceData;
                                        //ck.Dependencies[0].
                                        if (ck.Dependencies != null)
                                        {
                                            meshFiles = ck.Dependencies.Elements.Where(_ => _.DepotPath.EndsWith("mesh")).Select(_ => _.DepotPath).ToList();
                                            foreach (var mshpath in meshFiles)
                                            {
                                                var mfn = new FileInfo(mshpath).Name;
                                                if(mfn.StartsWith("h0_"))
                                                {
                                                    var headRigPath = mshpath.Substring(0, mshpath.Length-5) + "_skeleton.rig";
                                                    var facialSetupPath = mshpath.Substring(0, mshpath.Length - 5) + "_rigsetup.facialsetup";
                                                    var headRig = LoadResource( headRigPath, false);
                                                    if (headRig == null)
                                                        continue;
                                                    if (headRig.cr2w != null)
                                                    {
                                                        rigStreams.Add(headRig);
                                                    }
                                                    //var headSetup = LoadSingleFile(path, facialSetupPath, true);
                                                    //if (headSetup == null)
                                                    //    continue;
                                                    //if (headSetup.cr2w != null)
                                                    //{
                                                    ///    ss = "l;";
                                                        //rigStreams.Add(headSetup);
                                                    //}
                                                    ss = "l;";
                                                }
                                            }
                                            foreach (var mshpath in meshFiles)
                                            {

                                                var cmsh = LoadResource( mshpath, false);
                                               // var s = new MemoryStream(cmsh.cr2wstream.)
                                                if(cmsh.cr2w != null)
                                                {
                                                    if ((cmsh.cr2w.Chunks.FirstOrDefault()?.Data is CMesh msh))
                                                    {
                                                        var mhash = msh.GeometryHash != null ? msh.GeometryHash.Value : 0;
                                                        meshFileHashes[mhash] = mshpath;
                                                        cookedAppMeshLink[mshpath] = meshCookedAppHashes[mhash];
                                                        meshStreams.Add(cmsh);
                                                    }
                                                }
                                            }
                                            
                                        }
                                    }
                                }
                                ss = "";
                            }
                        }

                        //var cookedApp = appres.CommonCookData != null ? appres.CommonCookData.


                    }
                }
            }
            if(meshStreams.Count > 0)
            {
                var mstrm = meshStreams.Select(_ => _.cr2wstream).ToList();
                var rgstrm = rigStreams.Select(_ => _.cr2wstream).ToList();
                string outP = @"C:\dev\cyberpunk\out\test\caliburn";
                
                try
                {
                    archives = archiveMgr.Archives.Select(_ => _.Value as Archive).ToList();

                    _modTools.ExportMultiMeshWithRigMats(mstrm, rgstrm, new FileInfo(outP), archives, matRepot, meshFiles, EUncookExtension.tga, true, true);
                }
                catch (Exception e)
                {
                    _loggerService.Error(e.Message);
                    throw;
                }
                
                //{
            }

        }
    }

}

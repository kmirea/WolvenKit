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
using WolvenKit.Modkit.RED4;
using WolvenKit.RED4.CR2W.Archive;


using WolvenKit.Common.DDS;


using System.Collections.Concurrent;

using System.IO.MemoryMappedFiles;

using System.Threading.Tasks;

using WolvenKit.Common.Oodle;
using WolvenKit.Common.Extensions;
using Newtonsoft.Json;



namespace WolvenKit.Modkit.RED4
{
    using Vec4 = System.Numerics.Vector4;
    using Vec2 = System.Numerics.Vector2;
    using Vec3 = System.Numerics.Vector3;
    public partial class ModTools
    {
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
            ExtractSingleToStream(ar, hash, cr2WStream);

            var cr2w = _wolvenkitFileService.TryReadRED4File(cr2WStream);
            if (cr2w == null)
            {
                return null;
            }
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
            var cr2w = _wolvenkitFileService.TryReadRED4FileHeaders(cr2wStream);
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
        public Dictionary<int,List<ulong>> FindFiles(string path, string pattern, List<FileInfo> archiveFileInfos)
        {
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
                var bm = new ArchiveManager(_hashService);
                bm.LoadFromFolder(new DirectoryInfo(path));
                archives = bm.Archives.Values.Cast<Archive>().ToList();
            }

            Dictionary<EBaseEntityType, string> entityTypes = new Dictionary<EBaseEntityType, string>()
            {
                { EBaseEntityType.ManAverage, "man_base"},//base\characters\base_entities\man_base\man_base.ent, ma
                { EBaseEntityType.ManBig, "man_big"},//base\characters\base_entities\man_big\man_big.ent, mb
                { EBaseEntityType.ManMassive, "man_massive"},//base\characters\base_entities\man_massive\man_massive.ent, mm
                { EBaseEntityType.ManFat, "man_fat"},//base\characters\base_entities\man_fat\man_fat.ent, mf
                { EBaseEntityType.ManOld, "man_small"},//base\characters\base_entities\man_small\man_small.ent,
                { EBaseEntityType.ManSmall, "man_small"},//base\characters\base_entities\man_small\man_small.ent,
                { EBaseEntityType.ManChild, "man_child"},//base\characters\base_entities\man_child\man_child.ent, mc
                { EBaseEntityType.WomanAverage, "woman_base"},//base\characters\base_entities\woman_base\woman_base.ent, wa
                { EBaseEntityType.WomanBig, "woman_big"},//base\characters\base_entities\woman_big\woman_big.ent, wf
                { EBaseEntityType.WomanFat, "man_fat"},//base\characters\base_entities\man_fat\man_fat.ent,
                { EBaseEntityType.WomanOld, "woman_small"},//base\characters\base_entities\woman_small\woman_small.ent,
                { EBaseEntityType.WomanSmall, "woman_small"},//base\characters\base_entities\woman_small\woman_small.ent,
                { EBaseEntityType.WomanChild, "man_child"}//base\characters\base_entities\man_child\man_child.ent, wc
            };

            var cw = LoadSingleFile(path, pattern,false);
            if (cw.cr2w != null)
            {
                for (int i = 0; i < cw.cr2w.Chunks.Count; i++)
                {
                    if (cw.cr2w.Chunks[i].REDType == "animAnimSet")
                    {
                        var ani = cw.cr2w.Chunks[i].Data as animAnimSet;

                    }
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
                            var brig = LoadSingleFile(path, baseRig, false);
                            
                            var drig = LoadSingleFile(path, deformRig, false);
                            if (drig.cr2w != null)
                            {
                                rigStreams.Add(drig);
                            }
                            if (brig.cr2w != null)
                            {
                                rigStreams.Add(brig);
                            }
                        }
                        #endregion
                        if (appres.CommonCookData != null && appres.CommonCookData.DepotPath != null)
                        {
                            var cookedApp = LoadSingleFile(path, appres.CommonCookData.DepotPath,false);
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
                                                    var headRig = LoadSingleFile(path, headRigPath, false);
                                                    if (headRig.cr2w != null)
                                                    {
                                                        rigStreams.Add(headRig);
                                                    }
                                                    var headSetup = LoadSingleFile(path, facialSetupPath, true);
                                                    if (headSetup.cr2w != null)
                                                    {
                                                        ss = "l;";
                                                        //rigStreams.Add(headSetup);
                                                    }
                                                    ss = "l;";
                                                }
                                            }
                                            foreach (var mshpath in meshFiles)
                                            {

                                                var cmsh = LoadSingleFile(path, mshpath, false);
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
                string outP = @"C:\dev\cyberpunk\out\test\multiTest.g";
                //ExportMultiMeshWithRigMats()
                try
                {
                    ExportMultiMeshWithRigMats(mstrm, rgstrm, new FileInfo(outP), archives, matRepot, EUncookExtension.tga, true, true);
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

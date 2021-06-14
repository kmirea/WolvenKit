using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WolvenKit.RED4.CR2W.Types;
using WolvenKit.Common;
using WolvenKit.Common.DDS;
using WolvenKit.Common.Extensions;
using WolvenKit.Common.Model;
using WolvenKit.Common.Model.Arguments;
using WolvenKit.RED4.CR2W;

namespace WolvenKit.Modkit.RED4
{
    /// <summary>
    /// Collection of common modding utilities.
    /// </summary>
    public partial class ModTools
    {
        /// <summary>
        /// Imports a raw File to a RedEngine file (e.g. .dds to .xbm, .fbx to .mesh)
        /// </summary>
        /// <param name="rawFile">the raw file to be imported</param>
        /// <param name="args"></param>
        /// <param name="inDir"></param>
        /// <param name="outDir">can be a depotpath, or if null the parent directory of the rawfile</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool Import(RedRelativePath rawRelative, GlobalImportArgs args, DirectoryInfo outDir = null)
        {
            #region checks

            if (rawRelative is null or {Exists: false})
            {
                return false;
            }
            if (outDir is not { Exists: true })
            {
                outDir = rawRelative.BaseDirectory;
            }

            #endregion

            //var rawRelative = new RedRelativePath(inDir, rawFile.GetRelativePath(inDir));

            // check if the file can be directly imported
            // if not, rebuild buffers
            if (!Enum.TryParse(rawRelative.Extension, true, out ERawFileFormat extAsEnum))
            {
                return RebuildBuffer(rawRelative, outDir);
            }

            // import files
            switch (extAsEnum)
            {
                case ERawFileFormat.tga:
                case ERawFileFormat.dds:
                    // for now only xbm is supported
                    return ImportXbm(rawRelative, outDir, args.Get<XbmImportArgs>());
                case ERawFileFormat.fbx:
                case ERawFileFormat.gltf:
                case ERawFileFormat.glb:
                    return ImportMesh(rawRelative, outDir, args.Get<MeshImportArgs>());
                case ERawFileFormat.ttf:
                    return ImportTtf(rawRelative, outDir, args.Get<CommonImportArgs>());
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///  Imports or rebuilds a folder
        /// </summary>
        /// <param name="inDir"></param>
        /// <param name="args"></param>
        /// <param name="outDir">must match the relative paths in indir!</param>
        /// <returns></returns>
        public bool ImportFolder(DirectoryInfo inDir, GlobalImportArgs args, DirectoryInfo outDir = null)
        {
            #region checks

            if (inDir is not { Exists: true })
            {
                return false;
            }
            if (outDir is not { Exists: true })
            {
                outDir = inDir;
            }

            #endregion

            // process buffers
            // buffers need an existing redengine file to rebuild/import
            if (args.Get<CommonImportArgs>().Keep)
            {
                RebuildFolder(inDir, outDir);
            }

            // process all other raw files
            var allFiles = inDir.GetFiles("*", SearchOption.AllDirectories).ToList();
            var rawFilesList = allFiles.Where(_ => _.Extension.ToLower() != ".buffer").ToList();
            var failsCount = 0;
            foreach (var fi in rawFilesList)
            {
                var ext = fi.TrimmedExtension();
                if (!Enum.TryParse(ext, true, out ERawFileFormat extAsEnum))
                {
                    continue;
                }

                var redrelative = new RedRelativePath(inDir, fi.GetRelativePath(inDir));
                if (!Import(redrelative, args, outDir))
                {
                    failsCount++;
                }
            }

            _loggerService.Info($"Imported {rawFilesList.Count - failsCount}/{rawFilesList.Count} file(s)");

            return true;
        }

        private bool ImportTtf(RedRelativePath rawRelative, DirectoryInfo outDir, CommonImportArgs args)
        {
            if (args.Keep)
            {
                var buffer = new FileInfo(rawRelative.FullName);
                var redfile = FindRedFile(rawRelative,  outDir);

                if (string.IsNullOrEmpty(redfile))
                {
                    _loggerService.Warning($"No existing redfile found to rebuild for {rawRelative.Name}");
                    return false;
                }

                using var fileStream = new FileStream(redfile, FileMode.Open, FileAccess.ReadWrite);
                var result = Rebuild(fileStream, new List<FileInfo>() { buffer });

                if (result)
                {
                    _loggerService.Success($"Rebuilt {redfile} with buffers");
                }
                else
                {
                    _loggerService.Error($"Failed to rebuild {redfile} with buffers");
                }

                return result;
            }
            else
            {
                // create redengine file
                var red = new CR2WFile();
                var font = new rendFont(red, null, nameof(rendFont));
                font.CookingPlatform = new CEnum<Enums.ECookingPlatform>(red, font, "cookingPlatform")
                {
                    Value = Enums.ECookingPlatform.PLATFORM_PC,
                    IsSerialized = true
                };
                font.FontBuffer = new DataBuffer(red, font, "fontBuffer"){IsSerialized = true};
                font.FontBuffer.Buffer.SetValue((ushort)1);

                // add chunk
                red.CreateChunk(font, 0);

                // add fake buffer
                red.Buffers.Add(new CR2WBufferWrapper(new CR2WBuffer()));

                // write main file
                using var ms = new MemoryStream();
                using var bw = new BinaryWriter(ms);
                red.Write(bw);

                // write buffer
                var offset = (uint)bw.BaseStream.Position;
                var inbuffer = File.ReadAllBytes(rawRelative.FullName);
                var (zsize, crc) = bw.CompressAndWrite(inbuffer);

                // add buffer info
                red.Buffers[0] = new CR2WBufferWrapper(new CR2WBuffer()
                {
                    flags = 0xE0000, //TODO: find out what to set here, but for fnt files these are always 0xE0000
                    index = (uint)0,
                    offset = offset,
                    diskSize = zsize,
                    memSize = (uint)inbuffer.Length,
                    crc32 = crc
                });

                // write header again to update the buffer info
                bw.Seek(0, SeekOrigin.Begin);
                red.WriteHeader(bw);

                // write to file
                var redpath = Path.ChangeExtension(rawRelative.FullName, ECookedFileFormat.fnt.ToString());
                using var fs = new FileStream(redpath, FileMode.Create, FileAccess.Write);
                ms.Seek(0, SeekOrigin.Begin);
                ms.CopyTo(fs);

                return true;
            }



        }

        private static ECookedFileFormat FromRawExtension(ERawFileFormat rawextension) =>
            rawextension switch
            {
                ERawFileFormat.tga => ECookedFileFormat.xbm,
                ERawFileFormat.dds => ECookedFileFormat.xbm,
                ERawFileFormat.fbx => ECookedFileFormat.mesh,
                ERawFileFormat.gltf => ECookedFileFormat.mesh,
                ERawFileFormat.glb => ECookedFileFormat.mesh,
                ERawFileFormat.ttf => ECookedFileFormat.fnt,
                _ => throw new ArgumentOutOfRangeException(nameof(rawextension), rawextension, null)
            };


        public bool ImportXbm(RedRelativePath rawRelative, DirectoryInfo outDir, XbmImportArgs args)
        {
            var rawExt = rawRelative.Extension;
            // TODO: do this in a working directory?
            var ddsPath = Path.ChangeExtension(rawRelative.FullName, ERawFileFormat.dds.ToString());
            // convert to dds if not already
            if (rawExt != ERawFileFormat.dds.ToString())
            {
                try
                {
                    if (ddsPath.Length > 255)
                    {
                        _loggerService.Error($"{ddsPath} - Path length exceeds 255 chars. Please move the archive to a directory with a shorter path.");
                        return false;
                    }
                    TexconvWrapper.Convert(rawRelative.ToFileInfo().Directory.FullName, $"{ddsPath}", EUncookExtension.dds);
                }
                catch (Exception)
                {
                    //TODO: proper exception handling
                    return false;
                }

                if (!File.Exists(ddsPath))
                {
                    return false;
                }
            }

            if (args.Keep)
            {
                var buffer = new FileInfo(ddsPath);
                var redfile = FindRedFile(rawRelative, outDir);

                if (string.IsNullOrEmpty(redfile))
                {
                    _loggerService.Warning($"No existing redfile found to rebuild for {rawRelative.Name}");
                    return false;
                }

                using var fileStream = new FileStream(redfile, FileMode.Open, FileAccess.ReadWrite);
                var result = Rebuild(fileStream, new List<FileInfo>() {buffer});

                if (result)
                {
                    _loggerService.Success($"Rebuilt {redfile} with buffers");
                }
                else
                {
                    _loggerService.Error($"Failed to rebuild {redfile} with buffers");
                }

                return result;
            }
            else
            {
                _loggerService.Warning($"{rawRelative.Name} - Direct xbm importing is not implemented");
                return false;
            }



            // read dds metadata
#pragma warning disable 162
            var metadata = DDSUtils.ReadHeader(ddsPath);
            var width = metadata.Width;
            var height = metadata.Height;

            // create cr2wfile
            var cr2w = new CR2WFile();
            // create xbm chunk
            var xbm = new CBitmapTexture(cr2w, null, "CBitmapTexture");
            xbm.Width = new CUInt32(cr2w, xbm, "width") { Value = width, IsSerialized = true };
            xbm.Height = new CUInt32(cr2w, xbm, "height") { Value = height, IsSerialized = true };
            xbm.CookingPlatform = new CEnum<Enums.ECookingPlatform>(cr2w, xbm, "cookingPlatform"){ Value = Enums.ECookingPlatform.PLATFORM_PC, IsSerialized = true };
            xbm.Setup = new STextureGroupSetup(cr2w, xbm, "setup")
            {
                IsSerialized = true
            };
            SetTextureGroupSetup();



            // populate with dds metadata



            // kraken ddsfile
            // remove dds header

            // compress file

            // append to cr2wfile

            // update cr2w headers

            throw new NotImplementedException();
#pragma warning restore 162


            #region local functions

            void SetTextureGroupSetup()
            {
                // first check the user-texture group
                var (compression, rawformat, flags) = CommonFunctions.GetRedFormatsFromTextureGroup(args.TextureGroup);
                xbm.Setup.Group = new CEnum<Enums.GpuWrapApieTextureGroup>(cr2w, xbm, "group")
                {
                    IsSerialized = true,
                    Value = args.TextureGroup
                };
                if (flags is CommonFunctions.ETexGroupFlags.Both or CommonFunctions.ETexGroupFlags.CompressionOnly)
                {
                    xbm.Setup.Compression = new CEnum<Enums.ETextureCompression>(cr2w, xbm, "setup")
                    {
                        IsSerialized = true,
                        Value = compression
                    };
                }

                if (flags is CommonFunctions.ETexGroupFlags.Both or CommonFunctions.ETexGroupFlags.RawFormatOnly)
                {
                    xbm.Setup.RawFormat = new CEnum<Enums.ETextureRawFormat>(cr2w, xbm, "rawFormat")
                    {
                        IsSerialized = true,
                        Value = rawformat
                    };
                }

                // if that didn't work, interpret the filename suffix
                if (rawRelative.Name.Contains('_'))
                {
                    // try interpret suffix
                    switch (rawRelative.Name.Split('_').Last())
                    {
                        case "d":
                        case "d01":

                            break;
                        case "e":

                            break;
                        case "r":
                        case "r01":

                            break;
                        default:
                            break;
                    }
                }

                // if that also didn't work, just use default or skip
                //TODO
            }

            #endregion

        }

        private static string FindRedFile(RedRelativePath rawRelPath,  DirectoryInfo outDir)
        {
            var ext = rawRelPath.Extension;
            if (!Enum.TryParse(ext, true, out ERawFileFormat extAsEnum))
            {
                return "";
            }
            var redfile = new RedRelativePath(rawRelPath)
                .ChangeBaseDir(outDir)
                .ChangeExtension(FromRawExtension(extAsEnum).ToString());

            return !File.Exists(redfile.FullPath) ? "" : redfile.FullPath;
        }

        private bool ImportMesh(RedRelativePath rawRelative, DirectoryInfo outDir, MeshImportArgs args)
        {
            if (args.Keep)
            {
                var redfile = FindRedFile(rawRelative,  outDir);

                if (string.IsNullOrEmpty(redfile))
                {
                    _loggerService.Warning($"No existing redfile found to rebuild for {rawRelative.Name}");
                    return false;
                }

                var redfileName = Path.GetFileName(redfile);
                using var redFs = new FileStream(redfile, FileMode.Open, FileAccess.ReadWrite);
                try
                {
                    var result = _meshimporter.Import(rawRelative.ToFileInfo(), redFs);

                    if (result)
                    {
                        _loggerService.Success($"Rebuilt {redfileName} with buffers");
                    }
                    else
                    {
                        _loggerService.Error($"Failed to rebuild {redfileName} with buffers");
                    }
                    return result;
                }
                catch (Exception e)
                {
                    _loggerService.Error($"Unexpected error occured while importing {redfileName}: {e.Message}");
                    return false;
                }


            }


            _loggerService.Warning($"{rawRelative.Name} - Direct mesh importing is not implemented");
            return false;
        }
    }
}

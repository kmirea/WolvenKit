using System.Diagnostics;
using System.IO;
using System.Reflection;
using WolvenKit.Common.Services;

namespace WolvenKit.Common.DDS
{
    /// <summary>
    /// Texconv format: DXGI format without the DXGI_FORMAT_ prefix
    /// </summary>
    public enum EFormat
    {
        R32G32B32A32_FLOAT,

        //R32G32B32A32_UINT,
        //R32G32B32A32_SINT,
        //R32G32B32_FLOAT,
        //R32G32B32_UINT,
        //R32G32B32_SINT,
        R16G16B16A16_FLOAT,

        //R16G16B16A16_UNORM,
        //R16G16B16A16_UINT,
        //R16G16B16A16_SNORM,
        //R16G16B16A16_SINT,
        //R32G32_FLOAT,
        //R32G32_UINT,
        //R32G32_SINT,
        R10G10B10A2_UNORM,

        //R10G10B10A2_UINT,
        //R11G11B10_FLOAT,
        R8G8B8A8_UNORM,

        //R8G8B8A8_UNORM_SRGB,
        //R8G8B8A8_UINT,
        //R8G8B8A8_SNORM,
        //R8G8B8A8_SINT,
        //R16G16_FLOAT,
        //R16G16_UNORM,
        //R16G16_UINT,
        //R16G16_SNORM,
        //R16G16_SINT,
        //R32_FLOAT,
        R32_UINT,

        //R32_SINT,
        R8G8_UNORM,

        //R8G8_UINT,
        //R8G8_SNORM,
        //R8G8_SINT,
        R16_FLOAT,

        //R16_UNORM,
        //R16_UINT,
        //R16_SNORM,
        //R16_SINT,
        R8_UNORM,

        R8_UINT,

        //R8_SNORM,
        //R8_SINT,
        A8_UNORM,

        //R9G9B9E5_SHAREDEXP,
        //R8G8_B8G8_UNORM,
        //G8R8_G8B8_UNORM,
        BC1_UNORM,

        //BC1_UNORM_SRGB,
        BC2_UNORM,

        //BC2_UNORM_SRGB,
        BC3_UNORM,

        //BC3_UNORM_SRGB,
        BC4_UNORM,

        //BC4_SNORM,
        BC5_UNORM,

        //BC5_SNORM,
        //B5G6R5_UNORM,
        //B5G5R5A1_UNORM,
        //B8G8R8A8_UNORM,
        //B8G8R8X8_UNORM,
        //R10G10B10_XR_BIAS_A2_UNORM,
        //B8G8R8A8_UNORM_SRGB,
        //B8G8R8X8_UNORM_SRGB,
        //BC6H_UF16,
        //BC6H_SF16,
        BC7_UNORM,

        //BC7_UNORM_SRGB,
        //AYUV,
        //Y410,
        //Y416,
        //YUY2,
        //Y210,
        //Y216,
        //B4G4R4A4_UNORM,
        //DXT1,
        //DXT2,
        //DXT3,
        //DXT4,
        //DXT5,
        //RGBA,
        //BGRA,
        //FP16,
        //FP32,
        //BPTC,
        //BPTC_FLOAT
    }

    public enum EUncookExtension
    {
        dds,
        tga,
        bmp,
        jpg,
        jpeg,
        png,
    }

    public static class TexconvWrapper
    {
        #region Fields

        private static string textconvpath = Path.Combine(System.AppContext.BaseDirectory, "texconv.exe");

        #endregion Fields

        #region Methods

        public static string Convert(string outDir,
            string filepath,
            EUncookExtension filetype,
            EFormat format = EFormat.R8G8B8A8_UNORM,
            int mipmaps = 0
            )
        {

            var proc = new ProcessStartInfo(textconvpath)
            {
                WorkingDirectory = Path.GetDirectoryName(textconvpath),
                Arguments = $" -o \"{outDir}\" -y -f {format} -m {mipmaps} -l -ft {filetype} \"{filepath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            using (var p = Process.Start(proc))
            {
                p.WaitForExit();
            }

            var fi = new FileInfo(Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(filepath)}.{filetype}"));
            if (!fi.Exists)
            {
                return null;
            }

            return fi.FullName;
        }

        public static void VFlip(string outDir, string filepath, EFormat format = EFormat.R8G8B8A8_UNORM)
        {
            var proc = new ProcessStartInfo(textconvpath)
            {
                WorkingDirectory = Path.GetDirectoryName(textconvpath),
                Arguments = $" -o \"{outDir}\" -y -f {format} -vflip \"{filepath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            using (var p = Process.Start(proc))
            {
                p.WaitForExit();
            }
        }

        #endregion Methods
    }
}

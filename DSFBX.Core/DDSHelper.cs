using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeximpNet.DDS;

namespace DSFBX
{
    public class DDSHelper
    {
        public static int GetTpfFormatFromDdsBytes(DSFBXImporter importer, string texName, byte[] ddsBytes)
        {
            using (var ddsStream = new MemoryStream(ddsBytes))
            {
                DXGIFormat format = DDSFile.Read(ddsStream).Format;

                switch (format)
                {
                    //DSR:
                    case DXGIFormat.BC1_UNorm:
                    case DXGIFormat.BC1_UNorm_SRGB:
                        return 0; 
                    case DXGIFormat.BC2_UNorm:
                    case DXGIFormat.BC2_UNorm_SRGB:
                        return 3;
                    case DXGIFormat.BC3_UNorm:
                    case DXGIFormat.BC3_UNorm_SRGB:
                        return 5;
                    case DXGIFormat.R16G16_Float:
                        return 35;
                    case DXGIFormat.BC5_UNorm:
                        return 36;
                    case DXGIFormat.BC6H_UF16:
                        return 37;
                    case DXGIFormat.BC7_UNorm:
                    case DXGIFormat.BC7_UNorm_SRGB:
                        return 38;
                    //DS3:
                    //case DXGIFormat.B5G5R5A1_UNorm:
                    //    return 6;
                    //case DXGIFormat.B8G8R8A8_UNorm:
                    //case DXGIFormat.B8G8R8A8_UNorm_SRGB:
                    //    return 9;
                    //case DXGIFormat.B8G8R8X8_UNorm:
                    //case DXGIFormat.B8G8R8X8_UNorm_SRGB:
                    //    return 10;
                    //case DXGIFormat.R16G16B16A16_Float:
                    //    return 22;
                    default:
                        importer.PrintWarning($"Texture \"{texName}\" has an unrecognized" +
                            $" DDS format type ({format.ToString()}) and will likely appear garbled ingame. " +
                            $"For greatest compatibility, use DXT1 (aka BC1_UNorm) or DXT5 (aka BC3_UNorm) " +
                            $"for your textures.");
                        return 0;
                }
            }
                
        }
    }
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// Warper for WebP format in C#. (GPL) Jose M. Piñeiro
///////////////////////////////////////////////////////////////////////////////////////////////////////////// 
/// Main functions:
/// Save - Save a bitmap in WebP file.
/// Load - Load a WebP file in bitmap.
/// Decode - Decode WebP data (in byte array) to bitmap.
/// Encode - Encode bitmap to WebP (return a byte array). 
/// 
/// Another functions:
/// EncodeLossly - Encode bitmap to WebP with quality lost (return a byte array).
/// EncodeLossless - Encode bitmap to WebP without quality lost (return a byte array).
/////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Forms;

namespace WebP
{
    public sealed class clsWebP : IDisposable
    {
        private const int WEBP_DECODER_ABI_VERSION = 0x0208;
        public const string LibwebpDLLName = "libwebp32.dll";

        #region | Public Methods |
        /// <summary>Save bitmap to file in WebP format</summary>
        /// <param name="bmp">Bitmap with the WebP image</param>
        /// <param name="quality">Quality. 0 = minumin ... 100 = maximimun quality</param>
        /// <param name="pathFileName">The file to write</param>
        public void Save(Bitmap bmp, int quality, string pathFileName)
        {
            byte[] rawWebP;

            try
            {
                //Encode in webP format
                rawWebP = EncodeLossy(bmp, quality);

                //Write webP file
                File.WriteAllBytes(pathFileName, rawWebP);
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn clsWebP.Save"); }
        }

        /// <summary>Read a WebP file</summary>
        /// <param name="pathFileName">WebP file to load</param>
        /// <returns>Bitmap with the WebP image</returns>
        public Bitmap Load(string pathFileName)
        {
            byte[] rawWebP;
            Bitmap bmp = null;

            try
            {
                //Read webP file
                rawWebP = File.ReadAllBytes(pathFileName);

                bmp = Decode(rawWebP);

                return bmp;
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn clsWebP.Load"); }
        }

        /// <summary>Decode a WebP image</summary>
        /// <param name="webpData">the data to uncompress</param>
        /// <returns>Bitmap whit the image</returns>
        public Bitmap Decode(byte[] webpData)
        {
            int imgWidth;
            int imgHeight;
            int outputSize;
            Bitmap bmp = null;

            try
            {
                //Get image width and height
                GCHandle pinnedWebP = GCHandle.Alloc(webpData, GCHandleType.Pinned);
                IntPtr ptrData = pinnedWebP.AddrOfPinnedObject();
                UInt32 dataSize = (uint)webpData.Length;
                if (UnsafeNativeMethods.WebPGetInfo(ptrData, dataSize, out imgWidth, out imgHeight) == 0)
                    throw new Exception("Can´t get information of WebP\r\nIn clsWebP.Decode");

                //Create a BitmapData and Lock all pixels to be written
                bmp = new Bitmap(imgWidth, imgHeight, PixelFormat.Format24bppRgb);
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, imgWidth, imgHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);

                //Uncompress the image
                outputSize = bmpData.Stride * imgHeight;
                if (UnsafeNativeMethods.WebPDecodeBGRInto(ptrData, dataSize, bmpData.Scan0, outputSize, bmpData.Stride) == null)
                    throw new Exception("Can´t decode WebP\r\nIn clsWebP.Decode");

                //Unlock the pixels
                bmp.UnlockBits(bmpData);

                //Free memory
                pinnedWebP.Free();

                return bmp;
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn clsWebP.Decode"); }
        }

        /// <summary>Lossy encoding bitmap to WebP</summary>
        /// <param name="bmp">Bitmap with the image</param>
        /// <param name="quality">Quality. 0 = minumin ... 100 = maximimun quality</param>
        /// <returns>Compressed data</returns>
        public byte[] EncodeLossy(Bitmap bmp, int quality)
        {
            IntPtr unmanagedData;
            byte[] rawWebP = null;

            try
            {
                //Get bmp data
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                //Compress the bmp data
                int size = UnsafeNativeMethods.WebPEncodeBGR(bmpData.Scan0, bmp.Width, bmp.Height, bmpData.Stride, quality, out unmanagedData);

                //Copy image compress data to output array
                rawWebP = new byte[size];
                Marshal.Copy(unmanagedData, rawWebP, 0, size);

                //Unlock the pixels
                bmp.UnlockBits(bmpData);

                //Free memory
                UnsafeNativeMethods.WebPFree(unmanagedData);

                return rawWebP;
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn clsWebP.EncodeLossly"); }
        }

        /// <summary>Lossless encoding bitmap to WebP</summary>
        /// <param name="bmp">Bitmap with the image</param>
        /// <returns>Compressed data</returns>
        public byte[] EncodeSimpleLossless(Bitmap bmp)
        {
            IntPtr unmanagedData;
            byte[] rawWebP = null;

            try
            {
                //Get bmp data
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                //Compress the bmp data
                int size = UnsafeNativeMethods.WebPEncodeLosslessBGR(bmpData.Scan0, bmp.Width, bmp.Height, bmpData.Stride, out unmanagedData);

                //Copy image compress data to output array
                rawWebP = new byte[size];
                Marshal.Copy(unmanagedData, rawWebP, 0, size);

                //Unlock the pixels
                bmp.UnlockBits(bmpData);

                //Free memory
                UnsafeNativeMethods.WebPFree(unmanagedData);

                return rawWebP;
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn clsWebP.EncodeLossless"); }
        }

        /// <summary>Lossless encoding image in bitmap</summary>
        /// <param name="bmp">Bitmap with the image</param>
        /// <param name="speed">Speed of compresion. 0 = maximimun speed and size ... 9 = minimun speed & size</param>
        /// <returns>Compressed data</returns>
        public byte[] EncodeLossless(Bitmap bmp, int speed = 9)
        {
            byte[] rawWebP = null;

            try
            {
                //Inicialize config struct
                WebPConfig config = new WebPConfig();

                //Set compresion parameters
                if (UnsafeNativeMethods.WebPConfigInitInternal(ref config, WebPPreset.WEBP_PRESET_DEFAULT, 100, WEBP_DECODER_ABI_VERSION) == 0)
                    throw new Exception("Can´t config preset\r\nIn clsWebP.EncodeLossless");
                if (UnsafeNativeMethods.WebPConfigLosslessPreset(ref config, speed) == 0)
                    throw new Exception("Can´t config lossless preset\r\nIn clsWebP.EncodeLossless");
                config.pass = speed + 1;

                //Validate the config
                if (UnsafeNativeMethods.WebPValidateConfig(ref config) != 1)
                    throw new Exception("Bad config parameters\r\nIn clsWebP.EncodeLossless");

                // Setup the input data, allocating a the bitmap, width and height
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                WebPPicture wpic = new WebPPicture();
                if (UnsafeNativeMethods.WebPPictureInitInternal(ref wpic, WEBP_DECODER_ABI_VERSION) != 1)
                    throw new Exception("Can´t init WebPPictureInit\r\nIn clsWebP.EncodeLossless");
                wpic.width = (int)bmp.Width;
                wpic.height = (int)bmp.Height;
                wpic.use_argb = 1;

                //Put the bitmap componets in wpic
                if (UnsafeNativeMethods.WebPPictureImportBGR(ref wpic, bmpData.Scan0, bmpData.Stride) != 1)
                    throw new Exception("Can´t allocate memory in WebPPictureImportBGR\r\nIn clsWebP.EncodeLossless");

                //Set up stadistis of compresion
                WebPAuxStats stats = new WebPAuxStats();
                IntPtr ptrStats = Marshal.AllocHGlobal(Marshal.SizeOf(stats));
                Marshal.StructureToPtr(stats, ptrStats, false);
                wpic.stats = ptrStats;

                // Set up a byte-writing method (write-to-memory, in this case)
                webpMemory = new MemoryWriter();
                webpMemory.data = new byte[bmp.Width * bmp.Height * 24];
                webpMemory.size = 0;
                UnsafeNativeMethods.OnCallback = new UnsafeNativeMethods.WebPMemoryWrite(MyWriter);
                wpic.writer = Marshal.GetFunctionPointerForDelegate(UnsafeNativeMethods.OnCallback);

                //compress the input samples
                int ok = UnsafeNativeMethods.WebPEncode(ref config, ref wpic);
                if (ok != 1)
                    throw new Exception("Encoding error: " + ((WebPEncodingError)wpic.error_code).ToString());

                //Unlock the pixels
                bmp.UnlockBits(bmpData);

                //Copy output to webpData
                rawWebP = new byte[webpMemory.size];
                Array.Copy(webpMemory.data, rawWebP, webpMemory.size);

                //Show statistics
                stats = (WebPAuxStats)Marshal.PtrToStructure(ptrStats, typeof(WebPAuxStats));
                string features = "";
                if ((stats.lossless_features & 1) > 0) features = " PREDICTION";
                if ((stats.lossless_features & 2) > 0) features = features + " CROSS-COLOR-TRANSFORM";
                if ((stats.lossless_features & 4) > 0) features = features + " SUBTRACT-GREEN";
                if ((stats.lossless_features & 8) > 0) features = features + " PALETTE";
                MessageBox.Show("Dimension: " + wpic.width + " x " + wpic.height + " pixels\n" +
                                "Output:    " + stats.coded_size + " bytes\n" +
                                "Losslesss compressed size: " + stats.lossless_size + " bytes\n" +
                                "  * Header size: " + stats.lossless_hdr_size + " bytes\n" +
                                "  * Image data size: " + stats.lossless_data_size + " bytes\n" +
                                "  * Lossless features used:" + features + "\n" +
                                "  * Precision Bits: histogram=" + stats.histogram_bits + " transform=" + stats.transform_bits + " cache=" + stats.cache_bits);

                //Free memory
                Marshal.FreeHGlobal(ptrStats);
                UnsafeNativeMethods.WebPPictureFree(ref wpic);

                return rawWebP;
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn clsWebP.EncodeLossless"); }
        }

        /// <summary>Near lossless encoding image in bitmap</summary>
        /// <param name="bmp">Bitmap with the image</param>
        /// <param name="quality">Quality. 0 = minumin ... 100 = maximimun quality</param>
        /// <param name="speed">Speed of compresion. 0 = maximimun speed and size ... 9 = minimun speed & size</param>
        /// <returns>Compress data</returns>
        public byte[] EncodeNearLossless(Bitmap bmp, int quality, int speed=9)
        {
            byte[] rawWebP = null;

            try
            {
                //Inicialize config struct
                WebPConfig config = new WebPConfig();

                //Set compresion parameters
                if (UnsafeNativeMethods.WebPConfigInitInternal(ref config, WebPPreset.WEBP_PRESET_DEFAULT, 100, WEBP_DECODER_ABI_VERSION) == 0)
                    throw new Exception("Can´t config preset\r\nIn clsWebP.EncodeLossless");
                if (UnsafeNativeMethods.WebPConfigLosslessPreset(ref config, speed) == 0)
                    throw new Exception("Can´t config lossless preset\r\nIn clsWebP.EncodeLossless");
                config.pass = speed + 1;
                config.near_lossless = quality;

                //Validate the config
                if (UnsafeNativeMethods.WebPValidateConfig(ref config) != 1)
                    throw new Exception("Bad config parameters\r\nIn clsWebP.EncodeLossless");

                // Setup the input data, allocating a the bitmap, width and height
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                WebPPicture wpic = new WebPPicture();
                if (UnsafeNativeMethods.WebPPictureInitInternal(ref wpic, WEBP_DECODER_ABI_VERSION) != 1)
                    throw new Exception("Can´t init WebPPictureInit\r\nIn clsWebP.EncodeLossless");
                wpic.width = (int)bmp.Width;
                wpic.height = (int)bmp.Height;
                wpic.use_argb = 1;

                //Put the bitmap componets in wpic
                if (UnsafeNativeMethods.WebPPictureImportBGR(ref wpic, bmpData.Scan0, bmpData.Stride) != 1)
                    throw new Exception("Can´t allocate memory in WebPPictureImportBGR\r\nIn clsWebP.EncodeLossless");

                //Set up stadistis of compresion (enable if you want stadistics)
                //WebPAuxStats stats = new WebPAuxStats();
                //IntPtr ptrStats = Marshal.AllocHGlobal(Marshal.SizeOf(stats));
                //Marshal.StructureToPtr(stats, ptrStats, false);
                //wpic.stats = ptrStats;

                // Set up a byte-writing method (write-to-memory, in this case)
                webpMemory = new MemoryWriter();
                webpMemory.data = new byte[bmp.Width * bmp.Height * 24];
                webpMemory.size = 0;
                UnsafeNativeMethods.OnCallback = new UnsafeNativeMethods.WebPMemoryWrite(MyWriter);
                wpic.writer = Marshal.GetFunctionPointerForDelegate(UnsafeNativeMethods.OnCallback);

                //compress the input samples
                int ok = UnsafeNativeMethods.WebPEncode(ref config, ref wpic);
                if (ok != 1)
                    throw new Exception("Encoding error: " + ((WebPEncodingError)wpic.error_code).ToString());

                //Unlock the pixels
                bmp.UnlockBits(bmpData);

                //Copy output to webpData
                rawWebP = new byte[webpMemory.size];
                Array.Copy(webpMemory.data, rawWebP, webpMemory.size);

                //Show statistics (enable if you want stadistics)
                //stats = (WebPAuxStats)Marshal.PtrToStructure(ptrStats, typeof(WebPAuxStats));
                //string features = "";
                //if ((stats.lossless_features & 1) > 0) features = " PREDICTION";
                //if ((stats.lossless_features & 2) > 0) features = features + " CROSS-COLOR-TRANSFORM";
                //if ((stats.lossless_features & 4) > 0) features = features + " SUBTRACT-GREEN";
                //if ((stats.lossless_features & 8) > 0) features = features + " PALETTE";
                //MessageBox.Show("Dimension: " + wpic.width + " x " + wpic.height + " pixels\n" +
                //                "Output:    " + stats.coded_size + " bytes\n" +
                //                "Losslesss compressed size: " + stats.lossless_size + " bytes\n" +
                //                "  * Header size: " + stats.lossless_hdr_size + " bytes\n" +
                //                "  * Image data size: " + stats.lossless_data_size + " bytes\n" +
                //                "  * Lossless features used:" + features + "\n" +
                //                "  * Precision Bits: histogram=" + stats.histogram_bits + " transform=" + stats.transform_bits + " cache=" + stats.cache_bits);
                //Marshal.FreeHGlobal(ptrStats);

                //Free memory
                UnsafeNativeMethods.WebPPictureFree(ref wpic);

                return rawWebP;
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn clsWebP.EncodeLossless"); }
        }

        /// <summary>Get the libwebp version</summary>
        /// <returns>Version of library</returns>
        public string GetVersion()
        {
            try
            {
                uint v = (uint)UnsafeNativeMethods.WebPGetDecoderVersion();
                var revision = v % 256;
                var minor = (v >> 8) % 256;
                var major = (v >> 16) % 256;
                return major + "." + minor + "." + revision;
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn clsWebP.GetVersion"); }
        }

        /// <summary>Get info of WEBP data</summary>
        /// <param name="rawWebP">The data of WebP</param>
        /// <param name="width">width of image</param>
        /// <param name="height">height of image</param>
        /// <param name="has_alpha">Image has alpha channel</param>
        /// <param name="has_animation">Image is a animation</param>
        /// <param name="format">Format of image: 0 = undefined (/mixed), 1 = lossy, 2 = lossless</param>
        public void GetInfo(byte[] rawWebP, out int width, out int height, out bool has_alpha, out bool has_animation, out string format)
        {
            VP8StatusCode result;

            try
            {
                GCHandle pinnedRawWebP = GCHandle.Alloc(rawWebP, GCHandleType.Pinned);
                IntPtr ptrRawWebP = pinnedRawWebP.AddrOfPinnedObject();

                WebPBitstreamFeatures features = new WebPBitstreamFeatures();
                result = UnsafeNativeMethods.WebPGetFeaturesInternal(ptrRawWebP, (uint)rawWebP.Length, ref features, WEBP_DECODER_ABI_VERSION);
                if (result != 0)
                    throw new Exception(result.ToString());

                width = features.width;
                height =  features.height;
                if(features.has_alpha == 1) has_alpha = true; else has_alpha = false;
                if(features.has_animation == 1) has_animation = true; else has_animation = false;
                switch (features.format)
                {
                    case 1:
                        format = "lossy";
                        break;
                    case 2:
                        format = "lossless";
                        break;
                    default:
                        format = "undefined";
                        break;
                }

                pinnedRawWebP.Free();
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn clsWebP.GetInfo"); }
        }
        #endregion

        #region | Private Methods |
        private MemoryWriter webpMemory;

        private int MyWriter([InAttribute()] IntPtr data, UIntPtr data_size, ref WebPPicture picture)
        {
            Marshal.Copy(data, webpMemory.data, webpMemory.size, (int)data_size);
            webpMemory.size += (int)data_size;
            return 1;
        }

        private delegate int MyWriterDelegate([InAttribute()] IntPtr data, UIntPtr data_size, ref WebPPicture picture);

        private struct MemoryWriter
        {
            public byte[] data;                 // Data of WebP Image
            public int size;                   // Size of webP data
        }

        #endregion

        #region | Destruction |
        /// <summary>Free memory</summary>
        public void Dispose()
        {
            File.Delete(Path.Combine(Application.StartupPath, "libwebp.dll"));
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    #region | Import libwebp functions |
    [SuppressUnmanagedCodeSecurityAttribute]
    internal sealed partial class UnsafeNativeMethods
    {
        /// <summary>This function will initialize the configuration according to a predefined set of parameters (referred to by 'preset') and a given quality factor.</summary>
        /// <param name="config">The WebPConfig struct</param>
        /// <param name="preset">Type of image</param>
        /// <param name="quality">Quality of compresion</param>
        /// <returns>0 if error</returns>
        [DllImport(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPConfigInitInternal(ref WebPConfig config, WebPPreset preset, float quality, int WEBP_DECODER_ABI_VERSION);

        /// <summary>Get info of WepP image</summary>
        /// <param name="rawWebP">Bytes[] of webp image</param>
        /// <param name="data_size">Size of rawWebP</param>
        /// <param name="features">Features of WebP image</param>
        /// <returns>VP8StatusCode</returns>
        [DllImport(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VP8StatusCode WebPGetFeaturesInternal(IntPtr rawWebP, UInt32 data_size, ref WebPBitstreamFeatures features, int WEBP_DECODER_ABI_VERSION);

        /// <summary>Activate the lossless compression mode with the desired efficiency.</summary>
        /// <param name="config">The WebPConfig struct</param>
        /// <param name="level">between 0 (fastest, lowest compression) and 9 (slower, best compression)</param>
        /// <returns>0 in case of parameter errorr</returns>
        [DllImport(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPConfigLosslessPreset(ref WebPConfig config, int level);

        /// <summary>Check that 'config' is non-NULL and all configuration parameters are within their valid ranges.</summary>
        /// <param name="config">The WebPConfig struct</param>
        /// <returns>1 if config are OK</returns>
        [DllImport(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPValidateConfig(ref WebPConfig config);

        /// <summary>Init the struct WebPPicture ckecking the dll version</summary>
        /// <param name="wpic">The WebPPicture struct</param>
        /// <param name="param1"></param>
        /// <returns>1 if not error</returns>
        [DllImportAttribute(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPPictureInitInternal(ref WebPPicture wpic, int WEBP_DECODER_ABI_VERSION);

        /// <summary>Colorspace conversion function to import RGB samples.</summary>
        /// <param name="wpic">The WebPPicture struct</param>
        /// <param name="bgr">Point to BGR data</param>
        /// <param name="stride">stride of BGR data</param>
        /// <returns>Returns 0 in case of memory error.</returns>
        [DllImport(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPPictureImportBGR(ref WebPPicture wpic, IntPtr bgr, int stride);

        /// <summary>Colorspace conversion function to import RGB samples.</summary>
        /// <param name="wpic">The WebPPicture struct</param>
        /// <param name="bgr">Point to BGR data</param>
        /// <param name="stride">stride of BGR data</param>
        /// <returns>Returns 0 in case of memory error.</returns>
        [DllImport(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPPictureImportBGRX(ref WebPPicture wpic, IntPtr bgr, int stride);

        /// <summary>The writer type for output compress data</summary>
        /// <param name="data"></param>
        /// <param name="data_size"></param>
        /// <param name="picture"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int WebPMemoryWrite([InAttribute()] IntPtr data, UIntPtr data_size, ref WebPPicture picture);
        public static WebPMemoryWrite OnCallback;

        /// <summary>Compress to webp format</summary>
        /// <param name="config">The config struct for compresion parameters</param>
        /// <param name="picture">'picture'hold the source samples in both YUV(A) or ARGB input</param>
        /// <returns>Returns 0 in case of error, 1 otherwise. In case of error, picture->error_code is updated accordingly.</returns>
        [DllImportAttribute(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPEncode(ref WebPConfig config, ref WebPPicture picture);

        /// <summary>Release the memory allocated by WebPPictureAlloc() or WebPPictureImport*()
        /// Note that this function does _not_ free the memory used by the 'picture' object itself.
        /// Besides memory (which is reclaimed) all other fields of 'picture' are preserved.</summary>
        /// <param name="picture">Picture struct</param>
        [DllImportAttribute(clsWebP.LibwebpDLLName, EntryPoint = "WebPPictureFree")]
        public static extern void WebPPictureFree(ref WebPPicture picture);

        /// <summary>Validate the WebP image header and retrieve the image height and width. Pointers *width and *height can be passed NULL if deemed irrelevant</summary>
        /// <param name="data">Pointer to WebP image data</param>
        /// <param name="data_size">This is the size of the memory block pointed to by data containing the image data</param>
        /// <param name="width">The range is limited currently from 1 to 16383</param>
        /// <param name="height">The range is limited currently from 1 to 16383</param>
        /// <returns>1 if success, otherwise error code returned in the case of (a) formatting error(s).</returns>
        [DllImport(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPGetInfo(IntPtr data, UInt32 data_size, out int width, out int height);

        /// <summary>Decode WEBP image pointed to by *data and returns BGR samples into a pre-allocated buffer</summary>
        /// <param name="data">Pointer to WebP image data</param>
        /// <param name="data_size">This is the size of the memory block pointed to by data containing the image data</param>
        /// <param name="output_buffer">Pointer to decoded WebP image</param>
        /// <param name="output_buffer_size">Size of allocated buffer</param>
        /// <param name="output_stride">Specifies the distance between scanlines</param>
        /// <returns>output_buffer if function succeeds; NULL otherwise</returns>
        [DllImport(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr WebPDecodeBGRInto(IntPtr data, UInt32 data_size, IntPtr output_buffer, int output_buffer_size, int output_stride);

        /// <summary>Lossless encoding images pointed to by *data in WebP format</summary>
        /// <param name="rgb">Pointer to RGB image data</param>
        /// <param name="width">The range is limited currently from 1 to 16383</param>
        /// <param name="height">The range is limited currently from 1 to 16383</param>
        /// <param name="output_stride">Specifies the distance between scanlines</param>
        /// <param name="quality_factor">Ranges from 0 (lower quality) to 100 (highest quality). Controls the loss and quality during compression</param>
        /// <param name="output">output_buffer with WebP image</param>
        /// <returns>Size of WebP Image</returns>
        [DllImport(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPEncodeBGR(IntPtr bgr, int width, int height, int stride, float quality_factor, out IntPtr output);

        /// <summary>Lossless encoding images pointed to by *data in WebP format</summary>
        /// <param name="rgb">Pointer to RGB image data</param>
        /// <param name="width">The range is limited currently from 1 to 16383</param>
        /// <param name="height">The range is limited currently from 1 to 16383</param>
        /// <param name="output_stride">Specifies the distance between scanlines</param>
        /// <param name="quality_factor">Ranges from 0 (lower quality) to 100 (highest quality). Controls the loss and quality during compression</param>
        /// <param name="output">output_buffer with WebP image</param>
        /// <returns>Size of WebP Image</returns>
        [DllImport(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPEncodeLosslessBGR(IntPtr rgb, int width, int height, int stride, out IntPtr output);

        /// <summary>Releases memory returned by the WebPEncode</summary>
        /// <param name="p">Pointer to memory</param>
        [DllImport(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void WebPFree(IntPtr p);

        /// <summary>Get the webp version library</summary>
        /// <returns>8bits for each of major/minor/revision packet in integer. E.g: v2.5.7 is 0x020507</returns>
        [DllImport(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPGetDecoderVersion();

    }
    #endregion

    #region | Predefined |
    // Enumerate some predefined settings for WebPConfig, depending on the type of source picture. These presets are used when calling WebPConfigPreset().
    public enum WebPPreset
    {
        WEBP_PRESET_DEFAULT = 0,  // default preset.
        WEBP_PRESET_PICTURE,      // digital picture, like portrait, inner shot
        WEBP_PRESET_PHOTO,        // outdoor photograph, with natural lighting
        WEBP_PRESET_DRAWING,      // hand or line drawing, with high-contrast details
        WEBP_PRESET_ICON,         // small-sized colorful images
        WEBP_PRESET_TEXT          // text-like
    };

    public enum WebPImageHint
    {
        WEBP_HINT_DEFAULT = 0,  // default preset.
        WEBP_HINT_PICTURE,      // digital picture, like portrait, inner shot
        WEBP_HINT_PHOTO,        // outdoor photograph, with natural lighting
        WEBP_HINT_GRAPH,        // Discrete tone image (graph, map-tile etc).
        WEBP_HINT_LAST
    };

    // Encoding error conditions.
    public enum WebPEncodingError
    {
        VP8_ENC_OK = 0,
        VP8_ENC_ERROR_OUT_OF_MEMORY,
        VP8_ENC_ERROR_BITSTREAM_OUT_OF_MEMORY,
        VP8_ENC_ERROR_NULL_PARAMETER,
        VP8_ENC_ERROR_INVALID_CONFIGURATION,
        VP8_ENC_ERROR_BAD_DIMENSION,
        VP8_ENC_ERROR_PARTITION0_OVERFLOW,
        VP8_ENC_ERROR_PARTITION_OVERFLOW,
        VP8_ENC_ERROR_BAD_WRITE,
        VP8_ENC_ERROR_FILE_TOO_BIG,
        VP8_ENC_ERROR_USER_ABORT,
        VP8_ENC_ERROR_LAST,
    }

    public enum VP8StatusCode
    {
        VP8_STATUS_OK = 0,
        VP8_STATUS_OUT_OF_MEMORY,
        VP8_STATUS_INVALID_PARAM,
        VP8_STATUS_BITSTREAM_ERROR,
        VP8_STATUS_UNSUPPORTED_FEATURE,
        VP8_STATUS_SUSPENDED,
        VP8_STATUS_USER_ABORT,
        VP8_STATUS_NOT_ENOUGH_DATA,
    }
    #endregion

    #region | libwebp structs |
    // Features gathered from the bitstream
    [StructLayoutAttribute(LayoutKind.Sequential)]
    struct WebPBitstreamFeatures
    {
        public int width;                   // Width in pixels, as read from the bitstream.
        public int height;                  // Height in pixels, as read from the bitstream.
        public int has_alpha;               // True if the bitstream contains an alpha channel.
        public int has_animation;           // True if the bitstream is an animation.
        public int format;                  // 0 = undefined (/mixed), 1 = lossy, 2 = lossless
        private int pad1;                   // padding for later use
        private int pad2;                   // padding for later use
        private int pad3;                   // padding for later use
        private int pad4;                   // padding for later use
        private int pad5;                   // padding for later use
    };

    // Compression parameters.
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPConfig
    {
        public int lossless;                // Lossless encoding (0=lossy(default), 1=lossless).
        public float quality;               // between 0 (smallest file) and 100 (biggest)
        public int method;                  // quality/speed trade-off (0=fast, 6=slower-better)
        public UInt32 image_hint;           // Hint for image type (lossless only for now). Parameters related to lossy compression only.
        public int target_size;             // if non-zero, set the desired target size in bytes. Takes precedence over the 'compression' parameter.
        public float target_PSNR;           // if non-zero, specifies the minimal distortion to try to achieve. Takes precedence over target_size.
        public int segments;                // maximum number of segments to use, in [1..4]
        public int sns_strength;            // Spatial Noise Shaping. 0=off, 100=maximum.
        public int filter_strength;         // range: [0 = off .. 100 = strongest]
        public int filter_sharpness;        // range: [0 = off .. 7 = least sharp]
        public int filter_type;             // filtering type: 0 = simple, 1 = strong (only used if filter_strength > 0 or autofilter > 0)
        public int autofilter;              // Auto adjust filter's strength [0 = off, 1 = on]
        public int alpha_compression;       // Algorithm for encoding the alpha plane (0 = none, 1 = compressed with WebP lossless). Default is 1.
        public int alpha_filtering;         // Predictive filtering method for alpha plane. 0: none, 1: fast, 2: best. Default if 1.
        public int alpha_quality;           // Between 0 (smallest size) and 100 (lossless). Default is 100.
        public int pass;                    // number of entropy-analysis passes (in [1..10]).
        public int show_compressed;         // if true, export the compressed picture back. In-loop filtering is not applied.
        public int preprocessing;           // preprocessing filter (0=none, 1=segment-smooth)
        public int partitions;              // log2(number of token partitions) in [0..3] Default is set to 0 for easier progressive decoding.
        public int partition_limit;         // quality degradation allowed to fit the 512k limit on prediction modes coding (0: no degradation, 100: maximum possible degradation).
        public int emulate_jpeg_size;       // If true, compression parameters will be remapped to better match the expected output size from JPEG compression. Generally, the output size will be similar but the degradation will be lower.
        public int thread_level;            // If non-zero, try and use multi-threaded encoding.
        public int low_memory;              // If set, reduce memory usage (but increase CPU use).
        public int near_lossless;           // Near lossless encoding [0 = off(default) .. 100]. This feature is experimental.
        public int exact;                   // if non-zero, preserve the exact RGB values under transparent area. Otherwise, discard this invisible RGB information for better compression. The default value is 0.
        public int delta_palettization;     // WEBP_EXPERIMENTAL_FEATURE
        private int pad1;                   // padding for later use
        private int pad2;                   // padding for later use
    };

    // Main exchange structure (input samples, output bytes, statistics)
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPPicture
    {
        /////////////// INPUT
        public int use_argb;                // Recommended to use ARGB input (*argb, argb_stride) for lossless, and YUV input (*y, *u, *v, etc.) for lossy
        public UInt32 colorspace;           // colorspace: should be YUV420 for now (=Y'CbCr). Value = 0
        // Yuv input (mostly used for input to lossy compression)
        public int width;                   // Width picture dimensions (less or equal to WEBP_MAX_DIMENSION)
        public int height;                  // Height picture dimensions (less or equal to WEBP_MAX_DIMENSION)
        public IntPtr y;                    // pointer to luma plane.
        public IntPtr u;                    // pointers to chroma planes.
        public IntPtr v;
        public int y_stride;                // luma stride.
        public int uv_stride;               // chroma stride.
        public IntPtr a;                    // pointer to the alpha plane
        public int a_stride;                // stride of the alpha plane
        private Int32 pad1;                 // padding for later use
        private Int32 pad2;
        // ARGB input (mostly used for input to lossless compression)
        public IntPtr argb;                 // Pointer to argb (32 bit) plane.
        public int argb_stride;             // This is stride in pixels units, not bytes.
        public Int32 pad3;                  // padding for later use
        public Int32 pad4;
        public Int32 pad5;
        /////////////// OUTPUT
        public IntPtr writer;               // Byte-emission hook, to store compressed bytes as they are ready.
        public IntPtr custom_ptr;           // can be used by the writer.
        // map for extra information (only for lossy compression mode)
        public int extra_info_type;         // 1: intra type, 2: segment, 3: quant, 4: intra-16 prediction mode, 5: chroma prediction mode, 6: bit cost, 7: distortion
        public IntPtr extra_info;           // if not NULL, points to an array of size ((width + 15) / 16) * ((height + 15) / 16) that will be filled with a macroblock map, depending on extra_info_type.
        /////////////////////////// STATS AND REPORTS
        public IntPtr stats;                // Pointer to side statistics (updated only if not NULL)
        public UInt32 error_code;           // Error code for the latest error encountered during encoding
        public IntPtr progress_hook;        // If not NULL, report progress during encoding.
        public IntPtr user_data;            // this field is free to be set to any value and used during callbacks (like progress-report e.g.).
        private Int32 pad6;                 // padding for later use
        private Int32 pad7;
        private Int32 pad8;
        private IntPtr pad9;
        private IntPtr pad10;
        private Int32 pad11;
        private Int32 pad12;
        private Int32 pad13;
        private Int32 pad14;
        private Int32 pad15;
        private Int32 pad16;
        private Int32 pad17;
        private Int32 pad18;
        //////////////////// PRIVATE FIELDS
        private IntPtr memory_;             // row chunk of memory for yuva planes
        private IntPtr memory_argb_;        // and for argb too.
        private IntPtr pad19;               // padding for later use
        private IntPtr pad20;               // padding for later use
    };

    // Structure for storing auxiliary statistics (mostly for lossy encoding).
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPAuxStats
    {
        public int coded_size;                  // final size
        public float PSNRY;                     // peak-signal-to-noise ratio for Y
        public float PSNRU;                     // peak-signal-to-noise ratio for U
        public float PSNRV;                     // peak-signal-to-noise ratio for V
        public float PSNRALL;                   // peak-signal-to-noise ratio for All
        public float PSNRAlpha;                 // peak-signal-to-noise ratio for Alpha
        public int block_count_intra4;          // number of intra4
        public int block_count_intra16;         // number of intra16
        public int block_count_skipped;         // number of skipped macroblocks
        public int header_bytes;                // approximate number of bytes spent for header
        public int mode_partition_0;            // approximate number of bytes spent for  mode-partition #0
        public int residual_bytes_DC_segments0; // approximate number of bytes spent for DC coefficients for segment 0.
        public int residual_bytes_AC_segments0; // approximate number of bytes spent for AC coefficients for segment 0.
        public int residual_bytes_uv_segments0; // approximate number of bytes spent for uv coefficients for segment 0.
        public int residual_bytes_DC_segments1; // approximate number of bytes spent for DC coefficients for segment 1.
        public int residual_bytes_AC_segments1; // approximate number of bytes spent for AC coefficients for segment 1.
        public int residual_bytes_uv_segments1; // approximate number of bytes spent for uv coefficients for segment 1.
        public int residual_bytes_DC_segments2; // approximate number of bytes spent for DC coefficients for segment 2.
        public int residual_bytes_AC_segments2; // approximate number of bytes spent for AC coefficients for segment 2.
        public int residual_bytes_uv_segments2; // approximate number of bytes spent for uv coefficients for segment 2.
        public int residual_bytes_DC_segments3; // approximate number of bytes spent for DC coefficients for segment 3.
        public int residual_bytes_AC_segments3; // approximate number of bytes spent for AC coefficients for segment 3.
        public int residual_bytes_uv_segments3; // approximate number of bytes spent for uv coefficients for segment 3.
        public int segment_size_segments0;      // number of macroblocks in segments 0
        public int segment_size_segments1;      // number of macroblocks in segments 1
        public int segment_size_segments2;      // number of macroblocks in segments 2
        public int segment_size_segments3;      // number of macroblocks in segments 3
        public int segment_quant_segments0;     // quantizer values for segment 0
        public int segment_quant_segments1;     // quantizer values for segment 1
        public int segment_quant_segments2;     // quantizer values for segment 2
        public int segment_quant_segments3;     // quantizer values for segment 3
        public int segment_level_segments0;     // filtering strength for segment 0 [0..63]
        public int segment_level_segments1;     // filtering strength for segment 1 [0..63]
        public int segment_level_segments2;     // filtering strength for segment 2 [0..63]
        public int segment_level_segments3;     // filtering strength for segment 3 [0..63]
        public int alpha_data_size;             // size of the transparency data
        public int layer_data_size;             // size of the enhancement layer data
        // lossless encoder statistics
        public Int32 lossless_features;         // bit0:predictor bit1:cross-color transform bit2:subtract-green bit3:color indexing
        public int histogram_bits;              // number of precision bits of histogram
        public int transform_bits;              // precision bits for transform
        public int cache_bits;                  // number of bits for color cache lookup
        public int palette_size;                // number of color in palette, if used
        public int lossless_size;               // final lossless size
        public int lossless_hdr_size;           // lossless header (transform, huffman etc) size
        public int lossless_data_size;          // lossless image data size
        private UInt32 pad1;                    // padding for later use
        private UInt32 pad2;                    // padding for later use
    };
    #endregion
}



    //////////////// For future implementation /////////////////
    /*


    // WebPMemoryWrite: a special WebPWriterFunction that writes to memory using
    // the following WebPMemoryWriter object (to be set as a custom_ptr).
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPMemoryWriter
    {
        public IntPtr mem;                  // final buffer (of size 'max_size', larger than 'size').
        public UIntPtr size;                // final size
        public UIntPtr max_size;            // total capacity
        private Int32 pad1;                 // padding for later use
    }

    // The following must be called to deallocate writer->mem memory. The 'writer' object itself is not deallocated.
    [DllImportAttribute("libwebp", EntryPoint = "WebPPictureFree")]
    public static extern void WebPMemoryWriterClear(ref WebPMemoryWriter writer);
        
    /// <summary>Init the WebPMemoryWriter struct</summary>
    /// <param name="writer"></param>
    //[DllImportAttribute(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
    //public static extern void WebPMemoryWriterInit(out WebPMemoryWriter writer);

    /// Return Type: int
    ///param0: WebPDecoderConfig*
    ///param1: int
    [DllImport(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool WebPInitDecoderConfigInternal(out WebPDecoderConfig config, int WEBP_DECODER_ABI_VERSION);

    ///param0: uint8_t*
    ///param1: size_t->unsigned int
    ///param2: WebPBitstreamFeatures*
    ///param3: int
    [DllImport(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
    public static extern VP8StatusCode WebPGetFeaturesInternal(IntPtr dataIn, UInt32 data_size, out WebPBitstreamFeatures input, int WEBP_DECODER_ABI_VERSION);

    ///data: uint8_t*
    ///data_size: size_t->unsigned int
    ///config: WebPDecoderConfig*
    [DllImport(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
    public static extern VP8StatusCode WebPDecode(IntPtr data, UInt32 data_size, ref WebPDecoderConfig config);

    [DllImport(clsWebP.LibwebpDLLName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void WebPFreeDecBuffer(ref WebPDecBuffer output);

    [DllImportAttribute(clsWebP.LibwebpDLLName, EntryPoint = "WebPSafeFree", CallingConvention = CallingConvention.Cdecl)]
    public static extern void WebPSafeFree(IntPtr toDeallocate);
 
    [StructLayoutAttribute(LayoutKind.Sequential)]
    struct WebPDecoderConfig
    {
        public WebPBitstreamFeatures input;
        public WebPDecBuffer output;
        public WebPDecoderOptions options;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    struct WebPBitstreamFeatures
    {
        public int width;                      // Width in pixels, as read from the bitstream.
        public int height;                     // Height in pixels, as read from the bitstream.
        public int has_alpha;                  // True if the bitstream contains an alpha channel.
        public int has_animation;              // True if the bitstream is an animation.
        public int format;                     // 0 = undefined (/mixed), 1 = lossy, 2 = lossless
        // Unused for now:
        public int no_incremental_decoding;    // if true, using incremental decoding is not recommended.
        public int rotate;                     // TODO(later)
        public int uv_sampling;                // should be 0 for now. TODO(later)
        public UInt32 pad1;                    // padding for later use
        public UInt32 pad2;
    };

    // Output buffer
    [StructLayoutAttribute(LayoutKind.Sequential)]
    struct WebPDecBuffer
    {
        public int colorspace;                  // Colorspace.
        public int width;                       // Dimensions.
        public int height;
        public Int32 is_external_memory;        // If true, 'internal_memory' pointer is not used.
        public IntPtr rgba; // pointer to RGBA samples
        public int stride; // stride in bytes from one scanline to the next.
        #if _WIN64
            public UInt64 size; // total size of the *rgba buffer.
        #else
            public UInt32 size; // total size of the *rgba buffer.
        #endif
        public UInt32 pad1;                    // padding for later use
        public UInt32 pad2;
        public UInt32 pad3;
        public UInt32 pad4;
        public IntPtr private_memory; // Internally allocated memory (only when is_external_memory is false). Should not be used externally, but accessed via the buffer union.
    };

    [StructLayoutAttribute(LayoutKind.Sequential)]
    struct WebPDecoderOptions
    {
        public int bypass_filtering;            // if true, skip the in-loop filtering
        public int no_fancy_upsampling;         // if true, use faster pointwise upsampler
        public int use_cropping;                // if true, cropping is applied _first_
        public int crop_left;                   // top-left position for cropping. Will be snapped to even values.
        public int crop_top;
        public int crop_width;                  // dimension of the cropping area
        public int crop_height;
        public int use_scaling;                 // if true, scaling is applied _afterward_
        public int scaled_width;                // final resolution
        public int scaled_height;
        public int use_threads;                 // if true, use multi-threaded decoding
        public int dithering_strength;          // dithering strength (0=Off, 100=full)
        public int flip;                        // flip output vertically
        public int alpha_dithering_strength;    // alpha dithering strength in [0..100]
        // Unused for now:
        public int force_rotation;              // forced rotation (to be applied _last_)
        public int no_enhancement;              // if true, discard enhancement layer
        public UInt32 pad1;                     // padding for later use
        public UInt32 pad2;
        public UInt32 pad3;
    };

    public enum VP8StatusCode
    {
        VP8_STATUS_OK = 0,
        VP8_STATUS_OUT_OF_MEMORY,
        VP8_STATUS_INVALID_PARAM,
        VP8_STATUS_BITSTREAM_ERROR,
        VP8_STATUS_UNSUPPORTED_FEATURE,
        VP8_STATUS_SUSPENDED,
        VP8_STATUS_USER_ABORT,
        VP8_STATUS_NOT_ENOUGH_DATA,
    }

    public enum WEBP_CSP_MODE
    {
        MODE_RGB = 0, MODE_RGBA = 1,
        MODE_BGR = 2, MODE_BGRA = 3,
        MODE_ARGB = 4, MODE_RGBA_4444 = 5,
        MODE_RGB_565 = 6,
        // RGB-premultiplied transparent modes (alpha value is preserved)
        MODE_rgbA = 7,
        MODE_bgrA = 8,
        MODE_Argb = 9,
        MODE_rgbA_4444 = 10,
        // YUV modes must come after RGB ones.
        MODE_YUV = 11, MODE_YUVA = 12, // yuv 4:2:0
        MODE_LAST = 13
    };
    */


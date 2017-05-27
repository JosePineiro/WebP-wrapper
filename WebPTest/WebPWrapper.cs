/////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// Wrapper for WebP format in C#. (GPL) Jose M. Piñeiro
///////////////////////////////////////////////////////////////////////////////////////////////////////////// 
/// Main functions:
/// Save - Save a bitmap in WebP file.
/// Load - Load a WebP file in bitmap.
/// Decode - Decode WebP data (in byte array) to bitmap.
/// EncodeLossly - Encode bitmap to WebP with quality lost (return a byte array).
/// EncodeLossless - Encode bitmap to WebP without quality lost (return a byte array).
/// EncodeNearLossless - Encode bitmap to WebP in Near lossless (return a byte array).
/// 
/// 
/// Another functions:
/// GetVersion - Get the library version
/// GetInfo - Get information of WEBP data
/// GetPictureDistortion - Get PSNR, SSIM or LSIM distortion metric between two pictures
/////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Forms;

namespace WebPWrapper
{
    public sealed class WebP : IDisposable
    {
        #region | Public Decompress Functions |
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
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn WebP.Load"); }
        }

        /// <summary>Decode a WebP image</summary>
        /// <param name="webpData">the data to uncompress</param>
        /// <returns>Bitmap with the WebP image</returns>
        public Bitmap Decode(byte[] webpData)
        {
            int imgWidth;
            int imgHeight;
            int outputSize;
            Bitmap bmp = null;
            BitmapData bmpData = null;
            GCHandle pinnedWebP = GCHandle.Alloc(webpData, GCHandleType.Pinned);

            try
            {
                //Get image width and height
                IntPtr ptrData = pinnedWebP.AddrOfPinnedObject();
                UInt32 dataSize = (uint)webpData.Length;
                if (UnsafeNativeMethods.WebPGetInfo(ptrData, dataSize, out imgWidth, out imgHeight) == 0)
                    throw new Exception("Can´t get information of WebP");

                //Create a BitmapData and Lock all pixels to be written
                bmp = new Bitmap(imgWidth, imgHeight, PixelFormat.Format24bppRgb);
                bmpData = bmp.LockBits(new Rectangle(0, 0, imgWidth, imgHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);

                //Uncompress the image
                outputSize = bmpData.Stride * imgHeight;
                if (UnsafeNativeMethods.WebPDecodeBGRInto(ptrData, dataSize, bmpData.Scan0, outputSize, bmpData.Stride) == 0)
                    throw new Exception("Can´t decode WebP");

                return bmp;
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn WebP.Decode"); }
            finally
            {
                //Unlock the pixels
                if (bmpData != null)
                    bmp.UnlockBits(bmpData);

                //Free memory
                if (pinnedWebP.IsAllocated)
                    pinnedWebP.Free();
            }
        }
        #endregion

        #region | Public Compress Functions |
        /// <summary>Save bitmap to file in WebP format</summary>
        /// <param name="bmp">Bitmap with the WebP image</param>
        /// <param name="pathFileName">The file to write</param>
        /// <param name="quality">Between 0 (lower quality, lowest file size) and 100 (highest quality, higher file size)</param>
        public void Save(Bitmap bmp, string pathFileName, int quality = 75)
        {
            byte[] rawWebP;

            try
            {
                //Encode in webP format
                rawWebP = EncodeLossy(bmp, quality);

                //Write webP file
                File.WriteAllBytes(pathFileName, rawWebP);
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn WebP.Save"); }
        }

        /// <summary>Lossy encoding bitmap to WebP (Simple encoding API)</summary>
        /// <param name="bmp">Bitmap with the image</param>
        /// <param name="quality">Between 0 (lower quality, lowest file size) and 100 (highest quality, higher file size)</param>
        /// <returns>Compressed data</returns>
        public byte[] EncodeLossy(Bitmap bmp, int quality = 75)
        {
            BitmapData bmpData = null;
            IntPtr unmanagedData = IntPtr.Zero; 
            byte[] rawWebP = null;

            try
            {
                //Get bmp data
                bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                //Compress the bmp data
                int size = UnsafeNativeMethods.WebPEncodeBGR(bmpData.Scan0, bmp.Width, bmp.Height, bmpData.Stride, quality, out unmanagedData);
                if (size == 0)
                    throw new Exception("Can´t encode WebP");

                //Copy image compress data to output array
                rawWebP = new byte[size];
                Marshal.Copy(unmanagedData, rawWebP, 0, size);

                return rawWebP;
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn WebP.EncodeLossly (Simple)"); }
            finally
            {
                //Unlock the pixels
                if (bmpData != null)
                    bmp.UnlockBits(bmpData);

                //Free memory
                if (unmanagedData != IntPtr.Zero)
                    UnsafeNativeMethods.WebPFree(unmanagedData);
            }
        }

        /// <summary>Lossy encoding bitmap to WebP (Advanced encoding API)</summary>
        /// <param name="bmp">Bitmap with the image</param>
        /// <param name="quality">Between 0 (lower quality, lowest file size) and 100 (highest quality, higher file size)</param>
        /// <param name="speed">Between 0 (fastest, lowest compression) and 9 (slower, best compression)</param>
        /// <returns>Compressed data</returns>
        public byte[] EncodeLossy(Bitmap bmp, int quality, int speed, bool info = false)
        {
            byte[] rawWebP = null;
            WebPPicture wpic = new WebPPicture();
            BitmapData bmpData = null;
            WebPAuxStats stats = new WebPAuxStats();
            IntPtr ptrStats = IntPtr.Zero;

            try
            {
                //Inicialize config struct
                WebPConfig config = new WebPConfig();

                //Set compresion parameters
                if (UnsafeNativeMethods.WebPConfigInitInternal(ref config, WebPPreset.WEBP_PRESET_DEFAULT, 75) == 0)
                    throw new Exception("Can´t config preset");

                // Add additional tuning:
                config.method = speed;
                if (config.method > 6)
                    config.method = 6;
                config.quality = quality;
                config.autofilter = 1;
                config.pass = speed + 1;
                config.segments = 4;
                config.partitions = 3;
                config.thread_level = 1;

                //Validate the config
                if (UnsafeNativeMethods.WebPValidateConfig(ref config) != 1)
                    throw new Exception("Bad config parameters");

                // Setup the input data, allocating a the bitmap, width and height
                bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                if (UnsafeNativeMethods.WebPPictureInitInternal(ref wpic) != 1)
                    throw new Exception("Can´t init WebPPictureInit");
                wpic.width = (int)bmp.Width;
                wpic.height = (int)bmp.Height;
                wpic.use_argb = 1;

                //Put the bitmap componets in wpic
                if (UnsafeNativeMethods.WebPPictureImportBGR(ref wpic, bmpData.Scan0, bmpData.Stride) != 1)
                    throw new Exception("Can´t allocate memory in WebPPictureImportBGR");

                //Set up statistics of compresion
                if (info)
                {
                    stats = new WebPAuxStats();
                    ptrStats = Marshal.AllocHGlobal(Marshal.SizeOf(stats));
                    Marshal.StructureToPtr(stats, ptrStats, false);
                    wpic.stats = ptrStats;
                }

                // Set up a byte-writing method (write-to-memory, in this case)
                webpMemory = new MemoryWriter();
                webpMemory.data = new byte[bmp.Width * bmp.Height * 24];
                webpMemory.size = 0;
                UnsafeNativeMethods.OnCallback = new UnsafeNativeMethods.WebPMemoryWrite(MyWriter);
                wpic.writer = Marshal.GetFunctionPointerForDelegate(UnsafeNativeMethods.OnCallback);

                //compress the input samples
                if (UnsafeNativeMethods.WebPEncode(ref config, ref wpic) != 1)
                    throw new Exception("Encoding error: " + ((WebPEncodingError)wpic.error_code).ToString());

                //Unlock the pixels
                bmp.UnlockBits(bmpData);
                bmpData = null;

                //Copy output to webpData
                rawWebP = new byte[webpMemory.size];
                Array.Copy(webpMemory.data, rawWebP, webpMemory.size);

                //Show statistics
                if (info)
                {
                    stats = (WebPAuxStats)Marshal.PtrToStructure(ptrStats, typeof(WebPAuxStats));
                    MessageBox.Show("Dimension: " + wpic.width + " x " + wpic.height + " pixels\n" +
                                    "Output:    " + stats.coded_size + " bytes\n" +
                                    "PSNR Y:    " + stats.PSNRY + " db\n" +
                                    "PSNR u:    " + stats.PSNRU + " db\n" +
                                    "PSNR v:    " + stats.PSNRV + " db\n" +
                                    "PSNR ALL:  " + stats.PSNRALL + " db\n" +
                                    "Block intra4:  " + stats.block_count_intra4 + "\n" +
                                    "Block intra16: " + stats.block_count_intra16 + "\n" +
                                    "Block skipped: " + stats.block_count_skipped + "\n" +
                                    "Header size:    " + stats.header_bytes + " bytes\n" +
                                    "Mode-partition: " + stats.mode_partition_0 + " bytes\n" +
                                    "Macroblocks 0:  " + stats.segment_size_segments0 + " residuals bytes\n" +
                                    "Macroblocks 1:  " + stats.segment_size_segments1 + " residuals bytes\n" +
                                    "Macroblocks 2:  " + stats.segment_size_segments2 + " residuals bytes\n" +
                                    "Macroblocks 3:  " + stats.segment_size_segments3 + " residuals bytes\n" +
                                    "Quantizer   0:  " + stats.segment_quant_segments0 + " residuals bytes\n" +
                                    "Quantizer   1:  " + stats.segment_quant_segments1 + " residuals bytes\n" +
                                    "Quantizer   2:  " + stats.segment_quant_segments2 + " residuals bytes\n" +
                                    "Quantizer   3:  " + stats.segment_quant_segments3 + " residuals bytes\n" +
                                    "Filter level 0: " + stats.segment_level_segments0 + " residuals bytes\n" +
                                    "Filter level 1: " + stats.segment_level_segments1 + " residuals bytes\n" +
                                    "Filter level 2: " + stats.segment_level_segments2 + " residuals bytes\n" +
                                    "Filter level 3: " + stats.segment_level_segments3 + " residuals bytes\n");
                }

                return rawWebP;
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn WebP.EncodeLossly (Advanced)"); }
            finally
            {
                //Free statistics memory
                if (ptrStats != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptrStats);

                //Unlock the pixels
                if (bmpData != null)
                    bmp.UnlockBits(bmpData);

                //Free memory
                if (wpic.argb != IntPtr.Zero)
                    UnsafeNativeMethods.WebPPictureFree(ref wpic);
            }
        }

        /// <summary>Lossless encoding bitmap to WebP (Simple encoding API)</summary>
        /// <param name="bmp">Bitmap with the image</param>
        /// <returns>Compressed data</returns>
        public byte[] EncodeLossless(Bitmap bmp)
        {
            BitmapData bmpData = null;
            IntPtr unmanagedData = IntPtr.Zero;
            byte[] rawWebP = null;

            try
            {
                //Get bmp data
                bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                //Compress the bmp data
                int size = UnsafeNativeMethods.WebPEncodeLosslessBGR(bmpData.Scan0, bmp.Width, bmp.Height, bmpData.Stride, out unmanagedData);

                //Copy image compress data to output array
                rawWebP = new byte[size];
                Marshal.Copy(unmanagedData, rawWebP, 0, size);

                return rawWebP;
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn WebP.EncodeLossless (Simple)"); }
            finally
            {
                //Unlock the pixels
                if (bmpData != null)
                    bmp.UnlockBits(bmpData);

                //Free memory
                if (unmanagedData != IntPtr.Zero)
                    UnsafeNativeMethods.WebPFree(unmanagedData);
            }
        }

        /// <summary>Lossless encoding image in bitmap (Advanced encoding API)</summary>
        /// <param name="bmp">Bitmap with the image</param>
        /// <param name="speed">Between 0 (fastest, lowest compression) and 9 (slower, best compression)</param>
        /// <returns>Compressed data</returns>
        public byte[] EncodeLossless(Bitmap bmp, int speed, bool info = false)
        {

            byte[] rawWebP = null;
            WebPPicture wpic = new WebPPicture();
            BitmapData bmpData = null;
            WebPAuxStats stats = new WebPAuxStats();
            IntPtr ptrStats = IntPtr.Zero;

            try
            {
                //Inicialize config struct
                WebPConfig config = new WebPConfig();

                //Set compresion parameters
                if (UnsafeNativeMethods.WebPConfigInitInternal(ref config, WebPPreset.WEBP_PRESET_DEFAULT, (speed + 1) * 10) == 0)
                    throw new Exception("Can´t config preset");
                if (UnsafeNativeMethods.WebPConfigLosslessPreset(ref config, speed) == 0)
                    throw new Exception("Can´t config lossless preset");
                config.pass = speed + 1;

                //Validate the config
                if (UnsafeNativeMethods.WebPValidateConfig(ref config) != 1)
                    throw new Exception("Bad config parameters");

                // Setup the input data, allocating a the bitmap, width and height
                bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                if (UnsafeNativeMethods.WebPPictureInitInternal(ref wpic) != 1)
                    throw new Exception("Can´t init WebPPictureInit");
                wpic.width = (int)bmp.Width;
                wpic.height = (int)bmp.Height;
                wpic.use_argb = 1;

                //Put the bitmap componets in wpic
                if (UnsafeNativeMethods.WebPPictureImportBGR(ref wpic, bmpData.Scan0, bmpData.Stride) != 1)
                    throw new Exception("Can´t allocate memory in WebPPictureImportBGR");

                //Set up stadistis of compresion
                if (info)
                {
                    stats = new WebPAuxStats();
                    ptrStats = Marshal.AllocHGlobal(Marshal.SizeOf(stats));
                    Marshal.StructureToPtr(stats, ptrStats, false);
                    wpic.stats = ptrStats;
                }

                // Set up a byte-writing method (write-to-memory, in this case)
                webpMemory = new MemoryWriter();
                webpMemory.data = new byte[bmp.Width * bmp.Height * 24];
                webpMemory.size = 0;
                UnsafeNativeMethods.OnCallback = new UnsafeNativeMethods.WebPMemoryWrite(MyWriter);
                wpic.writer = Marshal.GetFunctionPointerForDelegate(UnsafeNativeMethods.OnCallback);

                //compress the input samples
                if (UnsafeNativeMethods.WebPEncode(ref config, ref wpic) != 1)
                    throw new Exception("Encoding error: " + ((WebPEncodingError)wpic.error_code).ToString());

                //Unlock the pixels
                bmp.UnlockBits(bmpData);
                bmpData = null;

                //Copy output to webpData
                rawWebP = new byte[webpMemory.size];
                Array.Copy(webpMemory.data, rawWebP, webpMemory.size);

                //Show statistics
                if (info)
                {
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
                }

                return rawWebP;
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn WebP.EncodeLossless (Advanced)"); }
            finally
            {
                //Free statistics memory
                if (ptrStats != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptrStats);

                //Unlock the pixels
                if (bmpData != null)
                    bmp.UnlockBits(bmpData);

                //Free memory
                if (wpic.argb != IntPtr.Zero)
                    UnsafeNativeMethods.WebPPictureFree(ref wpic);
            }
        }

        /// <summary>Near lossless encoding image in bitmap</summary>
        /// <param name="bmp">Bitmap with the image</param>
        /// <param name="quality">Between 0 (lower quality, lowest file size) and 100 (highest quality, higher file size)</param>
        /// <param name="speed">Between 0 (fastest, lowest compression) and 9 (slower, best compression)</param>
        /// <returns>Compress data</returns>
        public byte[] EncodeNearLossless(Bitmap bmp, int quality, int speed = 9, bool info = false)
        {
            byte[] rawWebP = null;
            WebPPicture wpic = new WebPPicture();
            BitmapData bmpData = null;
            WebPAuxStats stats = new WebPAuxStats();
            IntPtr ptrStats = IntPtr.Zero;

            try
            {
                //Inicialize config struct
                WebPConfig config = new WebPConfig();

                //Set compresion parameters
                if (UnsafeNativeMethods.WebPConfigInitInternal(ref config, WebPPreset.WEBP_PRESET_DEFAULT, (speed + 1) * 10) == 0)
                    throw new Exception("Can´t config preset");
                if (UnsafeNativeMethods.WebPConfigLosslessPreset(ref config, speed) == 0)
                    throw new Exception("Can´t config lossless preset");
                config.pass = speed + 1;
                config.near_lossless = quality;

                //Validate the config
                if (UnsafeNativeMethods.WebPValidateConfig(ref config) != 1)
                    throw new Exception("Bad config parameters");

                // Setup the input data, allocating the bitmap, width and height
                bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                wpic = new WebPPicture();
                if (UnsafeNativeMethods.WebPPictureInitInternal(ref wpic) != 1)
                    throw new Exception("Can´t init WebPPictureInit");
                wpic.width = (int)bmp.Width;
                wpic.height = (int)bmp.Height;
                wpic.use_argb = 1;

                //Put the bitmap componets in wpic
                if (UnsafeNativeMethods.WebPPictureImportBGR(ref wpic, bmpData.Scan0, bmpData.Stride) != 1)
                    throw new Exception("Can´t allocate memory in WebPPictureImportBGR");

                //Set up stadistis of compresion
                if (info)
                {
                    stats = new WebPAuxStats();
                    ptrStats = Marshal.AllocHGlobal(Marshal.SizeOf(stats));
                    Marshal.StructureToPtr(stats, ptrStats, false);
                    wpic.stats = ptrStats;
                }

                // Set up a byte-writing method (write-to-memory, in this case)
                webpMemory = new MemoryWriter();
                webpMemory.data = new byte[bmp.Width * bmp.Height * 24];
                webpMemory.size = 0;
                UnsafeNativeMethods.OnCallback = new UnsafeNativeMethods.WebPMemoryWrite(MyWriter);
                wpic.writer = Marshal.GetFunctionPointerForDelegate(UnsafeNativeMethods.OnCallback);

                //compress the input samples
                if (UnsafeNativeMethods.WebPEncode(ref config, ref wpic) != 1)
                    throw new Exception("Encoding error: " + ((WebPEncodingError)wpic.error_code).ToString());

                //Unlock the pixels
                bmp.UnlockBits(bmpData);
                bmpData = null;

                //Copy output to webpData
                rawWebP = new byte[webpMemory.size];
                Array.Copy(webpMemory.data, rawWebP, webpMemory.size);

                //Show statistics (enable if you want stadistics)
                if (info)
                {
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
                }

                return rawWebP;
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn WebP.EncodeNearLossless"); }
            finally
            {
                //Free statistics memory
                if (ptrStats != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptrStats);

                //Unlock the pixels
                if (bmpData != null)
                    bmp.UnlockBits(bmpData);

                //Free memory
                if (wpic.argb != IntPtr.Zero)
                    UnsafeNativeMethods.WebPPictureFree(ref wpic);
            }
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
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn WebP.GetVersion"); }
        }

        /// <summary>Compute PSNR, SSIM or LSIM distortion metric between two pictures. Warning: this function is rather CPU-intensive.</summary>
        /// <param name="source">Picture to measure</param>
        /// <param name="reference">Reference picture</param>
        /// <param name="metric_type">0 = PSNR, 1 = SSIM, 2 = LSIM</param>
        /// <returns>dB in the Y/U/V/Alpha/All order</returns>
        public float[] GetPictureDistortion(Bitmap source, Bitmap reference, int metric_type)
        {
            WebPPicture wpicSource = new WebPPicture();
            WebPPicture wpicReference = new WebPPicture();
            BitmapData sourceBmpData = null;
            BitmapData referenceBmpData = null;
            float[] result = new float[5];
            GCHandle pinnedResult = GCHandle.Alloc(result, GCHandleType.Pinned);

            try
            {
                if (source.Width != reference.Width || source.Height != reference.Height)
                    throw new Exception("Source and reference pictures don't have same dimension");

                // Setup the source picture data, allocating the bitmap, width and height
                sourceBmpData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                wpicSource = new WebPPicture();
                if (UnsafeNativeMethods.WebPPictureInitInternal(ref wpicSource) != 1)
                    throw new Exception("Can´t init WebPPictureInit");
                wpicSource.width = (int)source.Width;
                wpicSource.height = (int)source.Height;
                wpicSource.use_argb = 1;
                if (UnsafeNativeMethods.WebPPictureImportBGR(ref wpicSource, sourceBmpData.Scan0, sourceBmpData.Stride) != 1)
                    throw new Exception("Can´t allocate memory in WebPPictureImportBGR");

                // Setup the reference picture data, allocating the bitmap, width and height
                referenceBmpData = reference.LockBits(new Rectangle(0, 0, reference.Width, reference.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                wpicReference = new WebPPicture();
                if (UnsafeNativeMethods.WebPPictureInitInternal(ref wpicReference) != 1)
                    throw new Exception("Can´t init WebPPictureInit");
                wpicReference.width = (int)reference.Width;
                wpicReference.height = (int)reference.Height;
                wpicReference.use_argb = 1;
                if (UnsafeNativeMethods.WebPPictureImportBGR(ref wpicReference, referenceBmpData.Scan0, referenceBmpData.Stride) != 1)
                    throw new Exception("Can´t allocate memory in WebPPictureImportBGR");

                //Measure
                IntPtr ptrResult = pinnedResult.AddrOfPinnedObject();
                if (UnsafeNativeMethods.WebPPictureDistortion(ref wpicSource, ref wpicReference, metric_type, ptrResult) != 1)
                    throw new Exception("Can´t measure.");
                return result;
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn WebP.GetPictureDistortion"); }
            finally
            {
                //Unlock the pixels
                if (sourceBmpData != null)
                    source.UnlockBits(sourceBmpData);
                if (referenceBmpData != null)
                    reference.UnlockBits(referenceBmpData);

                //Free memory
                if (wpicSource.argb != IntPtr.Zero)
                    UnsafeNativeMethods.WebPPictureFree(ref wpicSource);
                if (wpicReference.argb != IntPtr.Zero)
                    UnsafeNativeMethods.WebPPictureFree(ref wpicReference);
                //Free memory
                if (pinnedResult.IsAllocated)
                    pinnedResult.Free();
            }
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
            GCHandle pinnedWebP = GCHandle.Alloc(rawWebP, GCHandleType.Pinned);

            try
            {
                IntPtr ptrRawWebP = pinnedWebP.AddrOfPinnedObject();

                WebPBitstreamFeatures features = new WebPBitstreamFeatures();
                result = UnsafeNativeMethods.WebPGetFeaturesInternal(ptrRawWebP, (uint)rawWebP.Length, ref features);
                if (result != 0)
                    throw new Exception(result.ToString());

                width = features.width;
                height = features.height;
                if (features.has_alpha == 1) has_alpha = true; else has_alpha = false;
                if (features.has_animation == 1) has_animation = true; else has_animation = false;
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
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\r\nIn WebP.GetInfo"); }
            finally
            {
                //Free memory
                if (pinnedWebP.IsAllocated)
                    pinnedWebP.Free();
            }
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

        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct MemoryWriter
        {
            public int size;                    // Size of webP data
            public byte[] data;                 // Data of WebP Image
        }
        #endregion

        #region | Destruction |
        /// <summary>Free memory</summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    #region | Import libwebp functions |
    [SuppressUnmanagedCodeSecurityAttribute]
    internal sealed partial class UnsafeNativeMethods
    {
        private static int WEBP_DECODER_ABI_VERSION = 0x0208;

        /// <summary>This function will initialize the configuration according to a predefined set of parameters (referred to by 'preset') and a given quality factor.</summary>
        /// <param name="config">The WebPConfig struct</param>
        /// <param name="preset">Type of image</param>
        /// <param name="quality">Quality of compresion</param>
        /// <returns>0 if error</returns>
        public static int WebPConfigInitInternal(ref WebPConfig config, WebPPreset preset, float quality)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPConfigInitInternal_x86(ref config,  preset,  quality, WEBP_DECODER_ABI_VERSION);
                case 8:
                    return WebPConfigInitInternal_x64(ref config, preset, quality, WEBP_DECODER_ABI_VERSION);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPConfigInitInternal")]
        private static extern int WebPConfigInitInternal_x86(ref WebPConfig config, WebPPreset preset, float quality, int WEBP_DECODER_ABI_VERSION);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPConfigInitInternal")]
        private static extern int WebPConfigInitInternal_x64(ref WebPConfig config, WebPPreset preset, float quality, int WEBP_DECODER_ABI_VERSION);

        /// <summary>Get info of WepP image</summary>
        /// <param name="rawWebP">Bytes[] of webp image</param>
        /// <param name="data_size">Size of rawWebP</param>
        /// <param name="features">Features of WebP image</param>
        /// <returns>VP8StatusCode</returns>
        public static VP8StatusCode WebPGetFeaturesInternal(IntPtr rawWebP, UInt32 data_size, ref WebPBitstreamFeatures features)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPGetFeaturesInternal_x86(rawWebP, data_size, ref features, WEBP_DECODER_ABI_VERSION);
                case 8:
                    return WebPGetFeaturesInternal_x64(rawWebP, data_size, ref features, WEBP_DECODER_ABI_VERSION);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetFeaturesInternal")]
        private static extern VP8StatusCode WebPGetFeaturesInternal_x86(IntPtr rawWebP, UInt32 data_size, ref WebPBitstreamFeatures features, int WEBP_DECODER_ABI_VERSION);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetFeaturesInternal")]
        private static extern VP8StatusCode WebPGetFeaturesInternal_x64(IntPtr rawWebP, UInt32 data_size, ref WebPBitstreamFeatures features, int WEBP_DECODER_ABI_VERSION);

        /// <summary>Activate the lossless compression mode with the desired efficiency.</summary>
        /// <param name="config">The WebPConfig struct</param>
        /// <param name="level">between 0 (fastest, lowest compression) and 9 (slower, best compression)</param>
        /// <returns>0 in case of parameter errorr</returns>
        public static int WebPConfigLosslessPreset(ref WebPConfig config, int level)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPConfigLosslessPreset_x86(ref config, level);
                case 8:
                    return WebPConfigLosslessPreset_x64(ref config, level);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPConfigLosslessPreset")]
        private static extern int WebPConfigLosslessPreset_x86(ref WebPConfig config, int level);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPConfigLosslessPreset")]
        private static extern int WebPConfigLosslessPreset_x64(ref WebPConfig config, int level);

        /// <summary>Check that 'config' is non-NULL and all configuration parameters are within their valid ranges.</summary>
        /// <param name="config">The WebPConfig struct</param>
        /// <returns>1 if config are OK</returns>
        public static int WebPValidateConfig(ref WebPConfig config)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPValidateConfig_x86(ref config);
                case 8:
                    return WebPValidateConfig_x64(ref config);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPValidateConfig")]
        private static extern int WebPValidateConfig_x86(ref WebPConfig config);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPValidateConfig")]
        private static extern int WebPValidateConfig_x64(ref WebPConfig config);

        /// <summary>Init the struct WebPPicture ckecking the dll version</summary>
        /// <param name="wpic">The WebPPicture struct</param>
        /// <param name="WEBP_DECODER_ABI_VERSION">Version of decoder</param>
        /// <returns>1 if not error</returns>
        public static int WebPPictureInitInternal(ref WebPPicture wpic)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPPictureInitInternal_x86(ref wpic, WEBP_DECODER_ABI_VERSION);
                case 8:
                    return WebPPictureInitInternal_x64(ref wpic, WEBP_DECODER_ABI_VERSION);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureInitInternal")]
        private static extern int WebPPictureInitInternal_x86(ref WebPPicture wpic, int WEBP_DECODER_ABI_VERSION);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureInitInternal")]
        private static extern int WebPPictureInitInternal_x64(ref WebPPicture wpic, int WEBP_DECODER_ABI_VERSION);

        /// <summary>Colorspace conversion function to import RGB samples.</summary>
        /// <param name="wpic">The WebPPicture struct</param>
        /// <param name="bgr">Point to BGR data</param>
        /// <param name="stride">stride of BGR data</param>
        /// <returns>Returns 0 in case of memory error.</returns>
        public static int WebPPictureImportBGR(ref WebPPicture wpic, IntPtr bgr, int stride)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPPictureImportBGR_x86(ref wpic, bgr, stride);
                case 8:
                    return WebPPictureImportBGR_x64(ref wpic, bgr, stride);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureImportBGR")]
        private static extern int WebPPictureImportBGR_x86(ref WebPPicture wpic, IntPtr bgr, int stride);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureImportBGR")]
        private static extern int WebPPictureImportBGR_x64(ref WebPPicture wpic, IntPtr bgr, int stride); 

        /// <summary>Colorspace conversion function to import RGB samples.</summary>
        /// <param name="wpic">The WebPPicture struct</param>
        /// <param name="bgr">Point to BGR data</param>
        /// <param name="stride">stride of BGR data</param>
        /// <returns>Returns 0 in case of memory error.</returns>
        public static int WebPPictureImportBGRX(ref WebPPicture wpic, IntPtr bgr, int stride)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPPictureImportBGRX_x86(ref wpic, bgr, stride);
                case 8:
                    return WebPPictureImportBGRX_x64(ref wpic, bgr, stride);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureImportBGRX")]
        private static extern int WebPPictureImportBGRX_x86(ref WebPPicture wpic, IntPtr bgr, int stride);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureImportBGRX")]
        private static extern int WebPPictureImportBGRX_x64(ref WebPPicture wpic, IntPtr bgr, int stride);

        /// <summary>The writer type for output compress data</summary>
        /// <param name="data">Data returned</param>
        /// <param name="data_size">Size of data returned</param>
        /// <param name="picture">Picture struct</param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int WebPMemoryWrite([InAttribute()] IntPtr data, UIntPtr data_size, ref WebPPicture wpic);
        public static WebPMemoryWrite OnCallback;

        /// <summary>Compress to webp format</summary>
        /// <param name="config">The config struct for compresion parameters</param>
        /// <param name="picture">'picture' hold the source samples in both YUV(A) or ARGB input</param>
        /// <returns>Returns 0 in case of error, 1 otherwise. In case of error, picture->error_code is updated accordingly.</returns>
        public static int WebPEncode(ref WebPConfig config, ref WebPPicture picture)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPEncode_x86(ref config, ref picture);
                case 8:
                    return WebPEncode_x64(ref config, ref picture);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncode")]
        private static extern int WebPEncode_x86(ref WebPConfig config, ref WebPPicture picture);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncode")]
        private static extern int WebPEncode_x64(ref WebPConfig config, ref WebPPicture picture);


        /// <summary>Release the memory allocated by WebPPictureAlloc() or WebPPictureImport*()
        /// Note that this function does _not_ free the memory used by the 'picture' object itself.
        /// Besides memory (which is reclaimed) all other fields of 'picture' are preserved.</summary>
        /// <param name="picture">Picture struct</param>
        public static void WebPPictureFree(ref WebPPicture picture)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    WebPPictureFree_x86(ref picture);
                    break;
                case 8:
                    WebPPictureFree_x64(ref picture);
                    break;
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureFree")]
        private static extern void WebPPictureFree_x86(ref WebPPicture wpic);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureFree")]
        private static extern void WebPPictureFree_x64(ref WebPPicture wpic);

        /// <summary>Validate the WebP image header and retrieve the image height and width. Pointers *width and *height can be passed NULL if deemed irrelevant</summary>
        /// <param name="data">Pointer to WebP image data</param>
        /// <param name="data_size">This is the size of the memory block pointed to by data containing the image data</param>
        /// <param name="width">The range is limited currently from 1 to 16383</param>
        /// <param name="height">The range is limited currently from 1 to 16383</param>
        /// <returns>1 if success, otherwise error code returned in the case of (a) formatting error(s).</returns>
        public static int WebPGetInfo(IntPtr data, UInt32 data_size, out int width, out int height)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPGetInfo_x86(data, data_size, out width, out height);
                case 8:
                    return WebPGetInfo_x64(data, data_size, out width, out height);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetInfo")]
        private static extern int WebPGetInfo_x86(IntPtr data, UInt32 data_size, out int width, out int height);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetInfo")]
        private static extern int WebPGetInfo_x64(IntPtr data, UInt32 data_size, out int width, out int height);

        /// <summary>Decode WEBP image pointed to by *data and returns BGR samples into a pre-allocated buffer</summary>
        /// <param name="data">Pointer to WebP image data</param>
        /// <param name="data_size">This is the size of the memory block pointed to by data containing the image data</param>
        /// <param name="output_buffer">Pointer to decoded WebP image</param>
        /// <param name="output_buffer_size">Size of allocated buffer</param>
        /// <param name="output_stride">Specifies the distance between scanlines</param>
        /// <returns>output_buffer if function succeeds; NULL otherwise</returns>
        public static int WebPDecodeBGRInto(IntPtr data, UInt32 data_size, IntPtr output_buffer, int output_buffer_size, int output_stride)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPDecodeBGRInto_x86(data, data_size, output_buffer, output_buffer_size, output_stride);
                case 8:
                    return WebPDecodeBGRInto_x64(data, data_size, output_buffer, output_buffer_size, output_stride);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDecodeBGRInto")]
        private static extern int WebPDecodeBGRInto_x86(IntPtr data, UInt32 data_size, IntPtr output_buffer, int output_buffer_size, int output_stride);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDecodeBGRInto")]
        private static extern int WebPDecodeBGRInto_x64(IntPtr data, UInt32 data_size, IntPtr output_buffer, int output_buffer_size, int output_stride);

        /// <summary>Lossy encoding images</summary>
        /// <param name="rgb">Pointer to RGB image data</param>
        /// <param name="width">The range is limited currently from 1 to 16383</param>
        /// <param name="height">The range is limited currently from 1 to 16383</param>
        /// <param name="output_stride">Specifies the distance between scanlines</param>
        /// <param name="quality_factor">Ranges from 0 (lower quality) to 100 (highest quality). Controls the loss and quality during compression</param>
        /// <param name="output">output_buffer with WebP image</param>
        /// <returns>Size of WebP Image or 0 if an error occurred</returns>
        public static int WebPEncodeBGR(IntPtr bgr, int width, int height, int stride, float quality_factor, out IntPtr output)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPEncodeBGR_x86(bgr, width, height, stride, quality_factor, out output);
                case 8:
                    return WebPEncodeBGR_x64(bgr, width, height, stride, quality_factor, out output);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeBGR")]
        private static extern int WebPEncodeBGR_x86(IntPtr bgr, int width, int height, int stride, float quality_factor, out IntPtr output);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeBGR")]
        private static extern int WebPEncodeBGR_x64(IntPtr bgr, int width, int height, int stride, float quality_factor, out IntPtr output);

        /// <summary>Lossless encoding images pointed to by *data in WebP format</summary>
        /// <param name="rgb">Pointer to RGB image data</param>
        /// <param name="width">The range is limited currently from 1 to 16383</param>
        /// <param name="height">The range is limited currently from 1 to 16383</param>
        /// <param name="output_stride">Specifies the distance between scanlines</param>
        /// <param name="output">output_buffer with WebP image</param>
        /// <returns>Size of WebP Image or 0 if an error occurred</returns>
        public static int WebPEncodeLosslessBGR(IntPtr bgr, int width, int height, int stride, out IntPtr output)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPEncodeLosslessBGR_x86(bgr, width, height, stride, out output);
                case 8:
                    return WebPEncodeLosslessBGR_x64(bgr, width, height, stride, out output);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeLosslessBGR")]
        private static extern int WebPEncodeLosslessBGR_x86(IntPtr bgr, int width, int height, int stride, out IntPtr output);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeLosslessBGR")]
        private static extern int WebPEncodeLosslessBGR_x64(IntPtr bgr, int width, int height, int stride, out IntPtr output);

        /// <summary>Releases memory returned by the WebPEncode</summary>
        /// <param name="p">Pointer to memory</param>
        public static void WebPFree(IntPtr p)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    WebPFree_x86(p);
                    break;
                case 8:
                    WebPFree_x64(p);
                    break;
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPFree")]
        private static extern void WebPFree_x86(IntPtr p);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPFree")]
        private static extern void WebPFree_x64(IntPtr p);

        /// <summary>Get the webp version library</summary>
        /// <returns>8bits for each of major/minor/revision packet in integer. E.g: v2.5.7 is 0x020507</returns>
        public static int WebPGetDecoderVersion()
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPGetDecoderVersion_x86();
                case 8:
                    return WebPGetDecoderVersion_x64();
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetDecoderVersion")]
        private static extern int WebPGetDecoderVersion_x86();
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetDecoderVersion")]
        private static extern int WebPGetDecoderVersion_x64();

        /// <summary>Compute PSNR, SSIM or LSIM distortion metric between two pictures.</summary>
        /// <param name="srcPicture">Picture to measure</param>
        /// <param name="refPicture">Reference picture</param>
        /// <param name="metric_type">0 = PSNR, 1 = SSIM, 2 = LSIM</param>
        /// <param name="result">dB in the Y/U/V/Alpha/All order</param>
        /// <returns>False in case of error (src and ref don't have same dimension, ...)</returns>
        public static int WebPPictureDistortion(ref WebPPicture srcPicture, ref WebPPicture refPicture, int metric_type, IntPtr pResult)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPPictureDistortion_x86(ref srcPicture, ref refPicture, metric_type, pResult);
                case 8:
                    return WebPPictureDistortion_x64(ref srcPicture, ref refPicture, metric_type, pResult);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureDistortion")]
        private static extern int WebPPictureDistortion_x86(ref WebPPicture srcPicture, ref WebPPicture refPicture, int metric_type, IntPtr pResult);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureDistortion")]
        private static extern int WebPPictureDistortion_x64(ref WebPPicture srcPicture, ref WebPPicture refPicture, int metric_type, IntPtr pResult);
    }
    #endregion

    #region | Predefined |
    /// <summary>
    /// Enumerate some predefined settings for WebPConfig, depending on the type of source picture. These presets are used when calling WebPConfigPreset().
    /// </summary>
    public enum WebPPreset
    {
        /// <summary>
        /// Default preset.
        /// </summary>
        WEBP_PRESET_DEFAULT = 0,
        /// <summary>
        /// Digital picture, like portrait, inner shot.
        /// </summary>
        WEBP_PRESET_PICTURE,
        /// <summary>
        /// Outdoor photograph, with natural lighting.
        /// </summary>
        WEBP_PRESET_PHOTO,
        /// <summary>
        /// Hand or line drawing, with high-contrast details.
        /// </summary>
        WEBP_PRESET_DRAWING,
        /// <summary>
        /// Small-sized colorful images.
        /// </summary>
        WEBP_PRESET_ICON,
        /// <summary>
        /// Text-like.
        /// </summary>
        WEBP_PRESET_TEXT
    };

    // Encoding error conditions.
    public enum WebPEncodingError
    {
        /// <summary>
        /// No error.
        /// </summary>
        VP8_ENC_OK = 0,
        /// <summary>
        /// Memory error allocating objects.
        /// </summary>
        VP8_ENC_ERROR_OUT_OF_MEMORY,
        /// <summary>
        /// Memory error while flushing bits.
        /// </summary>
        VP8_ENC_ERROR_BITSTREAM_OUT_OF_MEMORY,
        /// <summary>
        /// A  pointer parameter is NULL.
        /// </summary>
        VP8_ENC_ERROR_NULL_PARAMETER,
        /// <summary>
        /// Configuration is invalid.
        /// </summary>
        VP8_ENC_ERROR_INVALID_CONFIGURATION,
        /// <summary>
        /// Picture has invalid width/height.
        /// </summary>
        VP8_ENC_ERROR_BAD_DIMENSION,
        /// <summary>
        /// Partition is bigger than 512k.
        /// </summary>
        VP8_ENC_ERROR_PARTITION0_OVERFLOW, 
        /// <summary>
        /// Partition is bigger than 16M.
        /// </summary>
        VP8_ENC_ERROR_PARTITION_OVERFLOW,
        /// <summary>
        /// Error while flushing bytes.
        /// </summary>
        VP8_ENC_ERROR_BAD_WRITE,
        /// <summary>
        /// File is bigger than 4G.
        /// </summary>
        VP8_ENC_ERROR_FILE_TOO_BIG,
        /// <summary>
        /// Abort request by user.
        /// </summary>
        VP8_ENC_ERROR_USER_ABORT,
        /// <summary>
        /// List terminator. always last.
        /// </summary>
        VP8_ENC_ERROR_LAST,
    }

    // Enumeration of the status codes
    public enum VP8StatusCode
    {
        /// <summary>
        /// No error.
        /// </summary>
        VP8_STATUS_OK = 0,
        /// <summary>
        /// Memory error allocating objects.
        /// </summary>
        VP8_STATUS_OUT_OF_MEMORY,
        VP8_STATUS_INVALID_PARAM,
        VP8_STATUS_BITSTREAM_ERROR,
        /// <summary>
        /// Configuration is invalid.
        /// </summary>
        VP8_STATUS_UNSUPPORTED_FEATURE,
        VP8_STATUS_SUSPENDED,
        /// <summary>
        /// Abort request by user.
        /// </summary>
        VP8_STATUS_USER_ABORT,
        VP8_STATUS_NOT_ENOUGH_DATA,
    }
    #endregion

    #region | libwebp structs |
    // Features gathered from the bitstream
    [StructLayoutAttribute(LayoutKind.Sequential)]
    struct WebPBitstreamFeatures
    {
        public int width;                       // Width in pixels, as read from the bitstream.
        public int height;                      // Height in pixels, as read from the bitstream.
        public int has_alpha;                   // True if the bitstream contains an alpha channel.
        public int has_animation;               // True if the bitstream is an animation.
        public int format;                      // 0 = undefined (/mixed), 1 = lossy, 2 = lossless
        private int pad1;                       // padding for later use
        private int pad2;                       // padding for later use
        private int pad3;                       // padding for later use
        private int pad4;                       // padding for later use
        private int pad5;                       // padding for later use
    };

    // Compression parameters.
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPConfig
    {
        public int lossless;                // Lossless encoding (0=lossy(default), 1=lossless).
        public float quality;               // between 0 (smallest file) and 100 (biggest)
        public int method;                  // quality/speed trade-off (0=fast, 6=slower-better)
        public UInt32 image_hint;           // Hint for image type (lossless only for now).
        //Parameters related to lossy compression only.
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
        public int delta_palettization;     // reserved for future lossless feature
        private int pad1;                   // padding for later use
        private int pad2;                   // padding for later use
    };

    // Main exchange structure (input samples, output bytes, statistics)
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPPicture
    {
        /////////////// INPUT
        public int use_argb;                // Main flag for encoder selecting between ARGB or YUV input. Recommended to use ARGB input (*argb, argb_stride) for lossless, and YUV input (*y, *u, *v, etc.) for lossy
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

public enum WebPImageHint
{
    /// <summary>
    /// Default preset.
    /// </summary>
    WEBP_HINT_DEFAULT = 0,                  // default preset.
    WEBP_HINT_PICTURE,                      // digital picture, like portrait, inner shot
    WEBP_HINT_PHOTO,                        // outdoor photograph, with natural lighting
    WEBP_HINT_GRAPH,                        // Discrete tone image (graph, map-tile etc).
    WEBP_HINT_LAST                          // list terminator. always last.
};
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


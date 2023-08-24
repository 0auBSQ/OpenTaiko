using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FDK
{
    static class CVideoBitmap
    {
        #region ToBitmap

        /// <summary>
        /// Converts Mat to System.Drawing.Bitmap
        /// </summary>
        /// <param name="src">Mat</param>
        /// <returns></returns>
        public static Bitmap ToBitmap(this Mat src)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new NotSupportedException("Non-Windows OS are not supported");
            if (src is null)
                throw new ArgumentNullException(nameof(src));

            PixelFormat pf;
            switch (src.Channels())
            {
                case 1:
                    pf = PixelFormat.Format8bppIndexed; break;
                case 3:
                    pf = PixelFormat.Format24bppRgb; break;
                case 4:
                    pf = PixelFormat.Format32bppArgb; break;
                default:
                    throw new ArgumentException("Number of channels must be 1, 3 or 4.", nameof(src));
            }
            return ToBitmap(src, pf);
        }

        /// <summary>
        /// Converts Mat to System.Drawing.Bitmap
        /// </summary>
        /// <param name="src">Mat</param>
        /// <param name="pf">Pixel Depth</param>
        /// <returns></returns>
        public static Bitmap ToBitmap(this Mat src, PixelFormat pf)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new NotSupportedException("Non-Windows OS are not supported");
            if (src is null)
                throw new ArgumentNullException(nameof(src));
            src.ThrowIfDisposed();

            Bitmap bitmap = new Bitmap(src.Width, src.Height, pf);
            ToBitmap(src, bitmap);
            return bitmap;
        }

        /// <summary>
        /// Converts Mat to System.Drawing.Bitmap
        /// </summary>
        /// <param name="src">Mat</param>
        /// <param name="dst">Mat</param>
        /// <remarks>Author: shimat, Gummo (ROI support)</remarks>
        public static unsafe void ToBitmap(this Mat src, Bitmap dst)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new NotSupportedException("Non-Windows OS are not supported");
            if (src is null)
                throw new ArgumentNullException(nameof(src));
            if (dst is null)
                throw new ArgumentNullException(nameof(dst));
            if (src.IsDisposed)
                throw new ArgumentException("The image is disposed.", nameof(src));
            if (src.Depth() != MatType.CV_8U)
                throw new ArgumentException("Depth of the image must be CV_8U");
            //if (src.IsSubmatrix())
            //    throw new ArgumentException("Submatrix is not supported");
            if (src.Width != dst.Width || src.Height != dst.Height)
                throw new ArgumentException("");

            PixelFormat pf = dst.PixelFormat;

            // 1プレーン用の場合、グレースケールのパレット情報を生成する
            if (pf == PixelFormat.Format8bppIndexed)
            {
                ColorPalette plt = dst.Palette;
                for (int x = 0; x < 256; x++)
                {
                    plt.Entries[x] = Color.FromArgb(x, x, x);
                }
                dst.Palette = plt;
            }

            int w = src.Width;
            int h = src.Height;
            Rectangle rect = new Rectangle(0, 0, w, h);
            BitmapData bd = null;

            bool submat = src.IsSubmatrix();
            bool continuous = src.IsContinuous();

            try
            {
                bd = dst.LockBits(rect, ImageLockMode.WriteOnly, pf);

                IntPtr srcData = src.Data;
                byte* pSrc = (byte*)(srcData.ToPointer());
                byte* pDst = (byte*)(bd.Scan0.ToPointer());
                int ch = src.Channels();
                int srcStep = (int)src.Step();
                int dstStep = ((src.Width * ch) + 3) / 4 * 4; // 4の倍数に揃える
                int stride = bd.Stride;

                switch (pf)
                {
                    case PixelFormat.Format1bppIndexed:
                        {
                            if (submat)
                                throw new NotImplementedException("submatrix not supported");

                            // BitmapDataは4byte幅だが、IplImageは1byte幅
                            // 手作業で移し替える                 
                            //int offset = stride - (w / 8);
                            int x = 0;
                            byte b = 0;
                            for (int y = 0; y < h; y++)
                            {
                                for (int bytePos = 0; bytePos < stride; bytePos++)
                                {
                                    if (x < w)
                                    {
                                        for (int i = 0; i < 8; i++)
                                        {
                                            var mask = (byte)(0x80 >> i);
                                            if (x < w && pSrc[srcStep * y + x] == 0)
                                                b &= (byte)(mask ^ 0xff);
                                            else
                                                b |= mask;

                                            x++;
                                        }
                                        pDst[bytePos] = b;
                                    }
                                }
                                x = 0;
                                pDst += stride;
                            }
                            break;
                        }

                    case PixelFormat.Format8bppIndexed:
                    case PixelFormat.Format24bppRgb:
                    case PixelFormat.Format32bppArgb:
                        if (srcStep == dstStep && !submat && continuous)
                        {
                            long bytesToCopy = src.DataEnd.ToInt64() - src.Data.ToInt64();
                            Buffer.MemoryCopy(pSrc, pDst, bytesToCopy, bytesToCopy);
                        }
                        else
                        {
                            for (int y = 0; y < h; y++)
                            {
                                long offsetSrc = (y * srcStep);
                                long offsetDst = (y * dstStep);
                                long bytesToCopy = w * ch;
                                // 一列ごとにコピー
                                Buffer.MemoryCopy(pSrc + offsetSrc, pDst + offsetDst, bytesToCopy, bytesToCopy);
                            }
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
            finally
            {
                if (bd != null)
                    dst.UnlockBits(bd);
            }
        }
        #endregion
    }
}

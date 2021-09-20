/*
* Copyright (c) 2007-2009 SlimDX Group
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/
using SlimDX;
using SlimDX.Direct3D9;
using DXGI = SlimDX.DXGI;

namespace SampleFramework
{
    static class ConversionMethods
    {
        public static int GetDepthBits(Format format)
        {
            switch (format)
            {
                case Format.D32SingleLockable:
                case Format.D32:
                    return 32;

                case Format.D24X8:
                case Format.D24S8:
                case Format.D24X4S4:
                case Format.D24SingleS8:
                    return 24;

                case Format.D16Lockable:
                case Format.D16:
                    return 16;

                case Format.D15S1:
                    return 15;

                default:
                    return 0;
            }
        }

        public static int GetStencilBits(Format format)
        {
            switch (format)
            {
                case Format.D15S1:
                    return 1;

                case Format.D24X4S4:
                    return 4;

                case Format.D24S8:
                case Format.D24SingleS8:
                    return 8;

                default:
                    return 0;
            }
        }

        public static int GetColorBits(Format format)
        {
            switch (format)
            {
                case Format.R8G8B8:
                case Format.A8R8G8B8:
                case Format.A8B8G8R8:
                case Format.X8R8G8B8:
                    return 8;

                case Format.R5G6B5:
                case Format.X1R5G5B5:
                case Format.A1R5G5B5:
                    return 5;

                case Format.A4R4G4B4:
                case Format.X4R4G4B4:
                    return 4;

                case Format.R3G3B2:
                case Format.A8R3G3B2:
                    return 2;

                case Format.A2R10G10B10:
                case Format.A2B10G10R10:
                    return 10;

                case Format.A16B16G16R16:
                    return 16;

                default:
                    return 0;
            }
        }

        public static int GetColorBits(DXGI.Format format)
        {
            switch (format)
            {
                case SlimDX.DXGI.Format.R32G32B32A32_Float:
                case SlimDX.DXGI.Format.R32G32B32A32_SInt:
                case SlimDX.DXGI.Format.R32G32B32A32_Typeless:
                case SlimDX.DXGI.Format.R32G32B32A32_UInt:
                case SlimDX.DXGI.Format.R32G32B32_Float:
                case SlimDX.DXGI.Format.R32G32B32_SInt:
                case SlimDX.DXGI.Format.R32G32B32_Typeless:
                case SlimDX.DXGI.Format.R32G32B32_UInt:
                    return 32;

                case SlimDX.DXGI.Format.R16G16B16A16_Float:
                case SlimDX.DXGI.Format.R16G16B16A16_SInt:
                case SlimDX.DXGI.Format.R16G16B16A16_SNorm:
                case SlimDX.DXGI.Format.R16G16B16A16_Typeless:
                case SlimDX.DXGI.Format.R16G16B16A16_UInt:
                case SlimDX.DXGI.Format.R16G16B16A16_UNorm:
                    return 16;

                case SlimDX.DXGI.Format.R10G10B10A2_Typeless:
                case SlimDX.DXGI.Format.R10G10B10A2_UInt:
                case SlimDX.DXGI.Format.R10G10B10A2_UNorm:
                    return 10;

                case SlimDX.DXGI.Format.R8G8B8A8_SInt:
                case SlimDX.DXGI.Format.R8G8B8A8_SNorm:
                case SlimDX.DXGI.Format.R8G8B8A8_Typeless:
                case SlimDX.DXGI.Format.R8G8B8A8_UInt:
                case SlimDX.DXGI.Format.R8G8B8A8_UNorm:
                case SlimDX.DXGI.Format.R8G8B8A8_UNorm_SRGB:
                    return 8;

                case SlimDX.DXGI.Format.B5G5R5A1_UNorm:
                case SlimDX.DXGI.Format.B5G6R5_UNorm:
                    return 5;

                default:
                    return 0;
            }
        }

        public static MultisampleType ToDirect3D9(int type)
        {
            return (MultisampleType)type;
        }

        public static Format ToDirect3D9(DXGI.Format format)
        {
            switch (format)
            {
                case SlimDX.DXGI.Format.R8G8B8A8_UNorm:
                    return Format.A8R8G8B8;
                case SlimDX.DXGI.Format.B5G6R5_UNorm:
                    return Format.R5G6B5;
                case SlimDX.DXGI.Format.B5G5R5A1_UNorm:
                    return Format.A1R5G5B5;
                case SlimDX.DXGI.Format.A8_UNorm:
                    return Format.A8;
                case SlimDX.DXGI.Format.R10G10B10A2_UNorm:
                    return Format.A2B10G10R10;
                case SlimDX.DXGI.Format.B8G8R8A8_UNorm:
                    return Format.A8B8G8R8;
                case SlimDX.DXGI.Format.R16G16_UNorm:
                    return Format.G16R16;
                case SlimDX.DXGI.Format.R16G16B16A16_UNorm:
                    return Format.A16B16G16R16;
                case SlimDX.DXGI.Format.R16_Float:
                    return Format.R16F;
                case SlimDX.DXGI.Format.R16G16_Float:
                    return Format.G16R16F;
                case SlimDX.DXGI.Format.R16G16B16A16_Float:
                    return Format.A16B16G16R16F;
                case SlimDX.DXGI.Format.R32_Float:
                    return Format.R32F;
                case SlimDX.DXGI.Format.R32G32_Float:
                    return Format.G32R32F;
                case SlimDX.DXGI.Format.R32G32B32A32_Float:
                    return Format.A32B32G32R32F;
            }

            return Format.Unknown;
        }

        public static float ToFloat(Rational rational)
        {
            float denom = 1;
            if (rational.Denominator != 0)
                denom = rational.Denominator;
            return rational.Numerator / denom;
        }
    }
}

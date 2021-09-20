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
using System.Collections.Generic;
using System.Globalization;
using SlimDX.Direct3D9;

namespace SampleFramework
{
    class AdapterInfo9
    {
        public int AdapterOrdinal
        {
            get;
            set;
        }

        public AdapterDetails Details
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public List<DisplayMode> DisplayModes
        {
            get;
            private set;
        }

        public List<DeviceInfo9> Devices
        {
            get;
            private set;
        }

        public AdapterInfo9()
        {
            // create lists
            DisplayModes = new List<DisplayMode>();
            Devices = new List<DeviceInfo9>();
        }
    }

    class DeviceInfo9
    {
        public DeviceType DeviceType
        {
            get;
            set;
        }

        public Capabilities Capabilities
        {
            get;
            set;
        }

        public List<SettingsCombo9> DeviceSettings
        {
            get;
            private set;
        }

        public DeviceInfo9()
        {
            DeviceSettings = new List<SettingsCombo9>();
        }
    }

    class SettingsCombo9
    {
        public int AdapterOrdinal
        {
            get;
            set;
        }

        public DeviceType DeviceType
        {
            get;
            set;
        }

        public Format AdapterFormat
        {
            get;
            set;
        }

        public Format BackBufferFormat
        {
            get;
            set;
        }

        public bool Windowed
        {
            get;
            set;
        }

        public List<Format> DepthStencilFormats
        {
            get;
            internal set;
        }

        public List<MultisampleType> MultisampleTypes
        {
            get;
            private set;
        }

        public List<int> MultisampleQualities
        {
            get;
            private set;
        }

        public List<PresentInterval> PresentIntervals
        {
            get;
            private set;
        }

        public AdapterInfo9 AdapterInfo
        {
            get;
            set;
        }

        public DeviceInfo9 DeviceInfo
        {
            get;
            set;
        }

        public SettingsCombo9()
        {
            DepthStencilFormats = new List<Format>();
            MultisampleQualities = new List<int>();
            MultisampleTypes = new List<MultisampleType>();
            PresentIntervals = new List<PresentInterval>();
        }
    }

    class DisplayModeComparer9 : IComparer<DisplayMode>
    {
        static DisplayModeComparer9 comparer = new DisplayModeComparer9();

        public static DisplayModeComparer9 Comparer
        {
            get { return comparer; }
        }

        public DisplayModeComparer9()
        {
        }

        public int Compare(DisplayMode x, DisplayMode y)
        {
            if (x.Width > y.Width)
                return 1;
            if (x.Width < y.Width)
                return -1;
            if (x.Height > y.Height)
                return 1;
            if (x.Height < y.Height)
                return -1;
            if (x.Format > y.Format)
                return 1;
            if (x.Format < y.Format)
                return -1;
            if (x.RefreshRate > y.RefreshRate)
                return 1;
            if (x.RefreshRate < y.RefreshRate)
                return -1;

            return 0;
        }
    }

    static class Enumeration9
    {
        public static DeviceSettings MinimumSettings
        {
            get;
            set;
        }

        public static List<AdapterInfo9> Adapters
        {
            get;
            private set;
        }

        public static bool HasEnumerated
        {
            get;
            private set;
        }

        public static void Enumerate()
        {
            HasEnumerated = true;
            Adapters = new List<AdapterInfo9>();
            List<Format> adapterFormats = new List<Format>();
            Format[] allowedAdapterFormats = { Format.X8R8G8B8, Format.X1R5G5B5, Format.R5G6B5, 
                Format.A2R10G10B10 };

			foreach (AdapterInformation adapter in GraphicsDeviceManager.Direct3D9Object.Adapters)		//
            {
                AdapterInfo9 info = new AdapterInfo9();
                info.AdapterOrdinal = adapter.Adapter;
                info.Details = adapter.Details;

                adapterFormats.Clear();
                foreach (Format adapterFormat in allowedAdapterFormats)
                {
                    foreach (DisplayMode displayMode in adapter.GetDisplayModes(adapterFormat))
                    {
                        if (MinimumSettings != null)
                        {
                            if (displayMode.Width < MinimumSettings.BackBufferWidth ||
                                displayMode.Height < MinimumSettings.BackBufferHeight ||
                                displayMode.RefreshRate < MinimumSettings.RefreshRate)
                                continue;
                        }

                        info.DisplayModes.Add(displayMode);

                        if (!adapterFormats.Contains(displayMode.Format))
                            adapterFormats.Add(displayMode.Format);
                    }
                }

                if (!adapterFormats.Contains(adapter.CurrentDisplayMode.Format))
                    adapterFormats.Add(adapter.CurrentDisplayMode.Format);

                info.DisplayModes.Sort(DisplayModeComparer9.Comparer);

                EnumerateDevices(info, adapterFormats);

                if (info.Devices.Count > 0)
                    Adapters.Add(info);
            }

            bool unique = true;
            foreach (AdapterInfo9 adapter1 in Adapters)
            {
                foreach (AdapterInfo9 adapter2 in Adapters)
                {
                    if (adapter1 == adapter2)
                        continue;
                    if (adapter1.Details.Description == adapter2.Details.Description)
                    {
                        unique = false;
                        break;
                    }
                }

                if (!unique)
                    break;
            }

            foreach (AdapterInfo9 info in Adapters)
            {
                info.Description = info.Details.Description;
                if (!unique)
                    info.Description += " " + info.AdapterOrdinal.ToString(CultureInfo.CurrentCulture);
            }
        }

        static void EnumerateDevices(AdapterInfo9 info, List<Format> adapterFormats)
        {
            DeviceType[] deviceTypes = { DeviceType.Hardware, DeviceType.Reference };

            foreach (DeviceType deviceType in deviceTypes)
            {
                if (MinimumSettings != null && MinimumSettings.DeviceType != deviceType)
                    continue;

                DeviceInfo9 deviceInfo = new DeviceInfo9();
                deviceInfo.DeviceType = deviceType;
				try
				{
					deviceInfo.Capabilities = GraphicsDeviceManager.Direct3D9Object.GetDeviceCaps(info.AdapterOrdinal, deviceInfo.DeviceType);

					EnumerateSettingsCombos(info, deviceInfo, adapterFormats);

					if (deviceInfo.DeviceSettings.Count > 0)
						info.Devices.Add(deviceInfo);
				}
				catch (Direct3D9Exception)
				{
					// #23681 2010.11.17 yyagi: GetDeviceCaps()で例外が発生するモニタに対しては、enumerateをスキップする。
				}
            }
        }

        static void EnumerateSettingsCombos(AdapterInfo9 adapterInfo, DeviceInfo9 deviceInfo, List<Format> adapterFormats)
        {
            Format[] backBufferFormats = { Format.A8R8G8B8, Format.X8R8G8B8, Format.A2R10G10B10,
                Format.R5G6B5, Format.A1R5G5B5, Format.X1R5G5B5 };

            foreach (Format adapterFormat in adapterFormats)
            {
                foreach (Format backBufferFormat in backBufferFormats)
                {
                    for (int windowed = 0; windowed < 2; windowed++)
                    {
                        if (windowed == 0 && adapterInfo.DisplayModes.Count == 0)
                            continue;

                        if (!GraphicsDeviceManager.Direct3D9Object.CheckDeviceType(adapterInfo.AdapterOrdinal, deviceInfo.DeviceType,
                            adapterFormat, backBufferFormat, (windowed == 1)))
                            continue;

                        if (!GraphicsDeviceManager.Direct3D9Object.CheckDeviceFormat(adapterInfo.AdapterOrdinal,
                            deviceInfo.DeviceType, adapterFormat, Usage.QueryPostPixelShaderBlending,
                            ResourceType.Texture, backBufferFormat))
                            continue;

                        SettingsCombo9 combo = new SettingsCombo9();
                        combo.AdapterOrdinal = adapterInfo.AdapterOrdinal;
                        combo.DeviceType = deviceInfo.DeviceType;
                        combo.AdapterFormat = adapterFormat;
                        combo.BackBufferFormat = backBufferFormat;
                        combo.Windowed = (windowed == 1);
                        combo.AdapterInfo = adapterInfo;
                        combo.DeviceInfo = deviceInfo;

                        BuildDepthStencilFormatList(combo);
                        BuildMultisampleTypeList(combo);

                        if (combo.MultisampleTypes.Count == 0)
                            continue;

                        BuildPresentIntervalList(combo);

                        if (MinimumSettings != null)
                        {
                            if (MinimumSettings.BackBufferFormat != Format.Unknown &&
                                MinimumSettings.BackBufferFormat != backBufferFormat)
                                continue;

                            if (MinimumSettings.DepthStencilFormat != Format.Unknown &&
                                !combo.DepthStencilFormats.Contains(MinimumSettings.DepthStencilFormat))
                                continue;

                            if (!combo.MultisampleTypes.Contains(MinimumSettings.MultisampleType))
                                continue;
                        }

                        deviceInfo.DeviceSettings.Add(combo);
                    }
                }
            }
        }

        static void BuildDepthStencilFormatList(SettingsCombo9 combo)
        {
            List<Format> possibleDepthStencilFormats = new List<Format> {
                Format.D16,     Format.D15S1,   Format.D24X8,
                Format.D24S8,   Format.D24X4S4, Format.D32 };

            foreach (Format format in possibleDepthStencilFormats)
            {
                if (GraphicsDeviceManager.Direct3D9Object.CheckDeviceFormat(combo.AdapterOrdinal, combo.DeviceType, combo.AdapterFormat,
                    Usage.DepthStencil, ResourceType.Surface, format) &&
                    GraphicsDeviceManager.Direct3D9Object.CheckDepthStencilMatch(combo.AdapterOrdinal, combo.DeviceType,
                    combo.AdapterFormat, combo.BackBufferFormat, format))
                    combo.DepthStencilFormats.Add(format);
            }
        }

        static void BuildMultisampleTypeList(SettingsCombo9 combo)
        {
            List<MultisampleType> possibleMultisampleTypes = new List<MultisampleType>() {
                MultisampleType.None,               MultisampleType.NonMaskable,
                MultisampleType.TwoSamples,         MultisampleType.ThreeSamples,
                MultisampleType.FourSamples,        MultisampleType.FiveSamples,
                MultisampleType.SixSamples,         MultisampleType.SevenSamples,
                MultisampleType.EightSamples,       MultisampleType.NineSamples,
                MultisampleType.TenSamples,         MultisampleType.ElevenSamples,
                MultisampleType.TwelveSamples,      MultisampleType.ThirteenSamples,
                MultisampleType.FourteenSamples,    MultisampleType.FifteenSamples,
                MultisampleType.SixteenSamples
            };

            int quality;
            foreach (MultisampleType type in possibleMultisampleTypes)
            {
                if (GraphicsDeviceManager.Direct3D9Object.CheckDeviceMultisampleType(combo.AdapterOrdinal, combo.DeviceType,
                    combo.AdapterFormat, combo.Windowed, type, out quality))
                {
                    combo.MultisampleTypes.Add(type);
                    combo.MultisampleQualities.Add(quality);
                }
            }
        }

        static void BuildPresentIntervalList(SettingsCombo9 combo)
        {
            List<PresentInterval> possiblePresentIntervals = new List<PresentInterval>() {
                PresentInterval.Immediate,  PresentInterval.Default,
                PresentInterval.One,        PresentInterval.Two,
                PresentInterval.Three,      PresentInterval.Four
            };

            foreach (PresentInterval interval in possiblePresentIntervals)
            {
                if (combo.Windowed && (interval == PresentInterval.Two ||
                    interval == PresentInterval.Three || interval == PresentInterval.Four))
                    continue;

                if (interval == PresentInterval.Default ||
                    (combo.DeviceInfo.Capabilities.PresentationIntervals & interval) != 0)
                    combo.PresentIntervals.Add(interval);
            }
        }
    }
}

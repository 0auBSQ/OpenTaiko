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
using System;
using SlimDX.Direct3D9;
namespace SampleFramework
{
    /// <summary>
    /// Contains settings for creating a 3D device.
    /// </summary>
    public class DeviceSettings : ICloneable
    {
        /// <summary>
        /// Gets or sets the adapter ordinal.
        /// </summary>
        /// <value>The adapter ordinal.</value>
        public int AdapterOrdinal
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the type of the device.
        /// </summary>
        /// <value>The type of the device.</value>
        public DeviceType DeviceType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the refresh rate.
        /// </summary>
        /// <value>The refresh rate.</value>
        public int RefreshRate
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the width of the back buffer.
        /// </summary>
        /// <value>The width of the back buffer.</value>
        public int BackBufferWidth
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the height of the back buffer.
        /// </summary>
        /// <value>The height of the back buffer.</value>
        public int BackBufferHeight
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the back buffer format.
        /// </summary>
        /// <value>The back buffer format.</value>
        public Format BackBufferFormat
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the back buffer count.
        /// </summary>
        /// <value>The back buffer count.</value>
        public int BackBufferCount
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the device is windowed.
        /// </summary>
        /// <value><c>true</c> if windowed; otherwise, <c>false</c>.</value>
        public bool Windowed
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether VSync is enabled.
        /// </summary>
        /// <value><c>true</c> if VSync is enabled; otherwise, <c>false</c>.</value>
        public bool EnableVSync
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="DeviceSettings"/> is multithreaded.
        /// </summary>
        /// <value><c>true</c> if multithreaded; otherwise, <c>false</c>.</value>
        /// <remarks>This only has an effect for Direct3D9 devices.</remarks>
        public bool Multithreaded
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the multisample type.
        /// </summary>
        /// <value>The multisample type.</value>
        public MultisampleType MultisampleType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the multisample quality.
        /// </summary>
        /// <value>The multisample quality.</value>
        public int MultisampleQuality
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the depth stencil format.
        /// </summary>
        /// <value>The depth stencil format.</value>
        public Format DepthStencilFormat
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the Direct3D9 specific settings.
        /// </summary>
        /// <value>The Direct3D9 specific settings.</value>
        internal Direct3D9Settings Direct3D9
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceSettings"/> class.
        /// </summary>
        public DeviceSettings()
        {
            // set sane defaults
            DeviceType = DeviceType.Hardware;
            BackBufferFormat = Format.Unknown;
            BackBufferCount = 1;
            MultisampleType = MultisampleType.None;
            DepthStencilFormat = Format.Unknown;
            Windowed = true;
            EnableVSync = true;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public DeviceSettings Clone()
        {
            DeviceSettings result = new DeviceSettings();
            result.DeviceType = DeviceType;
            result.RefreshRate = RefreshRate;
            result.BackBufferCount = BackBufferCount;
            result.BackBufferFormat = BackBufferFormat;
            result.BackBufferHeight = BackBufferHeight;
            result.BackBufferWidth = BackBufferWidth;
            result.DepthStencilFormat = DepthStencilFormat;
            result.MultisampleQuality = MultisampleQuality;
            result.MultisampleType = MultisampleType;
            result.Windowed = Windowed;
            result.EnableVSync = EnableVSync;
            result.AdapterOrdinal = AdapterOrdinal;
            result.Multithreaded = Multithreaded;

            if (Direct3D9 != null)
                result.Direct3D9 = Direct3D9.Clone();

            return result;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// Finds valid device settings based upon the desired settings.
        /// </summary>
        /// <param name="settings">The desired settings.</param>
        /// <returns>The best valid device settings matching the input settings.</returns>
		public static DeviceSettings FindValidSettings( DeviceSettings settings )
		{
			try
			{
				GraphicsDeviceManager.EnsureD3D9();
			}
			catch( Exception e )
			{
				throw new NoCompatibleDevicesException( "Could not initialize Direct3D9.", e );
			}

			if( !Enumeration9.HasEnumerated )
				Enumeration9.Enumerate();

			DeviceSettings newSettings = settings.Clone();
			Direct3D9Settings d3d9 = FindValidD3D9Settings( settings );
			newSettings.Direct3D9 = d3d9;
			return newSettings;
		}

        static Direct3D9Settings FindValidD3D9Settings(DeviceSettings settings)
        {
            Direct3D9Settings optimal = Direct3D9Settings.BuildOptimalSettings(settings);

            SettingsCombo9 bestCombo = null;
            float bestRanking = -1.0f;

            foreach (AdapterInfo9 adapterInfo in Enumeration9.Adapters)
            {
                DisplayMode desktopMode = GraphicsDeviceManager.Direct3D9Object.GetAdapterDisplayMode(adapterInfo.AdapterOrdinal);
                foreach (DeviceInfo9 deviceInfo in adapterInfo.Devices)
                {
                    foreach (SettingsCombo9 combo in deviceInfo.DeviceSettings)
                    {
                        if (combo.Windowed && combo.AdapterFormat != desktopMode.Format)
                            continue;

                        float ranking = Direct3D9Settings.RankSettingsCombo(combo, optimal, desktopMode);
                        if (ranking > bestRanking)
                        {
                            bestCombo = combo;
                            bestRanking = ranking;
                        }
                    }
                }
            }

            if (bestCombo == null)
                throw new NoCompatibleDevicesException("No compatible Direct3D9 devices found.");

            return Direct3D9Settings.BuildValidSettings(bestCombo, optimal);
        }
    }
}

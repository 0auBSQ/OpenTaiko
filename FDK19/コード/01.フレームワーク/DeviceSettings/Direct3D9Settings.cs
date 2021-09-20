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
using System.Collections.Generic;
using SlimDX.Direct3D9;

namespace SampleFramework
{
	class Direct3D9Settings : ICloneable
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

		public CreateFlags CreationFlags
		{
			get;
			set;
		}

		public PresentParameters PresentParameters
		{
			get;
			private set;
		}

		public Direct3D9Settings()
		{
			PresentParameters = new PresentParameters();
			DeviceType = DeviceType.Hardware;
			PresentParameters.Windowed = true;
			AdapterFormat = Format.Unknown;
			CreationFlags = CreateFlags.HardwareVertexProcessing;
			PresentParameters.BackBufferFormat = Format.Unknown;
			PresentParameters.BackBufferCount = 1;
			PresentParameters.Multisample = MultisampleType.None;
			PresentParameters.SwapEffect = SwapEffect.Discard;
			PresentParameters.EnableAutoDepthStencil = true;
			PresentParameters.AutoDepthStencilFormat = Format.Unknown;
			PresentParameters.PresentFlags = PresentFlags.DiscardDepthStencil;
			PresentParameters.PresentationInterval = PresentInterval.Default;
		}

		public Direct3D9Settings Clone()
		{
			Direct3D9Settings clone = new Direct3D9Settings();
			clone.AdapterFormat = AdapterFormat;
			clone.AdapterOrdinal = AdapterOrdinal;
			clone.CreationFlags = CreationFlags;
			clone.DeviceType = DeviceType;
			clone.PresentParameters = PresentParameters.Clone();

			return clone;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public static Direct3D9Settings BuildOptimalSettings(DeviceSettings settings)
		{
			DisplayMode desktopMode = GraphicsDeviceManager.Direct3D9Object.GetAdapterDisplayMode(0);
			Direct3D9Settings optimal = new Direct3D9Settings();

			optimal.AdapterOrdinal = settings.AdapterOrdinal;
			optimal.DeviceType = settings.DeviceType;
			optimal.PresentParameters.Windowed = settings.Windowed;
			optimal.PresentParameters.BackBufferCount = settings.BackBufferCount;
			optimal.PresentParameters.Multisample = settings.MultisampleType;
			optimal.PresentParameters.MultisampleQuality = settings.MultisampleQuality;
			optimal.PresentParameters.FullScreenRefreshRateInHertz = settings.RefreshRate;

			if(settings.Multithreaded)
				optimal.CreationFlags |= CreateFlags.Multithreaded;

			if(optimal.PresentParameters.Windowed || ConversionMethods.GetColorBits(desktopMode.Format) >= 8)
				optimal.AdapterFormat = desktopMode.Format;
			else
				optimal.AdapterFormat = Format.X8R8G8B8;

			if(settings.BackBufferWidth == 0 || settings.BackBufferHeight == 0)
			{
				if(optimal.PresentParameters.Windowed)
				{
					optimal.PresentParameters.BackBufferWidth = 640;
					optimal.PresentParameters.BackBufferHeight = 480;
				}
				else
				{
					optimal.PresentParameters.BackBufferWidth = desktopMode.Width;
					optimal.PresentParameters.BackBufferHeight = desktopMode.Height;
				}
			}
			else
			{
				optimal.PresentParameters.BackBufferWidth = settings.BackBufferWidth;
				optimal.PresentParameters.BackBufferHeight = settings.BackBufferHeight;
			}

			if(settings.BackBufferFormat == Format.Unknown)
				optimal.PresentParameters.BackBufferFormat = optimal.AdapterFormat;
			else
				optimal.PresentParameters.BackBufferFormat = settings.BackBufferFormat;

			if(settings.DepthStencilFormat == Format.Unknown)
			{
				if(ConversionMethods.GetColorBits(optimal.PresentParameters.BackBufferFormat) >= 8)
					optimal.PresentParameters.AutoDepthStencilFormat = Format.D32;
				else
					optimal.PresentParameters.AutoDepthStencilFormat = Format.D16;
			}
			else
				optimal.PresentParameters.AutoDepthStencilFormat = settings.DepthStencilFormat;

			if(!settings.EnableVSync)
				optimal.PresentParameters.PresentationInterval = PresentInterval.Immediate;

			return optimal;
		}

		public static float RankSettingsCombo(SettingsCombo9 combo, Direct3D9Settings optimal, DisplayMode desktopMode)
		{
			float ranking = 0.0f;

			if(combo.AdapterOrdinal == optimal.AdapterOrdinal)
				ranking += 1000.0f;

			if(combo.DeviceType == optimal.DeviceType)
				ranking += 100.0f;

			if(combo.DeviceType == DeviceType.Hardware)
				ranking += 0.1f;

			if(combo.Windowed == optimal.PresentParameters.Windowed)
				ranking += 10.0f;

			if(combo.AdapterFormat == optimal.AdapterFormat)
				ranking += 1.0f;
			else
			{
				int bitDepthDelta = Math.Abs(ConversionMethods.GetColorBits(combo.AdapterFormat) -
					ConversionMethods.GetColorBits(optimal.AdapterFormat));
				float scale = Math.Max(0.9f - bitDepthDelta * 0.2f, 0.0f);
				ranking += scale;
			}

			if(!combo.Windowed)
			{
				bool match;
				if(ConversionMethods.GetColorBits(desktopMode.Format) >= 8)
					match = (combo.AdapterFormat == desktopMode.Format);
				else
					match = (combo.AdapterFormat == Format.X8R8G8B8);

				if(match)
					ranking += 0.1f;
			}

			if((optimal.CreationFlags & CreateFlags.HardwareVertexProcessing) != 0 &&
				(optimal.CreationFlags & CreateFlags.MixedVertexProcessing) != 0)
			{
				if((combo.DeviceInfo.Capabilities.DeviceCaps & DeviceCaps.HWTransformAndLight) != 0)
					ranking += 1.0f;
			}

			if((combo.DeviceInfo.Capabilities.DeviceCaps & DeviceCaps.HWTransformAndLight) != 0)
				ranking += 0.1f;

			foreach(DisplayMode displayMode in combo.AdapterInfo.DisplayModes)
			{
				if(displayMode.Format == combo.AdapterFormat &&
					displayMode.Width == optimal.PresentParameters.BackBufferWidth &&
					displayMode.Height == optimal.PresentParameters.BackBufferHeight)
				{
					ranking += 1.0f;
					break;
				}
			}

			if(combo.BackBufferFormat == optimal.PresentParameters.BackBufferFormat)
				ranking += 1.0f;
			else
			{
				int bitDepthDelta = Math.Abs(ConversionMethods.GetColorBits(combo.BackBufferFormat) -
					ConversionMethods.GetColorBits(optimal.PresentParameters.BackBufferFormat));
				float scale = Math.Max(0.9f - bitDepthDelta * 0.2f, 0.0f);
				ranking += scale;
			}

			if(combo.BackBufferFormat == combo.AdapterFormat)
				ranking += 0.1f;

			for(int i = 0; i < combo.MultisampleTypes.Count; i++)
			{
				MultisampleType type = combo.MultisampleTypes[i];
				int quality = combo.MultisampleQualities[i];

				if(type == optimal.PresentParameters.Multisample && quality == optimal.PresentParameters.MultisampleQuality)
				{
					ranking += 1.0f;
					break;
				}
			}

			if(combo.DepthStencilFormats.Contains(optimal.PresentParameters.AutoDepthStencilFormat))
				ranking += 1.0f;

			foreach(DisplayMode displayMode in combo.AdapterInfo.DisplayModes)
			{
				if(displayMode.Format == combo.AdapterFormat &&
					displayMode.RefreshRate == optimal.PresentParameters.FullScreenRefreshRateInHertz)
				{
					ranking += 1.0f;
					break;
				}
			}

			if(combo.PresentIntervals.Contains(optimal.PresentParameters.PresentationInterval))
				ranking += 1.0f;

			return ranking;
		}

		public static Direct3D9Settings BuildValidSettings(SettingsCombo9 combo, Direct3D9Settings input)
		{
			Direct3D9Settings settings = new Direct3D9Settings();

			settings.AdapterOrdinal = combo.AdapterOrdinal;
			settings.DeviceType = combo.DeviceType;
			settings.PresentParameters.Windowed = combo.Windowed;
			settings.AdapterFormat = combo.AdapterFormat;
			settings.PresentParameters.BackBufferFormat = combo.BackBufferFormat;
			settings.PresentParameters.SwapEffect = input.PresentParameters.SwapEffect;
			settings.PresentParameters.PresentFlags = input.PresentParameters.PresentFlags | PresentFlags.DiscardDepthStencil;

			settings.CreationFlags = input.CreationFlags;
			if((combo.DeviceInfo.Capabilities.DeviceCaps & DeviceCaps.HWTransformAndLight) == 0 &&
				((settings.CreationFlags & CreateFlags.HardwareVertexProcessing) != 0 ||
				(settings.CreationFlags & CreateFlags.MixedVertexProcessing) != 0))
			{
				settings.CreationFlags &= ~CreateFlags.HardwareVertexProcessing;
				settings.CreationFlags &= ~CreateFlags.MixedVertexProcessing;
				settings.CreationFlags |= CreateFlags.SoftwareVertexProcessing;
			}

			if((settings.CreationFlags & CreateFlags.HardwareVertexProcessing) == 0 &&
				(settings.CreationFlags & CreateFlags.MixedVertexProcessing) == 0 &&
				(settings.CreationFlags & CreateFlags.SoftwareVertexProcessing) == 0)
			{
				if((combo.DeviceInfo.Capabilities.DeviceCaps & DeviceCaps.HWTransformAndLight) != 0)
					settings.CreationFlags |= CreateFlags.HardwareVertexProcessing;
				else
					settings.CreationFlags |= CreateFlags.SoftwareVertexProcessing;
			}

			DisplayMode bestDisplayMode = FindValidResolution(combo, input);
			settings.PresentParameters.BackBufferWidth = bestDisplayMode.Width;
			settings.PresentParameters.BackBufferHeight = bestDisplayMode.Height;

			settings.PresentParameters.BackBufferCount = input.PresentParameters.BackBufferCount;
			if(settings.PresentParameters.BackBufferCount > 3)
				settings.PresentParameters.BackBufferCount = 3;
			if(settings.PresentParameters.BackBufferCount < 1)
				settings.PresentParameters.BackBufferCount = 1;

			if(input.PresentParameters.SwapEffect != SwapEffect.Discard)
			{
				settings.PresentParameters.Multisample = MultisampleType.None;
				settings.PresentParameters.MultisampleQuality = 0;
			}
			else
			{
				MultisampleType bestType = MultisampleType.None;
				int bestQuality = 0;

				for(int i = 0; i < combo.MultisampleTypes.Count; i++)
				{
					MultisampleType type = combo.MultisampleTypes[i];
					int quality = combo.MultisampleQualities[0];

					if(Math.Abs(type - input.PresentParameters.Multisample) < Math.Abs(bestType -
						input.PresentParameters.Multisample))
					{
						bestType = type;
						bestQuality = Math.Min(quality - 1, input.PresentParameters.MultisampleQuality);
					}
				}

				settings.PresentParameters.Multisample = bestType;
				settings.PresentParameters.MultisampleQuality = bestQuality;
			}

			List<int> rankings = new List<int>();
			int inputDepthBitDepth = ConversionMethods.GetDepthBits(input.PresentParameters.AutoDepthStencilFormat);
			int inputStencilBitDepth = ConversionMethods.GetStencilBits(input.PresentParameters.AutoDepthStencilFormat);

			foreach(Format format in combo.DepthStencilFormats)
			{
				int currentBitDepth = ConversionMethods.GetDepthBits(format);
				int currentStencilDepth = ConversionMethods.GetStencilBits(format);

				int ranking = Math.Abs(currentBitDepth - inputDepthBitDepth);
				ranking += Math.Abs(currentStencilDepth - inputStencilBitDepth);
				rankings.Add(ranking);
			}

			int bestRanking = int.MaxValue;
			foreach(int ranking in rankings)
			{
				if(ranking < bestRanking)
					bestRanking = ranking;
			}
			int bestIndex = rankings.IndexOf(bestRanking);

			if(bestIndex >= 0)
			{
				settings.PresentParameters.AutoDepthStencilFormat = combo.DepthStencilFormats[bestIndex];
				settings.PresentParameters.EnableAutoDepthStencil = true;
			}
			else
			{
				settings.PresentParameters.AutoDepthStencilFormat = Format.Unknown;
				settings.PresentParameters.EnableAutoDepthStencil = false;
			}

			if(combo.Windowed)
				settings.PresentParameters.FullScreenRefreshRateInHertz = 0;
			else
			{
				int match = input.PresentParameters.FullScreenRefreshRateInHertz;
				bestDisplayMode.RefreshRate = 0;
				if(match != 0)
				{
					bestRanking = 100000;
					foreach(DisplayMode displayMode in combo.AdapterInfo.DisplayModes)
					{
						if(displayMode.Format != combo.AdapterFormat ||
							displayMode.Width != bestDisplayMode.Width ||
							displayMode.Height != bestDisplayMode.Height)
							continue;

						int ranking = Math.Abs(displayMode.RefreshRate - match);

						if(ranking < bestRanking)
						{
							bestDisplayMode.RefreshRate = displayMode.RefreshRate;
							bestRanking = ranking;

							if(bestRanking == 0)
								break;
						}
					}
				}

				settings.PresentParameters.FullScreenRefreshRateInHertz = bestDisplayMode.RefreshRate;
			}

			if(combo.PresentIntervals.Contains(input.PresentParameters.PresentationInterval))
				settings.PresentParameters.PresentationInterval = input.PresentParameters.PresentationInterval;
			else
				settings.PresentParameters.PresentationInterval = PresentInterval.Default;

			return settings;
		}

		static DisplayMode FindValidResolution(SettingsCombo9 combo, Direct3D9Settings input)
		{
			DisplayMode bestMode = new DisplayMode();

			if(combo.Windowed)
			{
				bestMode.Width = input.PresentParameters.BackBufferWidth;
				bestMode.Height = input.PresentParameters.BackBufferHeight;
				return bestMode;
			}

			int bestRanking = 100000;
			int ranking;
			foreach(DisplayMode mode in combo.AdapterInfo.DisplayModes)
			{
				if(mode.Format != combo.AdapterFormat)
					continue;

				ranking = Math.Abs(mode.Width - input.PresentParameters.BackBufferWidth) +
					Math.Abs(mode.Height - input.PresentParameters.BackBufferHeight);

				if(ranking < bestRanking)
				{
					bestMode = mode;
					bestRanking = ranking;

					if(bestRanking == 0)
						break;
				}
			}

			if(bestMode.Width == 0)
			{
				bestMode.Width = input.PresentParameters.BackBufferWidth;
				bestMode.Height = input.PresentParameters.BackBufferHeight;
			}

			return bestMode;
		}
	}
}

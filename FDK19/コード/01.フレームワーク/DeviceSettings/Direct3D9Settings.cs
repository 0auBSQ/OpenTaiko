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
using SharpDX.Direct3D9;

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
			set;
		}

		public Direct3D9Settings()
		{
			DeviceType = DeviceType.Hardware;
			AdapterFormat = Format.Unknown;
			CreationFlags = CreateFlags.HardwareVertexProcessing;

			var pp = new PresentParameters();
			pp.Windowed = true;
			pp.BackBufferFormat = Format.Unknown;
			pp.BackBufferCount = 1;
			pp.MultiSampleType = MultisampleType.None;
			pp.SwapEffect = SwapEffect.Discard;
			pp.EnableAutoDepthStencil = true;
			pp.AutoDepthStencilFormat = Format.Unknown;
			pp.PresentFlags = PresentFlags.DiscardDepthStencil;
			pp.PresentationInterval = PresentInterval.Default;

			this.PresentParameters = pp;
		}

		public Direct3D9Settings Clone()
		{
			Direct3D9Settings clone = new Direct3D9Settings();
			clone.AdapterFormat = AdapterFormat;
			clone.AdapterOrdinal = AdapterOrdinal;
			clone.CreationFlags = CreationFlags;
			clone.DeviceType = DeviceType;
			clone.PresentParameters = PresentParameters;

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
			var pp = optimal.PresentParameters;

			optimal.AdapterOrdinal = settings.AdapterOrdinal;
			optimal.DeviceType = settings.DeviceType;
			pp.Windowed = settings.Windowed;
			pp.BackBufferCount = settings.BackBufferCount;
			pp.MultiSampleType = settings.MultisampleType;
			pp.MultiSampleQuality = settings.MultisampleQuality;
			pp.FullScreenRefreshRateInHz = settings.RefreshRate;

			if (settings.Multithreaded)
				optimal.CreationFlags |= CreateFlags.Multithreaded;

			if(optimal.PresentParameters.Windowed || ConversionMethods.GetColorBits(desktopMode.Format) >= 8)
				optimal.AdapterFormat = desktopMode.Format;
			else
				optimal.AdapterFormat = Format.X8R8G8B8;

			if(settings.BackBufferWidth == 0 || settings.BackBufferHeight == 0)
			{
				if(optimal.PresentParameters.Windowed)
				{
					pp.BackBufferWidth = 640;
					pp.BackBufferHeight = 480;
				}
				else
				{
					pp.BackBufferWidth = desktopMode.Width;
					pp.BackBufferHeight = desktopMode.Height;
				}
			}
			else
			{
				pp.BackBufferWidth = settings.BackBufferWidth;
				pp.BackBufferHeight = settings.BackBufferHeight;
			}

			if(settings.BackBufferFormat == Format.Unknown)
				pp.BackBufferFormat = optimal.AdapterFormat;
			else
				pp.BackBufferFormat = settings.BackBufferFormat;

			if(settings.DepthStencilFormat == Format.Unknown)
			{
				if(ConversionMethods.GetColorBits(optimal.PresentParameters.BackBufferFormat) >= 8)
					pp.AutoDepthStencilFormat = Format.D32;
				else
					pp.AutoDepthStencilFormat = Format.D16;
			}
			else
				pp.AutoDepthStencilFormat = settings.DepthStencilFormat;

			if(!settings.EnableVSync)
				pp.PresentationInterval = PresentInterval.Immediate;

			optimal.PresentParameters = pp;
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

				if(type == optimal.PresentParameters.MultiSampleType && quality == optimal.PresentParameters.MultiSampleQuality)
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
					displayMode.RefreshRate == optimal.PresentParameters.FullScreenRefreshRateInHz)
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
			var pp = settings.PresentParameters;

			settings.AdapterOrdinal = combo.AdapterOrdinal;
			settings.DeviceType = combo.DeviceType;
			settings.AdapterFormat = combo.AdapterFormat;

			pp.Windowed = combo.Windowed;
			pp.BackBufferFormat = combo.BackBufferFormat;
			pp.SwapEffect = input.PresentParameters.SwapEffect;
			pp.PresentFlags = input.PresentParameters.PresentFlags | PresentFlags.DiscardDepthStencil;

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
			pp.BackBufferWidth = bestDisplayMode.Width;
			pp.BackBufferHeight = bestDisplayMode.Height;

			pp.BackBufferCount = input.PresentParameters.BackBufferCount;
			if(pp.BackBufferCount > 3)
				pp.BackBufferCount = 3;
			if(pp.BackBufferCount < 1)
				pp.BackBufferCount = 1;

			if(input.PresentParameters.SwapEffect != SwapEffect.Discard)
			{
				pp.MultiSampleType = MultisampleType.None;
				pp.MultiSampleQuality = 0;
			}
			else
			{
				MultisampleType bestType = MultisampleType.None;
				int bestQuality = 0;

				for(int i = 0; i < combo.MultisampleTypes.Count; i++)
				{
					MultisampleType type = combo.MultisampleTypes[i];
					int quality = combo.MultisampleQualities[i];

					if(Math.Abs(type - input.PresentParameters.MultiSampleType) < Math.Abs(bestType -
						input.PresentParameters.MultiSampleType))
					{
						bestType = type;
						bestQuality = Math.Min(quality - 1, input.PresentParameters.MultiSampleQuality);
					}
				}

				pp.MultiSampleType = bestType;
				pp.MultiSampleQuality = bestQuality;
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
				pp.AutoDepthStencilFormat = combo.DepthStencilFormats[bestIndex];
				pp.EnableAutoDepthStencil = true;
			}
			else
			{
				pp.AutoDepthStencilFormat = Format.Unknown;
				pp.EnableAutoDepthStencil = false;
			}

			if(combo.Windowed)
				pp.FullScreenRefreshRateInHz = 0;
			else
			{
				int match = input.PresentParameters.FullScreenRefreshRateInHz;
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

				pp.FullScreenRefreshRateInHz = bestDisplayMode.RefreshRate;
			}

			if(combo.PresentIntervals.Contains(input.PresentParameters.PresentationInterval))
				pp.PresentationInterval = input.PresentParameters.PresentationInterval;
			else
				pp.PresentationInterval = PresentInterval.Default;

			settings.PresentParameters = pp;
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

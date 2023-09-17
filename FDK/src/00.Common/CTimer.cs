using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace FDK
{
	public class CTimer : CTimerBase
	{
		public enum TimerType
		{
			Unknown = -1,
			PerformanceCounter = 0,
			MultiMedia = 1,
			GetTickCount = 2,
		}
		public TimerType CurrentTimerType
		{
			get;
			protected set;
		}


		public override long SystemTimeMs
		{
			get
			{
				/*
				switch( this.eタイマ種別 )
				{
					case E種別.PerformanceCounter:
						{
							double num = 0.0;
							if( this.n現在の周波数 != 0L )
							{
								long x = 0L;
								QueryPerformanceCounter( ref x );
								num = ( (double) x ) / ( ( (double) this.n現在の周波数 ) / 1000.0 );
							}
							return (long) num;
						}
					case E種別.MultiMedia:
						return (long) timeGetTime();

					case E種別.GetTickCount:
						return (long) Environment.TickCount;
				}
				return 0;
				*/
				return SampleFramework.Game.TimeMs;
			}
		}

		public CTimer( TimerType timerType )
			:base()
		{
			this.CurrentTimerType = timerType;

			/*
			if( n参照カウント[ (int) this.eタイマ種別 ] == 0 )
			{
				switch( this.eタイマ種別 )
				{
					case E種別.PerformanceCounter:
						if( !this.b確認と設定_PerformanceCounter() && !this.b確認と設定_MultiMedia() )
							this.b確認と設定_GetTickCount();
						break;

					case E種別.MultiMedia:
						if( !this.b確認と設定_MultiMedia() && !this.b確認と設定_PerformanceCounter() )
							this.b確認と設定_GetTickCount();
						break;

					case E種別.GetTickCount:
						this.b確認と設定_GetTickCount();
						break;

					default:
						throw new ArgumentException( string.Format( "未知のタイマ種別です。[{0}]", this.eタイマ種別 ) );
				}
			}
			*/
	
			base.Reset();

			ReferenceCount[ (int) this.CurrentTimerType ]++;
		}
		
		public override void Dispose()
		{
			if( this.CurrentTimerType == TimerType.Unknown )
				return;

			int type = (int) this.CurrentTimerType;

			ReferenceCount[ type ] = Math.Max( ReferenceCount[ type ] - 1, 0 );

			if( ReferenceCount[ type ] == 0 )
			{
				/*
				if( this.eタイマ種別 == E種別.MultiMedia )
					timeEndPeriod( this.timeCaps.wPeriodMin );
				*/
			}

			this.CurrentTimerType = TimerType.Unknown;
		}

		#region [ protected ]
		//-----------------
		protected long CurrentFrequency;
		protected static int[] ReferenceCount = new int[ 3 ];
		//protected TimeCaps timeCaps;

		protected bool GetSetTickCount()
		{
			this.CurrentTimerType = TimerType.GetTickCount;
			return true;
		}
		/*
		protected bool b確認と設定_MultiMedia()
		{
			this.timeCaps = new TimeCaps();
			if( ( timeGetDevCaps( out this.timeCaps, (uint) Marshal.SizeOf( typeof( TimeCaps ) ) ) == 0 ) && ( this.timeCaps.wPeriodMin < 10 ) )
			{
				this.eタイマ種別 = E種別.MultiMedia;
				timeBeginPeriod( this.timeCaps.wPeriodMin );
				return true;
			}
			return false;
		}
		protected bool b確認と設定_PerformanceCounter()
		{
			if( QueryPerformanceFrequency( ref this.n現在の周波数 ) != 0 )
			{
				this.eタイマ種別 = E種別.PerformanceCounter;
				return true;
			}
			return false;
		}
		*/
		//-----------------
		#endregion

		#region [ DllImport ]
		//-----------------
		/*
		[DllImport( "kernel32.dll" )]
		protected static extern short QueryPerformanceCounter( ref long x );
		[DllImport( "kernel32.dll" )]
		protected static extern short QueryPerformanceFrequency( ref long x );
		[DllImport( "winmm.dll" )]
		protected static extern void timeBeginPeriod( uint x );
		[DllImport( "winmm.dll" )]
		protected static extern void timeEndPeriod( uint x );
		[DllImport( "winmm.dll" )]
		protected static extern uint timeGetDevCaps( out TimeCaps timeCaps, uint size );
		[DllImport( "winmm.dll" )]
		protected static extern uint timeGetTime();

		[StructLayout( LayoutKind.Sequential )]
		protected struct TimeCaps
		{
			public uint wPeriodMin;
			public uint wPeriodMax;
		}
		*/
		//-----------------
		#endregion
	}
}

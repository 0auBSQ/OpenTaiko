/*
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using ManagedBass;
//using Un4seen.BassAsio;
//using Un4seen.BassWasapi;
//using Un4seen.Bass.AddOn.Mix;
using ManagedBass.Fx;

namespace FDK
{
	public class CBeatDetect : IDisposable
	{
		public struct stBeatPos
		{
			public float fBeatTime;
			public int n小節番号;
			public int nGrid;
			public int n小節内Grid;
			public bool b無効;					// 
			public bool bレーン表示する;		// 未使用

			public stBeatPos( float _fBeatTime, int _n小節番号, int _nGrid, int _n小節内Grid, bool _b無効, bool _bレーン表示する )
			{
				fBeatTime = _fBeatTime;
				n小節番号 = _n小節番号;
				nGrid = _nGrid;
				n小節内Grid = _n小節内Grid;
				b無効 = _b無効;
				bレーン表示する= _bレーン表示する;
			}
		}

		#region [ コンストラクタ ]
		public CBeatDetect()
		{
			Initialize();
		}
		public CBeatDetect( string _filename )
		{
			this.filename = _filename;
			Initialize();
		}
		#endregion
		#region [ 初期化(コンストラクタから呼び出される) ]
		private void Initialize()
		{
			if ( this.listBeatPositions == null )
			{
				this.listBeatPositions = new List<stBeatPos>();
			}

			#region [ BASS の設定。]
			//this.bIsBASSFree = true;
			//Debug.Assert( Bass.BASS_SetConfig( BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0 ),		// 0:BASSストリームの自動更新を行わない。(サウンド出力しないため)
			//    string.Format( "BASS_SetConfig() に失敗しました。[{0}", Bass.BASS_ErrorGetCode() ) );
			#endregion
			#region [ BASS の初期化。]
			int nデバイス = 0;		// 0:"no sound" … BASS からはデバイスへアクセスさせない。
			int n周波数 = 44100;	// 仮決め。lデバイス（≠ドライバ）がネイティブに対応している周波数であれば何でもいい？ようだ。いずれにしろBASSMXで自動的にリサンプリングされる。
			if ( !Bass.Init( nデバイス, n周波数, DeviceInitFlags.Default, IntPtr.Zero ) )
				throw new Exception( string.Format( "BASS の初期化に失敗しました。(BASS_Init)[{0}]", Bass.LastError.ToString() ) );
			#endregion

			#region [ 指定されたサウンドファイルをBASSでオープンし、必要最小限の情報を取得する。]
			//this.hBassStream = Bass.BASS_StreamCreateFile( this.filename, 0, 0, BASSFlag.BASS_STREAM_PRESCAN | BASSFlag.BASS_STREAM_DECODE );
			this.hBassStream = Bass.CreateStream( this.filename, 0, 0, BassFlags.Decode );
			if ( this.hBassStream == 0 )
				throw new Exception( string.Format( "{0}: サウンドストリームの生成に失敗しました。(BASS_StreamCreateFile)[{1}]", filename, Bass.LastError.ToString() ) );

			this.nTotalBytes = Bass.ChannelGetLength( this.hBassStream );

			this.nTotalSeconds = Bass.ChannelBytes2Seconds( this.hBassStream, nTotalBytes );
			if ( !Bass.ChannelGetAttribute( this.hBassStream, ChannelAttribute.Frequency, out float fFreq ) )
			{
				string errmes = string.Format( "サウンドストリームの周波数取得に失敗しました。(BASS_ChannelGetAttribute)[{0}]", Bass.LastError.ToString() );
				Bass.Free();
				throw new Exception( errmes );
			}
			#endregion
		}
		#endregion

		/// <summary>
		/// 曲全体のテンポを取得する
		/// </summary>
		/// <returns>テンポ値</returns>
		/// <remarks>テンポ値の範囲は70-300</remarks>
		public float GetTempo()
		{
			fTempo = BassFx.BASS_FX_BPM_DecodeGet(
				this.hBassStream,
				0,
				nTotalSeconds,
				( 300 << 16 ) + 70,		// MAX BPM=320, MIN BPM=70
				//0,
				BASSFXBpm.BASS_FX_BPM_DEFAULT,		//BASSFXBpm.BASS_FX_BPM_MULT2,
				null,
				IntPtr.Zero );
			return fTempo;
		}
		/// <summary>
		/// 曲の一部分のテンポを取得する
		/// </summary>
		/// <param name="startSec">開始位置</param>
		/// <param name="endSec">終了位置</param>
		/// <returns>テンポ値</returns>
		/// <remarks>テンポ値の範囲は70-300</remarks>
		public float GetTempo( double startSec, double endSec )
		{
			fTempo = BassFx.BASS_FX_BPM_DecodeGet(
				this.hBassStream,
				startSec,
				endSec,
				( 300 << 16 ) + 70,		// MAX BPM=320, MIN BPM=70
				//0,
				BASSFXBpm.BASS_FX_BPM_DEFAULT,		//BASSFXBpm.BASS_FX_BPM_MULT2,
				null,
				IntPtr.Zero );
			return fTempo;
		}


		/// <summary>
		/// Beatの検出位置をListで返す
		/// </summary>
		/// <returns>Beat検出位置群</returns>
		public List<stBeatPos> GetBeatPositions()
		{
			#region [  BeatPosition格納リストの初期化 ]
			if ( this.listBeatPositions != null )
			{
				this.listBeatPositions.Clear();
			}
			else
			{
				this.listBeatPositions = new List<stBeatPos>();
			}
			#endregion

			BPMBEATPROC _beatProc = new BPMBEATPROC( GetBeat_ProgressCallback );

			bool ret = BassFx.BASS_FX_BPM_BeatDecodeGet(
				this.hBassStream,
				0,
				nTotalSeconds,
				//0,
				BASSFXBpm.BASS_FX_BPM_DEFAULT,		//BASSFXBpm.BASS_FX_BPM_MULT2,
				_beatProc,
				IntPtr.Zero );

			return this.listBeatPositions;
		}

		private void GetBeat_ProgressCallback( int channel, double beatpos, IntPtr user )
		{
			stBeatPos sbp = new stBeatPos(
				(float) beatpos,
				0,
				0,
				0,
				false,
				true
			);				
				

			listBeatPositions.Add( sbp );
//			Debug.WriteLine( "Beat at: " + beatpos.ToString() );
		}


	
		
		public void Dispose()	// 使い終わったら必ずDispose()すること。BASSのリソースを握りっぱなしにすると、他の再生に不都合が生じるため。
		{
			BassFx.BASS_FX_BPM_Free( this.hBassStream );
			Bass.BASS_StreamFree( this.hBassStream );
			this.hBassStream = -1;
			Bass.BASS_Free();
		}
	
		// =============
		private string filename = "";
		private int hBassStream = -1;
		private long nTotalBytes = 0;
		private double nTotalSeconds = 0.0f;
		private float fFreq = 0.0f;
		private float fTempo;
		private List<stBeatPos> listBeatPositions = null;
	}
}
*/
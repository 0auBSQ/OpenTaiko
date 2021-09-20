using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using Un4seen.Bass;
//using Un4seen.BassAsio;
//using Un4seen.BassWasapi;
//using Un4seen.Bass.AddOn.Mix;
using Un4seen.Bass.AddOn.Fx;

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

			#region [ BASS registration ]
			// BASS.NET ユーザ登録（BASSスプラッシュが非表示になる）。

			BassNet.Registration( "dtx2013@gmail.com", "2X9181017152222" );
			#endregion
			#region [ BASS Version Check ]
			// BASS のバージョンチェック。
			int nBASSVersion = Utils.HighWord( Bass.BASS_GetVersion() );
			if ( nBASSVersion != Bass.BASSVERSION )
				throw new DllNotFoundException( string.Format( "bass.dll のバージョンが異なります({0})。このプログラムはバージョン{1}で動作します。", nBASSVersion, Bass.BASSVERSION ) );

			int nBASSFXVersion = Utils.HighWord( BassFx.BASS_FX_GetVersion() );
			if ( nBASSFXVersion != BassFx.BASSFXVERSION )
				throw new DllNotFoundException( string.Format( "bass_fx.dll のバージョンが異なります({0})。このプログラムはバージョン{1}で動作します。", nBASSFXVersion, BassFx.BASSFXVERSION ) );
			#endregion

			#region [ BASS の設定。]
			//this.bIsBASSFree = true;
			//Debug.Assert( Bass.BASS_SetConfig( BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0 ),		// 0:BASSストリームの自動更新を行わない。(サウンド出力しないため)
			//    string.Format( "BASS_SetConfig() に失敗しました。[{0}", Bass.BASS_ErrorGetCode() ) );
			#endregion
			#region [ BASS の初期化。]
			int nデバイス = 0;		// 0:"no sound" … BASS からはデバイスへアクセスさせない。
			int n周波数 = 44100;	// 仮決め。lデバイス（≠ドライバ）がネイティブに対応している周波数であれば何でもいい？ようだ。いずれにしろBASSMXで自動的にリサンプリングされる。
			if ( !Bass.BASS_Init( nデバイス, n周波数, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero ) )
				throw new Exception( string.Format( "BASS の初期化に失敗しました。(BASS_Init)[{0}]", Bass.BASS_ErrorGetCode().ToString() ) );
			#endregion

			#region [ 指定されたサウンドファイルをBASSでオープンし、必要最小限の情報を取得する。]
			//this.hBassStream = Bass.BASS_StreamCreateFile( this.filename, 0, 0, BASSFlag.BASS_STREAM_PRESCAN | BASSFlag.BASS_STREAM_DECODE );
			this.hBassStream = Bass.BASS_StreamCreateFile( this.filename, 0, 0, BASSFlag.BASS_STREAM_DECODE );
			if ( this.hBassStream == 0 )
				throw new Exception( string.Format( "{0}: サウンドストリームの生成に失敗しました。(BASS_StreamCreateFile)[{1}]", filename, Bass.BASS_ErrorGetCode().ToString() ) );

			this.nTotalBytes = Bass.BASS_ChannelGetLength( this.hBassStream );

			this.nTotalSeconds = Bass.BASS_ChannelBytes2Seconds( this.hBassStream, nTotalBytes );
			if ( !Bass.BASS_ChannelGetAttribute( this.hBassStream, BASSAttribute.BASS_ATTRIB_FREQ, ref fFreq ) )
			{
				string errmes = string.Format( "サウンドストリームの周波数取得に失敗しました。(BASS_ChannelGetAttribute)[{0}]", Bass.BASS_ErrorGetCode().ToString() );
				Bass.BASS_Free();
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

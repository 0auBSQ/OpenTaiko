using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using FDK;

namespace TJAPlayer3
{
	public class CInvisibleChip : IDisposable
	{
		/// <summary>ミス後表示する時間(ms)</summary>
		public int nDisplayTimeMs
		{
			get;
			set;
		}
		/// <summary>表示期間終了後、フェードアウトする時間</summary>
		public int nFadeoutTimeMs
		{
			get;
			set;
		}
		/// <summary>楽器ごとのInvisibleモード</summary>
		public STDGBVALUE<EInvisible> eInvisibleMode;



		#region [ コンストラクタ ]
		public CInvisibleChip()
		{
			Initialize( 3000, 2000 );
		}
		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="_dbDisplayTime">ミス時再表示する時間(秒)</param>
		/// <param name="_dbFadeoutTime">再表示後フェードアウトする時間(秒)</param>
		public CInvisibleChip( int _nDisplayTimeMs, int _nFadeoutTimeMs )
		{
			Initialize( _nDisplayTimeMs, _nFadeoutTimeMs );
		}
		private void Initialize( int _nDisplayTimeMs, int _nFadeoutTimeMs )
		{
			nDisplayTimeMs = _nDisplayTimeMs;
			nFadeoutTimeMs = _nFadeoutTimeMs;
			Reset();
		}
		#endregion

		/// <summary>
		/// 内部状態を初期化する
		/// </summary>
		public void Reset()
		{
			for ( int i = 0; i < 5; i++ )
			{
				ccounter[ i ] = new CCounter();
				b演奏チップが１つでもバーを通過した[ i ] = false;
			}
		}

		/// <summary>
		/// まだSemi-Invisibleを開始していなければ、開始する
		/// </summary>
		/// <param name="eInst"></param>
		public void StartSemiInvisible( EInstrumentPad eInst )
		{
			int nInst = (int) eInst;
			if ( !b演奏チップが１つでもバーを通過した[ nInst ] )
			{
				b演奏チップが１つでもバーを通過した[ nInst ] = true;
				if ( this.eInvisibleMode[ nInst ] == EInvisible.SEMI )
				{
					ShowChipTemporally( eInst );
					ccounter[ nInst ].CurrentValue = nDisplayTimeMs;
				}
			}
		}
		/// <summary>
		/// 一時的にチップを表示するモードを開始する
		/// </summary>
		/// <param name="eInst">楽器パート</param>
		public void ShowChipTemporally( EInstrumentPad eInst )
		{
			ccounter[ (int) eInst ].Start( 0, nDisplayTimeMs + nFadeoutTimeMs + 1, 1, TJAPlayer3.Timer );
		}

		/// <summary>
		/// チップの表示/非表示の状態
		/// </summary>
		public enum EChipInvisibleState
		{
			SHOW,			// Missなどしてチップを表示中
			FADEOUT,		// 表示期間終了後、フェードアウト中
			INVISIBLE		// 完全非表示
		}

		internal EChipInvisibleState SetInvisibleStatus( ref CDTX.CChip cc )
		{
			if ( cc.e楽器パート == EInstrumentPad.UNKNOWN )
			{
				return EChipInvisibleState.SHOW;
			}
			int nInst = (int) cc.e楽器パート;
			EChipInvisibleState retcode = EChipInvisibleState.SHOW;

			ccounter[ nInst ].Tick();

			switch ( eInvisibleMode[ nInst ] )
			{
				case EInvisible.OFF:
					cc.b可視 = true;
					retcode = EChipInvisibleState.SHOW;
					break;

				case EInvisible.FULL:
					cc.b可視 = false;
					retcode = EChipInvisibleState.INVISIBLE;
					break;

				case EInvisible.SEMI:
					if ( !b演奏チップが１つでもバーを通過した[ nInst ] )	// まだ1つもチップがバーを通過していない時は、チップを表示する
					{
						cc.b可視 = true;
						cc.n透明度 = 255;
						return EChipInvisibleState.SHOW;
					}

					if ( ccounter[ nInst ].CurrentValue <= 0 || ccounter[ nInst ].CurrentValue > nDisplayTimeMs + nFadeoutTimeMs )
					// まだ一度もMissっていない or フェードアウトしきった後
					{
						cc.b可視 = false;
						cc.n透明度 = 255;
						retcode = EChipInvisibleState.INVISIBLE;
					}
					else if ( ccounter[ nInst ].CurrentValue < nDisplayTimeMs )								// 表示期間
					{
						cc.b可視 = true;
						cc.n透明度 = 255;
						retcode = EChipInvisibleState.SHOW;
					}
					else if ( ccounter[ nInst ].CurrentValue < nDisplayTimeMs + nFadeoutTimeMs )		// フェードアウト期間
					{
						cc.b可視 = true;
						cc.n透明度 = 255 - (int) ( Convert.ToDouble( ccounter[ nInst ].CurrentValue - nDisplayTimeMs ) / nFadeoutTimeMs * 255.0 );
						retcode = EChipInvisibleState.FADEOUT;
					}
					break;
				default:
					cc.b可視 = true;
					cc.n透明度 = 255;
					retcode = EChipInvisibleState.SHOW;
					break;
			}
			return retcode;
		}
		
		#region [ Dispose-Finalize パターン実装 ]
		//-----------------
		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}
		protected void Dispose( bool disposeManagedObjects )
		{
			if( this.bDispose完了済み )
				return;

			if( disposeManagedObjects )
			{
				// (A) Managed リソースの解放
				for ( int i = 0; i < 5; i++ )
				{
					// ctInvisibleTimer[ i ].Dispose();
					ccounter[ i ].Stop();
					ccounter[ i ] = null;
				}
			}

			// (B) Unamanaged リソースの解放

			this.bDispose完了済み = true;
		}
		~CInvisibleChip()
		{
			this.Dispose( false );
		}
		//-----------------
		#endregion

		private CCounter[] ccounter = new CCounter[5];
		private bool bDispose完了済み = false;
		private bool[] b演奏チップが１つでもバーを通過した = new bool[5];
	}
}

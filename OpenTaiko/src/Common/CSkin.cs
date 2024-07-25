using System.Diagnostics;
using System.Drawing;
using System.Text;
using FDK;

namespace TJAPlayer3 {
	// グローバル定数

	public enum Eシステムサウンド {
		BGMオプション画面 = 0,
		BGMコンフィグ画面,
		BGM起動画面,
		BGM選曲画面,
		SOUNDステージ失敗音,
		SOUNDカーソル移動音,
		SOUNDゲーム開始音,
		SOUNDゲーム終了音,
		SOUNDステージクリア音,
		SOUNDタイトル音,
		SOUNDフルコンボ音,
		SOUND歓声音,
		SOUND曲読込開始音,
		SOUND決定音,
		SOUND取消音,
		SOUND変更音,
		//SOUND赤,
		//SOUND青,
		SOUND風船,
		SOUND曲決定音,
		SOUND成績発表,
		SOUND特訓再生,
		SOUND特訓停止,
		sound特訓ジャンプポイント,
		sound特訓スキップ音,
		SOUND特訓スクロール,
		Count               // システムサウンド総数の計算用
	}

	internal class CSkin : IDisposable {
		// クラス

		public class CSystemSound : IDisposable {
			// static フィールド

			public static CSkin.CSystemSound r最後に再生した排他システムサウンド;

			private readonly ESoundGroup _soundGroup;

			// フィールド、プロパティ

			public bool bPlayed;
			public bool bCompact対象;
			public bool bLoop;
			public bool bNotLoadedYet;
			public bool bLoadedSuccessfuly;
			public bool bExclusive;
			public string strFileName = "";
			public bool bIsPlaying {
				get {
					if (this.rSound[1 - this.nNextPlayingSoundNumber] == null)
						return false;

					return this.rSound[1 - this.nNextPlayingSoundNumber].IsPlaying;
				}
			}
			public int nPosition_CurrentlyPlayingSound {
				get {
					CSound sound = this.rSound[1 - this.nNextPlayingSoundNumber];
					if (sound == null)
						return 0;

					return sound.SoundPosition;
				}
				set {
					CSound sound = this.rSound[1 - this.nNextPlayingSoundNumber];
					if (sound != null)
						sound.SoundPosition = value;
				}
			}
			public int nPosition_NextPlayingSound {
				get {
					CSound sound = this.rSound[this.nNextPlayingSoundNumber];
					if (sound == null)
						return 0;

					return sound.SoundPosition;
				}
				set {
					CSound sound = this.rSound[this.nNextPlayingSoundNumber];
					if (sound != null)
						sound.SoundPosition = value;
				}
			}
			public int nAutomationLevel_現在のサウンド {
				get {
					CSound sound = this.rSound[1 - this.nNextPlayingSoundNumber];
					if (sound == null)
						return 0;

					return sound.AutomationLevel;
				}
				set {
					CSound sound = this.rSound[1 - this.nNextPlayingSoundNumber];
					if (sound != null) {
						sound.AutomationLevel = value;
					}
				}
			}
			public int n長さ_現在のサウンド {
				get {
					CSound sound = this.rSound[1 - this.nNextPlayingSoundNumber];
					if (sound == null) {
						return 0;
					}
					return sound.TotalPlayTime;
				}
			}
			public int n長さ_次に鳴るサウンド {
				get {
					CSound sound = this.rSound[this.nNextPlayingSoundNumber];
					if (sound == null) {
						return 0;
					}
					return sound.TotalPlayTime;
				}
			}


			/// <summary>
			/// コンストラクタ
			/// </summary>
			/// <param name="strFileName"></param>
			/// <param name="bLoop"></param>
			/// <param name="bExclusive"></param>
			/// <param name="bCompact対象"></param>
			public CSystemSound(string strFileName, bool bLoop, bool bExclusive, bool bCompact対象, ESoundGroup soundGroup) {
				this.strFileName = strFileName;
				this.bLoop = bLoop;
				this.bExclusive = bExclusive;
				this.bCompact対象 = bCompact対象;
				_soundGroup = soundGroup;
				this.bNotLoadedYet = true;
				this.bPlayed = false;
			}


			// メソッド

			public void tLoading() {
				this.bNotLoadedYet = false;
				this.bLoadedSuccessfuly = false;
				if (string.IsNullOrEmpty(this.strFileName))
					throw new InvalidOperationException("ファイル名が無効です。");

				if (!File.Exists(CSkin.Path(this.strFileName))) {
					Trace.TraceWarning($"ファイルが存在しません。: {this.strFileName}");
					return;
				}
				////				for( int i = 0; i < 2; i++ )		// #27790 2012.3.10 yyagi 2回読み出しを、1回読みだし＋1回メモリコピーに変更
				////				{
				//                    try
				//                    {
				//                        this.rSound[ 0 ] = CDTXMania.Sound管理.tサウンドを生成する( CSkin.Path( this.strファイル名 ) );
				//                    }
				//                    catch
				//                    {
				//                        this.rSound[ 0 ] = null;
				//                        throw;
				//                    }
				//                    if ( this.rSound[ 0 ] == null )	// #28243 2012.5.3 yyagi "this.rSound[ 0 ].bストリーム再生する"時もCloneするようにし、rSound[1]がnullにならないよう修正→rSound[1]の再生正常化
				//                    {
				//                        this.rSound[ 1 ] = null;
				//                    }
				//                    else
				//                    {
				//                        this.rSound[ 1 ] = ( CSound ) this.rSound[ 0 ].Clone();	// #27790 2012.3.10 yyagi add: to accelerate loading chip sounds
				//                        CDTXMania.Sound管理.tサウンドを登録する( this.rSound[ 1 ] );	// #28243 2012.5.3 yyagi add (登録漏れによりストリーム再生処理が発生していなかった)
				//                    }

				////				}

				for (int i = 0; i < 2; i++)     // 一旦Cloneを止めてASIO対応に専念
				{
					try {
						this.rSound[i] = TJAPlayer3.SoundManager?.tCreateSound(CSkin.Path(this.strFileName), _soundGroup);
					} catch {
						this.rSound[i] = null;
						throw;
					}
				}
				this.bLoadedSuccessfuly = true;
			}
			public void tPlay() {
				if (this.bNotLoadedYet) {
					try {
						tLoading();
					} catch (Exception e) {
						Trace.TraceError(e.ToString());
						Trace.TraceError("例外が発生しましたが処理を継続します。 (17668977-4686-4aa7-b3f0-e0b9a44975b8)");
						this.bNotLoadedYet = false;
					}
				}
				if (this.bExclusive) {
					if (r最後に再生した排他システムサウンド != null)
						r最後に再生した排他システムサウンド.tStop();

					r最後に再生した排他システムサウンド = this;
				}
				CSound sound = this.rSound[this.nNextPlayingSoundNumber];
				if (sound != null)
					sound.PlayStart(this.bLoop);

				this.bPlayed = true;
				this.nNextPlayingSoundNumber = 1 - this.nNextPlayingSoundNumber;
			}
			public void tStop() {
				this.bPlayed = false;
				if (this.rSound[0] != null)
					this.rSound[0].Stop();

				if (this.rSound[1] != null)
					this.rSound[1].Stop();

				if (r最後に再生した排他システムサウンド == this)
					r最後に再生した排他システムサウンド = null;
			}

			public void tRemoveMixer() {
				if (TJAPlayer3.SoundManager.GetCurrentSoundDeviceType() != "DirectShow") {
					for (int i = 0; i < 2; i++) {
						if (this.rSound[i] != null) {
							TJAPlayer3.SoundManager.RemoveMixer(this.rSound[i]);
						}
					}
				}
			}

			#region [ IDisposable 実装 ]
			//-----------------
			public void Dispose() {
				if (!this.bDisposed) {
					for (int i = 0; i < 2; i++) {
						if (this.rSound[i] != null) {
							TJAPlayer3.SoundManager.tDisposeSound(this.rSound[i]);
							this.rSound[i] = null;
						}
					}
					this.bLoadedSuccessfuly = false;
					this.bDisposed = true;
				}
			}
			//-----------------
			#endregion

			#region [ private ]
			//-----------------
			private bool bDisposed;
			private int nNextPlayingSoundNumber;
			private CSound[] rSound = new CSound[2];
			//-----------------
			#endregion
		}


		// プロパティ

		// Hitsounds

		public CHitSounds hsHitSoundsInformations = null;

		// Character specific voice samples

		// Sounds{System.IO.Path.DirectorySeparatorChar}Clear

		public CSystemSound[] voiceClearFailed = new CSystemSound[5];
		public CSystemSound[] voiceClearClear = new CSystemSound[5];
		public CSystemSound[] voiceClearFullCombo = new CSystemSound[5];
		public CSystemSound[] voiceClearAllPerfect = new CSystemSound[5];
		public CSystemSound[] voiceAIWin = new CSystemSound[5];
		public CSystemSound[] voiceAILose = new CSystemSound[5];

		// Sounds{System.IO.Path.DirectorySeparatorChar}Menu

		public CSystemSound[] voiceMenuSongSelect = new CSystemSound[5];
		public CSystemSound[] voiceMenuSongDecide = new CSystemSound[5];
		public CSystemSound[] voiceMenuSongDecide_AI = new CSystemSound[5];
		public CSystemSound[] voiceMenuDiffSelect = new CSystemSound[5];
		public CSystemSound[] voiceMenuDanSelectStart = new CSystemSound[5];
		public CSystemSound[] voiceMenuDanSelectPrompt = new CSystemSound[5];
		public CSystemSound[] voiceMenuDanSelectConfirm = new CSystemSound[5];

		// Sounds{System.IO.Path.DirectorySeparatorChar}Title

		public CSystemSound[] voiceTitleSanka = new CSystemSound[5];

		// Sounds{System.IO.Path.DirectorySeparatorChar}Tower

		public CSystemSound[] voiceTowerMiss = new CSystemSound[5];

		// Sounds{System.IO.Path.DirectorySeparatorChar}Result

		public CSystemSound[] voiceResultBestScore = new CSystemSound[5];
		public CSystemSound[] voiceResultClearFailed = new CSystemSound[5];
		public CSystemSound[] voiceResultClearSuccess = new CSystemSound[5];
		public CSystemSound[] voiceResultDanFailed = new CSystemSound[5];
		public CSystemSound[] voiceResultDanRedPass = new CSystemSound[5];
		public CSystemSound[] voiceResultDanGoldPass = new CSystemSound[5];

		// General sound effects (Skin specific)

		public CSystemSound bgmオプション画面 = null;
		public CSystemSound bgmコンフィグ画面 = null;
		public CSystemSound bgm起動画面 = null;
		public CSystemSound soundSTAGEFAILED音 = null;
		public CSystemSound soundカーソル移動音 = null;
		public CSystemSound soundゲーム開始音 = null;
		public CSystemSound soundゲーム終了音 = null;
		public CSystemSound soundステージクリア音 = null;
		public CSystemSound soundフルコンボ音 = null;
		public CSystemSound sound歓声音 = null;
		public CSystemSound sound曲読込開始音 = null;
		public CSystemSound soundDecideSFX = null;
		public CSystemSound soundCancelSFX = null;
		public CSystemSound soundChangeSFX = null;
		public CSystemSound soundSongSelectChara = null;
		public CSystemSound soundSkip = null;
		public CSystemSound soundEntry = null;
		public CSystemSound soundError = null;
		public CSystemSound soundsanka = null;
		public CSystemSound soundBomb = null;
		//add
		public CSystemSound sound曲決定音 = null;
		public CSystemSound soundSongDecide_AI = null;
		public CSystemSound bgmリザルトイン音 = null;
		public CSystemSound bgmリザルト音 = null;
		public CSystemSound bgmResultIn_AI = null;
		public CSystemSound bgmResult_AI = null;

		public CSystemSound bgmDanResult = null;

		public CSystemSound bgmタイトル = null;
		public CSystemSound bgmタイトルイン = null;
		public CSystemSound bgm選曲画面 = null;
		public CSystemSound bgm選曲画面イン = null;
		public CSystemSound bgmSongSelect_AI = null;
		public CSystemSound bgmSongSelect_AI_In = null;
		public CSystemSound bgmリザルト = null;
		public CSystemSound bgmリザルトイン = null;

		public CSystemSound SoundBanapas = null;

		public CSystemSound sound特訓再生音 = null;
		public CSystemSound sound特訓停止音 = null;
		public CSystemSound soundTrainingToggleBookmarkSFX = null;
		public CSystemSound sound特訓スキップ音 = null;
		public CSystemSound soundTrainingModeScrollSFX = null;
		public CSystemSound soundPon = null;
		public CSystemSound soundGauge = null;
		public CSystemSound soundScoreDon = null;
		public CSystemSound soundChallengeVoice = null;
		public CSystemSound soundDanSelectStart = null;
		public CSystemSound soundDanSongSelectCheck = null;

		public CSystemSound soundDanSongSelectIn = null;

		public CSystemSound soundDanSelectBGM = null;
		public CSystemSound soundDanSongSelect = null;

		public CSystemSound soundHeyaBGM = null;
		public CSystemSound soundOnlineLoungeBGM = null;
		public CSystemSound soundEncyclopediaBGM = null;
		public CSystemSound soundTowerSelectBGM = null;

		public CSystemSound[] soundExToExtra = null;
		public CSystemSound[] soundExtraToEx = null;

		public CSystemSound calibrationTick = null;

		public CSystemSound[] soundModal = null;

		public CSystemSound soundCrownIn = null;
		public CSystemSound soundRankIn = null;

		public CSystemSound soundSelectAnnounce = null;

		// Tower Sfx
		public CSystemSound soundTowerMiss = null;
		public CSystemSound bgmTowerResult = null;

		//public Cシステムサウンド soundRed = null;
		//public Cシステムサウンド soundBlue = null;
		public CSystemSound soundBalloon = null;
		public CSystemSound soundKusudama = null;
		public CSystemSound soundKusudamaMiss = null;


		public readonly int nシステムサウンド数 = (int)Eシステムサウンド.Count;
		public CSystemSound this[Eシステムサウンド sound] {
			get {
				switch (sound) {
					case Eシステムサウンド.SOUNDカーソル移動音:
						return this.soundカーソル移動音;

					case Eシステムサウンド.SOUND決定音:
						return this.soundDecideSFX;

					case Eシステムサウンド.SOUND変更音:
						return this.soundChangeSFX;

					case Eシステムサウンド.SOUND取消音:
						return this.soundCancelSFX;

					case Eシステムサウンド.SOUND歓声音:
						return this.sound歓声音;

					case Eシステムサウンド.SOUNDステージ失敗音:
						return this.soundSTAGEFAILED音;

					case Eシステムサウンド.SOUNDゲーム開始音:
						return this.soundゲーム開始音;

					case Eシステムサウンド.SOUNDゲーム終了音:
						return this.soundゲーム終了音;

					case Eシステムサウンド.SOUNDステージクリア音:
						return this.soundステージクリア音;

					case Eシステムサウンド.SOUNDフルコンボ音:
						return this.soundフルコンボ音;

					case Eシステムサウンド.SOUND曲読込開始音:
						return this.sound曲読込開始音;

					case Eシステムサウンド.SOUNDタイトル音:
						return this.bgmタイトル;

					case Eシステムサウンド.BGM起動画面:
						return this.bgm起動画面;

					case Eシステムサウンド.BGMオプション画面:
						return this.bgmオプション画面;

					case Eシステムサウンド.BGMコンフィグ画面:
						return this.bgmコンフィグ画面;

					case Eシステムサウンド.BGM選曲画面:
						return this.bgm選曲画面;

					//case Eシステムサウンド.SOUND赤:
					//    return this.soundRed;

					//case Eシステムサウンド.SOUND青:
					//    return this.soundBlue;

					case Eシステムサウンド.SOUND風船:
						return this.soundBalloon;

					case Eシステムサウンド.SOUND曲決定音:
						return this.sound曲決定音;

					case Eシステムサウンド.SOUND成績発表:
						return this.bgmリザルトイン音;

					case Eシステムサウンド.SOUND特訓再生:
						return this.sound特訓再生音;

					case Eシステムサウンド.SOUND特訓停止:
						return this.sound特訓停止音;

					case Eシステムサウンド.sound特訓ジャンプポイント:
						return this.soundTrainingToggleBookmarkSFX;

					case Eシステムサウンド.sound特訓スキップ音:
						return this.sound特訓スキップ音;

					case Eシステムサウンド.SOUND特訓スクロール:
						return this.soundTrainingModeScrollSFX;

				}
				throw new IndexOutOfRangeException();
			}
		}
		public CSystemSound this[int index] {
			get {
				switch (index) {
					case 0:
						return this.soundカーソル移動音;

					case 1:
						return this.soundDecideSFX;

					case 2:
						return this.soundChangeSFX;

					case 3:
						return this.soundCancelSFX;

					case 4:
						return this.sound歓声音;

					case 5:
						return this.soundSTAGEFAILED音;

					case 6:
						return this.soundゲーム開始音;

					case 7:
						return this.soundゲーム終了音;

					case 8:
						return this.soundステージクリア音;

					case 9:
						return this.soundフルコンボ音;

					case 10:
						return this.sound曲読込開始音;

					case 11:
						return this.bgmタイトル;

					case 12:
						return this.bgm起動画面;

					case 13:
						return this.bgmオプション画面;

					case 14:
						return this.bgmコンフィグ画面;

					case 15:
						return this.bgm選曲画面;

					case 16:
						return this.soundBalloon;

					case 17:
						return this.sound曲決定音;

					case 18:
						return this.bgmリザルトイン音;

					case 19:
						return this.sound特訓再生音;

					case 20:
						return this.sound特訓停止音;

					case 21:
						return this.soundTrainingModeScrollSFX;

					case 22:
						return this.soundTrainingToggleBookmarkSFX;

					case 23:
						return this.sound特訓スキップ音;
				}
				throw new IndexOutOfRangeException();
			}
		}


		// スキンの切り替えについて___
		//
		// _スキンの種類は大きく分けて2種類。Systemスキンとboxdefスキン。
		// 　前者はSystem/フォルダにユーザーが自らインストールしておくスキン。
		// 　後者はbox.defで指定する、曲データ制作者が提示するスキン。
		//
		// _Config画面で、2種のスキンを区別無く常時使用するよう設定することができる。
		// _box.defの#SKINPATH 設定により、boxdefスキンを一時的に使用するよう設定する。
		// 　(box.defの効果の及ばない他のmuxic boxでは、当該boxdefスキンの有効性が無くなる)
		//
		// これを実現するために___
		// _Systemスキンの設定情報と、boxdefスキンの設定情報は、分離して持つ。
		// 　(strSystem～～ と、strBoxDef～～～)
		// _Config画面からは前者のみ書き換えできるようにし、
		// 　選曲画面からは後者のみ書き換えできるようにする。(SetCurrent...())
		// _読み出しは両者から行えるようにすると共に
		// 　選曲画面用に二種の情報を区別しない読み出し方法も提供する(GetCurrent...)

		private object lockBoxDefSkin;
		public static bool bUseBoxDefSkin = true;                       // box.defからのスキン変更を許容するか否か

		public string strSystemSkinRoot = null;
		public string[] strSystemSkinSubfolders = null;     // List<string>だとignoreCaseな検索が面倒なので、配列に逃げる :-)
		private string[] _strBoxDefSkinSubfolders = null;
		public string[] strBoxDefSkinSubfolders {
			get {
				lock (lockBoxDefSkin) {
					return _strBoxDefSkinSubfolders;
				}
			}
			set {
				lock (lockBoxDefSkin) {
					_strBoxDefSkinSubfolders = value;
				}
			}
		}           // 別スレッドからも書き込みアクセスされるため、スレッドセーフなアクセス法を提供

		private static string strSystemSkinSubfolderFullName;           // Config画面で設定されたスキン
		private static string strBoxDefSkinSubfolderFullName = "";      // box.defで指定されているスキン

		/// <summary>
		/// スキンパス名をフルパスで取得する
		/// </summary>
		/// <param name="bFromUserConfig">ユーザー設定用ならtrue, box.defからの設定ならfalse</param>
		/// <returns></returns>
		public string GetCurrentSkinSubfolderFullName(bool bFromUserConfig) {
			if (!bUseBoxDefSkin || bFromUserConfig == true || strBoxDefSkinSubfolderFullName == "") {
				return strSystemSkinSubfolderFullName;
			} else {
				return strBoxDefSkinSubfolderFullName;
			}
		}
		/// <summary>
		/// スキンパス名をフルパスで設定する
		/// </summary>
		/// <param name="value">スキンパス名</param>
		/// <param name="bFromUserConfig">ユーザー設定用ならtrue, box.defからの設定ならfalse</param>
		public void SetCurrentSkinSubfolderFullName(string value, bool bFromUserConfig) {
			if (bFromUserConfig) {
				strSystemSkinSubfolderFullName = value;
			} else {
				strBoxDefSkinSubfolderFullName = value;
			}
		}


		// コンストラクタ
		public CSkin(string _strSkinSubfolderFullName, bool _bUseBoxDefSkin) {
			lockBoxDefSkin = new object();
			strSystemSkinSubfolderFullName = _strSkinSubfolderFullName;
			bUseBoxDefSkin = _bUseBoxDefSkin;
			InitializeSkinPathRoot();
			ReloadSkinPaths();
			PrepareReloadSkin();
		}
		public CSkin() {
			lockBoxDefSkin = new object();
			InitializeSkinPathRoot();
			bUseBoxDefSkin = true;
			ReloadSkinPaths();
			PrepareReloadSkin();
		}
		private string InitializeSkinPathRoot() {
			strSystemSkinRoot = System.IO.Path.Combine(TJAPlayer3.strEXEのあるフォルダ, "System" + System.IO.Path.DirectorySeparatorChar);
			return strSystemSkinRoot;
		}

		/// <summary>
		/// Skin(Sounds)を再読込する準備をする(再生停止,Dispose,ファイル名再設定)。
		/// あらかじめstrSkinSubfolderを適切に設定しておくこと。
		/// その後、ReloadSkinPaths()を実行し、strSkinSubfolderの正当性を確認した上で、本メソッドを呼び出すこと。
		/// 本メソッド呼び出し後に、ReloadSkin()を実行することで、システムサウンドを読み込み直す。
		/// ReloadSkin()の内容は本メソッド内に含めないこと。起動時はReloadSkin()相当の処理をCEnumSongsで行っているため。
		/// </summary>
		public void PrepareReloadSkin() {
			Trace.TraceInformation("SkinPath設定: {0}",
				(strBoxDefSkinSubfolderFullName == "") ?
				strSystemSkinSubfolderFullName :
				strBoxDefSkinSubfolderFullName
			);

			for (int i = 0; i < nシステムサウンド数; i++) {
				if (this[i] != null && this[i].bLoadedSuccessfuly) {
					this[i].tStop();
					this[i].Dispose();
				}
			}

			this.soundカーソル移動音 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Move.ogg", false, false, false, ESoundGroup.SoundEffect);
			this.soundDecideSFX = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Decide.ogg", false, false, false, ESoundGroup.SoundEffect);
			this.soundChangeSFX = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Change.ogg", false, false, false, ESoundGroup.SoundEffect);
			this.soundCancelSFX = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Cancel.ogg", false, false, true, ESoundGroup.SoundEffect);
			this.sound歓声音 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Audience.ogg", false, false, true, ESoundGroup.SoundEffect);
			this.soundSTAGEFAILED音 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Stage failed.ogg", false, true, true, ESoundGroup.Voice);
			this.soundゲーム開始音 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Game start.ogg", false, false, false, ESoundGroup.Voice);
			this.soundゲーム終了音 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Game end.ogg", false, true, false, ESoundGroup.Voice);
			this.soundステージクリア音 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Stage clear.ogg", false, true, true, ESoundGroup.Voice);
			this.soundフルコンボ音 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Full combo.ogg", false, false, true, ESoundGroup.Voice);
			this.sound曲読込開始音 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Now loading.ogg", false, true, true, ESoundGroup.Unknown);
			//this.bgm選曲画面 = new Cシステムサウンド(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Select BGM.ogg", true, true, false, ESoundGroup.SongPlayback);
			//this.soundSongSelectChara = new Cシステムサウンド(@$"Sounds{System.IO.Path.DirectorySeparatorChar}SongSelect Chara.ogg", false, false, false, ESoundGroup.SongPlayback);
			this.soundSkip = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Skip.ogg", false, false, false, ESoundGroup.SoundEffect);
			this.SoundBanapas = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Banapas.ogg", false, false, false, ESoundGroup.SoundEffect);
			this.soundEntry = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Entry.ogg", true, false, false, ESoundGroup.Voice);
			this.soundError = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Error.ogg", false, false, false, ESoundGroup.SoundEffect);
			//this.soundsanka = new Cシステムサウンド(@$"Sounds{System.IO.Path.DirectorySeparatorChar}sanka.ogg", false, false, false, ESoundGroup.Voice);
			this.soundBomb = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Bomb.ogg", false, false, false, ESoundGroup.SoundEffect);

			//this.soundRed               = new Cシステムサウンド( @$"Sounds{System.IO.Path.DirectorySeparatorChar}dong.ogg",            false, false, true, ESoundType.SoundEffect );
			//this.soundBlue              = new Cシステムサウンド( @$"Sounds{System.IO.Path.DirectorySeparatorChar}ka.ogg",              false, false, true, ESoundType.SoundEffect );
			this.soundBalloon = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}balloon.ogg", false, false, true, ESoundGroup.SoundEffect);
			this.soundKusudama = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Kusudama.ogg", false, false, true, ESoundGroup.SoundEffect);
			this.soundKusudamaMiss = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}KusudamaMiss.ogg", false, false, true, ESoundGroup.SoundEffect);
			this.sound曲決定音 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}SongDecide.ogg", false, false, true, ESoundGroup.Voice);
			this.soundSongDecide_AI = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}SongDecide_AI.ogg", false, false, true, ESoundGroup.Voice);

			this.bgm起動画面 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}BGM{System.IO.Path.DirectorySeparatorChar}Setup.ogg", true, true, false, ESoundGroup.SongPlayback);
			this.bgmタイトルイン = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}BGM{System.IO.Path.DirectorySeparatorChar}Title_Start.ogg", false, false, true, ESoundGroup.SongPlayback);
			this.bgmタイトル = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}BGM{System.IO.Path.DirectorySeparatorChar}Title.ogg", true, false, true, ESoundGroup.SongPlayback);
			this.bgmオプション画面 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}BGM{System.IO.Path.DirectorySeparatorChar}Option.ogg", true, true, false, ESoundGroup.SongPlayback);
			this.bgmコンフィグ画面 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}BGM{System.IO.Path.DirectorySeparatorChar}Config.ogg", true, true, false, ESoundGroup.SongPlayback);
			this.bgm選曲画面イン = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}BGM{System.IO.Path.DirectorySeparatorChar}SongSelect_Start.ogg", false, false, true, ESoundGroup.SongPlayback);
			this.bgm選曲画面 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}BGM{System.IO.Path.DirectorySeparatorChar}SongSelect.ogg", true, false, true, ESoundGroup.SongPlayback);
			this.bgmSongSelect_AI_In = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}BGM{System.IO.Path.DirectorySeparatorChar}SongSelect_AI_Start.ogg", false, false, true, ESoundGroup.SongPlayback);
			this.bgmSongSelect_AI = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}BGM{System.IO.Path.DirectorySeparatorChar}SongSelect_AI.ogg", true, false, true, ESoundGroup.SongPlayback);
			this.bgmリザルトイン音 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}BGM{System.IO.Path.DirectorySeparatorChar}Result_In.ogg", false, false, true, ESoundGroup.SongPlayback);
			this.bgmリザルト音 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}BGM{System.IO.Path.DirectorySeparatorChar}Result.ogg", true, false, true, ESoundGroup.SongPlayback);
			this.bgmResultIn_AI = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}BGM{System.IO.Path.DirectorySeparatorChar}Result_In_AI.ogg", false, false, true, ESoundGroup.SongPlayback);
			this.bgmResult_AI = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}BGM{System.IO.Path.DirectorySeparatorChar}Result_AI.ogg", true, false, true, ESoundGroup.SongPlayback);

			this.bgmDanResult = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Dan{System.IO.Path.DirectorySeparatorChar}Dan_Result.ogg", true, false, false, ESoundGroup.SongPlayback);

			this.bgmTowerResult = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Tower{System.IO.Path.DirectorySeparatorChar}Tower_Result.ogg", true, false, false, ESoundGroup.SongPlayback);

			this.soundCrownIn = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}ResultScreen{System.IO.Path.DirectorySeparatorChar}CrownIn.ogg", false, false, false, ESoundGroup.SoundEffect);
			this.soundRankIn = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}ResultScreen{System.IO.Path.DirectorySeparatorChar}RankIn.ogg", false, false, false, ESoundGroup.SoundEffect);

			this.sound特訓再生音 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Resume.ogg", false, false, false, ESoundGroup.SoundEffect);
			this.sound特訓停止音 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Pause.ogg", false, false, false, ESoundGroup.SoundEffect);
			this.soundTrainingModeScrollSFX = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Scroll.ogg", false, false, false, ESoundGroup.SoundEffect);
			this.soundTrainingToggleBookmarkSFX = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Jump Point.ogg", false, false, false, ESoundGroup.SoundEffect);
			this.sound特訓スキップ音 = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Traning Skip.ogg", false, false, false, ESoundGroup.SoundEffect);
			this.soundPon = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Pon.ogg", false, false, false, ESoundGroup.SoundEffect);
			this.soundGauge = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Gauge.ogg", false, false, false, ESoundGroup.SoundEffect);
			this.soundScoreDon = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}ScoreDon.ogg", false, false, false, ESoundGroup.SoundEffect);

			this.soundDanSongSelectIn = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Dan{System.IO.Path.DirectorySeparatorChar}Dan_In.ogg", false, false, false, ESoundGroup.SoundEffect);

			this.soundDanSelectBGM = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Dan{System.IO.Path.DirectorySeparatorChar}DanSelectBGM.ogg", true, false, false, ESoundGroup.SongPlayback);
			this.soundDanSongSelect = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Dan{System.IO.Path.DirectorySeparatorChar}DanSongSelect.ogg", false, false, false, ESoundGroup.SoundEffect);

			this.soundHeyaBGM = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Heya{System.IO.Path.DirectorySeparatorChar}BGM.ogg", true, false, false, ESoundGroup.SongPlayback);
			this.soundOnlineLoungeBGM = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}OnlineLounge{System.IO.Path.DirectorySeparatorChar}BGM.ogg", true, false, false, ESoundGroup.SongPlayback);
			this.soundEncyclopediaBGM = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Encyclopedia{System.IO.Path.DirectorySeparatorChar}BGM.ogg", true, false, false, ESoundGroup.SongPlayback);
			this.soundTowerSelectBGM = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Tower{System.IO.Path.DirectorySeparatorChar}BGM.ogg", true, false, false, ESoundGroup.SongPlayback);

			soundExToExtra = new CSystemSound[1] { new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}SongSelect{System.IO.Path.DirectorySeparatorChar}0{System.IO.Path.DirectorySeparatorChar}ExToExtra.ogg", false, false, false, ESoundGroup.SoundEffect) }; // Placeholder until Komi decides
			soundExtraToEx = new CSystemSound[1] { new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}SongSelect{System.IO.Path.DirectorySeparatorChar}0{System.IO.Path.DirectorySeparatorChar}ExtraToEx.ogg", false, false, false, ESoundGroup.SoundEffect) }; // what to do with it lol

			calibrationTick = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Calibrate.ogg", false, false, false, ESoundGroup.SoundEffect);

			soundModal = new CSystemSound[6];
			for (int i = 0; i < soundModal.Length - 1; i++) {
				soundModal[i] = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Modals{System.IO.Path.DirectorySeparatorChar}" + i.ToString() + ".ogg", false, false, false, ESoundGroup.SoundEffect);
			}
			soundModal[soundModal.Length - 1] = new CSystemSound(@$"Sounds{System.IO.Path.DirectorySeparatorChar}Modals{System.IO.Path.DirectorySeparatorChar}Coin.ogg", false, false, false, ESoundGroup.SoundEffect);

			ReloadSkin();
			tReadSkinConfig();

			//hsHitSoundsInformations = new CHitSounds(Path(@$"Sounds{System.IO.Path.DirectorySeparatorChar}HitSounds{System.IO.Path.DirectorySeparatorChar}HitSounds.json"));
			hsHitSoundsInformations = new CHitSounds(@$"Global{System.IO.Path.DirectorySeparatorChar}HitSounds{System.IO.Path.DirectorySeparatorChar}HitSounds.json");
		}

		public void ReloadSkin() {
			for (int i = 0; i < nシステムサウンド数; i++) {
				if (!this[i].bExclusive)   // BGM系以外のみ読み込む。(BGM系は必要になったときに読み込む)
				{
					CSystemSound cシステムサウンド = this[i];
					if (!TJAPlayer3.bコンパクトモード || cシステムサウンド.bCompact対象) {
						try {
							cシステムサウンド.tLoading();
							Trace.TraceInformation("システムサウンドを読み込みました。({0})", cシステムサウンド.strFileName);
						} catch (FileNotFoundException e) {
							Trace.TraceWarning(e.ToString());
							Trace.TraceWarning("システムサウンドが存在しません。({0})", cシステムサウンド.strFileName);
						} catch (Exception e) {
							Trace.TraceWarning(e.ToString());
							Trace.TraceWarning("システムサウンドの読み込みに失敗しました。({0})", cシステムサウンド.strFileName);
						}
					}
				}
			}
		}


		/// <summary>
		/// Skinの一覧を再取得する。
		/// System/*****/Graphics (やSounds/) というフォルダ構成を想定している。
		/// もし再取得の結果、現在使用中のSkinのパス(strSystemSkinSubfloderFullName)が消えていた場合は、
		/// 以下の優先順位で存在確認の上strSystemSkinSubfolderFullNameを再設定する。
		/// 1. System/Default/
		/// 2. System/*****/ で最初にenumerateされたもの
		/// 3. System/ (従来互換)
		/// </summary>
		public void ReloadSkinPaths() {
			#region [ まず System/*** をenumerateする ]
			string[] tempSkinSubfolders = System.IO.Directory.GetDirectories(strSystemSkinRoot, "*");
			strSystemSkinSubfolders = new string[tempSkinSubfolders.Length];
			int size = 0;
			for (int i = 0; i < tempSkinSubfolders.Length; i++) {
				#region [ 検出したフォルダがスキンフォルダかどうか確認する]
				if (!bIsValid(tempSkinSubfolders[i]))
					continue;
				#endregion
				#region [ スキンフォルダと確認できたものを、strSkinSubfoldersに入れる ]
				// フォルダ名末尾に必ず{System.IO.Path.DirectorySeparatorChar}をつけておくこと。さもないとConfig読み出し側(必ず{System.IO.Path.DirectorySeparatorChar}をつける)とマッチできない
				if (tempSkinSubfolders[i][tempSkinSubfolders[i].Length - 1] != System.IO.Path.DirectorySeparatorChar) {
					tempSkinSubfolders[i] += System.IO.Path.DirectorySeparatorChar;
				}
				strSystemSkinSubfolders[size] = tempSkinSubfolders[i];
				Trace.TraceInformation("SkinPath検出: {0}", strSystemSkinSubfolders[size]);
				size++;
				#endregion
			}
			Trace.TraceInformation("SkinPath入力: {0}", strSystemSkinSubfolderFullName);
			Array.Resize(ref strSystemSkinSubfolders, size);
			Array.Sort(strSystemSkinSubfolders);    // BinarySearch実行前にSortが必要
			#endregion

			#region [ 現在のSkinパスがbox.defスキンをCONFIG指定していた場合のために、最初にこれが有効かチェックする。有効ならこれを使う。 ]
			if (bIsValid(strSystemSkinSubfolderFullName) &&
				Array.BinarySearch(strSystemSkinSubfolders, strSystemSkinSubfolderFullName,
				StringComparer.InvariantCultureIgnoreCase) < 0) {
				strBoxDefSkinSubfolders = new string[1] { strSystemSkinSubfolderFullName };
				return;
			}
			#endregion

			#region [ 次に、現在のSkinパスが存在するか調べる。あれば終了。]
			if (Array.BinarySearch(strSystemSkinSubfolders, strSystemSkinSubfolderFullName,
				StringComparer.InvariantCultureIgnoreCase) >= 0)
				return;
			#endregion
			#region [ カレントのSkinパスが消滅しているので、以下で再設定する。]
			/// 以下の優先順位で現在使用中のSkinパスを再設定する。
			/// 1. System/Default/
			/// 2. System/*****/ で最初にenumerateされたもの
			/// 3. System/ (従来互換)
			#region [ System/Default/ があるなら、そこにカレントSkinパスを設定する]
			string tempSkinPath_default = System.IO.Path.Combine(strSystemSkinRoot, "Default" + System.IO.Path.DirectorySeparatorChar);
			if (Array.BinarySearch(strSystemSkinSubfolders, tempSkinPath_default,
				StringComparer.InvariantCultureIgnoreCase) >= 0) {
				strSystemSkinSubfolderFullName = tempSkinPath_default;
				return;
			}
			#endregion
			#region [ System/SkinFiles.*****/ で最初にenumerateされたものを、カレントSkinパスに再設定する ]
			if (strSystemSkinSubfolders.Length > 0) {
				strSystemSkinSubfolderFullName = strSystemSkinSubfolders[0];
				return;
			}
			#endregion
			#region [ System/ に、カレントSkinパスを再設定する。]
			strSystemSkinSubfolderFullName = strSystemSkinRoot;
			strSystemSkinSubfolders = new string[1] { strSystemSkinSubfolderFullName };
			#endregion
			#endregion
		}

		// メソッド

		public static string Path(string strファイルの相対パス) {
			if (strBoxDefSkinSubfolderFullName == "" || !bUseBoxDefSkin) {
				return System.IO.Path.Combine(strSystemSkinSubfolderFullName, strファイルの相対パス);
			} else {
				return System.IO.Path.Combine(strBoxDefSkinSubfolderFullName, strファイルの相対パス);
			}
		}

		/// <summary>
		/// フルパス名を与えると、スキン名として、ディレクトリ名末尾の要素を返す
		/// 例: C:{System.IO.Path.DirectorySeparatorChar}foo{System.IO.Path.DirectorySeparatorChar}bar{System.IO.Path.DirectorySeparatorChar} なら、barを返す
		/// </summary>
		/// <param name="skinPathFullName">スキンが格納されたパス名(フルパス)</param>
		/// <returns>スキン名</returns>
		public static string GetSkinName(string skinPathFullName) {
			if (skinPathFullName != null) {
				if (skinPathFullName == "")     // 「box.defで未定義」用
					skinPathFullName = strSystemSkinSubfolderFullName;
				string[] tmp = skinPathFullName.Split(System.IO.Path.DirectorySeparatorChar);
				return tmp[tmp.Length - 2];     // ディレクトリ名の最後から2番目の要素がスキン名(最後の要素はnull。元stringの末尾が{System.IO.Path.DirectorySeparatorChar}なので。)
			}
			return null;
		}
		public static string[] GetSkinName(string[] skinPathFullNames) {
			string[] ret = new string[skinPathFullNames.Length];
			for (int i = 0; i < skinPathFullNames.Length; i++) {
				ret[i] = GetSkinName(skinPathFullNames[i]);
			}
			return ret;
		}


		public string GetSkinSubfolderFullNameFromSkinName(string skinName) {
			foreach (string s in strSystemSkinSubfolders) {
				if (GetSkinName(s) == skinName)
					return s;
			}
			foreach (string b in strBoxDefSkinSubfolders) {
				if (GetSkinName(b) == skinName)
					return b;
			}
			return null;
		}

		/// <summary>
		/// スキンパス名が妥当かどうか
		/// (タイトル画像にアクセスできるかどうかで判定する)
		/// </summary>
		/// <param name="skinPathFullName">妥当性を確認するスキンパス(フルパス)</param>
		/// <returns>妥当ならtrue</returns>
		public bool bIsValid(string skinPathFullName) {
			string filePathTitle;
			filePathTitle = System.IO.Path.Combine(skinPathFullName, @$"Graphics{System.IO.Path.DirectorySeparatorChar}1_Title{System.IO.Path.DirectorySeparatorChar}Background.png");
			return (File.Exists(filePathTitle));
		}


		public void tRemoveMixerAll() {
			for (int i = 0; i < nシステムサウンド数; i++) {
				if (this[i] != null && this[i].bLoadedSuccessfuly) {
					this[i].tStop();
					this[i].tRemoveMixer();
				}
			}

		}

		/// <summary>
		/// 変数の初期化
		/// </summary>
		public void tSkinConfigInit() {
			this.eDiffDispMode = E難易度表示タイプ.mtaikoに画像で表示;
			this.b現在のステージ数を表示しない = false;
		}

		public void LoadSkinConfigFromFile(string path, ref string work) {
			if (!File.Exists(Path(path))) return;
			using (var streamReader = new StreamReader(Path(path), Encoding.GetEncoding(TJAPlayer3.sEncType))) {
				while (streamReader.Peek() > -1) // 一行ずつ読み込む。
				{
					var nowLine = streamReader.ReadLine();
					if (nowLine.StartsWith("#include")) {
						// #include hogehoge.iniにぶち当たった
						var includePath = nowLine.Substring("#include ".Length).Trim();
						LoadSkinConfigFromFile(includePath, ref work); // 再帰的に読み込む
					} else {
						work += nowLine + Environment.NewLine;
					}
				}
			}
		}

		public void tReadSkinConfig() {
			var str = "";
			LoadSkinConfigFromFile(Path(@$"SkinConfig.ini"), ref str);
			this.t文字列から読み込み(str);

		}

		private void t文字列から読み込み(string strAllSettings)  // 2011.4.13 yyagi; refactored to make initial KeyConfig easier.
		{
			string[] delimiter = { "\n" };
			string[] strSingleLine = strAllSettings.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
			foreach (string s in strSingleLine) {
				string str = s.Replace('\t', ' ').TrimStart(new char[] { '\t', ' ' });
				if ((str.Length != 0) && (str[0] != ';')) {
					try {
						string strCommand;
						string strParam;
						string[] strArray = str.Split(new char[] { '=' });
						if (strArray.Length == 2) {
							strCommand = strArray[0].Trim();
							strParam = strArray[1].Trim();

							#region [Skin Settings]

							void ParseInt32(Action<int> setValue) {
								if (int.TryParse(strParam, out var unparsedValue)) {
									setValue(unparsedValue);
								} else {
									Trace.TraceWarning($"SkinConfigの値 {strCommand} は整数値である必要があります。現在の値: {strParam}");
								}
							}
							switch (strCommand) {
								case "Name": {
										this.Skin_Name = strParam;
										break;
									}
								case "Version": {
										this.Skin_Version = strParam;
										break;
									}
								case "Creator": {
										this.Skin_Creator = strParam;
										break;
									}
								case "Resolution": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Resolution[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								//case "FontName":
								//{
								//    strParam = strParam.Replace('/', System.IO.Path.DirectorySeparatorChar);
								//    strParam = strParam.Replace('\\', System.IO.Path.DirectorySeparatorChar);
								//    if (HPrivateFastFont.FontExists(strParam)) FontName = strParam;
								//    strParam = Path(strParam);
								//    if (HPrivateFastFont.FontExists(strParam)) FontName = strParam;
								//    break;
								//}
								//case "BoxFontName":
								//{
								//    strParam = strParam.Replace('/', System.IO.Path.DirectorySeparatorChar);
								//    strParam = strParam.Replace('\\', System.IO.Path.DirectorySeparatorChar);
								//    if (HPrivateFastFont.FontExists(strParam)) BoxFontName = strParam;
								//    strParam = Path(strParam);
								//    if (HPrivateFastFont.FontExists(Path(strParam))) BoxFontName = strParam;
								//    break;
								//}
								#endregion

								#region [Background Scroll]

								case "Background_Scroll_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											this.Background_Scroll_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}

								#endregion

								#region [Taiko Mode]
								//-----------------------------
								case "ScrollFieldP1Y": {
										this.nScrollFieldY[0] = CConversion.StringToInt(strParam, 192);
										break;
									}
								case "ScrollFieldP2Y": {
										this.nScrollFieldY[1] = CConversion.StringToInt(strParam, 192);
										break;
									}
								case "SENotesP1Y": {
										this.nSENotesY[0] = CConversion.StringToInt(strParam, this.nSENotesY[0]);
										break;
									}
								case "SENotesP2Y": {
										this.nSENotesY[1] = CConversion.StringToInt(strParam, this.nSENotesY[1]);
										break;
									}
								case "JudgePointP1Y": {
										this.nJudgePointY[0] = CConversion.StringToInt(strParam, this.nJudgePointY[0]);
										break;
									}
								case "JudgePointP2Y": {
										this.nJudgePointY[1] = CConversion.StringToInt(strParam, this.nJudgePointY[1]);
										break;
									}

								case "DiffDispMode": {
										this.eDiffDispMode = (E難易度表示タイプ)CConversion.n値を文字列から取得して範囲内に丸めて返す(strParam, 0, 2, (int)this.eDiffDispMode);
										break;
									}
								case "NowStageDisp": {
										this.b現在のステージ数を表示しない = CConversion.bONorOFF(strParam[0]);
										break;
									}

								//-----------------------------
								#endregion

								#region [Result screen]
								//-----------------------------
								case "ResultPanelP1X": {
										this.nResultPanelP1X = CConversion.StringToInt(strParam, 515);
										break;
									}
								case "ResultPanelP1Y": {
										this.nResultPanelP1Y = CConversion.StringToInt(strParam, 75);
										break;
									}
								case "ResultPanelP2X": {
										this.nResultPanelP2X = CConversion.StringToInt(strParam, 515);
										break;
									}
								case "ResultPanelP2Y": {
										this.nResultPanelP2Y = CConversion.StringToInt(strParam, 75);
										break;
									}
								case "ResultScoreP1X": {
										this.nResultScoreP1X = CConversion.StringToInt(strParam, 582);
										break;
									}
								case "ResultScoreP1Y": {
										this.nResultScoreP1Y = CConversion.StringToInt(strParam, 252);
										break;
									}
								//-----------------------------
								#endregion


								#region 新・SkinConfig

								#region Startup
								case nameof(StartUp_LangSelect_FontSize): {
										StartUp_LangSelect_FontSize = int.Parse(strParam);
										break;
									}
								#endregion

								#region Title
								case nameof(Title_LoadingPinInstances): {
										Title_LoadingPinInstances = int.Parse(strParam);
										break;
									}
								case nameof(Title_LoadingPinFrameCount): {
										Title_LoadingPinFrameCount = int.Parse(strParam);
										break;
									}
								case nameof(Title_LoadingPinCycle): {
										Title_LoadingPinCycle = int.Parse(strParam);
										break;
									}
								case "Title_LoadingPinBase": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_LoadingPinBase[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_LoadingPinDiff": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_LoadingPinDiff[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_Entry_Bar_Text_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_Entry_Bar_Text_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_Entry_Bar_Text_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_Entry_Bar_Text_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_Banapas_Load_Clear_Anime": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_Banapas_Load_Clear_Anime[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_Entry_Player_Select_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											Title_Entry_Player_Select_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_Entry_Player_Select_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											Title_Entry_Player_Select_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_Entry_Player_Select_Rect_0_Side": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Title_Entry_Player_Select_Rect[0][0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_Entry_Player_Select_Rect_0_Center": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Title_Entry_Player_Select_Rect[0][1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_Entry_Player_Select_Rect_1_Side": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Title_Entry_Player_Select_Rect[1][0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_Entry_Player_Select_Rect_1_Center": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Title_Entry_Player_Select_Rect[1][1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_Entry_Player_Select_Rect_2_Side": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Title_Entry_Player_Select_Rect[2][0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_Entry_Player_Select_Rect_2_Center": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Title_Entry_Player_Select_Rect[2][1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_Entry_NamePlate": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_Entry_NamePlate[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											Title_ModeSelect_Bar_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											Title_ModeSelect_Bar_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_ModeSelect_Bar_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Title_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_ModeSelect_Title_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Title_Scale": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_ModeSelect_Title_Scale[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Center_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											Title_ModeSelect_Bar_Center_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Center_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											Title_ModeSelect_Bar_Center_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Center_Rect_Up": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Title_ModeSelect_Bar_Center_Rect[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Center_Rect_Down": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Title_ModeSelect_Bar_Center_Rect[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Center_Rect_Center": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Title_ModeSelect_Bar_Center_Rect[2][i] = int.Parse(strSplit[i]);
										}
										break;
									}




								case "Title_ModeSelect_Bar_Overlay_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											Title_ModeSelect_Bar_Overlay_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Overlay_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											Title_ModeSelect_Bar_Overlay_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Overlay_Rect_Up": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Title_ModeSelect_Bar_Overlay_Rect[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Overlay_Rect_Down": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Title_ModeSelect_Bar_Overlay_Rect[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Overlay_Rect_Center": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Title_ModeSelect_Bar_Overlay_Rect[2][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_ModeSelect_Bar_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Move_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_ModeSelect_Bar_Move_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Overlay_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_ModeSelect_Overlay_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Overlay_Move_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_ModeSelect_Overlay_Move_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Chara_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_ModeSelect_Bar_Chara_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Chara_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_ModeSelect_Bar_Chara_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Chara_Move": {
										Title_ModeSelect_Bar_Chara_Move = int.Parse(strParam);
										break;
									}
								case "Title_ModeSelect_Bar_Center_Title": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_ModeSelect_Bar_Center_Title[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_ModeSelect_Bar_Center_Title_Move": {
										Title_ModeSelect_Bar_Center_Title_Move = int.Parse(strParam);
										break;
									}
								case "Title_ModeSelect_Bar_Center_Title_Move_X": {
										Title_ModeSelect_Bar_Center_Title_Move_X = int.Parse(strParam);
										break;
									}
								case "Title_ModeSelect_Bar_Center_BoxText": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Title_ModeSelect_Bar_Center_BoxText[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Title_VerticalText": {
										Title_VerticalText = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case "Title_VerticalBar": {
										Title_VerticalBar = CConversion.bONorOFF(strParam[0]);
										break;
									}
								#endregion

								#region Config
								case "Config_Arrow_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Config_Arrow_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Config_Arrow_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Config_Arrow_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Config_Arrow_Focus_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Config_Arrow_Focus_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Config_Arrow_Focus_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Config_Arrow_Focus_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Config_Item_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											Config_Item_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Config_Item_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											Config_Item_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Config_Item_Width": {
										Config_Item_Width = int.Parse(strParam);
										break;
									}
								case "Config_Item_Font_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Config_Item_Font_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Config_Font_Scale": {
										Config_Font_Scale = int.Parse(strParam);
										break;
									}
								case "Config_Selected_Menu_Text_Grad_Color_1": {
										Config_Selected_Menu_Text_Grad_Color_1 = ColorTranslator.FromHtml(strParam);
										break;
									}
								case "Config_Selected_Menu_Text_Grad_Color_2": {
										Config_Selected_Menu_Text_Grad_Color_2 = ColorTranslator.FromHtml(strParam);
										break;
									}
								case "Config_Font_Scale_Description": {
										Config_Font_Scale_Description = float.Parse(strParam);
										break;
									}
								case "Config_ItemBox_Count": {
										Config_ItemBox_Count = int.Parse(strParam);
										break;
									}
								case "Config_ItemBox_X": {
										Config_ItemBox_X = new int[Config_ItemBox_Count];

										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Config_ItemBox_Count; i++) {
											Config_ItemBox_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Config_ItemBox_Y": {
										Config_ItemBox_Y = new int[Config_ItemBox_Count];

										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Config_ItemBox_Count; i++) {
											Config_ItemBox_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Config_ItemBox_Font_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Config_ItemBox_Font_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Config_ItemBox_ItemValue_Font_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Config_ItemBox_ItemValue_Font_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Config_ExplanationPanel": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Config_ExplanationPanel[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Config_SkinSample1": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Config_SkinSample1[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Config_KeyAssign": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Config_KeyAssign[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Config_KeyAssign_Menu_Highlight": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Config_KeyAssign_Menu_Highlight[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Config_KeyAssign_Font": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Config_KeyAssign_Font[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Config_KeyAssign_Move": {
										Config_KeyAssign_Move = int.Parse(strParam);
										break;
									}
								case nameof(Config_Calibration_OffsetText): {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Config_Calibration_OffsetText[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case nameof(Config_Calibration_InfoText): {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Config_Calibration_InfoText[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case nameof(Config_Calibration_Highlights): {
										string[] strSplit = strParam.Split(',');
										int recs = Math.Min(strSplit.Length, 12);
										for (int i = 0; i + 3 < recs; i += 4) {
											Config_Calibration_Highlights[i / 4] = new Rectangle(int.Parse(strSplit[i]), int.Parse(strSplit[i + 1]), int.Parse(strSplit[i + 2]), int.Parse(strSplit[i + 3]));
										}
										break;
									}
								#endregion

								#region [Mod Icons]


								/*
                                * public int[] ModIcons_OffsetX = { 0, 30, 60, 90, 0, 30, 60, 90 };
                                    public int[] ModIcons_OffsetY = { 0, 0, 0, 0, 30, 30, 30, 30 };
                                    public int[] ModIcons_OffsetX_Menu = { 0, 30, 60, 90, 120, 150, 180, 210 };
                                    public int[] ModIcons_OffsetY_Menu = { 0, 0, 0, 0, 0, 0, 0, 0 };
                                */

								case "ModIcons_OffsetX": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 8; i++) {
											ModIcons_OffsetX[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "ModIcons_OffsetY": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 8; i++) {
											ModIcons_OffsetY[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "ModIcons_OffsetX_Menu": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 8; i++) {
											ModIcons_OffsetX_Menu[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "ModIcons_OffsetY_Menu": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 8; i++) {
											ModIcons_OffsetY_Menu[i] = int.Parse(strSplit[i]);
										}
										break;
									}

								#endregion

								#region SongSelect
								case "SongSelect_BoxExplanation_X": {
										SongSelect_BoxExplanation_X = int.Parse(strParam);
										break;
									}
								case "SongSelect_BoxExplanation_Y": {
										SongSelect_BoxExplanation_Y = int.Parse(strParam);
										break;
									}
								case "SongSelect_BoxExplanation_Interval": {
										SongSelect_BoxExplanation_Interval = int.Parse(strParam);
										break;
									}
								case "SongSelect_GenreName": {
										SongSelect_GenreName = this.strStringを配列に直す(strParam);
										break;
									}
								case "SongSelect_Bar_Count": {
										SongSelect_Bar_Count = int.Parse(strParam);
										break;
									}
								case "SongSelect_Bar_X": {
										SongSelect_Bar_X = new int[SongSelect_Bar_Count];

										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < SongSelect_Bar_Count; i++) {
											SongSelect_Bar_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Bar_Y": {
										SongSelect_Bar_Y = new int[SongSelect_Bar_Count];

										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < SongSelect_Bar_Count; i++) {
											SongSelect_Bar_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Bar_Anim_X": {
										SongSelect_Bar_Anim_X = new int[SongSelect_Bar_Count];

										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < SongSelect_Bar_Count; i++) {
											SongSelect_Bar_Anim_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Bar_Anim_Y": {
										SongSelect_Bar_Anim_Y = new int[SongSelect_Bar_Count];

										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < SongSelect_Bar_Count; i++) {
											SongSelect_Bar_Anim_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Scroll_Interval": {
										SongSelect_Scroll_Interval = float.Parse(strParam);
										break;
									}
								case "SongSelect_DanStatus_Offset_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_DanStatus_Offset_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_DanStatus_Offset_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_DanStatus_Offset_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_TowerStatus_Offset_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_TowerStatus_Offset_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_TowerStatus_Offset_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_TowerStatus_Offset_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_RegularCrowns_Offset_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_RegularCrowns_Offset_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_RegularCrowns_Offset_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_RegularCrowns_Offset_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_RegularCrowns_ScoreRank_Offset_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_RegularCrowns_ScoreRank_Offset_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_RegularCrowns_ScoreRank_Offset_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_RegularCrowns_ScoreRank_Offset_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_RegularCrowns_Difficulty_Cymbol_Offset_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_RegularCrowns_Difficulty_Cymbol_Offset_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_RegularCrowns_Difficulty_Cymbol_Offset_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_RegularCrowns_Difficulty_Cymbol_Offset_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_FavoriteStatus_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_FavoriteStatus_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Bar_Title_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Bar_Title_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Bar_Box_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Bar_Box_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Bar_BackBox_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Bar_BackBox_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Bar_Random_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Bar_Random_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Bar_SubTitle_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Bar_SubTitle_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}


								case "SongSelect_BoxName_Scale": {
										SongSelect_BoxName_Scale = int.Parse(strParam);
										break;
									}
								case "SongSelect_MusicName_Scale": {
										SongSelect_MusicName_Scale = int.Parse(strParam);
										break;
									}
								case "SongSelect_Subtitle_Scale": {
										SongSelect_Subtitle_Scale = int.Parse(strParam);
										break;
									}
								case "SongSelect_BoxText_Scale": {
										SongSelect_BoxText_Scale = int.Parse(strParam);
										break;
									}

								case "SongSelect_VerticalText": {
										SongSelect_VerticalText = CConversion.bONorOFF(strParam[0]);
										break;
									}

								case "SongSelect_Bar_Center_Move_X": {
										SongSelect_Bar_Center_Move_X = int.Parse(strParam);
										break;
									}

								case "SongSelect_Title_MaxSize": {
										SongSelect_Title_MaxSize = int.Parse(strParam);
										break;
									}
								case "SongSelect_SubTitle_MaxSize": {
										SongSelect_SubTitle_MaxSize = int.Parse(strParam);
										break;
									}
								case "SongSelect_Maker_Show": {
										SongSelect_Maker_Show = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case "SongSelect_Shorten_Frame_Fade": {
										SongSelect_Shorten_Frame_Fade = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case "SongSelect_Bar_Select_Skip_Fade": {
										SongSelect_Bar_Select_Skip_Fade = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case "SongSelect_Maker": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Maker[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Maker_Size": {
										SongSelect_Maker_Size = int.Parse(strParam);
										break;
									}
								case "SongSelect_Maker_MaxSize": {
										SongSelect_Maker_MaxSize = int.Parse(strParam);
										break;
									}

								case "SongSelect_Bar_Center_Move": {
										SongSelect_Bar_Center_Move = int.Parse(strParam);
										break;
									}
								case "SongSelect_Bar_Select": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Bar_Select[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Frame_Score_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											SongSelect_Frame_Score_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Frame_Score_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											SongSelect_Frame_Score_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Level_Number_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											SongSelect_Level_Number_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Level_Number_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											SongSelect_Level_Number_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Level_Number_Tower": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Level_Number_Tower[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Tower_Side": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Tower_Side[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Level_Number_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Level_Number_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Level_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											SongSelect_Level_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Level_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											SongSelect_Level_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Level_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Level_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Box_Opening_Interval": {
										SongSelect_Box_Opening_Interval = float.Parse(strParam);
										break;
									}
								case "SongSelect_Unlock_Conditions_Text": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Unlock_Conditions_Text[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Select_Title": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Difficulty_Select_Title[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Select_SubTitle": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Difficulty_Select_SubTitle[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Box_Chara_Move": {
										SongSelect_Box_Chara_Move = int.Parse(strParam);
										break;
									}
								case "SongSelect_Box_Chara_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Box_Chara_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Box_Chara_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Box_Chara_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_NamePlate_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_NamePlate_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_NamePlate_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_NamePlate_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Auto_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Auto_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Auto_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Auto_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_ModIcons_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_ModIcons_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_ModIcons_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_ModIcons_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Timer": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Timer[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Timer_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Timer_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Bpm_Show": {
										SongSelect_Bpm_Show = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case "SongSelect_Bpm_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											SongSelect_Bpm_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Bpm_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											SongSelect_Bpm_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Bpm_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Bpm_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_BPM_Text_Show": {
										SongSelect_BPM_Text_Show = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case "SongSelect_BPM_Text": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_BPM_Text[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_BPM_Text_Size": {
										SongSelect_BPM_Text_Size = int.Parse(strParam);
										break;
									}
								case "SongSelect_BPM_Text_MaxSize": {
										SongSelect_BPM_Text_MaxSize = int.Parse(strParam);
										break;
									}
								case "SongSelect_Explicit": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Explicit[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Movie": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Movie[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_FloorNum_Show": {
										SongSelect_FloorNum_Show = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case "SongSelect_FloorNum_X": {
										SongSelect_FloorNum_X = int.Parse(strParam);
										break;
									}
								case "SongSelect_FloorNum_Y": {
										SongSelect_FloorNum_Y = int.Parse(strParam);
										break;
									}
								case "SongSelect_FloorNum_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_FloorNum_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_DanInfo_Show": {
										SongSelect_DanInfo_Show = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case "SongSelect_DanInfo_Icon_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											SongSelect_DanInfo_Icon_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_DanInfo_Icon_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											SongSelect_DanInfo_Icon_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_DanInfo_Icon_Scale": {
										SongSelect_DanInfo_Icon_Scale = float.Parse(strParam);
										break;
									}
								case "SongSelect_DanInfo_Difficulty_Cymbol_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											SongSelect_DanInfo_Difficulty_Cymbol_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_DanInfo_Difficulty_Cymbol_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											SongSelect_DanInfo_Difficulty_Cymbol_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_DanInfo_Difficulty_Cymbol_Scale": {
										SongSelect_DanInfo_Difficulty_Cymbol_Scale = float.Parse(strParam);
										break;
									}
								case "SongSelect_DanInfo_Level_Number_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											SongSelect_DanInfo_Level_Number_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_DanInfo_Level_Number_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											SongSelect_DanInfo_Level_Number_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_DanInfo_Level_Number_Scale": {
										SongSelect_DanInfo_Level_Number_Scale = float.Parse(strParam);
										break;
									}
								case "SongSelect_DanInfo_Title_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											SongSelect_DanInfo_Title_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_DanInfo_Title_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											SongSelect_DanInfo_Title_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_DanInfo_Title_Size": {
										SongSelect_DanInfo_Title_Size = int.Parse(strParam);
										break;
									}
								case "SongSelect_DanInfo_Exam_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 6; i++) {
											SongSelect_DanInfo_Exam_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_DanInfo_Exam_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 6; i++) {
											SongSelect_DanInfo_Exam_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_DanInfo_Exam_Size": {
										SongSelect_DanInfo_Exam_Size = int.Parse(strParam);
										break;
									}
								case "SongSelect_DanInfo_Exam_Value_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											SongSelect_DanInfo_Exam_Value_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_DanInfo_Exam_Value_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 6; i++) {
											SongSelect_DanInfo_Exam_Value_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_DanInfo_Exam_Value_Scale": {
										SongSelect_DanInfo_Exam_Value_Scale = float.Parse(strParam);
										break;
									}
								case "SongSelect_Table_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_Table_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Table_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_Table_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_High_Score_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_High_Score_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_High_Score_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_High_Score_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_High_Score_Difficulty_Cymbol_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_High_Score_Difficulty_Cymbol_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_High_Score_Difficulty_Cymbol_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_High_Score_Difficulty_Cymbol_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_BoardNumber_1P_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 13; i++) {
											SongSelect_BoardNumber_X[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_BoardNumber_1P_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 13; i++) {
											SongSelect_BoardNumber_Y[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_BoardNumber_2P_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 13; i++) {
											SongSelect_BoardNumber_X[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_BoardNumber_2P_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 13; i++) {
											SongSelect_BoardNumber_Y[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_BoardNumber_3P_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 13; i++) {
											SongSelect_BoardNumber_X[2][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_BoardNumber_3P_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 13; i++) {
											SongSelect_BoardNumber_Y[2][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_BoardNumber_4P_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 13; i++) {
											SongSelect_BoardNumber_X[3][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_BoardNumber_4P_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 13; i++) {
											SongSelect_BoardNumber_Y[3][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_BoardNumber_5P_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 13; i++) {
											SongSelect_BoardNumber_X[4][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_BoardNumber_5P_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 13; i++) {
											SongSelect_BoardNumber_Y[4][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_BoardNumber_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_BoardNumber_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_SongNumber_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_SongNumber_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_SongNumber_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_SongNumber_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_SongNumber_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_SongNumber_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Search_Bar_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_Search_Bar_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Search_Bar_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_Search_Bar_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Level_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Level_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Colors": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 7; i++) {
											SongSelect_Difficulty_Colors[i] = ColorTranslator.FromHtml(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Back": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Difficulty_Back[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Bar_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 7; i++) {
											SongSelect_Difficulty_Bar_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Branch_Text_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Branch_Text_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Branch_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Branch_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Bar_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 7; i++) {
											SongSelect_Difficulty_Bar_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Bar_Back_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											SongSelect_Difficulty_Bar_Rect[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Bar_Option_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											SongSelect_Difficulty_Bar_Rect[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Bar_Easy_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											SongSelect_Difficulty_Bar_Rect[2][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Bar_Normal_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											SongSelect_Difficulty_Bar_Rect[3][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Bar_Hard_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											SongSelect_Difficulty_Bar_Rect[4][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Bar_Oni_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											SongSelect_Difficulty_Bar_Rect[5][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Bar_Edit_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											SongSelect_Difficulty_Bar_Rect[6][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Star_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_Difficulty_Star_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Star_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_Difficulty_Star_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Star_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Difficulty_Star_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Number_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_Difficulty_Number_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Number_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_Difficulty_Number_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Number_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Difficulty_Number_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Crown_1P_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_Difficulty_Crown_X[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Crown_2P_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_Difficulty_Crown_X[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Crown_1P_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_Difficulty_Crown_Y[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Crown_2P_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_Difficulty_Crown_Y[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_ScoreRank_1P_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_Difficulty_ScoreRank_X[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_ScoreRank_2P_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_Difficulty_ScoreRank_X[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_ScoreRank_1P_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_Difficulty_ScoreRank_Y[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_ScoreRank_2P_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_Difficulty_ScoreRank_Y[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Select_Bar_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 7; i++) {
											SongSelect_Difficulty_Select_Bar_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Select_Bar_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 7; i++) {
											SongSelect_Difficulty_Select_Bar_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Select_Bar_Back_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 7; i++) {
											SongSelect_Difficulty_Select_Bar_Back_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Select_Bar_Back_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 7; i++) {
											SongSelect_Difficulty_Select_Bar_Back_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Select_Bar_Cursor_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											SongSelect_Difficulty_Select_Bar_Rect[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Select_Bar_Back1_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											SongSelect_Difficulty_Select_Bar_Rect[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Select_Bar_Back2_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											SongSelect_Difficulty_Select_Bar_Rect[2][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Select_Bar_Anime": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Difficulty_Select_Bar_Anime[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Select_Bar_AnimeIn": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Difficulty_Select_Bar_AnimeIn[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Select_Bar_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Difficulty_Select_Bar_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Difficulty_Bar_ExExtra_AnimeDuration": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Difficulty_Bar_ExExtra_AnimeDuration[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Preimage": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Preimage[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Preimage_Size": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Preimage_Size[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Option_Select_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Option_Select_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Option_Font_Scale": {
										SongSelect_Option_Font_Scale = int.Parse(strParam);
										break;
									}
								case "SongSelect_Option_OptionType_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Option_OptionType_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Option_OptionType_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Option_OptionType_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Option_Value_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Option_Value_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Option_Value_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Option_Value_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Option_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Option_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Option_ModMults1_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Option_ModMults1_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Option_ModMults2_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Option_ModMults2_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Option_ModMults1_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Option_ModMults1_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_Option_ModMults2_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_Option_ModMults2_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_NewHeya_Close_Select": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_NewHeya_Close_Select[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_NewHeya_PlayerPlate_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_NewHeya_PlayerPlate_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_NewHeya_PlayerPlate_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_NewHeya_PlayerPlate_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_NewHeya_ModeBar_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_NewHeya_ModeBar_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_NewHeya_ModeBar_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											SongSelect_NewHeya_ModeBar_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_NewHeya_ModeBar_Font_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_NewHeya_ModeBar_Font_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_NewHeya_Box_Count": {
										SongSelect_NewHeya_Box_Count = int.Parse(strParam);
										break;
									}
								case "SongSelect_NewHeya_Box_X": {
										string[] strSplit = strParam.Split(',');
										SongSelect_NewHeya_Box_X = new int[SongSelect_NewHeya_Box_Count];
										for (int i = 0; i < SongSelect_NewHeya_Box_Count; i++) {
											SongSelect_NewHeya_Box_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_NewHeya_Box_Y": {
										string[] strSplit = strParam.Split(',');
										SongSelect_NewHeya_Box_Y = new int[SongSelect_NewHeya_Box_Count];
										for (int i = 0; i < SongSelect_NewHeya_Box_Count; i++) {
											SongSelect_NewHeya_Box_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_NewHeya_Box_Chara_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_NewHeya_Box_Chara_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_NewHeya_Box_Name_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_NewHeya_Box_Name_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_NewHeya_Box_Author_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_NewHeya_Box_Author_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_NewHeya_Lock_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_NewHeya_Lock_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongSelect_NewHeya_InfoSection_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongSelect_NewHeya_InfoSection_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}


								case "SongSelect_ForeColor_JPOP": {
										SongSelect_ForeColor_JPOP = ColorTranslator.FromHtml(strParam);
										break;
									}
								case "SongSelect_ForeColor_Anime": {
										SongSelect_ForeColor_Anime = ColorTranslator.FromHtml(strParam);
										break;
									}
								case "SongSelect_ForeColor_VOCALOID": {
										SongSelect_ForeColor_VOCALOID = ColorTranslator.FromHtml(strParam);
										break;
									}
								case "SongSelect_ForeColor_Children": {
										SongSelect_ForeColor_Children = ColorTranslator.FromHtml(strParam);
										break;
									}
								case "SongSelect_ForeColor_Variety": {
										SongSelect_ForeColor_Variety = ColorTranslator.FromHtml(strParam);
										break;
									}
								case "SongSelect_ForeColor_Classic": {
										SongSelect_ForeColor_Classic = ColorTranslator.FromHtml(strParam);
										break;
									}
								case "SongSelect_ForeColor_GameMusic": {
										SongSelect_ForeColor_GameMusic = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(SongSelect_ForeColor_Namco): {
										SongSelect_ForeColor_GameMusic = ColorTranslator.FromHtml(strParam);
										break;
									}
								case "SongSelect_BackColor_JPOP": {
										SongSelect_BackColor_JPOP = ColorTranslator.FromHtml(strParam);
										break;
									}
								case "SongSelect_BackColor_Anime": {
										SongSelect_BackColor_Anime = ColorTranslator.FromHtml(strParam);
										break;
									}
								case "SongSelect_BackColor_VOCALOID": {
										SongSelect_BackColor_VOCALOID = ColorTranslator.FromHtml(strParam);
										break;
									}
								case "SongSelect_BackColor_Children": {
										SongSelect_BackColor_Children = ColorTranslator.FromHtml(strParam);
										break;
									}
								case "SongSelect_BackColor_Variety": {
										SongSelect_BackColor_Variety = ColorTranslator.FromHtml(strParam);
										break;
									}
								case "SongSelect_BackColor_Classic": {
										SongSelect_BackColor_Classic = ColorTranslator.FromHtml(strParam);
										break;
									}
								case "SongSelect_BackColor_GameMusic": {
										SongSelect_BackColor_GameMusic = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(SongSelect_BackColor_Namco): {
										SongSelect_BackColor_Namco = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(SongSelect_CorrectionX_Chara): {
										SongSelect_CorrectionX_Chara = strParam.Split(',').ToArray();
										break;
									}
								case nameof(SongSelect_CorrectionY_Chara): {
										SongSelect_CorrectionY_Chara = strParam.Split(',').ToArray();
										break;
									}
								case nameof(SongSelect_CorrectionX_Chara_Value): {
										SongSelect_CorrectionX_Chara_Value = int.Parse(strParam);
										break;
									}
								case nameof(SongSelect_CorrectionY_Chara_Value): {
										SongSelect_CorrectionY_Chara_Value = int.Parse(strParam);
										break;
									}
								case nameof(SongSelect_Rotate_Chara): {
										SongSelect_Rotate_Chara = strParam.Split(',').ToArray();
										break;
									}
								#endregion

								#region DaniSelect
								/*
                                case "DaniSelect_Dan_Text_X":
                                {
                                    string[] strSplit = strParam.Split(',');
                                    for (int i = 0; i < 4; i++)
                                    {
                                        DaniSelect_Dan_Text_X[i] = int.Parse(strSplit[i]);
                                    }
                                }
                                case "DaniSelect_Dan_Text_Y":
                                {
                                    string[] strSplit = strParam.Split(',');
                                    for (int i = 0; i < 4; i++)
                                    {
                                        DaniSelect_Dan_Text_Y[i] = int.Parse(strSplit[i]);
                                    }
                                }
                                */
								case "DaniSelect_DanSides_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_DanSides_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_DanSides_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_DanSides_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_DanPlate": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_DanPlate[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Rank": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_Rank[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Bloc2": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_Bloc2[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Text_Gauge": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_Text_Gauge[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Value_Gauge": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_Value_Gauge[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_DanIcon_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_DanIcon_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_DanIcon_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_DanIcon_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Title_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Title_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Title_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Title_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Difficulty_Cymbol_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Difficulty_Cymbol_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Difficulty_Cymbol_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Difficulty_Cymbol_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Level_Number_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Level_Number_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Level_Number_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Level_Number_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Level_Number_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_Level_Number_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Soul_Number_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_Soul_Number_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Soul_Number_Text_Width": {
										DaniSelect_Soul_Number_Text_Width = int.Parse(strParam);
										break;
									}
								case "DaniSelect_Exam_Number_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_Exam_Number_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Exam_Number_Text_Width": {
										DaniSelect_Exam_Number_Text_Width = int.Parse(strParam);
										break;
									}
								case "DaniSelect_Font_DanFolder_Size": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_Font_DanFolder_Size[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_FolderText_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											DaniSelect_FolderText_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_FolderText_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											DaniSelect_FolderText_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Font_DanSong_Size": {
										DaniSelect_Font_DanSong_Size = int.Parse(strParam);
										break;
									}
								case "DaniSelect_Font_Exam_Size": {
										DaniSelect_Font_Exam_Size = int.Parse(strParam);
										break;
									}
								case "DaniSelect_Exam_Bloc_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Exam_Bloc_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Exam_Bloc_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Exam_Bloc_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Exam_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Exam_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Exam_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Exam_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Exam_X_Ex": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Exam_X_Ex[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Exam_Y_Ex": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Exam_Y_Ex[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Exam_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Exam_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Exam_Title_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Exam_Title_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Exam_Title_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Exam_Title_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Challenge_Select_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Challenge_Select_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Challenge_Select_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DaniSelect_Challenge_Select_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Challenge_Select_Rect_Option": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											DaniSelect_Challenge_Select_Rect[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Challenge_Select_Rect_Start": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											DaniSelect_Challenge_Select_Rect[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Challenge_Select_Rect_Back": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											DaniSelect_Challenge_Select_Rect[2][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Plate": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_Plate[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Plate_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_Plate_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Plate_Center_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_Plate_Center_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_Plate_Title_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_Plate_Title_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_DanIconTitle_Size": {
										DaniSelect_DanIconTitle_Size = int.Parse(strParam);
										break;
									}
								case "DaniSelect_DanIconTitle_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_DanIconTitle_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DaniSelect_DanIcon_Color": {
										string[] strSplit = strParam.Split(',');
										DaniSelect_DanIcon_Color = new Color[strSplit.Length];
										for (int i = 0; i < strSplit.Length; i++) {
											DaniSelect_DanIcon_Color[i] = ColorTranslator.FromHtml(strSplit[i]);
										}
										break;
									}
								#endregion

								#region SongLoading
								case nameof(SongLoading_Plate_X): {
										SongLoading_Plate_X = int.Parse(strParam);
										break;
									}
								case nameof(SongLoading_Plate_Y): {
										SongLoading_Plate_Y = int.Parse(strParam);
										break;
									}
								case nameof(SongLoading_Title_X): {
										SongLoading_Title_X = int.Parse(strParam);
										break;
									}
								case nameof(SongLoading_Title_Y): {
										SongLoading_Title_Y = int.Parse(strParam);
										break;
									}
								case nameof(SongLoading_Title_MaxSize): {
										SongLoading_Title_MaxSize = int.Parse(strParam);
										break;
									}
								case nameof(SongLoading_SubTitle_X): {
										SongLoading_SubTitle_X = int.Parse(strParam);
										break;
									}
								case nameof(SongLoading_SubTitle_Y): {
										SongLoading_SubTitle_Y = int.Parse(strParam);
										break;
									}
								case nameof(SongLoading_SubTitle_MaxSize): {
										SongLoading_SubTitle_MaxSize = int.Parse(strParam);
										break;
									}
								case nameof(SongLoading_Plate_X_AI): {
										SongLoading_Plate_X_AI = int.Parse(strParam);
										break;
									}
								case nameof(SongLoading_Plate_Y_AI): {
										SongLoading_Plate_Y_AI = int.Parse(strParam);
										break;
									}
								case nameof(SongLoading_Title_X_AI): {
										SongLoading_Title_X_AI = int.Parse(strParam);
										break;
									}
								case nameof(SongLoading_Title_Y_AI): {
										SongLoading_Title_Y_AI = int.Parse(strParam);
										break;
									}
								case nameof(SongLoading_SubTitle_X_AI): {
										SongLoading_SubTitle_X_AI = int.Parse(strParam);
										break;
									}
								case nameof(SongLoading_SubTitle_Y_AI): {
										SongLoading_SubTitle_Y_AI = int.Parse(strParam);
										break;
									}
								case "SongLoading_Fade_AI_Anime_Ring": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongLoading_Fade_AI_Anime_Ring[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongLoading_Fade_AI_Anime_LoadBar": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongLoading_Fade_AI_Anime_LoadBar[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "SongLoading_DanPlate": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongLoading_DanPlate[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case nameof(SongLoading_Title_FontSize): {
										if (int.Parse(strParam) > 0)
											SongLoading_Title_FontSize = int.Parse(strParam);
										break;
									}
								case nameof(SongLoading_SubTitle_FontSize): {
										if (int.Parse(strParam) > 0)
											SongLoading_SubTitle_FontSize = int.Parse(strParam);
										break;
									}
								case "SongLoading_Chara_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											SongLoading_Chara_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case nameof(SongLoading_Plate_ReferencePoint): {
										SongLoading_Plate_ReferencePoint = (ReferencePoint)int.Parse(strParam);
										break;
									}
								case nameof(SongLoading_Title_ReferencePoint): {
										SongLoading_Title_ReferencePoint = (ReferencePoint)int.Parse(strParam);
										break;
									}
								case nameof(SongLoading_SubTitle_ReferencePoint): {
										SongLoading_SubTitle_ReferencePoint = (ReferencePoint)int.Parse(strParam);
										break;
									}

								case nameof(SongLoading_Title_ForeColor): {
										SongLoading_Title_ForeColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(SongLoading_Title_BackColor): {
										SongLoading_Title_BackColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(SongLoading_SubTitle_ForeColor): {
										SongLoading_SubTitle_ForeColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(SongLoading_SubTitle_BackColor): {
										SongLoading_SubTitle_BackColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(SongLoading_Plate_ScreenBlend): {
										SongLoading_Plate_ScreenBlend = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case nameof(DaniSelect_DanPlateTitle_Size): {
										DaniSelect_DanPlateTitle_Size = int.Parse(strParam);
										break;
									}
								case "DaniSelect_DanPlateTitle_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DaniSelect_DanPlateTitle_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								#endregion

								#region Game
								case "Game_Notes_Anime": {
										Game_Notes_Anime = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case "Game_ScrollField_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											nScrollFieldX[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_ScrollField_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											nScrollFieldY[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_ScrollField_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											nScrollField_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_ScrollField_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											nScrollField_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_SENotes_Offset_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											nSENotesX[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_SENotes_Offset_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											nSENotesY[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_SENotes_Offset_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											nSENotes_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_SENotes_Offset_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											nSENotes_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Notes_Size": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Notes_Size[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_SENote_Size": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_SENote_Size[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case nameof(Game_Notes_Interval): {
										Game_Notes_Interval = int.Parse(strParam);
										break;
									}
								case "Game_Notes_Arm_Offset_Left_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Notes_Arm_Offset_Left_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Notes_Arm_Offset_Right_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Notes_Arm_Offset_Right_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Notes_Arm_Offset_Left_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Notes_Arm_Offset_Left_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Notes_Arm_Offset_Right_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Notes_Arm_Offset_Right_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Notes_Arm_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Notes_Arm_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Judge_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Judge_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Judge_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Judge_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Judge_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Judge_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_ScoreRank_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_ScoreRank_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_ScoreRank_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_ScoreRank_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_ScoreRank_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_ScoreRank_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_StageText": {
										Game_StageText = strParam;
										break;
									}
								case nameof(Game_RollColorMode): {
										Game_RollColorMode = (RollColorMode)int.Parse(strParam);
										break;
									}
								case nameof(Game_JudgeFrame_AddBlend): {
										Game_JudgeFrame_AddBlend = CConversion.bONorOFF(strParam[0]);
										break;
									}


								case "Game_Judge_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Judge_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Judge_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Judge_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_UIMove_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_UIMove_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_UIMove_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_UIMove_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_ScoreRank_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_ScoreRank_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_ScoreRank_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_ScoreRank_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}

								#region CourseSymbol
								case "Game_CourseSymbol_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_CourseSymbol_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_CourseSymbol_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_CourseSymbol_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_CourseSymbol_Back_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_CourseSymbol_Back_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_CourseSymbol_Back_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_CourseSymbol_Back_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_CourseSymbol_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_CourseSymbol_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_CourseSymbol_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_CourseSymbol_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_CourseSymbol_Back_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_CourseSymbol_Back_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_CourseSymbol_Back_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_CourseSymbol_Back_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_CourseSymbol_Back_Rect_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Game_CourseSymbol_Back_Rect_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_CourseSymbol_Back_Rect_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Game_CourseSymbol_Back_Rect_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								#endregion

								#region PanelFont
								case nameof(Game_MusicName_X): {
										Game_MusicName_X = int.Parse(strParam);
										break;
									}
								case nameof(Game_MusicName_Y): {
										Game_MusicName_Y = int.Parse(strParam);
										break;
									}
								case nameof(Game_MusicName_FontSize): {
										if (int.Parse(strParam) > 0)
											Game_MusicName_FontSize = int.Parse(strParam);
										break;
									}
								case nameof(Game_MusicName_MaxWidth): {
										Game_MusicName_MaxWidth = int.Parse(strParam);
										break;
									}
								case nameof(Game_MusicName_ReferencePoint): {
										Game_MusicName_ReferencePoint = (ReferencePoint)int.Parse(strParam);
										break;
									}
								case nameof(Game_Genre_X): {
										Game_Genre_X = int.Parse(strParam);
										break;
									}
								case nameof(Game_Genre_Y): {
										Game_Genre_Y = int.Parse(strParam);
										break;
									}
								case "Game_GenreText_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_GenreText_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case nameof(Game_GenreText_FontSize): {
										Game_GenreText_FontSize = int.Parse(strParam);
										break;
									}
								case nameof(Game_Lyric_X): {
										Game_Lyric_X = int.Parse(strParam);
										break;
									}
								case nameof(Game_Lyric_Y): {
										Game_Lyric_Y = int.Parse(strParam);
										break;
									}
								case nameof(Game_Lyric_FontName): {
										strParam = strParam.Replace('/', System.IO.Path.DirectorySeparatorChar);
										strParam = strParam.Replace('\\', System.IO.Path.DirectorySeparatorChar);
										if (HPrivateFastFont.FontExists(strParam)) Game_Lyric_FontName = strParam;
										strParam = Path(strParam);
										if (HPrivateFastFont.FontExists(strParam)) Game_Lyric_FontName = strParam;
										break;
									}
								case nameof(Game_Lyric_FontSize): {
										if (int.Parse(strParam) > 0)
											Game_Lyric_FontSize = int.Parse(strParam);
										break;
									}
								case nameof(Game_Lyric_VTTRubyOffset): {
										Game_Lyric_VTTRubyOffset = int.Parse(strParam);
										break;
									}
								case nameof(Game_Lyric_VTTForeColor): {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 8; i++) {
											Game_Lyric_VTTForeColor[i] = ColorTranslator.FromHtml(strSplit[i]);
										}
										break;
									}
								case nameof(Game_Lyric_VTTBackColor): {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 8; i++) {
											Game_Lyric_VTTBackColor[i] = ColorTranslator.FromHtml(strSplit[i]);
										}
										break;
									}
								case nameof(Game_Lyric_ReferencePoint): {
										Game_Lyric_ReferencePoint = (ReferencePoint)int.Parse(strParam);
										break;
									}

								case nameof(Game_MusicName_ForeColor): {
										Game_MusicName_ForeColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(Game_StageText_ForeColor): {
										Game_StageText_ForeColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(Game_Lyric_ForeColor): {
										Game_Lyric_ForeColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(Game_MusicName_BackColor): {
										Game_MusicName_BackColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(Game_StageText_BackColor): {
										Game_StageText_BackColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(Game_Lyric_BackColor): {
										Game_Lyric_BackColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								case "Game_Judge_Meter": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Judge_Meter[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Judge_Meter_Perfect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Judge_Meter_Perfect[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Judge_Meter_Good": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Judge_Meter_Good[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Judge_Meter_Miss": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Judge_Meter_Miss[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Judge_Meter_Roll": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Judge_Meter_Roll[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Judge_Meter_HitRate": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Judge_Meter_HitRate[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Judge_Meter_PerfectRate": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Judge_Meter_PerfectRate[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Judge_Meter_GoodRate": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Judge_Meter_GoodRate[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Judge_Meter_MissRate": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Judge_Meter_MissRate[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								#endregion

								// Chara read
								#region Chara
								case "Game_Chara_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Chara_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Chara_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Chara_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Chara_Balloon_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Chara_Balloon_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Chara_Balloon_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Chara_Balloon_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case nameof(Game_Chara_Balloon_Timer): {
										if (int.Parse(strParam) > 0)
											Game_Chara_Balloon_Timer = int.Parse(strParam);
										break;
									}
								case nameof(Game_Chara_Balloon_Delay): {
										if (int.Parse(strParam) > 0)
											Game_Chara_Balloon_Delay = int.Parse(strParam);
										break;
									}
								case nameof(Game_Chara_Balloon_FadeOut): {
										if (int.Parse(strParam) > 0)
											Game_Chara_Balloon_FadeOut = int.Parse(strParam);
										break;
									}
								// パターン数の設定はTextureLoader.csで反映されます。
								case "Game_Chara_Motion_Normal": {
										Game_Chara_Motion_Normal = strParam;
										break;
									}
								case "Game_Chara_Motion_Clear": {
										Game_Chara_Motion_Clear = strParam;
										break;
									}
								case "Game_Chara_Motion_GoGo": {
										Game_Chara_Motion_GoGo = strParam;
										break;
									}
								case "Game_Chara_Beat_Normal": {
										ParseInt32(value => Game_Chara_Beat_Normal = value);
										break;
									}
								case "Game_Chara_Beat_Clear": {
										ParseInt32(value => Game_Chara_Beat_Clear = value);
										break;
									}
								case "Game_Chara_Beat_GoGo": {
										ParseInt32(value => Game_Chara_Beat_GoGo = value);
										break;
									}
								#endregion

								#region Mob
								case "Game_Mob_Beat": {
										ParseInt32(value => Game_Mob_Beat = value);
										break;
									}
								case "Game_Mob_Ptn_Beat": {
										ParseInt32(value => Game_Mob_Ptn_Beat = value);
										break;
									}
								#endregion

								#region Score
								case "Game_Score_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											this.Game_Score_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Score_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											this.Game_Score_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Score_Add_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											this.Game_Score_Add_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Score_Add_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											this.Game_Score_Add_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Score_AddBonus_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											this.Game_Score_AddBonus_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Score_AddBonus_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											this.Game_Score_AddBonus_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}

								case "Game_Score_Padding": {
										ParseInt32(value => Game_Score_Padding = value);
										break;
									}
								case "Game_Score_Size": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Score_Size[i] = int.Parse(strSplit[i]);
										}
										break;
									}


								case "Game_Score_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Score_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Score_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Score_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Score_Add_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Score_Add_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Score_Add_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Score_Add_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Score_AddBonus_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Score_AddBonus_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Score_AddBonus_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Score_AddBonus_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}

								#endregion

								#region Taiko
								case "Game_Taiko_Background_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Background_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Background_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Background_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_ModIcons_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_ModIcons_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_ModIcons_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_ModIcons_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_NamePlate_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_NamePlate_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_NamePlate_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_NamePlate_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}

								case "Game_Taiko_PlayerNumber_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_PlayerNumber_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}

								case "Game_Taiko_PlayerNumber_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_PlayerNumber_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}

								case "Game_Taiko_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Ex_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_Ex_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Ex_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_Ex_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Ex4_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_Ex4_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Ex4_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_Ex4_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Padding": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											Game_Taiko_Combo_Padding[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Size": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_Size[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Size_Ex": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_Size_Ex[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Scale": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											Game_Taiko_Combo_Scale[i] = float.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Text_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_Text_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Text_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_Text_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Text_Size": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_Text_Size[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case nameof(Game_Taiko_Combo_Ex_IsJumping): {
										Game_Taiko_Combo_Ex_IsJumping = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case "Game_Taiko_LevelChange_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_LevelChange_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_LevelChange_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_LevelChange_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Frame_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Frame_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Frame_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Frame_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}


								case "Game_Taiko_Background_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Background_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Background_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Background_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_ModIcons_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_ModIcons_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_ModIcons_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_ModIcons_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_NamePlate_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_NamePlate_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_NamePlate_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_NamePlate_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_PlayerNumber_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_PlayerNumber_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_PlayerNumber_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_PlayerNumber_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Ex_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_Ex_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Ex_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_Ex_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Ex4_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_Ex4_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Ex4_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_Ex4_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Text_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_Text_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Combo_Text_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Combo_Text_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Frame_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Frame_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Taiko_Frame_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Taiko_Frame_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								#endregion

								#region Gauge
								case "Game_Gauge_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Gauge_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Gauge_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Gauge_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Gauge_X_AI": {
										Game_Gauge_X_AI = int.Parse(strParam);
										break;
									}
								case "Game_Gauge_Y_AI": {
										Game_Gauge_Y_AI = int.Parse(strParam);
										break;
									}
								case "Game_Gauge_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Game_Gauge_Rect[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Gauge_ClearText_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Gauge_ClearText_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Gauge_ClearText_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Gauge_ClearText_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Gauge_ClearText_X_AI": {
										Game_Gauge_ClearText_X_AI = int.Parse(strParam);
										break;
									}
								case "Game_Gauge_ClearText_Y_AI": {
										Game_Gauge_ClearText_Y_AI = int.Parse(strParam);
										break;
									}
								case "Game_Gauge_ClearText_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Game_Gauge_ClearText_Rect[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Gauge_ClearText_Clear_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Game_Gauge_ClearText_Clear_Rect[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Gauge_Soul_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Gauge_Soul_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Gauge_Soul_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Gauge_Soul_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Gauge_Soul_X_AI": {
										Gauge_Soul_X_AI = int.Parse(strParam);
										break;
									}
								case "Gauge_Soul_Y_AI": {
										Gauge_Soul_Y_AI = int.Parse(strParam);
										break;
									}
								case "Gauge_Soul_X_Tower": {
										Gauge_Soul_X_Tower = int.Parse(strParam);
										break;
									}
								case "Gauge_Soul_Y_Tower": {
										Gauge_Soul_Y_Tower = int.Parse(strParam);
										break;
									}
								case "Gauge_Soul_Fire_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Gauge_Soul_Fire_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Gauge_Soul_Fire_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Gauge_Soul_Fire_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Gauge_Soul_Fire_X_AI": {
										Gauge_Soul_Fire_X_AI = int.Parse(strParam);
										break;
									}
								case "Gauge_Soul_Fire_Y_AI": {
										Gauge_Soul_Fire_Y_AI = int.Parse(strParam);
										break;
									}
								case "Gauge_Soul_Fire_X_Tower": {
										Gauge_Soul_Fire_X_Tower = int.Parse(strParam);
										break;
									}
								case "Gauge_Soul_Fire_Y_Tower": {
										Gauge_Soul_Fire_Y_Tower = int.Parse(strParam);
										break;
									}
								case "Game_Gauge_Rainbow_Timer": {
										if (int.Parse(strParam) != 0) {
											Game_Gauge_Rainbow_Timer = int.Parse(strParam);
										}
										break;
									}
								case "Game_Tower_Floor_Number": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Tower_Floor_Number[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Tower_Life_Number": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Tower_Life_Number[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Tower_Font_TouTatsuKaiSuu": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Tower_Font_TouTatsuKaiSuu[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Tower_Font_Kai": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Tower_Font_Kai[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Tower_Font_TowerText": {
										Game_Tower_Font_TowerText = int.Parse(strParam);
										break;
									}


								case "Game_Gauge_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Gauge_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Gauge_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Gauge_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Gauge_Soul_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Gauge_Soul_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Gauge_Soul_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Gauge_Soul_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Gauge_Soul_Fire_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Gauge_Soul_Fire_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Gauge_Soul_Fire_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Gauge_Soul_Fire_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								#endregion

								#region Balloon
								case "Game_Balloon_Combo_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Combo_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Combo_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Combo_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Combo_Number_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Combo_Number_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Combo_Number_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Combo_Number_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Combo_Number_Ex_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Combo_Number_Ex_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Combo_Number_Ex_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Combo_Number_Ex_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Combo_Number_Size": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Combo_Number_Size[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Combo_Number_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Combo_Number_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Combo_Text_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Combo_Text_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Combo_Text_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Combo_Text_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Combo_Text_Ex_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Combo_Text_Ex_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Combo_Text_Ex_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Combo_Text_Ex_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Combo_Text_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Game_Balloon_Combo_Text_Rect[i] = int.Parse(strSplit[i]);
										}
										break;
									}


								case "Game_Balloon_Balloon_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											this.Game_Balloon_Balloon_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Balloon_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											this.Game_Balloon_Balloon_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Balloon_Frame_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											this.Game_Balloon_Balloon_Frame_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Balloon_Frame_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											this.Game_Balloon_Balloon_Frame_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Balloon_Number_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											this.Game_Balloon_Balloon_Number_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Balloon_Number_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											this.Game_Balloon_Balloon_Number_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}

								case "Game_Balloon_Roll_Frame_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Roll_Frame_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Roll_Frame_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Roll_Frame_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Roll_Number_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Roll_Number_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Roll_Number_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Roll_Number_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Number_Size": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Number_Size[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Number_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Number_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Roll_Number_Scale": {
										ParseInt32(value => Game_Balloon_Roll_Number_Scale = value);
										break;
									}
								case "Game_Balloon_Balloon_Number_Scale": {
										ParseInt32(value => Game_Balloon_Balloon_Number_Scale = value);
										break;
									}
								case "Game_Kusudama_Number_X": {
										ParseInt32(value => Game_Kusudama_Number_X = value);
										break;
									}
								case "Game_Kusudama_Number_Y": {
										ParseInt32(value => Game_Kusudama_Number_Y = value);
										break;
									}


								case "Game_Balloon_Balloon_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Balloon_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Balloon_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Balloon_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Balloon_Frame_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Balloon_Frame_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Balloon_Frame_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Balloon_Frame_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Balloon_Number_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Balloon_Number_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Balloon_Balloon_Number_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Balloon_Balloon_Number_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}

								#endregion

								#region Effects
								case nameof(Game_Effect_Roll_StartPoint_X): {
										Game_Effect_Roll_StartPoint_X = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_Roll_StartPoint_Y): {
										Game_Effect_Roll_StartPoint_Y = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_Roll_StartPoint_1P_X): {
										Game_Effect_Roll_StartPoint_1P_X = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_Roll_StartPoint_1P_Y): {
										Game_Effect_Roll_StartPoint_1P_Y = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_Roll_StartPoint_2P_X): {
										Game_Effect_Roll_StartPoint_2P_X = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_Roll_StartPoint_2P_Y): {
										Game_Effect_Roll_StartPoint_2P_Y = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_Roll_Speed_X): {
										Game_Effect_Roll_Speed_X = strParam.Split(',').Select(float.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_Roll_Speed_Y): {
										Game_Effect_Roll_Speed_Y = strParam.Split(',').Select(float.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_Roll_Speed_1P_X): {
										Game_Effect_Roll_Speed_1P_X = strParam.Split(',').Select(float.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_Roll_Speed_1P_Y): {
										Game_Effect_Roll_Speed_1P_Y = strParam.Split(',').Select(float.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_Roll_Speed_2P_X): {
										Game_Effect_Roll_Speed_2P_X = strParam.Split(',').Select(float.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_Roll_Speed_2P_Y): {
										Game_Effect_Roll_Speed_2P_Y = strParam.Split(',').Select(float.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_NotesFlash): {
										Game_Effect_NotesFlash = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_NotesFlash_Timer): {
										Game_Effect_NotesFlash_Timer = int.Parse(strParam);
										break;
									}
								case nameof(Game_Effect_GoGoSplash): {
										Game_Effect_GoGoSplash = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_GoGoSplash_X): {
										Game_Effect_GoGoSplash_X = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_GoGoSplash_Y): {
										Game_Effect_GoGoSplash_Y = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_GoGoSplash_Rotate): {
										Game_Effect_GoGoSplash_Rotate = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case nameof(Game_Effect_GoGoSplash_Timer): {
										Game_Effect_GoGoSplash_Timer = int.Parse(strParam);
										break;
									}
								case nameof(Game_Effect_FlyingNotes_StartPoint_X): {
										Game_Effect_FlyingNotes_StartPoint_X = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_FlyingNotes_StartPoint_Y): {
										Game_Effect_FlyingNotes_StartPoint_Y = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_FlyingNotes_EndPoint_X): {
										Game_Effect_FlyingNotes_EndPoint_X = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_FlyingNotes_EndPoint_Y): {
										Game_Effect_FlyingNotes_EndPoint_Y = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_FlyingNotes_EndPoint_X_AI): {
										Game_Effect_FlyingNotes_EndPoint_X_AI = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_FlyingNotes_EndPoint_Y_AI): {
										Game_Effect_FlyingNotes_EndPoint_Y_AI = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_FlyingNotes_Sine): {
										Game_Effect_FlyingNotes_Sine = int.Parse(strParam);
										break;
									}
								case nameof(Game_Effect_FlyingNotes_IsUsingEasing): {
										Game_Effect_FlyingNotes_IsUsingEasing = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case nameof(Game_Effect_FlyingNotes_Timer): {
										Game_Effect_FlyingNotes_Timer = int.Parse(strParam);
										break;
									}
								case nameof(Game_Effect_FireWorks): {
										Game_Effect_FireWorks = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Effect_FireWorks_Timer): {
										Game_Effect_FireWorks_Timer = int.Parse(strParam);
										break;
									}
								case "Game_Effect_Rainbow_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											this.Game_Effect_Rainbow_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Effect_Rainbow_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											this.Game_Effect_Rainbow_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case nameof(Game_Effect_Rainbow_Timer): {
										Game_Effect_Rainbow_Timer = int.Parse(strParam);
										break;
									}
								case "Game_Effects_Hit_Explosion_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Effects_Hit_Explosion_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Effects_Hit_Explosion_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Effects_Hit_Explosion_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case nameof(Game_Effect_HitExplosion_AddBlend): {
										Game_Effect_HitExplosion_AddBlend = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case nameof(Game_Effect_HitExplosionBig_AddBlend): {
										Game_Effect_HitExplosionBig_AddBlend = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case "Game_Effect_Fire_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Effect_Fire_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Effect_Fire_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Effect_Fire_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case nameof(Game_Effect_FireWorks_AddBlend): {
										Game_Effect_FireWorks_AddBlend = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case nameof(Game_Effect_Fire_AddBlend): {
										Game_Effect_Fire_AddBlend = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case nameof(Game_Effect_GoGoSplash_AddBlend): {
										Game_Effect_GoGoSplash_AddBlend = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case nameof(Game_Effect_FireWorks_Timing): {
										Game_Effect_FireWorks_Timing = int.Parse(strParam);
										break;
									}


								case "Game_Effects_Hit_Explosion_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Effects_Hit_Explosion_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Effects_Hit_Explosion_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Effects_Hit_Explosion_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Effect_Fire_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Effect_Fire_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Effect_Fire_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Effect_Fire_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}

								#endregion

								#region Lane

								case "Game_Lane_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Lane_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Lane_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Lane_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Lane_Sub_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Lane_Sub_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Lane_Sub_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Lane_Sub_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}

								case "Game_Lane_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Lane_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Lane_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Lane_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								#endregion

								#region Runner
								#endregion

								#region Dan_C
								case nameof(Game_DanC_Title_ForeColor): {
										Game_DanC_Title_ForeColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(Game_DanC_Title_BackColor): {
										Game_DanC_Title_BackColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(Game_DanC_SubTitle_ForeColor): {
										Game_DanC_SubTitle_ForeColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(Game_DanC_SubTitle_BackColor): {
										Game_DanC_SubTitle_BackColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(Game_DanC_X): {
										Game_DanC_X = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_DanC_Y): {
										Game_DanC_Y = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_DanC_Base_Offset_X): {
										Game_DanC_Base_Offset_X = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_DanC_Base_Offset_Y): {
										Game_DanC_Base_Offset_Y = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_DanC_SmallBase_Offset_X): {
										Game_DanC_SmallBase_Offset_X = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_DanC_SmallBase_Offset_Y): {
										Game_DanC_SmallBase_Offset_Y = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}

								case nameof(Game_DanC_Size): {
										Game_DanC_Size = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}

								case nameof(Game_DanC_Padding): {
										ParseInt32(value => Game_DanC_Padding = value);
										break;
									}

								case nameof(Game_DanC_Offset): {
										Game_DanC_Offset = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}

								case nameof(Game_DanC_Number_Size): {
										Game_DanC_Number_Size = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}

								case nameof(Game_DanC_Small_Number_Size): {
										Game_DanC_Small_Number_Size = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}

								case nameof(Game_DanC_MiniNumber_Size): {
										Game_DanC_MiniNumber_Size = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}

								case nameof(Game_DanC_Number_Padding): {
										ParseInt32(value => Game_DanC_Number_Padding = value);
										break;
									}

								case nameof(Game_DanC_Number_Small_Scale): {
										Game_DanC_Number_Small_Scale = float.Parse(strParam);
										break;
									}

								case nameof(Game_DanC_Number_Small_Padding): {
										ParseInt32(value => Game_DanC_Number_Small_Padding = value);
										break;
									}

								case nameof(Game_DanC_Number_XY): {
										Game_DanC_Number_XY = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_DanC_Number_Small_Number_Offset): {
										Game_DanC_Number_Small_Number_Offset = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_DanC_ExamType_Size): {
										Game_DanC_ExamType_Size = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_DanC_ExamRange_Size): {
										Game_DanC_ExamRange_Size = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}

								case nameof(Game_DanC_ExamRange_Padding): {
										ParseInt32(value => Game_DanC_ExamRange_Padding = value);
										break;
									}

								case nameof(Game_DanC_Percent_Hit_Score_Padding): {
										Game_DanC_Percent_Hit_Score_Padding = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_DanC_ExamUnit_Size): {
										Game_DanC_ExamUnit_Size = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_DanC_Exam_Offset): {
										Game_DanC_Exam_Offset = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_DanC_Dan_Plate): {
										Game_DanC_Dan_Plate = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_DanC_DanIcon_Offset): {
										Game_DanC_DanIcon_Offset = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_DanC_DanIcon_Offset_Mini): {
										Game_DanC_DanIcon_Offset_Mini = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_DanC_Title_X): {
										Game_DanC_Title_X = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_DanC_Title_Y): {
										Game_DanC_Title_Y = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_DanC_SubTitle): {
										Game_DanC_SubTitle = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}

								case nameof(Game_DanC_Title_Size): {
										ParseInt32(value => Game_DanC_Title_Size = value);
										break;
									}
								case nameof(Game_DanC_SubTitle_Size): {
										ParseInt32(value => Game_DanC_SubTitle_Size = value);
										break;
									}
								case nameof(Game_DanC_ExamFont_Size): {
										ParseInt32(value => Game_DanC_ExamFont_Size = value);
										break;
									}
								case nameof(Game_DanC_Title_MaxWidth): {
										ParseInt32(value => Game_DanC_Title_MaxWidth = value);
										break;
									}
								case nameof(Game_DanC_SubTitle_MaxWidth): {
										ParseInt32(value => Game_DanC_SubTitle_MaxWidth = value);
										break;
									}

								#endregion

								#region PuchiChara
								case nameof(Game_PuchiChara_X): {
										Game_PuchiChara_X = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_PuchiChara_Y): {
										Game_PuchiChara_Y = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_PuchiChara_4P): {
										Game_PuchiChara_4P = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_PuchiChara_5P): {
										Game_PuchiChara_5P = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_PuchiChara_BalloonX): {
										Game_PuchiChara_BalloonX = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_PuchiChara_BalloonY): {
										Game_PuchiChara_BalloonY = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_PuchiChara_KusudamaX): {
										Game_PuchiChara_KusudamaX = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_PuchiChara_KusudamaY): {
										Game_PuchiChara_KusudamaY = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_PuchiChara_Scale): {
										Game_PuchiChara_Scale = strParam.Split(',').Select(float.Parse).ToArray();
										break;
									}
								case nameof(Game_PuchiChara): {
										Game_PuchiChara = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_PuchiChara_Sine): {
										ParseInt32(value => Game_PuchiChara_Sine = value);
										break;
									}
								case nameof(Game_PuchiChara_Timer): {
										ParseInt32(value => Game_PuchiChara_Timer = value);
										break;
									}
								case nameof(Game_PuchiChara_SineTimer): {
										Game_PuchiChara_SineTimer = double.Parse(strParam);
										break;
									}
								#endregion

								#region Training
								case "Game_Training_DownBG": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Training_DownBG[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Training_BigTaiko": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Training_BigTaiko[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Training_Speed_Measure": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Training_Speed_Measure[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case nameof(Game_Training_ScrollTime): {
										Game_Training_ScrollTime = int.Parse(strParam);
										break;
									}
								case nameof(Game_Training_ProgressBar_XY): {
										Game_Training_ProgressBar_XY = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Training_GoGoPoint_Y): {
										Game_Training_GoGoPoint_Y = int.Parse(strParam);
										break;
									}
								case nameof(Game_Training_JumpPoint_Y): {
										Game_Training_JumpPoint_Y = int.Parse(strParam);
										break;
									}
								case nameof(Game_Training_MaxMeasureCount_XY): {
										Game_Training_MaxMeasureCount_XY = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Training_CurrentMeasureCount_XY): {
										Game_Training_CurrentMeasureCount_XY = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Training_SpeedDisplay_XY): {
										Game_Training_SpeedDisplay_XY = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Game_Training_SmallNumber_Width): {
										Game_Training_SmallNumber_Width = int.Parse(strParam);
										break;
									}
								case nameof(Game_Training_BigNumber_Width): {
										Game_Training_BigNumber_Width = int.Parse(strParam);
										break;
									}
								#endregion

								#region Tower
								case "Game_Tower_Sky_Gradient": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Tower_Sky_Gradient[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Tower_Sky_Gradient_Size": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Tower_Sky_Gradient_Size[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Tower_Floors_Body": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Tower_Floors_Body[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Tower_Floors_Deco": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Tower_Floors_Deco[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Tower_Floors_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Tower_Floors_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Tower_Don": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Tower_Don[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Tower_Don_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Tower_Don_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_Tower_Miss": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_Tower_Miss[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								#endregion

								#region AIBattle
								case "Game_AIBattle_CharaMove": {
										Game_AIBattle_CharaMove = int.Parse(strParam);
										break;
									}
								case "Game_AIBattle_SectionTime_Panel": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_SectionTime_Panel[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_SectionTime_Bar": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_SectionTime_Bar[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Batch_Base": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Batch_Base[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Batch": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Batch[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Batch_Size": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Batch_Size[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Batch_Anime": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Batch_Anime[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Batch_Anime_Size": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Batch_Anime_Size[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Batch_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Batch_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Judge_Meter_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Judge_Meter_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Judge_Meter_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Judge_Meter_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Judge_Number_Perfect_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Judge_Number_Perfect_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Judge_Number_Perfect_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Judge_Number_Perfect_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Judge_Number_Good_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Judge_Number_Good_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Judge_Number_Good_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Judge_Number_Good_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Judge_Number_Miss_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Judge_Number_Miss_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Judge_Number_Miss_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Judge_Number_Miss_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Judge_Number_Roll_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Judge_Number_Roll_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Judge_Number_Roll_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Judge_Number_Roll_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Game_AIBattle_Judge_Number_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Game_AIBattle_Judge_Number_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								#endregion

								#endregion

								#region Result
								case "Result_Use1PUI": {
										Result_Use1PUI = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case nameof(Result_Cloud_Count): {
										Result_Cloud_Count = int.Parse(strParam);
										break;
									}
								case "Result_Cloud_X": {
										Result_Cloud_X = new int[Result_Cloud_Count];
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Result_Cloud_Count; i++) {
											Result_Cloud_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Result_Cloud_Y": {
										Result_Cloud_Y = new int[Result_Cloud_Count];
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Result_Cloud_Count; i++) {
											Result_Cloud_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Result_Cloud_MaxMove": {
										Result_Cloud_MaxMove = new int[Result_Cloud_Count];
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Result_Cloud_Count; i++) {
											Result_Cloud_MaxMove[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case nameof(Result_Shine_Count): {
										Result_Shine_Count = int.Parse(strParam);
										break;
									}
								case "Result_Shine_1P_X": {
										Result_Shine_X[0] = new int[Result_Shine_Count];
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Result_Shine_Count; i++) {
											Result_Shine_X[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Shine_2P_X": {
										Result_Shine_X[1] = new int[Result_Shine_Count];
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Result_Shine_Count; i++) {
											Result_Shine_X[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Shine_1P_Y": {
										Result_Shine_Y[0] = new int[Result_Shine_Count];
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Result_Shine_Count; i++) {
											Result_Shine_Y[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Shine_2P_Y": {
										Result_Shine_Y[1] = new int[Result_Shine_Count];
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Result_Shine_Count; i++) {
											Result_Shine_Y[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Shine_Size": {
										Result_Shine_Size = new float[Result_Shine_Count];
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Result_Cloud_Count; i++) {
											Result_Shine_Size[i] = float.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Work_1P_X": {
										Result_Work_X[0] = new int[3];
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Result_Cloud_Count; i++) {
											Result_Work_X[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Work_2P_X": {
										Result_Work_X[1] = new int[3];
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Result_Cloud_Count; i++) {
											Result_Work_X[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Work_1P_Y": {
										Result_Work_Y[0] = new int[3];
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Result_Cloud_Count; i++) {
											Result_Work_Y[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Work_2P_Y": {
										Result_Work_Y[1] = new int[3];
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Result_Cloud_Count; i++) {
											Result_Work_Y[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_DifficultyBar_Size": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_DifficultyBar_Size[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_DifficultyBar_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_DifficultyBar_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_DifficultyBar_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_DifficultyBar_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_Base_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Gauge_Base_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_Base_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Gauge_Base_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Gauge_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Gauge_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Result_Gauge_Rect[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_Rainbow_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Gauge_Rainbow_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_Rainbow_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Gauge_Rainbow_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_Rainbow_Interval": {
										Result_Gauge_Rainbow_Interval = int.Parse(strParam);
										break;
									}
								case "Result_Gauge_ClearText_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Gauge_ClearText_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_ClearText_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Gauge_ClearText_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_ClearText_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Result_Gauge_ClearText_Rect[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_ClearText_Clear_Rect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Result_Gauge_ClearText_Clear_Rect[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Number_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Number_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Number_Scale_4P": {
										Result_Number_Scale_4P = float.Parse(strParam);
										break;
									}
								case "Result_Number_Scale_5P": {
										Result_Number_Scale_5P = float.Parse(strParam);
										break;
									}
								case "Result_Soul_Fire_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Soul_Fire_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Soul_Fire_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Soul_Fire_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Soul_Text_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Soul_Text_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Soul_Text_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Soul_Text_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Perfect_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Perfect_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Perfect_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Perfect_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Good_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Good_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Good_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Good_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Miss_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Miss_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Miss_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Miss_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Roll_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Roll_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Roll_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Roll_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_MaxCombo_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_MaxCombo_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_MaxCombo_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_MaxCombo_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_ADLib_Show": {
										Result_ADLib_Show = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case "Result_ADLib_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_ADLib_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_ADLib_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_ADLib_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Bomb_Show": {
										Result_Bomb_Show = CConversion.bONorOFF(strParam[0]);
										break;
									}
								case "Result_Bomb_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Bomb_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Bomb_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Bomb_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Score_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Score_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Score_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Score_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Score_Number_Interval": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Score_Number_Interval[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Score_Scale_4P": {
										Result_Score_Scale_4P = float.Parse(strParam);
										break;
									}
								case "Result_Score_Scale_5P": {
										Result_Score_Scale_5P = float.Parse(strParam);
										break;
									}
								case "Result_ScoreRankEffect_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_ScoreRankEffect_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_ScoreRankEffect_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_ScoreRankEffect_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_CrownEffect_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_CrownEffect_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_CrownEffect_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_CrownEffect_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Speech_Bubble_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Speech_Bubble_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Speech_Bubble_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Speech_Bubble_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Speech_Bubble_V2_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Speech_Bubble_V2_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Speech_Bubble_V2_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Speech_Bubble_V2_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Speech_Bubble_V2_2P_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Speech_Bubble_V2_2P_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Speech_Bubble_V2_2P_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Speech_Bubble_V2_2P_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case nameof(Result_MusicName_X): {
										Result_MusicName_X = int.Parse(strParam);
										break;
									}
								case nameof(Result_MusicName_Y): {
										Result_MusicName_Y = int.Parse(strParam);
										break;
									}
								case nameof(Result_MusicName_FontSize): {
										if (int.Parse(strParam) > 0)
											Result_MusicName_FontSize = int.Parse(strParam);
										break;
									}
								case nameof(Result_MusicName_MaxSize): {
										Result_MusicName_MaxSize = int.Parse(strParam);
										break;
									}
								case nameof(Result_MusicName_ReferencePoint): {
										Result_MusicName_ReferencePoint = (ReferencePoint)int.Parse(strParam);
										break;
									}
								case nameof(Result_StageText_X): {
										Result_StageText_X = int.Parse(strParam);
										break;
									}
								case nameof(Result_StageText_Y): {
										Result_StageText_Y = int.Parse(strParam);
										break;
									}
								case nameof(Result_StageText_FontSize): {
										if (int.Parse(strParam) > 0)
											Result_StageText_FontSize = int.Parse(strParam);
										break;
									}
								case nameof(Result_StageText_ReferencePoint): {
										Result_StageText_ReferencePoint = (ReferencePoint)int.Parse(strParam);
										break;
									}

								case nameof(Result_MusicName_ForeColor): {
										Result_MusicName_ForeColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(Result_StageText_ForeColor): {
										Result_StageText_ForeColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								//case nameof(Result_StageText_ForeColor_Red):
								//{
								//    Result_StageText_ForeColor_Red = ColorTranslator.FromHtml(strParam);
								//}
								case nameof(Result_MusicName_BackColor): {
										Result_MusicName_BackColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								case nameof(Result_StageText_BackColor): {
										Result_StageText_BackColor = ColorTranslator.FromHtml(strParam);
										break;
									}
								//case nameof(Result_StageText_BackColor_Red):
								//{
								//    Result_StageText_BackColor_Red = ColorTranslator.FromHtml(strParam);
								//}

								case "Result_NamePlate_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_NamePlate_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_NamePlate_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_NamePlate_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_ModIcons_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_ModIcons_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_ModIcons_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_ModIcons_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Flower_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Flower_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Flower_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Flower_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Flower_Rotate_1P_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											Result_Flower_Rotate_X[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Flower_Rotate_2P_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											Result_Flower_Rotate_X[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Flower_Rotate_1P_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											Result_Flower_Rotate_Y[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Flower_Rotate_2P_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											Result_Flower_Rotate_Y[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case nameof(Result_PlateShine_Count): {
										Result_PlateShine_Count = int.Parse(strParam);
										break;
									}
								case "Result_PlateShine_1P_X": {
										Result_PlateShine_X[0] = new int[Result_PlateShine_Count];
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Result_PlateShine_Count; i++) {
											Result_PlateShine_X[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_PlateShine_2P_X": {
										Result_PlateShine_X[1] = new int[Result_PlateShine_Count];
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Result_PlateShine_Count; i++) {
											Result_PlateShine_X[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_PlateShine_1P_Y": {
										Result_PlateShine_Y[0] = new int[Result_PlateShine_Count];
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Result_PlateShine_Count; i++) {
											Result_PlateShine_Y[0][i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_PlateShine_2P_Y": {
										Result_PlateShine_Y[1] = new int[Result_PlateShine_Count];
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Result_PlateShine_Count; i++) {
											Result_PlateShine_Y[1][i] = int.Parse(strSplit[i]);
										}
										break;
									}

								case nameof(Result_Dan): {
										Result_Dan = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Result_Dan_XY): {
										Result_Dan_XY = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}
								case nameof(Result_Dan_Plate_XY): {
										Result_Dan_Plate_XY = strParam.Split(',').Select(int.Parse).ToArray();
										break;
									}


								case "Result_UIMove_4P": {
										string[] strSplit = strParam.Split(',');
										// for (int i = 0; i < 2; i++)
										// {
										//Result_UIMove_4P[i] = int.Parse(strSplit[i]);
										// }

										for (int i = 0; i < 4; i++) {
											int moveX = int.Parse(strSplit[0]);
											Result_UIMove_4P_X[i] = moveX * i;

											int moveY = int.Parse(strSplit[1]);
											Result_UIMove_4P_Y[i] = moveY * i;
										}
										break;
									}
								case "Result_UIMove_5P": {
										string[] strSplit = strParam.Split(',');
										// for (int i = 0; i < 2; i++)
										// {
										//Result_UIMove_5P[i] = int.Parse(strSplit[i]);
										// }

										for (int i = 0; i < 5; i++) {
											int moveX = int.Parse(strSplit[0]);
											Result_UIMove_5P_X[i] = moveX * i;

											int moveY = int.Parse(strSplit[1]);
											Result_UIMove_5P_Y[i] = moveY * i;
										}
										break;
									}
								case "Result_UIMove_4P_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Result_UIMove_4P_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_UIMove_4P_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Result_UIMove_4P_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_UIMove_5P_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											Result_UIMove_5P_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_UIMove_5P_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											Result_UIMove_5P_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_DifficultyBar_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_DifficultyBar_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_DifficultyBar_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_DifficultyBar_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_Base_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Gauge_Base_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_Base_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Gauge_Base_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Gauge_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Gauge_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_ClearText_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Gauge_ClearText_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_ClearText_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Gauge_ClearText_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_Rainbow_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Gauge_Rainbow_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Gauge_Rainbow_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Gauge_Rainbow_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Soul_Fire_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Soul_Fire_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Soul_Fire_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Soul_Fire_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Soul_Text_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Soul_Text_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Soul_Text_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Soul_Text_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Perfect_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Perfect_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Perfect_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Perfect_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Good_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Good_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Good_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Good_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Miss_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Miss_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Miss_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Miss_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Roll_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Roll_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Roll_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Roll_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_MaxCombo_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_MaxCombo_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_MaxCombo_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_MaxCombo_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_ADLib_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_ADLib_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_ADLib_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_ADLib_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Bomb_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Bomb_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Bomb_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Bomb_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Score_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Score_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Score_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Score_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_ScoreRankEffect_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_ScoreRankEffect_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_ScoreRankEffect_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_ScoreRankEffect_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_CrownEffect_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_CrownEffect_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_CrownEffect_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_CrownEffect_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Speech_Bubble_V2_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Speech_Bubble_V2_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Speech_Bubble_V2_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Speech_Bubble_V2_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Speech_Text_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_Speech_Text_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_Speech_Text_Size": {
										Result_Speech_Text_Size = int.Parse(strParam);
										break;
									}
								case "Result_Speech_Text_MaxWidth": {
										Result_Speech_Text_MaxWidth = int.Parse(strParam);
										break;
									}
								case "Result_NamePlate_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_NamePlate_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_NamePlate_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_NamePlate_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_ModIcons_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_ModIcons_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_ModIcons_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_ModIcons_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}

								#endregion

								#region AIResult
								case "Result_AIBattle_Batch": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_AIBattle_Batch[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_AIBattle_Batch_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_AIBattle_Batch_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_AIBattle_SectionPlate_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_AIBattle_SectionPlate_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_AIBattle_SectionText_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_AIBattle_SectionText_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Result_AIBattle_SectionText_Scale": {
										Result_AIBattle_SectionText_Scale = int.Parse(strParam);
										break;
									}
								case "Result_AIBattle_WinFlag": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Result_AIBattle_WinFlag[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								#endregion

								#region DanResult
								case "DanResult_StatePanel": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DanResult_StatePanel[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_SongPanel_Main_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_SongPanel_Main_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_SongPanel_Main_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_SongPanel_Main_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Difficulty_Cymbol_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_Difficulty_Cymbol_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Difficulty_Cymbol_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_Difficulty_Cymbol_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Level_Number_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_Level_Number_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Level_Number_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_Level_Number_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Sections_Perfect_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_Sections_Perfect_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Sections_Perfect_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_Sections_Perfect_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Sections_Good_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_Sections_Good_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Sections_Good_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_Sections_Good_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Sections_Miss_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_Sections_Miss_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Sections_Miss_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_Sections_Miss_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Sections_Roll_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_Sections_Roll_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Sections_Roll_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_Sections_Roll_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Perfect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DanResult_Perfect[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Good": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DanResult_Good[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Miss": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DanResult_Miss[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Roll": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DanResult_Roll[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_MaxCombo": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DanResult_MaxCombo[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_TotalHit": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DanResult_TotalHit[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Score": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DanResult_Score[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Exam": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DanResult_Exam[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_DanTitles_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_DanTitles_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_DanTitles_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_DanTitles_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_DanIcon_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_DanIcon_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_DanIcon_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 3; i++) {
											DanResult_DanIcon_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Rank": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											DanResult_Rank[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "DanResult_Font_DanTitles_Size": {
										DanResult_Font_DanTitles_Size = int.Parse(strParam);
										break;
									}
								#endregion

								#region TowerResult
								case "TowerResult_ScoreRankEffect": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											TowerResult_ScoreRankEffect[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "TowerResult_Toutatsu": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											TowerResult_Toutatsu[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "TowerResult_MaxFloors": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											TowerResult_MaxFloors[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "TowerResult_Ten": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											TowerResult_Ten[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "TowerResult_Score": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											TowerResult_Score[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "TowerResult_CurrentFloor": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											TowerResult_CurrentFloor[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "TowerResult_ScoreCount": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											TowerResult_ScoreCount[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "TowerResult_RemainingLifes": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											TowerResult_RemainingLifes[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "TowerResult_Gauge_Soul": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											TowerResult_Gauge_Soul[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "TowerResult_Font_TowerText": {
										TowerResult_Font_TowerText = int.Parse(strParam);
										break;
									}
								case "TowerResult_Font_TowerText48": {
										TowerResult_Font_TowerText48 = int.Parse(strParam);
										break;
									}
								case "TowerResult_Font_TowerText72": {
										TowerResult_Font_TowerText72 = int.Parse(strParam);
										break;
									}
								#endregion

								#region Heya
								case "Heya_Main_Menu_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											Heya_Main_Menu_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Heya_Main_Menu_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											Heya_Main_Menu_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Heya_Main_Menu_Font_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											Heya_Main_Menu_Font_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Heya_Font_Scale": {
										Heya_Font_Scale = int.Parse(strParam);
										break;
									}
								case "Heya_Center_Menu_Box_Count": {
										Heya_Center_Menu_Box_Count = int.Parse(strParam);
										break;
									}
								case "Heya_Center_Menu_Box_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Heya_Center_Menu_Box_Count; i++) {
											Heya_Center_Menu_Box_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Heya_Center_Menu_Box_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Heya_Center_Menu_Box_Count; i++) {
											Heya_Center_Menu_Box_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Heya_Center_Menu_Box_Item_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Heya_Center_Menu_Box_Item_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Heya_Center_Menu_Box_Name_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Heya_Center_Menu_Box_Name_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Heya_Center_Menu_Box_Authors_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Heya_Center_Menu_Box_Authors_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Heya_Side_Menu_Count": {
										Heya_Side_Menu_Count = int.Parse(strParam);
										break;
									}
								case "Heya_Side_Menu_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Heya_Side_Menu_Count; i++) {
											Heya_Side_Menu_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Heya_Side_Menu_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < Heya_Side_Menu_Count; i++) {
											Heya_Side_Menu_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Heya_Side_Menu_Font_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Heya_Side_Menu_Font_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Heya_InfoSection": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Heya_InfoSection[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Heya_DescriptionTextOrigin": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Heya_DescriptionTextOrigin[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								#endregion

								#region OnlineLounge 
								case "OnlineLounge_Side_Menu": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OnlineLounge_Side_Menu[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OnlineLounge_Side_Menu_Text_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OnlineLounge_Side_Menu_Text_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OnlineLounge_Side_Menu_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OnlineLounge_Side_Menu_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OnlineLounge_Song": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OnlineLounge_Song[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OnlineLounge_Song_Title_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OnlineLounge_Song_Title_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OnlineLounge_Song_SubTitle_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OnlineLounge_Song_SubTitle_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OnlineLounge_Song_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OnlineLounge_Song_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OnlineLounge_Context_Charter": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OnlineLounge_Context_Charter[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OnlineLounge_Context_Genre": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OnlineLounge_Context_Genre[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OnlineLounge_Context_Couse_Symbol": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OnlineLounge_Context_Couse_Symbol[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OnlineLounge_Context_Level": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OnlineLounge_Context_Level[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OnlineLounge_Context_Couse_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OnlineLounge_Context_Couse_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OnlineLounge_Downloading": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OnlineLounge_Downloading[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OnlineLounge_Font_OLFont": {
										OnlineLounge_Font_OLFont = int.Parse(strParam);
										break;
									}
								case "OnlineLounge_Font_OLFontLarge": {
										OnlineLounge_Font_OLFontLarge = int.Parse(strParam);
										break;
									}
								#endregion

								#region TowerSelect
								case "TowerSelect_Title_Size": {
										TowerSelect_Title_Size = int.Parse(strParam);
										break;
									}
								case "TowerSelect_Title_MaxWidth": {
										TowerSelect_Title_MaxWidth = int.Parse(strParam);
										break;
									}
								case "TowerSelect_Title_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											TowerSelect_Title_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "TowerSelect_SubTitle_Size": {
										TowerSelect_SubTitle_Size = int.Parse(strParam);
										break;
									}
								case "TowerSelect_SubTitle_MaxWidth": {
										TowerSelect_SubTitle_MaxWidth = int.Parse(strParam);
										break;
									}
								case "TowerSelect_SubTitle_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											TowerSelect_SubTitle_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "TowerSelect_Bar_Count": {
										TowerSelect_Bar_Count = int.Parse(strParam);
										break;
									}
								case "TowerSelect_Bar_X": {
										TowerSelect_Bar_X = new int[TowerSelect_Bar_Count];

										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < TowerSelect_Bar_Count; i++) {
											TowerSelect_Bar_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "TowerSelect_Bar_Y": {
										TowerSelect_Bar_Y = new int[TowerSelect_Bar_Count];

										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < TowerSelect_Bar_Count; i++) {
											TowerSelect_Bar_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								#endregion

								#region OpenEncyclopedia 
								case "OpenEncyclopedia_Context_Item2": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OpenEncyclopedia_Context_Item2[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OpenEncyclopedia_Context_Item3": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OpenEncyclopedia_Context_Item3[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OpenEncyclopedia_Context_PageText": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OpenEncyclopedia_Context_PageText[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OpenEncyclopedia_Side_Menu": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OpenEncyclopedia_Side_Menu[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OpenEncyclopedia_Side_Menu_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OpenEncyclopedia_Side_Menu_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OpenEncyclopedia_Side_Menu_Text_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											OpenEncyclopedia_Side_Menu_Text_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "OpenEncyclopedia_Font_EncyclopediaMenu_Size": {
										OpenEncyclopedia_Font_EncyclopediaMenu_Size = int.Parse(strParam);
										break;
									}
								#endregion

								#region Exit
								case "Exit_Duration": {
										Exit_Duration = int.Parse(strParam);
										break;
									}
								#endregion

								#region Font
								case nameof(Font_Edge_Ratio): //Config画面や簡易メニューのフォントについて(rhimm)
								{
										if (int.Parse(strParam) > 0)
											Font_Edge_Ratio = int.Parse(strParam);
										break;
									}
								case nameof(Font_Edge_Ratio_Vertical): //TITLEやSUBTITLEなど、縦に書かれることのあるフォントについて(rhimm)
								{
										if (int.Parse(strParam) > 0)
											Font_Edge_Ratio_Vertical = int.Parse(strParam);
										break;
									}
								case nameof(Text_Correction_X): {
										Text_Correction_X = int.Parse(strParam);
										break;
									}
								case nameof(Text_Correction_Y): {
										Text_Correction_Y = int.Parse(strParam);
										break;
									}
								#endregion

								#region PopupMenu
								case "PopupMenu_Menu_Title": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											PopupMenu_Menu_Title[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "PopupMenu_Title": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											PopupMenu_Title[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "PopupMenu_Menu_Highlight": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											PopupMenu_Menu_Highlight[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "PopupMenu_MenuItem_Name": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											PopupMenu_MenuItem_Name[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "PopupMenu_MenuItem_Value": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											PopupMenu_MenuItem_Value[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "PopupMenu_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											PopupMenu_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "PopupMenu_Font_Size": {
										PopupMenu_Font_Size = int.Parse(strParam);
										break;
									}
								#endregion

								#region NamePlate
								case "NamePlate_Title_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											NamePlate_Title_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "NamePlate_Dan_Offset": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											NamePlate_Dan_Offset[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "NamePlate_Name_Offset_Normal": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											NamePlate_Name_Offset_Normal[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "NamePlate_Name_Offset_WithTitle": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											NamePlate_Name_Offset_WithTitle[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "NamePlate_Name_Offset_Full": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											NamePlate_Name_Offset_Full[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "NamePlate_Name_Width_Normal": {
										NamePlate_Name_Width_Normal = int.Parse(strParam);
										break;
									}
								case "NamePlate_Name_Width_Full": {
										NamePlate_Name_Width_Full = int.Parse(strParam);
										break;
									}
								case "NamePlate_Title_Width": {
										NamePlate_Title_Width = int.Parse(strParam);
										break;
									}
								case "NamePlate_Dan_Width": {
										NamePlate_Dan_Width = int.Parse(strParam);
										break;
									}
								case "NamePlate_Font_Name_Size_Normal": {
										NamePlate_Font_Name_Size_Normal = int.Parse(strParam);
										break;
									}
								case "NamePlate_Font_Name_Size_WithTitle": {
										NamePlate_Font_Name_Size_WithTitle = int.Parse(strParam);
										break;
									}
								case "NamePlate_Font_Title_Size": {
										NamePlate_Font_Title_Size = int.Parse(strParam);
										break;
									}
								case "NamePlate_Font_Dan_Size": {
										NamePlate_Font_Dan_Size = int.Parse(strParam);
										break;
									}
								#endregion

								#region Modal 
								case "Modal_Title_Full": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Modal_Title_Full[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Title_Half_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Modal_Title_Half_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Title_Half_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Modal_Title_Half_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Text_Full": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Modal_Text_Full[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Text_Full_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Modal_Text_Full_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Text_Half_X": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Modal_Text_Half_X[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Text_Half_Y": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Modal_Text_Half_Y[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Text_Half_Move": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Modal_Text_Half_Move[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Font_ModalContentHalf_Size": {
										Modal_Font_ModalContentHalf_Size = int.Parse(strParam);
										break;
									}
								case "Modal_Font_ModalTitleHalf_Size": {
										Modal_Font_ModalTitleHalf_Size = int.Parse(strParam);
										break;
									}
								case "Modal_Font_ModalContentFull_Size": {
										Modal_Font_ModalContentFull_Size = int.Parse(strParam);
										break;
									}
								case "Modal_Font_ModalTitleFull_Size": {
										Modal_Font_ModalTitleFull_Size = int.Parse(strParam);
										break;
									}
								case "Modal_Title_Half_X_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Modal_Title_Half_X_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Title_Half_X_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											Modal_Title_Half_X_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Title_Half_Y_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Modal_Title_Half_Y_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Title_Half_Y_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											Modal_Title_Half_Y_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Text_Half_X_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Modal_Text_Half_X_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Text_Half_X_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											Modal_Text_Half_X_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Text_Half_Y_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 4; i++) {
											Modal_Text_Half_Y_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Text_Half_Y_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 5; i++) {
											Modal_Text_Half_Y_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Text_Half_Move_4P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Modal_Text_Half_Move_4P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								case "Modal_Text_Half_Move_5P": {
										string[] strSplit = strParam.Split(',');
										for (int i = 0; i < 2; i++) {
											Modal_Text_Half_Move_5P[i] = int.Parse(strSplit[i]);
										}
										break;
									}
								#endregion
								default:
									foreach (string code in CLangManager.Langcodes) {
										if (strCommand == "FontName" + code.ToUpper()) {
											strParam = strParam.Replace('/', System.IO.Path.DirectorySeparatorChar);
											strParam = strParam.Replace('\\', System.IO.Path.DirectorySeparatorChar);
											if (HPrivateFastFont.FontExists(strParam)) _fontNameLocalized.Add(code, strParam);
											strParam = Path(strParam);
											if (HPrivateFastFont.FontExists(strParam)) _fontNameLocalized.Add(code, strParam);
										}
										if (strCommand == "BoxFontName" + code.ToUpper()) {
											strParam = strParam.Replace('/', System.IO.Path.DirectorySeparatorChar);
											strParam = strParam.Replace('\\', System.IO.Path.DirectorySeparatorChar);
											if (HPrivateFastFont.FontExists(strParam)) _boxFontNameLocalized.Add(code, strParam);
											strParam = Path(strParam);
											if (HPrivateFastFont.FontExists(Path(strParam))) _boxFontNameLocalized.Add(code, strParam);
										}
									}
									break;

									#endregion
							}
						}
						continue;
					} catch (Exception exception) {
						Trace.TraceError(exception.ToString());
						Trace.TraceError("例外が発生しましたが処理を継続します。 (6a32cc37-1527-412e-968a-512c1f0135cd)");
						continue;
					}
				}
			}
		}

		private void t座標の追従設定() {
			//
			if (bFieldBgPointOverride == true) {

			}
		}

		#region [ IDisposable 実装 ]
		//-----------------
		public void Dispose() {
			if (!this.bDisposed済み) {
				for (int i = 0; i < this.nシステムサウンド数; i++)
					this[i].Dispose();

				this.bDisposed済み = true;
			}
		}
		//-----------------
		#endregion


		// その他

		#region [ private ]
		//-----------------
		private bool bDisposed済み;
		//-----------------
		#endregion

		#region 背景(スクロール)
		public int[] Background_Scroll_Y = new int[] { 0, 536 };
		#endregion


		#region[ 座標 ]
		//2017.08.11 kairera0467 DP実用化に向けてint配列に変更

		//フィールド位置　Xは判定枠部分の位置。Yはフィールドの最上部の座標。
		//現時点ではノーツ画像、Senotes画像、判定枠が連動する。
		//Xは中央基準描画、Yは左上基準描画
		public int[] nScrollFieldX = new int[] { 349, 349 };
		public int[] nScrollFieldY = new int[] { 192, 368 };

		public int[] nScrollField_4P = new int[] { 349, 46 };
		public int[] nScrollField_5P = new int[] { 349, 27 };

		//中心座標指定
		public int[] nJudgePointX = new int[] { 413, 413, 413, 413 };
		public int[] nJudgePointY = new int[] { 256, 433, 0, 0 };

		//フィールド背景画像
		//ScrollField座標への追従設定が可能。
		//分岐背景、ゴーゴー背景が連動する。(全て同じ大きさ、位置で作成すること。)
		//左上基準描画
		public bool bFieldBgPointOverride = false;
		/*
        public int[] nScrollFieldBGX = new int[] { 333, 333, 333, 333 };
        public int[] nScrollFieldBGY = new int[] { 192, 368, 0, 0 };
        */
		//SEnotes
		//音符座標に加算
		public int[] nSENotesX = new int[] { -2, -2 };
		public int[] nSENotesY = new int[] { 131, 131 };

		public int[] nSENotes_4P = new int[] { -2, 100 };
		public int[] nSENotes_5P = new int[] { -2, 94 };

		//光る太鼓部分
		public int nMtaikoBackgroundX = 0;
		public int nMtaikoBackgroundY = 184;
		public int nMtaikoFieldX = 0;
		public int nMtaikoFieldY = 184;
		public int nMtaikoMainX = 0;
		public int nMtaikoMainY = 0;

		//コンボ
		public int[] nComboNumberX = new int[] { 0, 0, 0, 0 };
		public int[] nComboNumberY = new int[] { 212, 388, 0, 0 };
		public int[] nComboNumberTextY = new int[] { 271, 447, 0, 0 };
		public int[] nComboNumberTextLargeY = new int[] { 270, 446, 0, 0 };
		public float fComboNumberSpacing = 0;
		public float fComboNumberSpacing_l = 0;

		public E難易度表示タイプ eDiffDispMode;
		public bool b現在のステージ数を表示しない;

		//リザルト画面
		//現在のデフォルト値はダミーです。
		public int nResultPanelP1X = 0;
		public int nResultPanelP1Y = 0;
		public int nResultPanelP2X = 515;
		public int nResultPanelP2Y = 75;
		public int nResultScoreP1X = 295;
		public int nResultScoreP1Y = 212;
		public int nResultJudge1_P1X = 495;
		public int nResultJudge1_P1Y = 182;
		public int nResultJudge2_P1X = 968;
		public int nResultJudge2_P1Y = 174;

		public int nResultNumberP1X = 490;
		public int nResultNumberP2X = 875;

		public int nResultNumberY = 188;
		public int nResultNumberYPadding = 42;

		public int nResultGaugeBaseP1X = 56;
		public int nResultGaugeBaseP1Y = 141;
		public int nResultGaugeBaseP2X = 555;
		public int nResultGaugeBaseP2Y = 122;
		public int nResultGaugeBodyP1X = 559;
		public int nResultGaugeBodyP1Y = 125;
		#endregion

		public string[] strStringを配列に直す(string str) {
			string[] strArray = str.Split(',');
			return strArray;
		}

		public enum RollColorMode {
			None, // PS4, Switchなど
			All, // 旧筐体(旧作含む)
			WithoutStart // 新筐体
		}
		public enum ReferencePoint //テクスチャ描画の基準点を変更可能にするための値(rhimm)
		{
			Center,
			Left,
			Right
		}

		#region 新・SkinConfig
		#region General
		public string Skin_Name = "Unknown";
		public string Skin_Version = "Unknown";
		public string Skin_Creator = "Unknown";
		public int[] Resolution = new int[] { 1280, 720 };
		public string FontName { get { return _fontNameLocalized.TryGetValue(CLangManager.fetchLang(), out string value) ? value : ""; } }
		private Dictionary<string, string> _fontNameLocalized = new Dictionary<string, string>();
		public string BoxFontName { get { return _boxFontNameLocalized.TryGetValue(CLangManager.fetchLang(), out string value) ? value : ""; } }
		private Dictionary<string, string> _boxFontNameLocalized = new Dictionary<string, string>();
		#endregion

		#region Config

		public int Config_NamePlate_Ptn_Title;
		public int[] Config_NamePlate_Ptn_Title_Boxes;

		public int[] Config_Arrow_X = new int[] { 552, 552 };
		public int[] Config_Arrow_Y = new int[] { 297, 363 };

		public int[] Config_Arrow_Focus_X = new int[] { 552, 552 };
		public int[] Config_Arrow_Focus_Y = new int[] { 279, 381 };

		public int[] Config_Item_X = new int[] { 282, 282, 282 };
		public int[] Config_Item_Y = new int[] { 153, 192, 231 };
		public int Config_Item_Width = 100;
		public int[] Config_Item_Font_Offset = new int[] { 0, 8 };

		public int Config_Font_Scale = 20;
		public float Config_Font_Scale_Description = 14.0f;

		public Color Config_Selected_Menu_Text_Grad_Color_1 = Color.Yellow;
		public Color Config_Selected_Menu_Text_Grad_Color_2 = Color.OrangeRed;

		public int Config_ItemBox_Count = 10;
		public int[] Config_ItemBox_X = new int[] { 602, 602, 602, 602, 602, 602, 602, 602, 602, 602 };
		public int[] Config_ItemBox_Y = new int[] { 4, 79, 154, 229, 304, 379, 454, 529, 604, 679 };
		public int[] Config_ItemBox_Font_Offset = new int[] { 20, 12 };
		public int[] Config_ItemBox_ItemValue_Font_Offset = new int[] { 400, 12 };

		public int[] Config_ExplanationPanel = new int[] { 67, 382 };
		public int[] Config_SkinSample1 = new int[] { 124, 449 };

		public int[] Config_KeyAssign = new int[] { 389, 215 };
		public int[] Config_KeyAssign_Menu_Highlight = new int[] { 324, 66 };
		public int[] Config_KeyAssign_Font = new int[] { 308, 64 };
		public int Config_KeyAssign_Move = 20;

		public int[] Config_Calibration_OffsetText = new int[] { 300, 288 };
		public int[] Config_Calibration_InfoText = new int[] { 8, 550 };
		public Rectangle[] Config_Calibration_Highlights = new Rectangle[] {
			new Rectangle(371, 724, 371, 209),
			new Rectangle(774, 724, 371, 209),
			new Rectangle(1179, 724, 371, 209)
		};

		#endregion

		#region Puchichara

		public int Puchichara_Ptn;
		public string[] Puchicharas_Name;

		#endregion

		#region Characters

		public int Characters_Ptn;
		public string[] Characters_DirName;
		public int[] Characters_Normal_Ptn,
			Characters_Normal_Missed_Ptn,
			Characters_Normal_MissedDown_Ptn,
			Characters_Normal_Cleared_Ptn,
			Characters_Normal_Maxed_Ptn,
			Characters_MissIn_Ptn,
			Characters_MissDownIn_Ptn,
			Characters_GoGoTime_Ptn,
			Characters_GoGoTime_Maxed_Ptn,
			Characters_10Combo_Ptn,
			Characters_10Combo_Clear_Ptn,
			Characters_10Combo_Maxed_Ptn,
			Characters_GoGoStart_Ptn,
			Characters_GoGoStart_Clear_Ptn,
			Characters_GoGoStart_Maxed_Ptn,
			Characters_Become_Cleared_Ptn,
			Characters_Become_Maxed_Ptn,
			Characters_SoulOut_Ptn,
			Characters_ClearOut_Ptn,
			Characters_Return_Ptn,
			Characters_Balloon_Breaking_Ptn,
			Characters_Balloon_Broke_Ptn,
			Characters_Balloon_Miss_Ptn,
			Characters_Kusudama_Idle_Ptn,
			Characters_Kusudama_Breaking_Ptn,
			Characters_Kusudama_Broke_Ptn,
			Characters_Kusudama_Miss_Ptn,
			Characters_Title_Entry_Ptn,
			Characters_Title_Normal_Ptn,
			Characters_Menu_Loop_Ptn,
			Characters_Menu_Select_Ptn,
			Characters_Menu_Start_Ptn,
			Characters_Menu_Wait_Ptn,
			Characters_Result_Clear_Ptn,
			Characters_Result_Failed_Ptn,
			Characters_Result_Failed_In_Ptn,
			Characters_Result_Normal_Ptn,
			Characters_Tower_Standing_Ptn,
			Characters_Tower_Climbing_Ptn,
			Characters_Tower_Running_Ptn,
			Characters_Tower_Clear_Ptn,
			Characters_Tower_Fail_Ptn,
			Characters_Tower_Standing_Tired_Ptn,
			Characters_Tower_Climbing_Tired_Ptn,
			Characters_Tower_Running_Tired_Ptn,
			Characters_Tower_Clear_Tired_Ptn;

		// Config

		public int[][] Characters_Resolution;
		public int[][] Characters_Heya_Render_Offset;
		public bool[] Characters_UseResult1P;
		public int[][] Characters_X;
		public int[][] Characters_Y;
		public int[][] Characters_4P;
		public int[][] Characters_5P;
		public int[][] Characters_X_AI;
		public int[][] Characters_Y_AI;
		public int[][] Characters_Balloon_X;
		public int[][] Characters_Balloon_Y;
		public int[][] Characters_Balloon_4P;
		public int[][] Characters_Balloon_5P;
		public int[][] Characters_Kusudama_X;
		public int[][] Characters_Kusudama_Y;
		public int[][] Characters_Motion_Normal,
			Characters_Motion_10Combo,
			Characters_Motion_10Combo_Clear,
			Characters_Motion_10ComboMax,
			Characters_Motion_Miss,
			Characters_Motion_MissDown,
			Characters_Motion_ClearIn,
			Characters_Motion_Clear,
			Characters_Motion_ClearMax,
			Characters_Motion_MissIn,
			Characters_Motion_MissDownIn,
			Characters_Motion_GoGoStart,
			Characters_Motion_GoGoStart_Clear,
			Characters_Motion_GoGoStartMax,
			Characters_Motion_GoGo,
			Characters_Motion_GoGoMax,
			Characters_Motion_SoulIn,
			Characters_Motion_SoulOut,
			Characters_Motion_ClearOut,
			Characters_Motion_Return;
		public float[] Characters_Beat_Normal,
			Characters_Beat_10Combo,
			Characters_Beat_10Combo_Clear,
			Characters_Beat_10ComboMax,
			Characters_Beat_Miss,
			Characters_Beat_MissDown,
			Characters_Beat_ClearIn,
			Characters_Beat_Clear,
			Characters_Beat_ClearMax,
			Characters_Beat_MissIn,
			Characters_Beat_MissDownIn,
			Characters_Beat_GoGoStart,
			Characters_Beat_GoGoStart_Clear,
			Characters_Beat_GoGoStartMax,
			Characters_Beat_GoGo,
			Characters_Beat_GoGoMax,
			Characters_Beat_SoulIn,
			Characters_Beat_SoulOut,
			Characters_Beat_ClearOut,
			Characters_Beat_Return,
			Characters_Beat_Tower_Standing,
			Characters_Beat_Tower_Standing_Tired,
			Characters_Beat_Tower_Fail,
			Characters_Beat_Tower_Clear,
			Characters_Beat_Tower_Clear_Tired;
		public bool[] Characters_Tower_Clear_IsLooping,
			Characters_Tower_Clear_Tired_IsLooping,
			Characters_Tower_Fail_IsLooping;
		public int[] Characters_Balloon_Timer;
		public int[] Characters_Balloon_Delay;
		public int[] Characters_Balloon_FadeOut;

		public int[] Characters_Title_Entry_AnimationDuration;
		public int[] Characters_Title_Normal_AnimationDuration;
		public int[] Characters_Menu_Loop_AnimationDuration;
		public int[] Characters_Menu_Select_AnimationDuration;
		public int[] Characters_Menu_Start_AnimationDuration;
		public int[] Characters_Menu_Wait_AnimationDuration;
		public int[] Characters_Result_Normal_AnimationDuration;
		public int[] Characters_Result_Clear_AnimationDuration;
		public int[] Characters_Result_Failed_In_AnimationDuration;
		public int[] Characters_Result_Failed_AnimationDuration;

		#endregion

		#region [Adjustments]
		public int[] Adjustments_MenuPuchichara_X = new int[] { -100, 100 };
		public int[] Adjustments_MenuPuchichara_Y = new int[] { -100, -100 };

		#endregion

		#region [Startup]
		public int StartUp_LangSelect_FontSize = 16;
		#endregion

		#region [Title Screen]

		public int Title_LoadingPinInstances = 5;
		public int Title_LoadingPinFrameCount = 8;
		public int Title_LoadingPinCycle = 320;
		public int[] Title_LoadingPinBase = new int[] { 480, 410 };
		public int[] Title_LoadingPinDiff = new int[] { 90, 0 };

		public int[] Title_Entry_Bar_Text_X = new int[] { 563, 563 };
		public int[] Title_Entry_Bar_Text_Y = new int[] { 312, 430 };

		public int[] Title_Banapas_Load_Clear_Anime = new int[] { 198, 514 };

		public int[] Title_Entry_Player_Select_X = new int[] { 337, 529, 743 };
		public int[] Title_Entry_Player_Select_Y = new int[] { 488, 487, 486 };

		public int[][][] Title_Entry_Player_Select_Rect = new int[][][] {
			new int[][] { new int[] { 0, 0, 199, 92 } ,new int[] { 199, 0, 224, 92 } },
			new int[][] { new int[] { 0, 92, 199, 92 } ,new int[] { 199, 92, 224, 92 } },
			new int[][] { new int[] { 0, 184, 199, 92 } ,new int[] { 199, 184, 224, 92 } }
		};
		public int[] Title_Entry_NamePlate = new int[] { 530, 385 };

		public int[] Title_ModeSelect_Bar_X = new int[] { 290, 319, 356 };
		public int[] Title_ModeSelect_Bar_Y = new int[] { 107, 306, 513 };

		public int[] Title_ModeSelect_Bar_Offset = new int[] { 20, 112 };

		public int[] Title_ModeSelect_Title_Offset = new int[] { 311, 72 };
		public int[] Title_ModeSelect_Title_Scale = new int[] { 36, 15 };

		public int[] Title_ModeSelect_Bar_Center_X = new int[] { 320, 320, 640 };
		public int[] Title_ModeSelect_Bar_Center_Y = new int[] { 338, 360, 360 };
		public int[][] Title_ModeSelect_Bar_Center_Rect = new int[][] {
			new int[] { 0, 0, 641, 27 },
			new int[] { 0, 76, 641, 30 },
			new int[] { 0, 27, 641, 45 },
		};

		public int[] Title_ModeSelect_Bar_Overlay_X = new int[] { 320, 320, 640 };
		public int[] Title_ModeSelect_Bar_Overlay_Y = new int[] { 306, 333, 333 };
		public int[][] Title_ModeSelect_Bar_Overlay_Rect = new int[][] {
			new int[] { 0, 0, 641, 27 },
			new int[] { 0, 71, 641, 35 },
			new int[] { 0, 27, 641, 1 },
		};

		public int[] Title_ModeSelect_Bar_Move = new int[] { 40, 100 };
		public int[] Title_ModeSelect_Bar_Move_X = new int[] { 0, 0 };
		public int[] Title_ModeSelect_Overlay_Move = new int[] { 40, 120 };
		public int[] Title_ModeSelect_Overlay_Move_X = new int[] { 0, 0 };

		public int[] Title_ModeSelect_Bar_Chara_X = new int[] { 446, 835 };
		public int[] Title_ModeSelect_Bar_Chara_Y = new int[] { 360, 360 };

		public int Title_ModeSelect_Bar_Chara_Move = 45;

		public int[] Title_ModeSelect_Bar_Center_Title = new int[] { 631, 379 };
		public int Title_ModeSelect_Bar_Center_Title_Move = 60;
		public int Title_ModeSelect_Bar_Center_Title_Move_X = 0;

		public int[] Title_ModeSelect_Bar_Center_BoxText = new int[] { 640, 397 };

		public bool Title_VerticalText = false;
		public bool Title_VerticalBar = false;

		#endregion

		#region SongSelect
		//public int SongSelect_Overall_Y = 123;
		public string[] SongSelect_GenreName = { "ポップス", "アニメ", "ゲームバラエティ", "ナムコオリジナル", "クラシック", "バラエティ", "キッズ", "ボーカロイド", "最近遊んだ曲" };

		public int SongSelect_Bar_Count = 9;

		public int[] SongSelect_Bar_X = new int[] { 214, 239, 263, 291, 324, 358, 386, 411, 436 };
		public int[] SongSelect_Bar_Y = new int[] { -127, -36, 55, 145, 314, 485, 574, 665, 756 };
		public int[] SongSelect_Bar_Anim_X = new int[] { 0, 600, 500, 400, 0, -400, -500, -600, 0 };
		public int[] SongSelect_Bar_Anim_Y = new int[] { 0, 1800, 1500, 1200, 0, -1200, -1500, -1800, 0 };

		public float SongSelect_Scroll_Interval = 0.12f;

		public int[] SongSelect_Bar_Title_Offset = new int[] { 316, 62 };
		public int[] SongSelect_Bar_Box_Offset = new int[] { 316, 62 };
		public int[] SongSelect_Bar_BackBox_Offset = new int[] { 316, 62 };
		public int[] SongSelect_Bar_Random_Offset = new int[] { 316, 62 };
		public int[] SongSelect_Bar_SubTitle_Offset = new int[] { 316, 90 };

		public int[] SongSelect_DanStatus_Offset_X = new int[] { 30, 602 };
		public int[] SongSelect_DanStatus_Offset_Y = new int[] { 30, 30 };

		public int[] SongSelect_TowerStatus_Offset_X = new int[] { 30, 602 };
		public int[] SongSelect_TowerStatus_Offset_Y = new int[] { 30, 30 };

		public int[] SongSelect_RegularCrowns_Offset_X = new int[] { 30, 602 };
		public int[] SongSelect_RegularCrowns_Offset_Y = new int[] { 30, 30 };

		public int[] SongSelect_RegularCrowns_ScoreRank_Offset_X = new int[] { 0, 0 };
		public int[] SongSelect_RegularCrowns_ScoreRank_Offset_Y = new int[] { 0, 30 };

		public int[] SongSelect_RegularCrowns_Difficulty_Cymbol_Offset_X = new int[] { 22, 22 };
		public int[] SongSelect_RegularCrowns_Difficulty_Cymbol_Offset_Y = new int[] { 22, 52 };

		public int[] SongSelect_FavoriteStatus_Offset = new int[] { 90, 30 };

		public int SongSelect_BoxName_Scale = 28;
		public int SongSelect_MusicName_Scale = 22;
		public int SongSelect_Subtitle_Scale = 13;
		public int SongSelect_BoxText_Scale = 14;
		public bool SongSelect_VerticalText = false;

		public int SongSelect_Title_MaxSize = 550;
		public int SongSelect_SubTitle_MaxSize = 510;

		public bool SongSelect_Maker_Show = false;
		public int[] SongSelect_Maker = new int[] { 1285, 190 };
		public int SongSelect_Maker_Size = 23;
		public int SongSelect_Maker_MaxSize = 180;

		public bool SongSelect_BPM_Text_Show = false;
		public int[] SongSelect_BPM_Text = new int[] { 1240, 20 };
		public int SongSelect_BPM_Text_MaxSize = 180;
		public int SongSelect_BPM_Text_Size = 23;

		public bool SongSelect_Shorten_Frame_Fade = false;
		public bool SongSelect_Bar_Select_Skip_Fade = false;

		public int[] SongSelect_Explicit = new int[] { 1240, 60 };
		public int[] SongSelect_Movie = new int[] { 0, 0 };

		public int SongSelect_Bar_Center_Move = 62;
		public int SongSelect_Bar_Center_Move_X = 0;

		public int[] SongSelect_Bar_Select = new int[] { 309, 235 };

		public int[] SongSelect_Frame_Score_X = new int[] { 400, 522, 644, 766 };
		public int[] SongSelect_Frame_Score_Y = new int[] { 228, 228, 228, 228 };

		public int[] SongSelect_Level_Number_X = new int[] { 485, 607, 729, 851 };
		public int[] SongSelect_Level_Number_Y = new int[] { 400, 400, 400, 400 };
		public int[] SongSelect_Level_Number_Tower = new int[] { 485, 400 };
		public int[] SongSelect_Tower_Side = new int[] { 485, 400 };

		public int[] SongSelect_Level_X = new int[] { 485, 607, 729, 851 };
		public int[] SongSelect_Level_Y = new int[] { 400, 400, 400, 400 };
		public int[] SongSelect_Level_Move = new int[] { 0, -17 };

		public int[] SongSelect_Unlock_Conditions_Text = new int[] { 72, 128 };

		public int[] SongSelect_Level_Number_Interval = new int[] { 11, 0 };

		public float SongSelect_Box_Opening_Interval = 1f;

		public int[] SongSelect_Difficulty_Select_Title = new int[] { 640, 140 };
		public int[] SongSelect_Difficulty_Select_SubTitle = new int[] { 640, 180 };

		public int SongSelect_Box_Chara_Move = 114;

		public int[] SongSelect_Box_Chara_X = new int[] { 434, 846 };
		public int[] SongSelect_Box_Chara_Y = new int[] { 360, 360 };

		public int SongSelect_BoxExplanation_X = 640;
		public int SongSelect_BoxExplanation_Y = 360;

		public int SongSelect_BoxExplanation_Interval = 30;

		public int[] SongSelect_NamePlate_X = new int[] { 36, 1020, 216, 840, 396 };
		public int[] SongSelect_NamePlate_Y = new int[] { 615, 615, 561, 561, 615 };
		public int[] SongSelect_Auto_X = new int[] { 60, 950 };
		public int[] SongSelect_Auto_Y = new int[] { 650, 650 };
		public int[] SongSelect_ModIcons_X = new int[] { 40, 1020, 220, 840, 400 };
		public int[] SongSelect_ModIcons_Y = new int[] { 672, 672, 618, 618, 672 };

		public int[] SongSelect_Timer = new int[] { 1148, 57 };
		public int[] SongSelect_Timer_Interval = new int[] { 46, 0 };

		public bool SongSelect_Bpm_Show = false;
		public int[] SongSelect_Bpm_X = new int[] { 1240, 1240, 1240 };
		public int[] SongSelect_Bpm_Y = new int[] { 20, 66, 112 };
		public int[] SongSelect_Bpm_Interval = new int[] { 22, 0 };

		public bool SongSelect_FloorNum_Show = false;
		public int SongSelect_FloorNum_X = 1200;
		public int SongSelect_FloorNum_Y = 205;
		public int[] SongSelect_FloorNum_Interval = new int[] { 30, 0 };

		public bool SongSelect_DanInfo_Show = false;
		public int[] SongSelect_DanInfo_Icon_X = new int[] { 1001, 1001, 1001 };
		public int[] SongSelect_DanInfo_Icon_Y = new int[] { 269, 309, 349 };
		public float SongSelect_DanInfo_Icon_Scale = 0.5f;
		public int[] SongSelect_DanInfo_Difficulty_Cymbol_X = new int[] { 1028, 1028, 1028 };
		public int[] SongSelect_DanInfo_Difficulty_Cymbol_Y = new int[] { 263, 303, 343 };
		public float SongSelect_DanInfo_Difficulty_Cymbol_Scale = 0.5f;
		public int[] SongSelect_DanInfo_Level_Number_X = new int[] { 1040, 1040, 1040 };
		public int[] SongSelect_DanInfo_Level_Number_Y = new int[] { 267, 307, 347 };
		public float SongSelect_DanInfo_Level_Number_Scale = 0.5f;
		public int[] SongSelect_DanInfo_Title_X = new int[] { 1032, 1032, 1032 };
		public int[] SongSelect_DanInfo_Title_Y = new int[] { 258, 298, 338 };
		public int SongSelect_DanInfo_Title_Size = 12;
		public int[] SongSelect_DanInfo_Exam_X = new int[] { 1030, 1030, 1030, 1030, 1030, 1030 };
		public int[] SongSelect_DanInfo_Exam_Y = new int[] { 398, 426, 454, 482, 510, 538 };
		public int SongSelect_DanInfo_Exam_Size = 10;
		public int[] SongSelect_DanInfo_Exam_Value_X = new int[] { 1097, 1162, 1227 };
		public int[] SongSelect_DanInfo_Exam_Value_Y = new int[] { 388, 416, 444, 472, 500, 528 };
		public float SongSelect_DanInfo_Exam_Value_Scale = 0.5f;

		public int[] SongSelect_Table_X = new int[] { 0, 1034, 180, 854, 360 };
		public int[] SongSelect_Table_Y = new int[] { 0, 0, -204, -204, 0 };

		public int[] SongSelect_High_Score_X = new int[] { 124, 1158, 304, 978, 484 };
		public int[] SongSelect_High_Score_Y = new int[] { 416, 416, 212, 212, 416 };

		public int[] SongSelect_High_Score_Difficulty_Cymbol_X = new int[] { 46, 1080, 226, 900, 406 };
		public int[] SongSelect_High_Score_Difficulty_Cymbol_Y = new int[] { 418, 418, 214, 214, 418 };

		public int[][] SongSelect_BoardNumber_X = new int[][] {
			new int[] { 62, 125, 190, 62, 125, 190, 190, 62, 125, -100, 190, 74, 114 },
			new int[] { 1096, 1159, 1224, 1096, 1159, 1224, 1224, 1096, 1159, -100, 1224, 1214, 1148 },

			new int[] { 242, 305, 370, 242, 305, 370, 370, 242, 305, -100, 370, 254, 294 },
			new int[] { 916, 979, 1044, 916, 979, 1044, 1044, 916, 979, -100, 1044, 1034, 968 },
			new int[] { 422, 485, 550, 422, 485, 550, 550, 422, 485, 550, -100, 434, 474 }
		};
		public int[][] SongSelect_BoardNumber_Y = new int[][] {
			new int[] { 276, 276, 276, 251, 251, 251, 226, 304, 304, -100, 304, 353, 415 },
			new int[] { 276, 276, 276, 251, 251, 251, 226, 304, 304, -100, 304, 353, 415 },
			new int[] { 72,72,72,47,47,47,22,100,100, -100, 100, 149,211 },
			new int[] { 72,72,72,47,47,47,22,100,100, -100, 100, 149,211 },
			new int[] { 276, 276, 276, 251, 251, 251, 226, 304, 304, -100, 304, 353, 415 }
		};
		public int[] SongSelect_BoardNumber_Interval = new int[] { 9, 0 };

		public int[] SongSelect_SongNumber_X = new int[] { 1090, 1183 };
		public int[] SongSelect_SongNumber_Y = new int[] { 167, 167 };
		public int[] SongSelect_SongNumber_Interval = new int[] { 16, 0 };

		public int[] SongSelect_Search_Bar_X = new int[] { 640, 640, 640, 640, 640 };
		public int[] SongSelect_Search_Bar_Y = new int[] { 320, 420, 520, 620, 720 };

		public int[] SongSelect_Difficulty_Back = new int[] { 640, 290 };
		public int[] SongSelect_Level_Offset = new int[] { 610, 40 };
		public Color[] SongSelect_Difficulty_Colors = new Color[] {
			ColorTranslator.FromHtml("#88d2fd"),
			ColorTranslator.FromHtml("#58de85"),
			ColorTranslator.FromHtml("#ffc224"),
			ColorTranslator.FromHtml("#d80b2c"),
			ColorTranslator.FromHtml("#9065e2"),
			ColorTranslator.FromHtml("#e9943b"),
			ColorTranslator.FromHtml("#3b55a5")
		};

		public int[] SongSelect_Difficulty_Bar_X = new int[] { 255, 341, 426, 569, 712, 855, 855 };
		public int[] SongSelect_Difficulty_Bar_Y = new int[] { 270, 270, 270, 270, 270, 270, 270 };
		public int[] SongSelect_Branch_Text_Offset = new int[] { 276, 6 };
		public int[] SongSelect_Branch_Offset = new int[] { 6, 6 };

		public int[][] SongSelect_Difficulty_Bar_Rect = new int[][] {
			new int[] { 0, 0, 86, 236 },
			new int[] { 86, 0, 86, 236 },
			new int[] { 171, 0, 138, 236 },
			new int[] { 314, 0, 138, 236 },
			new int[] { 457, 0, 138, 236 },
			new int[] { 600, 0, 138, 236 },
			new int[] { 743, 0, 138, 236 },
		};

		public int[] SongSelect_Difficulty_Star_X = new int[] { 444, 587, 730, 873, 873 };
		public int[] SongSelect_Difficulty_Star_Y = new int[] { 459, 459, 459, 459, 459 };
		public int[] SongSelect_Difficulty_Star_Interval = new int[] { 10, 0 };

		public int[] SongSelect_Difficulty_Number_X = new int[] { 498, 641, 784, 927, 927 };
		public int[] SongSelect_Difficulty_Number_Y = new int[] { 435, 435, 435, 435, 435 };
		public int[] SongSelect_Difficulty_Number_Interval = new int[] { 11, 0 };

		public int[][] SongSelect_Difficulty_Crown_X = new int[][] {
			new int[] { 445, 589, 733, 877, 877 },
			new int[] { 519, 663, 807, 951, 951 },
		};
		public int[][] SongSelect_Difficulty_Crown_Y = new int[][] {
			new int[] { 284, 284, 284, 284, 284 },
			new int[] { 284, 284, 284, 284, 284 },
		};

		public int[][] SongSelect_Difficulty_ScoreRank_X = new int[][] {
			new int[] { 467, 611, 755, 899, 899 },
			new int[] { 491, 635, 779, 923, 923 },
		};
		public int[][] SongSelect_Difficulty_ScoreRank_Y = new int[][] {
			new int[] { 281, 281, 281, 281, 281 },
			new int[] { 281, 281, 281, 281, 281 },
		};

		public int[] SongSelect_Difficulty_Select_Bar_X = new int[] { 163, 252, 367, 510, 653, 796, 796 };
		public int[] SongSelect_Difficulty_Select_Bar_Y = new int[] { 176, 176, 176, 176, 176, 176, 176 };

		public int[] SongSelect_Difficulty_Select_Bar_Back_X = new int[] { 163, 252, 367, 510, 653, 796, 796 };
		public int[] SongSelect_Difficulty_Select_Bar_Back_Y = new int[] { 242, 242, 242, 242, 242, 242, 242 };

		public int[][] SongSelect_Difficulty_Select_Bar_Rect = new int[][] {
			new int[] { 0, 0, 259, 114 },
			new int[] { 0, 114, 259, 275 },
			new int[] { 0, 387, 259, 111 },
		};

		public int[] SongSelect_Difficulty_Select_Bar_Anime = new int[] { 0, 10 };
		public int[] SongSelect_Difficulty_Select_Bar_AnimeIn = new int[] { 0, 50 };
		public int[] SongSelect_Difficulty_Select_Bar_Move = new int[] { 25, 0 };

		public int[] SongSelect_Difficulty_Bar_ExExtra_AnimeDuration = new int[] { -1, -1 };

		public int[] SongSelect_Preimage = new int[] { 120, 110 };
		public int[] SongSelect_Preimage_Size = new int[] { 200, 200 };

		public int[] SongSelect_Option_Select_Offset = new int[] { 0, -286 };

		public int SongSelect_Option_Font_Scale = 13;
		public int[] SongSelect_Option_OptionType_X = new int[] { 16, 1004 };
		public int[] SongSelect_Option_OptionType_Y = new int[] { 93, 93 };
		public int[] SongSelect_Option_Value_X = new int[] { 200, 1188 };
		public int[] SongSelect_Option_Value_Y = new int[] { 93, 93 };
		public int[] SongSelect_Option_Interval = new int[] { 0, 41 };

		public int[] SongSelect_Option_ModMults1_X = new int[] { 108, 1096 };
		public int[] SongSelect_Option_ModMults1_Y = new int[] { 11, 11 };

		public int[] SongSelect_Option_ModMults2_X = new int[] { 108, 1096 };
		public int[] SongSelect_Option_ModMults2_Y = new int[] { 52, 52 };


		public int[] SongSelect_NewHeya_Close_Select = new int[] { 0, 0 };

		public int[] SongSelect_NewHeya_PlayerPlate_X = new int[] { 0, 256, 513, 770, 1026 };
		public int[] SongSelect_NewHeya_PlayerPlate_Y = new int[] { 66, 66, 66, 66, 66 };

		public int[] SongSelect_NewHeya_ModeBar_X = new int[] { 0, 256, 513, 770, 1026 };
		public int[] SongSelect_NewHeya_ModeBar_Y = new int[] { 200, 200, 200, 200, 200 };
		public int[] SongSelect_NewHeya_ModeBar_Font_Offset = new int[] { 128, 33 };


		public int SongSelect_NewHeya_Box_Count = 7;
		public int[] SongSelect_NewHeya_Box_X = new int[] { -424, -120, 184, 488, 792, 1096, 1400 };
		public int[] SongSelect_NewHeya_Box_Y = new int[] { 273, 273, 273, 273, 273, 273, 273 };
		public int[] SongSelect_NewHeya_Box_Chara_Offset = new int[] { 152, 200 };
		public int[] SongSelect_NewHeya_Box_Name_Offset = new int[] { 152, 386 };
		public int[] SongSelect_NewHeya_Box_Author_Offset = new int[] { 152, 413 };
		public int[] SongSelect_NewHeya_Lock_Offset = new int[] { 0, 73 };
		public int[] SongSelect_NewHeya_InfoSection_Offset = new int[] { 152, 206 };

		public Color SongSelect_ForeColor_JPOP = ColorTranslator.FromHtml("#FFFFFF");
		public Color SongSelect_ForeColor_Anime = ColorTranslator.FromHtml("#FFFFFF");
		public Color SongSelect_ForeColor_VOCALOID = ColorTranslator.FromHtml("#FFFFFF");
		public Color SongSelect_ForeColor_Children = ColorTranslator.FromHtml("#FFFFFF");
		public Color SongSelect_ForeColor_Variety = ColorTranslator.FromHtml("#FFFFFF");
		public Color SongSelect_ForeColor_Classic = ColorTranslator.FromHtml("#FFFFFF");
		public Color SongSelect_ForeColor_GameMusic = ColorTranslator.FromHtml("#FFFFFF");
		public Color SongSelect_ForeColor_Namco = ColorTranslator.FromHtml("#FFFFFF");
		public Color SongSelect_BackColor_JPOP = ColorTranslator.FromHtml("#01455B");
		public Color SongSelect_BackColor_Anime = ColorTranslator.FromHtml("#99001F");
		public Color SongSelect_BackColor_VOCALOID = ColorTranslator.FromHtml("#5B6278");
		public Color SongSelect_BackColor_Children = ColorTranslator.FromHtml("#9D3800");
		public Color SongSelect_BackColor_Variety = ColorTranslator.FromHtml("#366600");
		public Color SongSelect_BackColor_Classic = ColorTranslator.FromHtml("#875600");
		public Color SongSelect_BackColor_GameMusic = ColorTranslator.FromHtml("#412080");
		public Color SongSelect_BackColor_Namco = ColorTranslator.FromHtml("#980E00");

		public string[] SongSelect_CorrectionX_Chara = { "ここにX座標を補正したい文字をカンマで区切って記入" };
		public string[] SongSelect_CorrectionY_Chara = { "ここにY座標を補正したい文字をカンマで区切って記入" };
		public int SongSelect_CorrectionX_Chara_Value = 0;
		public int SongSelect_CorrectionY_Chara_Value = 0;
		public string[] SongSelect_Rotate_Chara = { "ここに90℃回転させたい文字をカンマで区切って記入" };

		#endregion
		#region DaniSelect
		//public int[] DaniSelect_Dan_Text_X = new int[] { 300, 980, 300, 980 };
		//public int[] DaniSelect_Dan_Text_Y = new int[] { 198, 198, 522, 522 };

		public int[] DaniSelect_DanSides_X = new int[] { 243, 1199 };
		public int[] DaniSelect_DanSides_Y = new int[] { 143, 143 };

		public int[] DaniSelect_DanPlate = new int[] { 173, 301 };
		public int[] DaniSelect_Rank = new int[] { 173, 422 };
		public int[] DaniSelect_Bloc2 = new int[] { 291, 412 };
		public int[] DaniSelect_Text_Gauge = new int[] { 396, 429 };
		public int[] DaniSelect_Value_Gauge = new int[] { 370, 462 };

		public int[] DaniSelect_DanIcon_X = new int[] { 314, 314, 314 };
		public int[] DaniSelect_DanIcon_Y = new int[] { 190, 263, 336 };

		public int[] DaniSelect_Title_X = new int[] { 401, 401, 401 };
		public int[] DaniSelect_Title_Y = new int[] { 173, 246, 319 };

		public int[] DaniSelect_Difficulty_Cymbol_X = new int[] { 377, 377, 377 };
		public int[] DaniSelect_Difficulty_Cymbol_Y = new int[] { 180, 253, 326 };

		public int[] DaniSelect_Level_Number_X = new int[] { 383, 383, 383 };
		public int[] DaniSelect_Level_Number_Y = new int[] { 207, 280, 353 };

		public int[] DaniSelect_Level_Number_Interval = new int[] { 10, 0 };

		public int[] DaniSelect_Soul_Number_Interval = new int[] { 16, 0 };
		public int DaniSelect_Soul_Number_Text_Width = 80;

		public int[] DaniSelect_Exam_Number_Interval = new int[] { 16, 0 };
		public int DaniSelect_Exam_Number_Text_Width = 45;

		public int[] DaniSelect_Font_DanFolder_Size = new int[] { 64, 32 };
		public int[] DaniSelect_FolderText_X = new int[] { 640, 640, 640, 640 };
		public int[] DaniSelect_FolderText_Y = new int[] { 320, 413, 460, 507 };
		public int DaniSelect_Font_DanSong_Size = 24;
		public int DaniSelect_Font_Exam_Size = 13;

		public int[] DaniSelect_Exam_Bloc_X = new int[] { 515, 515, 515 };
		public int[] DaniSelect_Exam_Bloc_Y = new int[] { 412, 500, 588 };

		public int[] DaniSelect_Exam_X = new int[] { 590, 590, 590 };
		public int[] DaniSelect_Exam_Y = new int[] { 455, 543, 631 };

		public int[] DaniSelect_Exam_X_Ex = new int[] { 536, 536, 536 };
		public int[] DaniSelect_Exam_Y_Ex = new int[] { 455, 543, 631 };

		public int[] DaniSelect_Exam_Interval = new int[] { 220, 0 };

		public int[] DaniSelect_Exam_Title_X = new int[] { 614, 614, 614 };
		public int[] DaniSelect_Exam_Title_Y = new int[] { 429, 517, 605 };

		public int[] DaniSelect_Challenge_Select_X = new int[] { 228, 456, 684 };
		public int[] DaniSelect_Challenge_Select_Y = new int[] { 0, 0, 0 };
		public int[][] DaniSelect_Challenge_Select_Rect = new int[][] {
			new int[] { 228, 0, 228, 720 },
			new int[] { 456, 0, 228, 720 },
			new int[] { 684, 0, 228, 720 },
		};

		public int[] DaniSelect_Plate = new int[] { 640, 10 };
		public int[] DaniSelect_Plate_Move = new int[] { 52, 0 };
		public int[] DaniSelect_Plate_Center_Move = new int[] { 0, 15 };
		public int[] DaniSelect_Plate_Title_Offset = new int[] { 2, 36 };

		public int DaniSelect_DanPlateTitle_Size = 60;
		public int[] DaniSelect_DanPlateTitle_Offset = new int[] { 0, -50 };

		public int DaniSelect_DanIconTitle_Size = 18;
		public int[] DaniSelect_DanIconTitle_Offset = new int[] { 0, 6 };

		public Color[] DaniSelect_DanIcon_Color = new Color[]
			{
				Color.Red,
				Color.Green,
				Color.Blue,
				Color.Magenta,
				Color.Yellow,
				Color.Cyan,
				Color.Brown,
				Color.Gray,
				Color.DarkGreen,
				Color.Black
			};
		#endregion
		#region SongLoading
		public int SongLoading_Plate_X = 640;
		public int SongLoading_Plate_Y = 360;
		public int SongLoading_Title_X = 640;
		public int SongLoading_Title_Y = 280;
		public int SongLoading_Title_MaxSize = 710;
		public int SongLoading_SubTitle_X = 640;
		public int SongLoading_SubTitle_Y = 325;
		public int SongLoading_SubTitle_MaxSize = 710;

		public int SongLoading_Plate_X_AI = 640;
		public int SongLoading_Plate_Y_AI = 360;
		public int SongLoading_Title_X_AI = 640;
		public int SongLoading_Title_Y_AI = 313;
		public int SongLoading_SubTitle_X_AI = 640;
		public int SongLoading_SubTitle_Y_AI = 365;

		public int[] SongLoading_Fade_AI_Anime_Ring = new int[] { 466, 185 };
		public int[] SongLoading_Fade_AI_Anime_LoadBar = new int[] { 490, 382 };

		public int[] SongLoading_DanPlate = new int[] { 1121, 213 };

		public int SongLoading_Title_FontSize = 31;
		public int SongLoading_SubTitle_FontSize = 20;
		public int[] SongLoading_Chara_Move = new int[] { 250, -80 };
		public ReferencePoint SongLoading_Plate_ReferencePoint = ReferencePoint.Center;
		public ReferencePoint SongLoading_Title_ReferencePoint = ReferencePoint.Center;
		public ReferencePoint SongLoading_SubTitle_ReferencePoint = ReferencePoint.Center;
		public Color SongLoading_Title_ForeColor = ColorTranslator.FromHtml("#FFFFFF");
		public Color SongLoading_Title_BackColor = ColorTranslator.FromHtml("#000000");
		public Color SongLoading_SubTitle_ForeColor = ColorTranslator.FromHtml("#000000");
		public Color SongLoading_SubTitle_BackColor = ColorTranslator.FromHtml("#00000000");
		public bool SongLoading_Plate_ScreenBlend = false;

		#endregion
		#region Game

		// Game parameters here

		public bool Game_Notes_Anime = false;
		public int[] Game_Notes_Size = new int[] { 130, 130 };
		public int[] Game_SENote_Size = new int[] { 136, 30 };
		public int Game_Notes_Interval = 960;

		public int[] Game_Notes_Arm_Offset_Left_X = new int[] { 25, 25 };
		public int[] Game_Notes_Arm_Offset_Right_X = new int[] { 60, 60 };

		public int[] Game_Notes_Arm_Offset_Left_Y = new int[] { 74, -44 };
		public int[] Game_Notes_Arm_Offset_Right_Y = new int[] { 104, -14 };
		public int[] Game_Notes_Arm_Move = new int[] { 0, 30 };

		public int[] Game_Judge_X = new int[] { 364, 364 };
		public int[] Game_Judge_Y = new int[] { 152, 328 };
		public int[] Game_Judge_Move = new int[] { 0, 20 };
		public int[] Game_ScoreRank_X = new int[] { 87, 87 };
		public int[] Game_ScoreRank_Y = new int[] { 98, 622 };
		public int[] Game_ScoreRank_Move = new int[] { 0, 51 };
		public string Game_StageText = "1曲目";
		public RollColorMode Game_RollColorMode = RollColorMode.All;
		public bool Game_JudgeFrame_AddBlend = true;

		public int[] Game_Judge_Meter = new int[] { 0, 360 };
		public int[] Game_Judge_Meter_Perfect = new int[] { 102, 494 };
		public int[] Game_Judge_Meter_Good = new int[] { 102, 532 };
		public int[] Game_Judge_Meter_Miss = new int[] { 102, 570 };
		public int[] Game_Judge_Meter_Roll = new int[] { 102, 634 };
		public int[] Game_Judge_Meter_HitRate = new int[] { 206, 436 };
		public int[] Game_Judge_Meter_PerfectRate = new int[] { 206, 494 };
		public int[] Game_Judge_Meter_GoodRate = new int[] { 206, 532 };
		public int[] Game_Judge_Meter_MissRate = new int[] { 206, 570 };

		public int[] Game_Judge_4P = new int[] { 364, 32 };
		public int[] Game_Judge_5P = new int[] { 364, 24 };

		public int[] Game_UIMove_4P = new int[] { 0, 176 };
		public int[] Game_UIMove_5P = new int[] { 0, 144 };

		public int[] Game_ScoreRank_4P = new int[] { 87, 88 };
		public int[] Game_ScoreRank_5P = new int[] { 87, 80 };

		public DBSkinPreset.SkinPreset Game_SkinScenes = null;

		#region Chara

		public int[] Game_Chara_X = new int[] { 0, 0 };
		public int[] Game_Chara_Y = new int[] { 0, 537 };
		public int[] Game_Chara_Balloon_X = new int[] { 240, 240, 0, 0 };
		public int[] Game_Chara_Balloon_Y = new int[] { 0, 297, 0, 0 };
		public int Game_Chara_Ptn_Normal,
			Game_Chara_Ptn_GoGo,
			Game_Chara_Ptn_Clear,
			Game_Chara_Ptn_10combo,
			Game_Chara_Ptn_10combo_Max,
			Game_Chara_Ptn_GoGoStart,
			Game_Chara_Ptn_GoGoStart_Max,
			Game_Chara_Ptn_ClearIn,
			Game_Chara_Ptn_SoulIn,
			Game_Chara_Ptn_Balloon_Breaking,
			Game_Chara_Ptn_Balloon_Broke,
			Game_Chara_Ptn_Balloon_Miss;
		public string Game_Chara_Motion_Normal,
			Game_Chara_Motion_Clear,
			Game_Chara_Motion_GoGo = "0";
		public int Game_Chara_Beat_Normal = 1;
		public int Game_Chara_Beat_Clear = 2;
		public int Game_Chara_Beat_GoGo = 2;
		public int Game_Chara_Balloon_Timer = 28;
		public int Game_Chara_Balloon_Delay = 500;
		public int Game_Chara_Balloon_FadeOut = 84;

		#endregion

		#region Dancer

		public int[] Game_Dancer_X = new int[] { 640, 430, 856, 215, 1070 };
		public int[] Game_Dancer_Y = new int[] { 500, 500, 500, 500, 500 };
		public string Game_Dancer_Motion = "0";
		//public int Game_Dancer_Ptn = 0;
		//public int Game_Dancer_Beat = 8;
		public int[] Game_Dancer_Gauge = new int[] { 0, 0, 0, 40, 80 };

		#endregion

		#region Tower

		public int Game_Tower_Ptn;
		public int[] Game_Tower_Ptn_Deco,
			Game_Tower_Ptn_Base;

		public string[] Game_Tower_Names;
		public int Game_Tower_Ptn_Result;

		public int Game_Tower_Ptn_Don;
		public int[] Game_Tower_Ptn_Don_Standing,
			Game_Tower_Ptn_Don_Jump,
			Game_Tower_Ptn_Don_Climbing,
			Game_Tower_Ptn_Don_Running;

		#endregion

		#region Mob
		public int Game_Mob_Beat,
			Game_Mob_Ptn_Beat = 1;
		#endregion
		#region CourseSymbol
		public int[] Game_CourseSymbol_X = new int[] { -4, -4 };
		public int[] Game_CourseSymbol_Y = new int[] { 232, 582 };
		public int[] Game_CourseSymbol_Back_X = new int[] { 280, 280 };
		public int[] Game_CourseSymbol_Back_Y = new int[] { -110, 427 };

		public int[] Game_CourseSymbol_4P = new int[] { -4, 56 };
		public int[] Game_CourseSymbol_5P = new int[] { -4, 48 };

		public int[] Game_CourseSymbol_Back_4P = new int[] { 896, 47 };
		public int[] Game_CourseSymbol_Back_5P = new int[] { 896, 39 };

		public int[] Game_CourseSymbol_Back_Rect_4P = new int[] { 0, 128, 384, 129 };
		public int[] Game_CourseSymbol_Back_Rect_5P = new int[] { 0, 140, 384, 105 };
		#endregion
		#region PanelFont
		public int Game_MusicName_X = 1260;
		public int Game_MusicName_Y = 24;
		public int Game_MusicName_FontSize = 27;
		public int Game_MusicName_MaxWidth = 660;
		public ReferencePoint Game_MusicName_ReferencePoint = ReferencePoint.Center;
		public int Game_Genre_X = 1015;
		public int Game_Genre_Y = 70;
		public int[] Game_GenreText_Offset = new int[2] { 145, 23 };
		public int Game_GenreText_FontSize = 12;
		public int Game_Lyric_X = 640;
		public int Game_Lyric_Y = 630;
		public string Game_Lyric_FontName = CFontRenderer.DefaultFontName;
		public int Game_Lyric_FontSize = 38;
		public int Game_Lyric_VTTRubyOffset = 65;
		public ReferencePoint Game_Lyric_ReferencePoint = ReferencePoint.Center;

		public Color Game_MusicName_ForeColor = ColorTranslator.FromHtml("#FFFFFF");
		public Color Game_StageText_ForeColor = ColorTranslator.FromHtml("#FFFFFF");
		public Color Game_Lyric_ForeColor = ColorTranslator.FromHtml("#FFFFFF");
		public Color Game_MusicName_BackColor = ColorTranslator.FromHtml("#000000");
		public Color Game_StageText_BackColor = ColorTranslator.FromHtml("#000000");
		public Color Game_Lyric_BackColor = ColorTranslator.FromHtml("#0000FF");
		public Color[] Game_Lyric_VTTForeColor = new Color[] { Color.White, Color.Lime, Color.Cyan, Color.Red, Color.Yellow, Color.Magenta, Color.Blue, Color.Black };
		public Color[] Game_Lyric_VTTBackColor = new Color[] { Color.White, Color.Lime, Color.Cyan, Color.Red, Color.Yellow, Color.Magenta, Color.Blue, Color.Black };

		#endregion
		#region Score
		public int[] Game_Score_X = new int[] { 20, 20, 0, 0 };
		public int[] Game_Score_Y = new int[] { 226, 530, 0, 0 };
		public int[] Game_Score_Add_X = new int[] { 20, 20, 0, 0 };
		public int[] Game_Score_Add_Y = new int[] { 186, 570, 0, 0 };
		public int[] Game_Score_AddBonus_X = new int[] { 20, 20, 0, 0 };
		public int[] Game_Score_AddBonus_Y = new int[] { 136, 626, 0, 0 };
		public int Game_Score_Padding = 20;
		public int[] Game_Score_Size = new int[] { 24, 40 };

		public int[] Game_Score_4P = new int[] { 20, 54 };
		public int[] Game_Score_5P = new int[] { 20, 46 };

		public int[] Game_Score_Add_4P = new int[] { 20, 94 };
		public int[] Game_Score_Add_5P = new int[] { 20, 86 };

		public int[] Game_Score_AddBonus_4P = new int[] { 20, 134 };
		public int[] Game_Score_AddBonus_5P = new int[] { 20, 126 };
		#endregion
		#region Taiko
		public int[] Game_Taiko_Background_X = new int[] { 0, 0 };
		public int[] Game_Taiko_Background_Y = new int[] { 184, 360 };

		public int[] Game_Taiko_ModIcons_X = new int[] { 80, 80 };
		public int[] Game_Taiko_ModIcons_Y = new int[] { 236, 426 };
		public int[] Game_Taiko_NamePlate_X = new int[] { -5, -5 };
		public int[] Game_Taiko_NamePlate_Y = new int[] { 297, 371 };
		public int[] Game_Taiko_PlayerNumber_X = new int[] { 4, 4 };
		public int[] Game_Taiko_PlayerNumber_Y = new int[] { 233, 435 };
		public int[] Game_Taiko_X = new int[] { 205, 205 };
		public int[] Game_Taiko_Y = new int[] { 206, 384 };
		public int[] Game_Taiko_Combo_X = new int[] { 267, 267 };
		public int[] Game_Taiko_Combo_Y = new int[] { 272, 447 };
		public int[] Game_Taiko_Combo_Ex_X = new int[] { 267, 267 };
		public int[] Game_Taiko_Combo_Ex_Y = new int[] { 274, 451 };
		public int[] Game_Taiko_Combo_Ex4_X = new int[] { 267, 267 };
		public int[] Game_Taiko_Combo_Ex4_Y = new int[] { 269, 447 };
		public int[] Game_Taiko_Combo_Padding = new int[] { 34, 34, 30 };
		public int[] Game_Taiko_Combo_Size = new int[] { 40, 48 };
		public int[] Game_Taiko_Combo_Size_Ex = new int[] { 40, 48 };
		public float[] Game_Taiko_Combo_Scale = new float[] { 1.0f, 1.0f, 0.9f };
		public int[] Game_Taiko_Combo_Text_X = new int[] { 268, 268 };
		public int[] Game_Taiko_Combo_Text_Y = new int[] { 298, 475 };
		public int[] Game_Taiko_Combo_Text_Size = new int[] { 100, 50 };
		public bool Game_Taiko_Combo_Ex_IsJumping = true;
		public int[] Game_Taiko_LevelChange_X = new int[] { 311, 311 };
		public int[] Game_Taiko_LevelChange_Y = new int[] { 154, 566 };
		public int[] Game_Taiko_Frame_X = new int[] { 329, 329 };
		public int[] Game_Taiko_Frame_Y = new int[] { 136, 360 };


		public int[] Game_Taiko_Background_4P = new int[] { 0, 8 };
		public int[] Game_Taiko_Background_5P = new int[] { 0, 0 };

		public int[] Game_Taiko_ModIcons_4P = new int[] { 80, 60 };
		public int[] Game_Taiko_ModIcons_5P = new int[] { 80, 50 };

		public int[] Game_Taiko_NamePlate_4P = new int[] { -55, 121 };
		public int[] Game_Taiko_NamePlate_5P = new int[] { -55, 97 };

		public int[] Game_Taiko_PlayerNumber_4P = new int[] { 4, 57 };
		public int[] Game_Taiko_PlayerNumber_5P = new int[] { 4, 49 };

		public int[] Game_Taiko_4P = new int[] { 205, 30 };
		public int[] Game_Taiko_5P = new int[] { 205, 22 };

		public int[] Game_Taiko_Combo_4P = new int[] { 267, 73 };
		public int[] Game_Taiko_Combo_5P = new int[] { 267, 65 };

		public int[] Game_Taiko_Combo_Ex_4P = new int[] { 267, 75 };
		public int[] Game_Taiko_Combo_Ex_5P = new int[] { 267, 67 };

		public int[] Game_Taiko_Combo_Ex4_4P = new int[] { 267, 70 };
		public int[] Game_Taiko_Combo_Ex4_5P = new int[] { 267, 62 };

		public int[] Game_Taiko_Combo_Text_4P = new int[] { 268, 99 };
		public int[] Game_Taiko_Combo_Text_5P = new int[] { 268, 91 };

		public int[] Game_Taiko_Frame_4P = new int[] { 333, 8 };
		public int[] Game_Taiko_Frame_5P = new int[] { 333, 0 };

		#endregion
		#region Gauge
		public int[] Game_Gauge_X = new int[] { 492, 492 };
		public int[] Game_Gauge_Y = new int[] { 144, 532 };
		public int Game_Gauge_X_AI = 650;
		public int Game_Gauge_Y_AI = 153;
		public int[] Game_Gauge_Rect = new int[] { 0, 0, 700, 44 };
		public int[] Game_Gauge_ClearText_X = new int[] { 1038, 1038 };
		public int[] Game_Gauge_ClearText_Y = new int[] { 144, 554 };
		public int Game_Gauge_ClearText_X_AI = 1087;
		public int Game_Gauge_ClearText_Y_AI = 153;
		public int[] Game_Gauge_ClearText_Rect = new int[] { 0, 44, 58, 24 };
		public int[] Game_Gauge_ClearText_Clear_Rect = new int[] { 58, 44, 58, 24 };
		public int[] Gauge_Soul_X = new int[] { 1184, 1184 };
		public int[] Gauge_Soul_Y = new int[] { 125, 516 };
		public int Gauge_Soul_X_AI = 1200;
		public int Gauge_Soul_Y_AI = 140;
		public int Gauge_Soul_X_Tower = 958;
		public int Gauge_Soul_Y_Tower = 95;
		public int[] Gauge_Soul_Fire_X = new int[] { 1112, 1112 };
		public int[] Gauge_Soul_Fire_Y = new int[] { 52, 443 };
		public int Gauge_Soul_Fire_X_AI = 1143;
		public int Gauge_Soul_Fire_Y_AI = 83;
		public int Gauge_Soul_Fire_X_Tower = 886;
		public int Gauge_Soul_Fire_Y_Tower = 22;
		public int Game_Gauge_Rainbow_Ptn;
		public int Game_Gauge_Rainbow_2PGauge_Ptn;
		public int Game_Gauge_Rainbow_Flat_Ptn;
		public int Game_Gauge_Dan_Rainbow_Ptn;
		public int Game_Gauge_Rainbow_Timer = 50;

		public int[] Game_Gauge_4P = new int[] { 492, -4 };
		public int[] Game_Gauge_5P = new int[] { 492, -12 };

		public int[] Gauge_Soul_4P = new int[] { 1184, -12 };
		public int[] Gauge_Soul_5P = new int[] { 1184, -20 };

		public int[] Gauge_Soul_Fire_4P = new int[] { 1112, -85 };
		public int[] Gauge_Soul_Fire_5P = new int[] { 1112, -93 };

		#endregion
		#region Balloon
		public int[] Game_Balloon_Combo_X = new int[] { 253, 253 };
		public int[] Game_Balloon_Combo_Y = new int[] { -11, 538 };
		public int[] Game_Balloon_Combo_Number_X = new int[] { 257, 257 };
		public int[] Game_Balloon_Combo_Number_Y = new int[] { 54, 603 };
		public int[] Game_Balloon_Combo_Number_Ex_X = new int[] { 257, 257 };
		public int[] Game_Balloon_Combo_Number_Ex_Y = new int[] { 54, 603 };
		public int[] Game_Balloon_Combo_Number_Size = new int[] { 53, 62 };
		public int[] Game_Balloon_Combo_Number_Interval = new int[] { 45, 0 };
		public int[] Game_Balloon_Combo_Text_X = new int[] { 440, 440 };
		public int[] Game_Balloon_Combo_Text_Y = new int[] { 85, 634 };
		public int[] Game_Balloon_Combo_Text_Ex_X = new int[] { 440, 440 };
		public int[] Game_Balloon_Combo_Text_Ex_Y = new int[] { 85, 594 };
		public int[] Game_Balloon_Combo_Text_Rect = new int[] { 0, 124, 100, 30 };

		public int[] Game_Balloon_Balloon_X = new int[] { 382, 382 };
		public int[] Game_Balloon_Balloon_Y = new int[] { 115, 290 };
		public int[] Game_Balloon_Balloon_Frame_X = new int[] { 382, 382 };
		public int[] Game_Balloon_Balloon_Frame_Y = new int[] { 80, 260 };
		public int[] Game_Balloon_Balloon_Number_X = new int[] { 423, 423 };
		public int[] Game_Balloon_Balloon_Number_Y = new int[] { 187, 373 };
		public int[] Game_Balloon_Roll_Frame_X = new int[] { 218, 218 };
		public int[] Game_Balloon_Roll_Frame_Y = new int[] { -3, 514 };
		public int[] Game_Balloon_Roll_Number_X = new int[] { 313, 313 };
		public int[] Game_Balloon_Roll_Number_Y = new int[] { 122, 633 };
		public int[] Game_Balloon_Number_Size = new int[] { 63, 75 };
		public int[] Game_Balloon_Number_Interval = new int[] { 55, 0 };
		public float Game_Balloon_Roll_Number_Scale = 1.000f;
		public float Game_Balloon_Balloon_Number_Scale = 0.879f;

		public int[] Game_Balloon_Balloon_4P = new int[] { 382, -61 };
		public int[] Game_Balloon_Balloon_5P = new int[] { 382, -53 };

		public int[] Game_Balloon_Balloon_Frame_4P = new int[] { 382, -12 };
		public int[] Game_Balloon_Balloon_Frame_5P = new int[] { 382, -4 };

		public int[] Game_Balloon_Balloon_Number_4P = new int[] { 423, 95 };
		public int[] Game_Balloon_Balloon_Number_5P = new int[] { 423, 87 };

		public int Game_Kusudama_Number_X = 960;
		public int Game_Kusudama_Number_Y = 540;
		#endregion
		#region Effects
		public int[] Game_Effect_Roll_StartPoint_X = new int[] { 56, -10, 200, 345, 100, 451, 600, 260, -30, 534, 156, 363 };
		public int[] Game_Effect_Roll_StartPoint_Y = new int[] { 720 };
		public int[] Game_Effect_Roll_StartPoint_1P_X = new int[] { 56, -10, 200, 345, 100, 451, 600, 260, -30, 534, 156, 363 };
		public int[] Game_Effect_Roll_StartPoint_1P_Y = new int[] { 240 };
		public int[] Game_Effect_Roll_StartPoint_2P_X = new int[] { 56, -10, 200, 345, 100, 451, 600, 260, -30, 534, 156, 363 };
		public int[] Game_Effect_Roll_StartPoint_2P_Y = new int[] { 360 };
		public float[] Game_Effect_Roll_Speed_X = new float[] { 0.6f };
		public float[] Game_Effect_Roll_Speed_Y = new float[] { -0.6f };
		public float[] Game_Effect_Roll_Speed_1P_X = new float[] { 0.6f };
		public float[] Game_Effect_Roll_Speed_1P_Y = new float[] { -0.6f };
		public float[] Game_Effect_Roll_Speed_2P_X = new float[] { 0.6f };
		public float[] Game_Effect_Roll_Speed_2P_Y = new float[] { 0.6f };
		public int Game_Effect_Roll_Ptn;
		public int[] Game_Effect_NotesFlash = new int[] { 180, 180, 16 }; // Width, Height, Ptn
		public int Game_Effect_NotesFlash_Timer = 20;
		public int[] Game_Effect_GoGoSplash = new int[] { 300, 400, 30 };
		public int[] Game_Effect_GoGoSplash_X = new int[] { 120, 300, 520, 760, 980, 1160 };
		public int[] Game_Effect_GoGoSplash_Y = new int[] { 740, 730, 720, 720, 730, 740 };
		public bool Game_Effect_GoGoSplash_Rotate = true;
		public int Game_Effect_GoGoSplash_Timer = 18;
		// super-flying-notes AioiLight
		public int[] Game_Effect_FlyingNotes_StartPoint_X = new int[] { 414, 414 };
		public int[] Game_Effect_FlyingNotes_StartPoint_Y = new int[] { 260, 434 };
		public int[] Game_Effect_FlyingNotes_EndPoint_X = new int[] { 1222, 1222 }; // 1P, 2P
		public int[] Game_Effect_FlyingNotes_EndPoint_Y = new int[] { 164, 554 };
		public int[] Game_Effect_FlyingNotes_EndPoint_X_AI = new int[] { 1222, 1222 }; // 1P, 2P
		public int[] Game_Effect_FlyingNotes_EndPoint_Y_AI = new int[] { -230, 820 };

		public int Game_Effect_FlyingNotes_Sine = 220;
		public bool Game_Effect_FlyingNotes_IsUsingEasing = true;
		public int Game_Effect_FlyingNotes_Timer = 4;
		public int[] Game_Effect_FireWorks = new int[] { 180, 180, 30 };
		public int Game_Effect_FireWorks_Timer = 5;
		public int[] Game_Effect_Rainbow_X = new int[] { 360, 360 };
		public int[] Game_Effect_Rainbow_Y = new int[] { -100, 410 };
		public int Game_Effect_Rainbow_Timer = 8;

		public int[] Game_Effects_Hit_Explosion_X = new int[] { 284, 284 };
		public int[] Game_Effects_Hit_Explosion_Y = new int[] { 126, 303 };

		public bool Game_Effect_HitExplosion_AddBlend = true;
		public bool Game_Effect_HitExplosionBig_AddBlend = true;

		public int[] Game_Effect_Fire_X = new int[] { 240, 240 };
		public int[] Game_Effect_Fire_Y = new int[] { 71, 248 };

		public bool Game_Effect_FireWorks_AddBlend = true;
		public bool Game_Effect_Fire_AddBlend = true;
		public bool Game_Effect_GoGoSplash_AddBlend = true;
		public int Game_Effect_FireWorks_Timing = 8;

		public int[] Game_Effects_Hit_Explosion_4P = new int[] { 284, -20 };
		public int[] Game_Effects_Hit_Explosion_5P = new int[] { 284, -39 };

		public int[] Game_Effect_Fire_4P = new int[] { 240, -75 };
		public int[] Game_Effect_Fire_5P = new int[] { 240, -94 };
		#endregion
		#region Lane
		public int[] Game_Lane_X = new int[] { 333, 333 };
		public int[] Game_Lane_Y = new int[] { 192, 368 };
		public int[] Game_Lane_Sub_X = new int[] { 333, 333 };
		public int[] Game_Lane_Sub_Y = new int[] { 326, 502 };

		public int[] Game_Lane_4P = new int[] { 333, 46 };
		public int[] Game_Lane_5P = new int[] { 333, 39 };
		#endregion
		#region Runner
		#endregion
		#region PuchiChara
		public int[] Game_PuchiChara_X = new int[] { 100, 100 };
		public int[] Game_PuchiChara_Y = new int[] { 140, 675 };
		public int[] Game_PuchiChara_4P = new int[] { 230, 162 };
		public int[] Game_PuchiChara_5P = new int[] { 230, 150 };
		public int[] Game_PuchiChara_BalloonX = new int[] { 300, 300 };
		public int[] Game_PuchiChara_BalloonY = new int[] { 240, 500 };
		public int[] Game_PuchiChara_KusudamaX = new int[] { 290, 690, 90, 890, 490 };
		public int[] Game_PuchiChara_KusudamaY = new int[] { 420, 420, 420, 420, 420 };
		public float[] Game_PuchiChara_Scale = new float[] { 0.6f, 0.8f }; // 通常時、 ふうせん連打時
		public int[] Game_PuchiChara = new int[] { 256, 256, 2 }; // Width, Height, Ptn
		public int Game_PuchiChara_Sine = 20;
		public int Game_PuchiChara_Timer = 4800;
		public double Game_PuchiChara_SineTimer = 2;
		#endregion
		#region Dan-C
		public Color Game_DanC_Title_ForeColor = ColorTranslator.FromHtml("#FFFFFF");
		public Color Game_DanC_Title_BackColor = ColorTranslator.FromHtml("#000000");
		public Color Game_DanC_SubTitle_ForeColor = ColorTranslator.FromHtml("#FFFFFF");
		public Color Game_DanC_SubTitle_BackColor = ColorTranslator.FromHtml("#000000");

		public int[] Game_DanC_Size = new int[] { 1006, 92 };
		public int[] Game_DanC_Number_Size = new int[] { 48, 58 };
		public int[] Game_DanC_Small_Number_Size = new int[] { 24, 29 };
		public int[] Game_DanC_MiniNumber_Size = new int[] { 23, 28 };
		public int[] Game_DanC_ExamType_Size = new int[] { 247, 28 };
		public int[] Game_DanC_ExamRange_Size = new int[] { 54, 30 };
		public int[] Game_DanC_ExamUnit_Size = new int[] { 30, 36 };

		public int[] Game_DanC_Offset = new int[] { 259, 27 };
		public int[] Game_DanC_Number_Small_Number_Offset = new int[] { 285, 38 };
		public int[] Game_DanC_Exam_Offset = new int[] { 222, 27 };

		public int[] Game_DanC_X = new int[] { 807, 70, 70, 70 }; // 329, 437
		public int[] Game_DanC_Y = new int[] { 116, 292, 292, 292 }; // { 116, 190, 236, 292 };
		public int[] Game_DanC_Base_Offset_X = new int[] { 0, 503 };
		public int[] Game_DanC_Base_Offset_Y = new int[] { 0, 0 };
		public int[] Game_DanC_SmallBase_Offset_X = new int[] { 745, 410 };
		public int[] Game_DanC_SmallBase_Offset_Y = new int[] { 119, 119 };
		public int[] Game_DanC_Number_XY = new int[] { 214, 67 };
		public int[] Game_DanC_Dan_Plate = new int[] { 149, 416 };

		public int[] Game_DanC_DanIcon_Offset = new int[] { 44, 57 };
		public int[] Game_DanC_DanIcon_Offset_Mini = new int[] { -19, 11 };

		public int[] Game_DanC_Title_X = new int[] { 806, 806 };
		public int[] Game_DanC_Title_Y = new int[] { 257, 237 };
		public int[] Game_DanC_SubTitle = new int[] { 806, 277 };


		public int Game_DanC_Title_Size = 30;
		public int Game_DanC_SubTitle_Size = 22;
		public int Game_DanC_ExamFont_Size = 14;

		public int Game_DanC_Title_MaxWidth = 710;
		public int Game_DanC_SubTitle_MaxWidth = 710;

		public int Game_DanC_Padding = 9;
		public int Game_DanC_Number_Padding = 35;
		public int Game_DanC_Number_Small_Padding = 41;
		public int Game_DanC_ExamRange_Padding = 49;
		public int[] Game_DanC_Percent_Hit_Score_Padding = new int[] { 20, 20, 20 };

		public float Game_DanC_Number_Small_Scale = 0.92f;
		public float Game_DanC_Exam_Number_Scale = 0.47f;

		#endregion
		#region Training
		public int[] Game_Training_DownBG = new int[] { 0, 360 };
		public int[] Game_Training_BigTaiko = new int[] { 334, 400 };
		public int[] Game_Training_Speed_Measure = new int[] { 0, 360 };
		public int Game_Training_ScrollTime = 350;
		public int[] Game_Training_ProgressBar_XY = { 333, 378 };
		public int Game_Training_GoGoPoint_Y = 396;
		public int Game_Training_JumpPoint_Y = 375;
		public int[] Game_Training_MaxMeasureCount_XY = { 284, 377 };
		public int[] Game_Training_CurrentMeasureCount_XY = { 254, 370 };
		public int[] Game_Training_SpeedDisplay_XY = { 110, 370 };
		public int Game_Training_SmallNumber_Width = 17;
		public int Game_Training_BigNumber_Width = 20;
		#endregion
		#region Tower
		public int[] Game_Tower_Sky_Gradient = new int[] { 0, 360 };
		public int[] Game_Tower_Sky_Gradient_Size = new int[] { 1280, 316 };

		public int[] Game_Tower_Floors_Body = new int[] { 640, 676 };
		public int[] Game_Tower_Floors_Deco = new int[] { 460, 640 };
		public int[] Game_Tower_Floors_Move = new int[] { 0, 288 };

		public int[] Game_Tower_Don = new int[] { 590, 648 };
		public int[] Game_Tower_Don_Move = new int[] { 300, 0 };

		public int[] Game_Tower_Miss = new int[] { 640, 520 };

		public int[] Game_Tower_Floor_Number = new int[] { 556, 84 };

		public int[] Game_Tower_Life_Number = new int[] { 996, 106 };

		public int[] Game_Tower_Font_TouTatsuKaiSuu = new int[] { 350, 32 };
		public int[] Game_Tower_Font_Kai = new int[] { 550, 104 };

		public int Game_Tower_Font_TowerText = 28;
		#endregion
		#region AIBattle
		public int Game_AIBattle_CharaMove = 71;

		public int[] Game_AIBattle_SectionTime_Panel = new int[] { 202, 178 };
		public int[] Game_AIBattle_SectionTime_Bar = new int[] { 205, 193 };

		public int[] Game_AIBattle_Batch_Base = new int[] { 150, 83 };

		public int[] Game_AIBattle_Batch = new int[] { 150, 83 };
		public int[] Game_AIBattle_Batch_Size = new int[] { 70, 70 };

		public int[] Game_AIBattle_Batch_Anime = new int[] { 260, -35 };
		public int[] Game_AIBattle_Batch_Anime_Size = new int[] { 274, 274 };

		public int[] Game_AIBattle_Batch_Move = new int[] { 30, 15 };

		public int[] Game_AIBattle_Judge_Meter_X = new int[] { 3, 3 };
		public int[] Game_AIBattle_Judge_Meter_Y = new int[] { 55, 418 };

		public int[] Game_AIBattle_Judge_Number_Perfect_X = new int[] { 107, 107 };
		public int[] Game_AIBattle_Judge_Number_Perfect_Y = new int[] { 74, 437 };

		public int[] Game_AIBattle_Judge_Number_Good_X = new int[] { 107, 107 };
		public int[] Game_AIBattle_Judge_Number_Good_Y = new int[] { 91, 454 };

		public int[] Game_AIBattle_Judge_Number_Miss_X = new int[] { 107, 107 };
		public int[] Game_AIBattle_Judge_Number_Miss_Y = new int[] { 108, 471 };

		public int[] Game_AIBattle_Judge_Number_Roll_X = new int[] { 107, 107 };
		public int[] Game_AIBattle_Judge_Number_Roll_Y = new int[] { 125, 488 };

		public int[] Game_AIBattle_Judge_Number_Interval = new int[] { 10, 0 };
		#endregion

		#endregion
		#region Result
		/*
        public int[] Result_UIMove_4P = new int[] { 320, 0 };
        public int[] Result_UIMove_5P = new int[] { 256, 0 };
        */

		public bool Result_Use1PUI = false;
		public int[] Result_UIMove_4P_X = new int[] { 0, 320, 640, 960 };
		public int[] Result_UIMove_4P_Y = new int[] { 0, 0, 0, 0 };
		public int[] Result_UIMove_5P_X = new int[] { 0, 256, 512, 768, 1024 };
		public int[] Result_UIMove_5P_Y = new int[] { 0, 0, 0, 0, 0 };

		public int Result_Cloud_Count = 11;
		public int[] Result_Cloud_X = new int[] { 642, 612, 652, 1148, 1180, 112, 8, 1088, 1100, 32, 412 };
		public int[] Result_Cloud_Y = new int[] { 202, 424, 636, 530, 636, 636, 102, 52, 108, 326, 644 };
		public int[] Result_Cloud_MaxMove = new int[] { 150, 120, 180, 60, 90, 150, 120, 50, 45, 120, 180 };

		public int Result_Shine_Count = 6;
		public int[][] Result_Shine_X = new int[][] {
			new int[] { 885, 1255, 725, 890, 1158, 1140 },
			new int[] { 395, 25, 555, 390, 122, 140 },
		};
		public int[][] Result_Shine_Y = new int[][] {
			new int[] { 650, 405, 645, 420, 202, 585 },
			new int[] { 650, 405, 645, 420, 202, 585 }
		};
		public float[] Result_Shine_Size = { 0.44f, 0.6f, 0.4f, 0.15f, 0.35f, 0.6f };

		public int[][] Result_Work_X = new int[][] {
			new int[] { 800, 900, 1160 },
			new int[] { 480, 380, 120 }
		};
		public int[][] Result_Work_Y = new int[][] {
			new int[] { 435, 185, 260 },
			new int[] { 435, 185, 260 }
		};

		public int[] Result_DifficultyBar_Size = new int[] { 185, 54 };
		public int[] Result_DifficultyBar_X = new int[] { 18, 653 };
		public int[] Result_DifficultyBar_Y = new int[] { 101, 101 };

		public int[] Result_Gauge_Base_X = new int[] { 55, 690 };
		public int[] Result_Gauge_Base_Y = new int[] { 140, 140 };

		public int[] Result_Gauge_X = new int[] { 57, 692 };
		public int[] Result_Gauge_Y = new int[] { 140, 140 };
		public int[] Result_Gauge_Rect = new int[] { 0, 0, 487, 36 };

		public int[] Result_Gauge_Rainbow_X = new int[] { 57, 692 };
		public int[] Result_Gauge_Rainbow_Y = new int[] { 144, 144 };
		public int Result_Gauge_Rainbow_Ptn;
		public int Result_Gauge_Rainbow_Interval = 1000 / 60;

		public int[] Result_Gauge_ClearText_X = new int[] { 441, 1076 };
		public int[] Result_Gauge_ClearText_Y = new int[] { 142, 142 };
		public int[] Result_Gauge_ClearText_Rect = new int[] { 0, 35, 42, 20 };
		public int[] Result_Gauge_ClearText_Clear_Rect = new int[] { 42, 35, 42, 20 };

		public int[] Result_Number_Interval = new int[] { 22, 0 };

		public float Result_Number_Scale_4P = 1.0f;

		public float Result_Number_Scale_5P = 1.0f;

		public int[] Result_Soul_Fire_X = new int[] { 576, 1211 };
		public int[] Result_Soul_Fire_Y = new int[] { 160, 160 };

		public int[] Result_Soul_Text_X = new int[] { 575, 1210 };
		public int[] Result_Soul_Text_Y = new int[] { 159, 159 };

		public int[] Result_Perfect_X = new int[] { 490, 1125 };
		public int[] Result_Perfect_Y = new int[] { 188, 188 };

		public int[] Result_Good_X = new int[] { 490, 1125 };
		public int[] Result_Good_Y = new int[] { 230, 230 };

		public int[] Result_Miss_X = new int[] { 490, 1125 };
		public int[] Result_Miss_Y = new int[] { 272, 272 };

		public int[] Result_Roll_X = new int[] { 490, 1125 };
		public int[] Result_Roll_Y = new int[] { 314, 314 };

		public int[] Result_MaxCombo_X = new int[] { 490, 1125 };
		public int[] Result_MaxCombo_Y = new int[] { 356, 356 };

		public bool Result_ADLib_Show = false;
		public int[] Result_ADLib_X = new int[] { 0, 0 };
		public int[] Result_ADLib_Y = new int[] { 0, 0 };

		public bool Result_Bomb_Show = false;
		public int[] Result_Bomb_X = new int[] { 0, 0 };
		public int[] Result_Bomb_Y = new int[] { 0, 0 };

		public int[] Result_Score_X = new int[] { 295, 930 };
		public int[] Result_Score_Y = new int[] { 212, 212 };
		public int[] Result_Score_Number_Interval = new int[] { 33, 0 };

		public float Result_Score_Scale_4P = 1.0f;
		public float Result_Score_Scale_5P = 1.0f;

		public int[] Result_ScoreRankEffect_X = new int[] { 135, 770 };
		public int[] Result_ScoreRankEffect_Y = new int[] { 339, 339 };

		public int[] Result_CrownEffect_X = new int[] { 262, 897 };
		public int[] Result_CrownEffect_Y = new int[] { 336, 336 };

		public int[] Result_Speech_Bubble_X = new int[] { 430, 850 };
		public int[] Result_Speech_Bubble_Y = new int[] { 526, 526 };

		public int[] Result_Speech_Bubble_V2_X = new int[] { 0, 0 };
		public int[] Result_Speech_Bubble_V2_Y = new int[] { 0, 0 };

		public int[] Result_Speech_Bubble_V2_2P_X = new int[] { 0, 0 };
		public int[] Result_Speech_Bubble_V2_2P_Y = new int[] { 0, 0 };

		public int[] Result_NamePlate_X = new int[] { 28, 1032 };
		public int[] Result_NamePlate_Y = new int[] { 621, 621 };

		public int[] Result_ModIcons_X = new int[] { 32, 1028 };
		public int[] Result_ModIcons_Y = new int[] { 678, 678 };

		public int[] Result_Flower_X = new int[] { 182, 1098 };
		public int[] Result_Flower_Y = new int[] { 602, 602 };

		public int[][] Result_Flower_Rotate_X = new int[][] {
			new int[] { 48, 125, 48, 240, 87 },
			new int[] { 964, 1041, 964, 1156, 1003 },
		};
		public int[][] Result_Flower_Rotate_Y = new int[][] {
			new int[] { 549, 585, 546, 501, 509 },
			new int[] { 549, 585, 546, 501, 509 }
		};

		public int Result_PlateShine_Count = 6;
		public int[][] Result_PlateShine_X = new int[][] {
			new int[] { 333, 342, 184, 198, 189, 309 },
			new int[] { 1249, 1258, 1100, 1114, 1105, 1225 }
		};
		public int[][] Result_PlateShine_Y = new int[][] {
			new int[] { 670, 620, 650, 687, 558, 542 },
			new int[] { 670, 620, 650, 687, 558, 542 }
		};

		public int Result_MusicName_X = 640;
		public int Result_MusicName_Y = 30;
		public int Result_MusicName_FontSize = 25;
		public int Result_MusicName_MaxSize = 660;
		public ReferencePoint Result_MusicName_ReferencePoint = ReferencePoint.Center;
		public int Result_StageText_X = 230;
		public int Result_StageText_Y = 6;
		public int Result_StageText_FontSize = 30;
		public ReferencePoint Result_StageText_ReferencePoint = ReferencePoint.Left;

		public Color Result_MusicName_ForeColor = ColorTranslator.FromHtml("#FFFFFF");
		public Color Result_StageText_ForeColor = ColorTranslator.FromHtml("#FFFFFF");
		//public Color Result_StageText_ForeColor_Red = ColorTranslator.FromHtml("#FFFFFF");
		public Color Result_MusicName_BackColor = ColorTranslator.FromHtml("#000000");
		public Color Result_StageText_BackColor = ColorTranslator.FromHtml("#000000");
		//public Color Result_StageText_BackColor_Red = ColorTranslator.FromHtml("#FF0000");


		public int[] Result_Dan = new int[] { 500, 500 };
		public int[] Result_Dan_XY = new int[] { 0, 420 };
		public int[] Result_Dan_Plate_XY = new int[] { 149, 149 };

		public int[] Result_DifficultyBar_4P = new int[] { 6, 101 };
		public int[] Result_DifficultyBar_5P = new int[] { -9, 101 };

		public int[] Result_Gauge_Base_4P = new int[] { 25, 140 };
		public int[] Result_Gauge_Base_5P = new int[] { 4, 140 };

		public int[] Result_Gauge_4P = new int[] { 27, 140 };
		public int[] Result_Gauge_5P = new int[] { 6, 140 };

		public int[] Result_Gauge_ClearText_4P = new int[] { 218, 142 };
		public int[] Result_Gauge_ClearText_5P = new int[] { 197, 142 };

		public int[] Result_Gauge_Rainbow_4P = new int[] { 26, 144 };
		public int[] Result_Gauge_Rainbow_5P = new int[] { 5, 144 };

		public int[] Result_Soul_Fire_4P = new int[] { 284, 160 };
		public int[] Result_Soul_Fire_5P = new int[] { 232, 120 };

		public int[] Result_Soul_Text_4P = new int[] { 283, 159 };
		public int[] Result_Soul_Text_5P = new int[] { 231, 119 };

		public int[] Result_Perfect_4P = new int[] { 183, 251 };
		public int[] Result_Perfect_5P = new int[] { 151, 251 };

		public int[] Result_Good_4P = new int[] { 183, 293 };
		public int[] Result_Good_5P = new int[] { 151, 293 };

		public int[] Result_Miss_4P = new int[] { 183, 335 };
		public int[] Result_Miss_5P = new int[] { 151, 335 };

		public int[] Result_Roll_4P = new int[] { 183, 377 };
		public int[] Result_Roll_5P = new int[] { 151, 377 };

		public int[] Result_MaxCombo_4P = new int[] { 183, 419 };
		public int[] Result_MaxCombo_5P = new int[] { 151, 419 };

		public int[] Result_ADLib_4P = new int[] { 0, 0 };
		public int[] Result_ADLib_5P = new int[] { 0, 0 };

		public int[] Result_Bomb_4P = new int[] { 0, 0 };
		public int[] Result_Bomb_5P = new int[] { 0, 0 };

		public int[] Result_Score_4P = new int[] { 253, 180 };
		public int[] Result_Score_5P = new int[] { 221, 180 };

		public int[] Result_ScoreRankEffect_4P = new int[] { 100, 545 };
		public int[] Result_ScoreRankEffect_5P = new int[] { 68, 545 };

		public int[] Result_CrownEffect_4P = new int[] { 220, 545 };
		public int[] Result_CrownEffect_5P = new int[] { 188, 545 };


		public int[] Result_Speech_Bubble_V2_4P = new int[] { 0, 0 };

		public int[] Result_Speech_Bubble_V2_5P = new int[] { 0, 0 };


		public int[] Result_Speech_Text_Offset = new int[] { 0, 0 };
		public int Result_Speech_Text_Size = 60;
		public int Result_Speech_Text_MaxWidth = 560;

		public int[] Result_NamePlate_4P = new int[] { 80, 621 };
		public int[] Result_NamePlate_5P = new int[] { 31, 621 };

		public int[] Result_ModIcons_4P = new int[] { 15, 678 };
		public int[] Result_ModIcons_5P = new int[] { -17, 678 };
		#endregion

		#region AIResult

		public int[] Result_AIBattle_Batch = new int[] { 884, 188 };
		public int[] Result_AIBattle_Batch_Move = new int[] { 104, 43 };
		public int[] Result_AIBattle_SectionPlate_Offset = new int[] { 55, 8 };

		public int[] Result_AIBattle_SectionText_Offset = new int[] { 110, 27 };
		public int Result_AIBattle_SectionText_Scale = 13;

		public int[] Result_AIBattle_WinFlag = new int[] { 946, 526 };

		#endregion

		#region DanResult

		public int[] DanResult_StatePanel = new int[] { 0, -4 };

		public int[] DanResult_SongPanel_Main_X = new int[] { 255, 255, 255 };
		public int[] DanResult_SongPanel_Main_Y = new int[] { 100, 283, 466 };

		public int[] DanResult_Difficulty_Cymbol_X = new int[] { 377, 377, 377 };
		public int[] DanResult_Difficulty_Cymbol_Y = new int[] { 146, 329, 512 };

		public int[] DanResult_Level_Number_X = new int[] { 383, 383, 383 };
		public int[] DanResult_Level_Number_Y = new int[] { 173, 356, 539 };

		public int[] DanResult_Sections_Perfect_X = new int[] { 455, 455, 455 };
		public int[] DanResult_Sections_Perfect_Y = new int[] { 204, 387, 570 };

		public int[] DanResult_Sections_Good_X = new int[] { 666, 666, 666 };
		public int[] DanResult_Sections_Good_Y = new int[] { 204, 387, 570 };

		public int[] DanResult_Sections_Miss_X = new int[] { 877, 877, 877 };
		public int[] DanResult_Sections_Miss_Y = new int[] { 204, 387, 570 };

		public int[] DanResult_Sections_Roll_X = new int[] { 1088, 1088, 1088 };
		public int[] DanResult_Sections_Roll_Y = new int[] { 204, 387, 570 };


		public int[] DanResult_Perfect = new int[] { 720, 95 };
		public int[] DanResult_Good = new int[] { 720, 137 };
		public int[] DanResult_Miss = new int[] { 720, 179 };
		public int[] DanResult_Roll = new int[] { 1022, 95 };
		public int[] DanResult_MaxCombo = new int[] { 1022, 137 };
		public int[] DanResult_TotalHit = new int[] { 1022, 179 };
		public int[] DanResult_Score = new int[] { 566, 119 };

		public int[] DanResult_Exam = new int[] { 232, 254 };

		public int[] DanResult_DanTitles_X = new int[] { 401, 401, 401 };
		public int[] DanResult_DanTitles_Y = new int[] { 139, 322, 505 };

		public int[] DanResult_DanIcon_X = new int[] { 315, 315, 315 };
		public int[] DanResult_DanIcon_Y = new int[] { 158, 342, 526 };

		public int[] DanResult_Rank = new int[] { 130, 380 };

		public int DanResult_Font_DanTitles_Size = 24;

		#endregion

		#region TowerResult

		public int[] TowerResult_ScoreRankEffect = new int[] { 1000, 220 };

		public int[] TowerResult_Toutatsu = new int[] { 196, 160 };
		public int[] TowerResult_MaxFloors = new int[] { 616, 296 };
		public int[] TowerResult_Ten = new int[] { 982, 394 };
		public int[] TowerResult_Score = new int[] { 248, 394 };

		public int[] TowerResult_CurrentFloor = new int[] { 688, 258 };
		public int[] TowerResult_ScoreCount = new int[] { 1026, 394 };
		public int[] TowerResult_RemainingLifes = new int[] { 1068, 490 };

		public int[] TowerResult_Gauge_Soul = new int[] { 248, 474 };

		public int TowerResult_Font_TowerText = 28;
		public int TowerResult_Font_TowerText48 = 48;
		public int TowerResult_Font_TowerText72 = 72;

		#endregion

		#region Heya

		public int[] Heya_Main_Menu_X = new int[] { 164, 164, 164, 164, 164 };
		public int[] Heya_Main_Menu_Y = new int[] { 26, 106, 186, 266, 346 };
		public int[] Heya_Main_Menu_Font_Offset = new int[] { 0, 14 };
		public int Heya_Center_Menu_Box_Count = 11;
		public int[] Heya_Center_Menu_Box_X = new int[] { -890, -588, -286, 16, 318, 620, 922, 1224, 1526, 1828, 2130 };
		public int[] Heya_Center_Menu_Box_Y = new int[] { 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200 };
		public int[] Heya_Center_Menu_Box_Item_Offset = new int[] { 0, 120 };
		public int[] Heya_Center_Menu_Box_Name_Offset = new int[] { 0, 234 };
		public int[] Heya_Center_Menu_Box_Authors_Offset = new int[] { 0, 260 };
		public int Heya_Side_Menu_Count = 13;
		public int[] Heya_Side_Menu_X = new int[] { 670, 680, 690, 700, 710, 720, 730, 720, 710, 700, 690, 680, 670 };
		public int[] Heya_Side_Menu_Y = new int[] { -80, -10, 60, 130, 200, 270, 340, 410, 480, 550, 620, 690, 760 };
		public int[] Heya_Side_Menu_Font_Offset = new int[] { 0, 14 };
		public int[] Heya_InfoSection = new int[] { 620, 560 };
		public int[] Heya_DescriptionTextOrigin = new int[] { 0, 0 };
		public int Heya_Font_Scale = 14;

		#endregion

		#region OnlineLounge 

		public int[] OnlineLounge_Side_Menu = new int[] { 640, 360 };
		public int[] OnlineLounge_Side_Menu_Text_Offset = new int[] { 0, 18 };
		public int[] OnlineLounge_Side_Menu_Move = new int[] { 0, 80 };

		public int[] OnlineLounge_Song = new int[] { 350, 360 };
		public int[] OnlineLounge_Song_Title_Offset = new int[] { 0, 18 };
		public int[] OnlineLounge_Song_SubTitle_Offset = new int[] { 0, 46 };
		public int[] OnlineLounge_Song_Move = new int[] { 0, 100 };

		public int[] OnlineLounge_Context_Charter = new int[] { 980, 300 };
		public int[] OnlineLounge_Context_Genre = new int[] { 980, 340 };
		public int[] OnlineLounge_Context_Couse_Symbol = new int[] { 800, 480 };
		public int[] OnlineLounge_Context_Level = new int[] { 900, 494 };
		public int[] OnlineLounge_Context_Couse_Move = new int[] { 240, 60 };

		public int[] OnlineLounge_Downloading = new int[] { 640, 605 };

		public int OnlineLounge_Font_OLFont = 14;
		public int OnlineLounge_Font_OLFontLarge = 28;

		#endregion

		#region TowerSelect 

		public int TowerSelect_Title_Size = 30;
		public int TowerSelect_Title_MaxWidth = 230;
		public int[] TowerSelect_Title_Offset = new int[] { 0, -30 };
		public int TowerSelect_SubTitle_Size = 30;
		public int TowerSelect_SubTitle_MaxWidth = 230;
		public int[] TowerSelect_SubTitle_Offset = new int[] { 0, 10 };
		public int TowerSelect_Bar_Count = 7;
		public int[] TowerSelect_Bar_X = new int[] { -260, 40, 340, 640, 940, 1240, 1540 };
		public int[] TowerSelect_Bar_Y = new int[] { 420, 400, 380, 360, 380, 400, 420 };

		#endregion

		#region OpenEncyclopedia 

		public int[] OpenEncyclopedia_Context_Item2 = new int[] { 960, 180 };
		public int[] OpenEncyclopedia_Context_Item3 = new int[] { 640, 360 };
		public int[] OpenEncyclopedia_Context_PageText = new int[] { 960, 720 };
		public int[] OpenEncyclopedia_Side_Menu = new int[] { 320, 360 };
		public int[] OpenEncyclopedia_Side_Menu_Move = new int[] { 0, 90 };
		public int[] OpenEncyclopedia_Side_Menu_Text_Offset = new int[] { 0, 0 };
		public int OpenEncyclopedia_Font_EncyclopediaMenu_Size = 14;

		#endregion

		#region Exit
		public int Exit_Duration = 3000;
		#endregion

		#region Font
		public int Font_Edge_Ratio = 30;
		public int Font_Edge_Ratio_Vertical = 30;
		public int Text_Correction_X = 0;
		public int Text_Correction_Y = 0;
		#endregion

		#region NamePlate

		public int[] NamePlate_Title_Offset = new int[] { 124, 22 };
		public int[] NamePlate_Dan_Offset = new int[] { 69, 44 };
		public int[] NamePlate_Name_Offset_Normal = new int[] { 121, 36 };
		public int[] NamePlate_Name_Offset_WithTitle = new int[] { 121, 44 };
		public int[] NamePlate_Name_Offset_Full = new int[] { 144, 44 };
		public int NamePlate_Name_Width_Normal = 220;
		public int NamePlate_Name_Width_Full = 120;
		public int NamePlate_Title_Width = 160;
		public int NamePlate_Dan_Width = 66;

		public int NamePlate_Font_Name_Size_Normal = 15;
		public int NamePlate_Font_Name_Size_WithTitle = 12;
		public int NamePlate_Font_Title_Size = 11;
		public int NamePlate_Font_Dan_Size = 12;

		#endregion

		#region [Mod icons]

		public int[] ModIcons_OffsetX = { 0, 30, 60, 90, 0, 30, 60, 90 };
		public int[] ModIcons_OffsetY = { 0, 0, 0, 0, 30, 30, 30, 30 };
		public int[] ModIcons_OffsetX_Menu = { 0, 30, 60, 90, 120, 150, 180, 210 };
		public int[] ModIcons_OffsetY_Menu = { 0, 0, 0, 0, 0, 0, 0, 0 };

		#endregion

		#region Modal

		public int[] Modal_Title_Full = new int[] { 640, 140 };
		public int[] Modal_Title_Half_X = new int[] { 320, 960 };
		public int[] Modal_Title_Half_Y = new int[] { 290, 290 };

		public int[] Modal_Text_Full = new int[] { 640, 327 };//445
		public int[] Modal_Text_Full_Move = new int[] { 0, 118 };
		public int[] Modal_Text_Half_X = new int[] { 320, 960 };
		public int[] Modal_Text_Half_Y = new int[] { 383, 383 };
		public int[] Modal_Text_Half_Move = new int[] { 0, 59 };

		public int Modal_Font_ModalContentHalf_Size = 28;
		public int Modal_Font_ModalTitleHalf_Size = 28;
		public int Modal_Font_ModalContentFull_Size = 56;
		public int Modal_Font_ModalTitleFull_Size = 56;

		public int[] Modal_Title_Half_X_4P = new int[] { 320, 960, 320, 960 };
		public int[] Modal_Title_Half_X_5P = new int[] { 320, 960, 320, 960, 320 };

		public int[] Modal_Title_Half_Y_4P = new int[] { 66, 66, 426, 426 };
		public int[] Modal_Title_Half_Y_5P = new int[] { 50, 50, 290, 290, 530 };

		public int[] Modal_Text_Half_X_4P = new int[] { 320, 960, 320, 960 };
		public int[] Modal_Text_Half_X_5P = new int[] { 320, 960, 320, 960, 320 };

		public int[] Modal_Text_Half_Y_4P = new int[] { 159, 159, 519, 519 };
		public int[] Modal_Text_Half_Y_5P = new int[] { 107, 107, 347, 347, 587 };

		public int[] Modal_Text_Half_Move_4P = new int[] { 0, 59 };
		public int[] Modal_Text_Half_Move_5P = new int[] { 0, 40 };

		#endregion

		#region PopupMenu

		public int[] PopupMenu_Menu_Title = new int[2] { 460, 40 };
		public int[] PopupMenu_Title = new int[2] { 540, 44 };
		public int[] PopupMenu_Menu_Highlight = new int[2] { 480, 46 };
		public int[] PopupMenu_MenuItem_Name = new int[2] { 480, 77 };
		public int[] PopupMenu_MenuItem_Value = new int[2] { 630, 77 };
		public int[] PopupMenu_Move = new int[2] { 0, 32 };
		public int PopupMenu_Font_Size = 18;

		#endregion

		#endregion
	}
}

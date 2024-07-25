using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FDK.BassMixExtension;
using FDK.ExtensionMethods;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Mix;


namespace FDK {
	// CSound は、サウンドデバイスが変更されたときも、インスタンスを再作成することなく、新しいデバイスで作り直せる必要がある。
	// そのため、デバイスごとに別のクラスに分割するのではなく、１つのクラスに集約するものとする。

	public class CSound : IDisposable {
		public const int MinimumSongVol = 0;
		public const int MaximumSongVol = 200; // support an approximate doubling in volume.
		public const int DefaultSongVol = 100;

		// 2018-08-19 twopointzero: Note the present absence of a MinimumAutomationLevel.
		// We will revisit this if/when song select BGM fade-in/fade-out needs
		// updating due to changing the type or range of AutomationLevel
		public const int MaximumAutomationLevel = 100;
		public const int DefaultAutomationLevel = 100;

		public const int MinimumGroupLevel = 0;
		public const int MaximumGroupLevel = 100;
		public const int DefaultGroupLevel = 100;
		public const int DefaultSoundEffectLevel = 80;
		public const int DefaultVoiceLevel = 90;
		public const int DefaultSongPreviewLevel = 90;
		public const int DefaultSongPlaybackLevel = 90;

		public static readonly Lufs MinimumLufs = new Lufs(-100.0);
		public static readonly Lufs MaximumLufs = new Lufs(10.0); // support an approximate doubling in volume.

		private static readonly Lufs DefaultGain = new Lufs(0.0);

		public readonly ESoundGroup SoundGroup;

		#region [ DTXMania用拡張 ]

		public int TotalPlayTime {
			get;
			private set;
		}
		public int SoundBufferSize      // 取りあえず0固定★★★★★★★★★★★★★★★★★★★★
		{
			get { return 0; }
		}
		public bool IsStreamPlay            // 取りあえずfalse固定★★★★★★★★★★★★★★★★★★★★
											// trueにすると同一チップ音の多重再生で問題が出る(4POLY音源として動かない)
		{
			get { return false; }
		}
		public double Frequency {
			get {
				return _Frequency;
			}
			set {
				if (_Frequency != value) {
					_Frequency = value;
					if (IsBassSound) {
						Bass.ChannelSetAttribute(this.hBassStream, ChannelAttribute.Frequency, (float)(_Frequency * _PlaySpeed * nオリジナルの周波数));
					}
				}
			}
		}
		public double PlaySpeed {
			get {
				return _PlaySpeed;
			}
			set {
				if (_PlaySpeed != value) {
					_PlaySpeed = value;
					IsNormalSpeed = (_PlaySpeed == 1.000f);
					if (IsBassSound) {
						if (_hTempoStream != 0 && !this.IsNormalSpeed)  // 再生速度がx1.000のときは、TempoStreamを用いないようにして高速化する
						{
							this.hBassStream = _hTempoStream;
						} else {
							this.hBassStream = _hBassStream;
						}

						if (SoundManager.bIsTimeStretch) {
							Bass.ChannelSetAttribute(this.hBassStream, ChannelAttribute.Tempo, (float)(PlaySpeed * 100 - 100));
							//double seconds = Bass.BASS_ChannelBytes2Seconds( this.hTempoStream, nBytes );
							//this.n総演奏時間ms = (int) ( seconds * 1000 );
						} else {
							Bass.ChannelSetAttribute(this.hBassStream, ChannelAttribute.Frequency, (float)(_Frequency * _PlaySpeed * nオリジナルの周波数));
						}
					}
				}
			}
		}
		#endregion

		public bool b速度上げすぎ問題 = false;
		public bool b演奏終了後も再生が続くチップである = false; // これがtrueなら、本サウンドの再生終了のコールバック時に自動でミキサーから削除する

		private SyncProcedure _cbEndofStream;

		/// <summary>
		/// Gain is applied "first" to the audio data, much as in a physical or
		/// software mixer. Later steps in the flow of audio apply "channel" level
		/// (e.g. AutomationLevel) and mixing group level (e.g. GroupLevel) before
		/// the audio is output.
		/// 
		/// This method, taking an integer representing a percent value, is used
		/// for mixing in the SONGVOL value, when available. It is also used for
		/// DTXViewer preview mode.
		/// </summary>
		public void SetGain(int songVol) {
			SetGain(LinearIntegerPercentToLufs(songVol), null);
		}

		private static Lufs LinearIntegerPercentToLufs(int percent) {
			// 2018-08-27 twopointzero: We'll use the standard conversion until an appropriate curve can be selected
			return new Lufs(20.0 * Math.Log10(percent / 100.0));
		}

		/// <summary>
		/// Gain is applied "first" to the audio data, much as in a physical or
		/// software mixer. Later steps in the flow of audio apply "channel" level
		/// (e.g. AutomationLevel) and mixing group level (e.g. GroupLevel) before
		/// the audio is output.
		/// 
		/// This method, taking a LUFS gain value and a LUFS true audio peak value,
		/// is used for mixing in the loudness-metadata-base gain value, when available.
		/// </summary>
		public void SetGain(Lufs gain, Lufs? truePeak) {
			if (Equals(_gain, gain)) {
				return;
			}

			_gain = gain;
			_truePeak = truePeak;

			if (SoundGroup == ESoundGroup.SongPlayback) {
				Trace.TraceInformation($"{nameof(CSound)}.{nameof(SetGain)}: Gain: {_gain}. True Peak: {_truePeak}");
			}

			SetVolume();
		}

		/// <summary>
		/// AutomationLevel is applied "second" to the audio data, much as in a
		/// physical or sofware mixer and its channel level. Before this Gain is
		/// applied, and after this the mixing group level is applied.
		///
		/// This is currently used only for automated fade in and out as is the
		/// case right now for the song selection screen background music fade
		/// in and fade out.
		/// </summary>
		public int AutomationLevel {
			get => _automationLevel;
			set {
				if (_automationLevel == value) {
					return;
				}

				_automationLevel = value;

				if (SoundGroup == ESoundGroup.SongPlayback) {
					Trace.TraceInformation($"{nameof(CSound)}.{nameof(AutomationLevel)} set: {AutomationLevel}");
				}

				SetVolume();
			}
		}

		/// <summary>
		/// GroupLevel is applied "third" to the audio data, much as in the sub
		/// mixer groups of a physical or software mixer. Before this both the
		/// Gain and AutomationLevel are applied, and after this the audio
		/// flows into the audio subsystem for mixing and output based on the
		/// master volume.
		///
		/// This is currently automatically managed for each sound based on the
		/// configured and dynamically adjustable sound group levels for each of
		/// sound effects, voice, song preview, and song playback.
		///
		/// See the SoundGroupLevelController and related classes for more.
		/// </summary>
		public int GroupLevel {
			private get => _groupLevel;
			set {
				if (_groupLevel == value) {
					return;
				}

				_groupLevel = value;

				if (SoundGroup == ESoundGroup.SongPlayback) {
					Trace.TraceInformation($"{nameof(CSound)}.{nameof(GroupLevel)} set: {GroupLevel}");
				}

				SetVolume();
			}
		}

		private void SetVolume() {
			var automationLevel = LinearIntegerPercentToLufs(AutomationLevel);
			var groupLevel = LinearIntegerPercentToLufs(GroupLevel);

			var gain =
				_gain +
				automationLevel +
				groupLevel;

			var safeTruePeakGain = _truePeak?.Negate() ?? new Lufs(0);
			var finalGain = gain.Min(safeTruePeakGain);

			if (SoundGroup == ESoundGroup.SongPlayback) {
				Trace.TraceInformation(
					$"{nameof(CSound)}.{nameof(SetVolume)}: Gain:{_gain}. Automation Level: {automationLevel}. Group Level: {groupLevel}. Summed Gain: {gain}. Safe True Peak Gain: {safeTruePeakGain}. Final Gain: {finalGain}.");
			}

			lufs音量 = finalGain;
		}

		private Lufs lufs音量 {
			set {
				if (this.IsBassSound) {
					var db音量 = ((value.ToDouble() / 100.0) + 1.0).Clamp(0, 1);
					Bass.ChannelSetAttribute(this._hBassStream, ChannelAttribute.Volume, (float)db音量);
					Bass.ChannelSetAttribute(this._hTempoStream, ChannelAttribute.Volume, (float)db音量);
				}
			}
		}

		/// <summary>
		/// <para>左:-100～中央:0～100:右。set のみ。</para>
		/// </summary>
		public int SoundPosition {
			get {
				if (this.IsBassSound) {
					float f位置 = 0.0f;
					if (!Bass.ChannelGetAttribute(this.hBassStream, ChannelAttribute.Pan, out f位置))
						//if( BassMix.BASS_Mixer_ChannelGetEnvelopePos( this.hBassStream, BASSMIXEnvelope.BASS_MIXER_ENV_PAN, ref f位置 ) == -1 )
						return 0;
					return (int)(f位置 * 100);
				}
				return -9999;
			}
			set {
				if (this.IsBassSound) {
					float f位置 = Math.Min(Math.Max(value, -100), 100) / 100.0f;  // -100～100 → -1.0～1.0
																				//var nodes = new BASS_MIXER_NODE[ 1 ] { new BASS_MIXER_NODE( 0, f位置 ) };
																				//BassMix.BASS_Mixer_ChannelSetEnvelope( this.hBassStream, BASSMIXEnvelope.BASS_MIXER_ENV_PAN, nodes );
					Bass.ChannelSetAttribute(this.hBassStream, ChannelAttribute.Pan, f位置);
				}
			}
		}

		/// <summary>
		/// <para>全インスタンスリスト。</para>
		/// <para>～を作成する() で追加され、t解放する() or Dispose() で解放される。</para>
		/// </summary>
		public static readonly ObservableCollection<CSound> SoundInstances = new ObservableCollection<CSound>();

		public static void ShowAllCSoundFiles() {
			int i = 0;
			foreach (CSound cs in SoundInstances) {
				Debug.WriteLine(i++.ToString("d3") + ": " + Path.GetFileName(cs.FileName));
			}
		}

		public CSound(ESoundGroup soundGroup) {
			SoundGroup = soundGroup;
			this.SoundPosition = 0;
			this._Frequency = 1.0;
			this._PlaySpeed = 1.0;
			//			this._cbRemoveMixerChannel = new WaitCallback( RemoveMixerChannelLater );
			this._hBassStream = -1;
			this._hTempoStream = 0;
		}

		public void CreateBassSound(string fileName, int hMixer) {
			this.CurrentSoundDeviceType = ESoundDeviceType.Bass;        // 作成後に設定する。（作成に失敗してると例外発出されてここは実行されない）
			this.CreateBassSound(fileName, hMixer, BassFlags.Decode);
		}
		public void CreateASIOSound(string fileName, int hMixer) {
			this.CurrentSoundDeviceType = ESoundDeviceType.ASIO;        // 作成後に設定する。（作成に失敗してると例外発出されてここは実行されない）
			this.CreateBassSound(fileName, hMixer, BassFlags.Decode);
		}
		public void CreateWASAPISound(string fileName, int hMixer, ESoundDeviceType deviceType) {
			this.CurrentSoundDeviceType = deviceType;       // 作成後に設定する。（作成に失敗してると例外発出されてここは実行されない）
			this.CreateBassSound(fileName, hMixer, BassFlags.Decode | BassFlags.Float);
		}

		#region [ DTXMania用の変換 ]

		public void DisposeSound(CSound cs) {
			cs.tDispose();
		}
		public void PlayStart() {
			tSetPositonToBegin();
			if (!b速度上げすぎ問題)
				tPlaySound(false);
		}
		public void PlayStart(bool looped) {
			if (IsBassSound) {
				if (looped) {
					Bass.ChannelFlags(this.hBassStream, BassFlags.Loop, BassFlags.Loop);
				} else {
					Bass.ChannelFlags(this.hBassStream, BassFlags.Default, BassFlags.Default);
				}
			}
			tSetPositonToBegin();
			tPlaySound(looped);
		}
		public void Stop() {
			tStopSound();
			tSetPositonToBegin();
		}
		public void Pause() {
			tStopSound(true);
			this.PauseCount++;
		}
		public void Resume(long t)  // ★★★★★★★★★★★★★★★★★★★★★★★★★★★★
		{
			Debug.WriteLine("t再生を再開する(long " + t + ")");
			tSetPositonToBegin(t);
			tPlaySound();
			this.PauseCount--;
		}
		public bool IsPaused {
			get {
				if (this.IsBassSound) {
					bool ret = (!BassMixExtensions.ChannelIsPlaying(this.hBassStream)) &
								(BassMix.ChannelGetPosition(this.hBassStream) > 0);
					return ret;
				} else {
					return (this.PauseCount > 0);
				}
			}
		}
		public bool IsPlaying {
			get {
				// 基本的にはBASS_ACTIVE_PLAYINGなら再生中だが、最後まで再生しきったchannelも
				// BASS_ACTIVE_PLAYINGのままになっているので、小細工が必要。
				bool ret = (BassMixExtensions.ChannelIsPlaying(this.hBassStream));
				if (BassMix.ChannelGetPosition(this.hBassStream) >= nBytes) {
					ret = false;
				}
				return ret;
			}
		}
		//public lint t時刻から位置を返す( long t )
		//{
		//    double num = ( n時刻 * this.db再生速度 ) * this.db周波数倍率;
		//    return (int) ( ( num * 0.01 ) * this.nSamplesPerSecond );
		//}
		#endregion


		public void tDispose() {
			tDispose(false);
		}

		public void tDispose(bool deleteInstance) {
			if (this.IsBassSound)       // stream数の削減用
			{
				tRemoveSoundFromMixer();
				//_cbStreamXA = null;
				SoundManager.nStreams--;
			}
			bool disposeWithManaged = true;
			this.Dispose(disposeWithManaged, deleteInstance);
			//Debug.WriteLine( "Disposed: " + _bインスタンス削除 + " : " + Path.GetFileName( this.strファイル名 ) );
		}
		public void tPlaySound() {
			tPlaySound(false);
		}
		private void tPlaySound(bool bループする) {
			if (this.IsBassSound)           // BASSサウンド時のループ処理は、t再生を開始する()側に実装。ここでは「bループする」は未使用。
			{
				//Debug.WriteLine( "再生中?: " +  System.IO.Path.GetFileName(this.strファイル名) + " status=" + BassMix.BASS_Mixer_ChannelIsActive( this.hBassStream ) + " current=" + BassMix.BASS_Mixer_ChannelGetPosition( this.hBassStream ) + " nBytes=" + nBytes );
				bool b = BassMixExtensions.ChannelPlay(this.hBassStream);
				if (!b) {
					//Debug.WriteLine( "再生しようとしたが、Mixerに登録されていなかった: " + Path.GetFileName( this.strファイル名 ) + ", stream#=" + this.hBassStream + ", ErrCode=" + Bass.BASS_ErrorGetCode() );

					bool bb = AddBassSoundFromMixer();
					if (!bb) {
						Debug.WriteLine("Mixerへの登録に失敗: " + Path.GetFileName(this.FileName) + ", ErrCode=" + Bass.LastError);
					} else {
						//Debug.WriteLine( "Mixerへの登録に成功: " + Path.GetFileName( this.strファイル名 ) + ": " + Bass.BASS_ErrorGetCode() );
					}
					//this.t再生位置を先頭に戻す();

					bool bbb = BassMixExtensions.ChannelPlay(this.hBassStream);
					if (!bbb) {
						Debug.WriteLine("更に再生に失敗: " + Path.GetFileName(this.FileName) + ", ErrCode=" + Bass.LastError);
					} else {
						//						Debug.WriteLine("再生成功(ミキサー追加後)                       : " + Path.GetFileName(this.strファイル名));
					}
				} else {
					//Debug.WriteLine( "再生成功: " + Path.GetFileName( this.strファイル名 ) + " (" + hBassStream + ")" );
				}
			}
		}
		public void tStopSoundAndRemoveSoundFromMixer() {
			tStopSound(false);
			if (IsBassSound) {
				tRemoveSoundFromMixer();
			}
		}
		public void tStopSound() {
			tStopSound(false);
		}
		public void tStopSound(bool pause) {
			if (this.IsBassSound) {
				//Debug.WriteLine( "停止: " + System.IO.Path.GetFileName( this.strファイル名 ) + " status=" + BassMix.BASS_Mixer_ChannelIsActive( this.hBassStream ) + " current=" + BassMix.BASS_Mixer_ChannelGetPosition( this.hBassStream ) + " nBytes=" + nBytes );
				BassMixExtensions.ChannelPause(this.hBassStream);
				if (!pause) {
					//		tBASSサウンドをミキサーから削除する();		// PAUSEと再生停止を区別できるようにすること!!
				}
			}
			this.PauseCount = 0;
		}

		public void tSetPositonToBegin() {
			if (this.IsBassSound) {
				BassMix.ChannelSetPosition(this.hBassStream, 0);
				//pos = 0;
			}
		}
		public void tSetPositonToBegin(long positionMs) {
			if (this.IsBassSound) {
				bool b = true;
				try {
					b = BassMix.ChannelSetPosition(this.hBassStream, Bass.ChannelSeconds2Bytes(this.hBassStream, positionMs * this.Frequency * this.PlaySpeed / 1000.0), PositionFlags.Bytes);
				} catch (Exception e) {
					Trace.TraceError(e.ToString());
					Trace.TraceInformation(Path.GetFileName(this.FileName) + ": Seek error: " + e.ToString() + ": " + positionMs + "ms");
				} finally {
					if (!b) {
						Errors be = Bass.LastError;
						Trace.TraceInformation(Path.GetFileName(this.FileName) + ": Seek error: " + be.ToString() + ": " + positionMs + "MS");
					}
				}
				//if ( this.n総演奏時間ms > 5000 )
				//{
				//    Trace.TraceInformation( Path.GetFileName( this.strファイル名 ) + ": Seeked to " + n位置ms + "ms = " + Bass.BASS_ChannelSeconds2Bytes( this.hBassStream, n位置ms * this.db周波数倍率 * this.db再生速度 / 1000.0 ) );
				//}
			}
		}
		/// <summary>
		/// デバッグ用
		/// </summary>
		/// <param name="positionByte"></param>
		/// <param name="positionMs"></param>
		public void tGetPlayPositon(out long positionByte, out double positionMs) {
			if (this.IsBassSound) {
				positionByte = BassMix.ChannelGetPosition(this.hBassStream);
				positionMs = Bass.ChannelBytes2Seconds(this.hBassStream, positionByte);
			} else {
				positionByte = 0;
				positionMs = 0.0;
			}
		}


		public static void tResetAllSound() {
			foreach (var sound in CSound.SoundInstances) {
				sound.tDispose(false);
			}
		}
		internal static void tReloadSound(ISoundDevice device) {
			if (CSound.SoundInstances.Count == 0)
				return;


			// サウンドを再生する際にインスタンスリストも更新されるので、配列にコピーを取っておき、リストはクリアする。

			var sounds = CSound.SoundInstances.ToArray();
			CSound.SoundInstances.Clear();


			// 配列に基づいて個々のサウンドを作成する。

			for (int i = 0; i < sounds.Length; i++) {
				switch (sounds[i].CurrnetCreateType) {
					#region [ ファイルから ]
					case CreateType.FromFile:
						string strファイル名 = sounds[i].FileName;
						sounds[i].Dispose(true, false);
						device.tCreateSound(strファイル名, sounds[i]);
						break;
						#endregion
				}
			}
		}

		#region [ Dispose-Finalizeパターン実装 ]
		//-----------------
		public void Dispose() {
			this.Dispose(true, true);
			GC.SuppressFinalize(this);
		}
		private void Dispose(bool deleteWithManaged, bool deleteInstance) {
			if (this.IsBassSound) {
				#region [ ASIO, WASAPI の解放 ]
				//-----------------
				if (_hTempoStream != 0) {
					BassMix.MixerRemoveChannel(this._hTempoStream);
					Bass.StreamFree(this._hTempoStream);
				}
				BassMix.MixerRemoveChannel(this._hBassStream);
				Bass.StreamFree(this._hBassStream);
				this.hBassStream = -1;
				this._hBassStream = -1;
				this._hTempoStream = 0;
				//-----------------
				#endregion
			}

			if (deleteWithManaged) {
				//int freeIndex = -1;

				//if ( CSound.listインスタンス != null )
				//{
				//    freeIndex = CSound.listインスタンス.IndexOf( this );
				//    if ( freeIndex == -1 )
				//    {
				//        Debug.WriteLine( "ERR: freeIndex==-1 : Count=" + CSound.listインスタンス.Count + ", filename=" + Path.GetFileName( this.strファイル名 ) );
				//    }
				//}

				this.CurrentSoundDeviceType = ESoundDeviceType.Unknown;

				if (deleteInstance) {
					//try
					//{
					//    CSound.listインスタンス.RemoveAt( freeIndex );
					//}
					//catch
					//{
					//    Debug.WriteLine( "FAILED to remove CSound.listインスタンス: Count=" + CSound.listインスタンス.Count + ", filename=" + Path.GetFileName( this.strファイル名 ) );
					//}
					bool b = CSound.SoundInstances.Remove(this);    // これだと、Clone()したサウンドのremoveに失敗する
					if (!b) {
						Debug.WriteLine("FAILED to remove CSound.listインスタンス: Count=" + CSound.SoundInstances.Count + ", filename=" + Path.GetFileName(this.FileName));
					}

				}
			}
		}
		~CSound() {
			this.Dispose(false, true);
		}
		//-----------------
		#endregion

		#region [ protected ]
		//-----------------
		protected enum CreateType { FromFile, Unknown }
		protected CreateType CurrnetCreateType = CreateType.Unknown;
		protected ESoundDeviceType CurrentSoundDeviceType = ESoundDeviceType.Unknown;
		public string FileName = null;
		protected GCHandle hGC;
		protected int _hTempoStream = 0;
		protected int _hBassStream = -1;                    // ASIO, WASAPI 用
		protected int hBassStream = 0;                      // #31076 2013.4.1 yyagi; プロパティとして実装すると動作が低速になったため、
															// tBASSサウンドを作成する_ストリーム生成後の共通処理()のタイミングと、
															// 再生速度を変更したタイミングでのみ、
															// hBassStreamを更新するようにした。
															//{
															//    get
															//    {
															//        if ( _hTempoStream != 0 && !this.bIs1倍速再生 )	// 再生速度がx1.000のときは、TempoStreamを用いないようにして高速化する
															//        {
															//            return _hTempoStream;
															//        }
															//        else
															//        {
															//            return _hBassStream;
															//        }
															//    }
															//    set
															//    {
															//        _hBassStream = value;
															//    }
															//}
		protected int hMixer = -1;  // 設計壊してゴメン Mixerに後で登録するときに使う
									//-----------------
		#endregion

		#region [ private ]
		//-----------------
		private bool IsBassSound {
			get {
				return (
					this.CurrentSoundDeviceType == ESoundDeviceType.Bass ||
					this.CurrentSoundDeviceType == ESoundDeviceType.ASIO ||
					this.CurrentSoundDeviceType == ESoundDeviceType.ExclusiveWASAPI ||
					this.CurrentSoundDeviceType == ESoundDeviceType.SharedWASAPI);
			}
		}
		private int _n位置 = 0;
		private int _n位置db;
		private Lufs _gain = DefaultGain;
		private Lufs? _truePeak = null;
		private int _automationLevel = DefaultAutomationLevel;
		private int _groupLevel = DefaultGroupLevel;
		private long nBytes = 0;
		private int PauseCount = 0;
		private int nオリジナルの周波数 = 0;
		private double _Frequency = 1.0;
		private double _PlaySpeed = 1.0;
		private bool IsNormalSpeed = true;

		public void CreateBassSound(string strファイル名, int hMixer, BassFlags flags) {
			this.CurrnetCreateType = CreateType.FromFile;
			this.FileName = strファイル名;


			// BASSファイルストリームを作成。

			this._hBassStream = Bass.CreateStream(strファイル名, 0, 0, flags);
			if (this._hBassStream == 0)
				throw new Exception(string.Format("サウンドストリームの生成に失敗しました。(BASS_StreamCreateFile)[{0}]", Bass.LastError.ToString()));

			nBytes = Bass.ChannelGetLength(this._hBassStream);

			tBASSサウンドを作成する_ストリーム生成後の共通処理(hMixer);
		}

		private void tBASSサウンドを作成する_ストリーム生成後の共通処理(int hMixer) {
			SoundManager.nStreams++;

			// 個々のストリームの出力をテンポ変更のストリームに入力する。テンポ変更ストリームの出力を、Mixerに出力する。

			//			if ( CSound管理.bIsTimeStretch )	// TimeStretchのON/OFFに関わりなく、テンポ変更のストリームを生成する。後からON/OFF切り替え可能とするため。
			{
				this._hTempoStream = BassFx.TempoCreate(this._hBassStream, BassFlags.Decode | BassFlags.FxFreeSource);
				if (this._hTempoStream == 0) {
					hGC.Free();
					throw new Exception(string.Format("サウンドストリームの生成に失敗しました。(BASS_FX_TempoCreate)[{0}]", Bass.LastError.ToString()));
				} else {
					Bass.ChannelSetAttribute(this._hTempoStream, ChannelAttribute.TempoUseQuickAlgorithm, 1f);  // 高速化(音の品質は少し落ちる)
				}
			}

			if (_hTempoStream != 0 && !this.IsNormalSpeed)  // 再生速度がx1.000のときは、TempoStreamを用いないようにして高速化する
			{
				this.hBassStream = _hTempoStream;
			} else {
				this.hBassStream = _hBassStream;
			}

			// #32248 再生終了時に発火するcallbackを登録する (演奏終了後に再生終了するチップを非同期的にミキサーから削除するため。)
			_cbEndofStream = new SyncProcedure(CallbackEndofStream);
			Bass.ChannelSetSync(hBassStream, SyncFlags.End | SyncFlags.Mixtime, 0, _cbEndofStream, IntPtr.Zero);

			// n総演奏時間の取得; DTXMania用に追加。
			double seconds = Bass.ChannelBytes2Seconds(this._hBassStream, nBytes);
			this.TotalPlayTime = (int)(seconds * 1000);
			//this.pos = 0;
			this.hMixer = hMixer;
			float freq = 0.0f;
			if (!Bass.ChannelGetAttribute(this._hBassStream, ChannelAttribute.Frequency, out freq)) {
				hGC.Free();
				throw new Exception(string.Format("サウンドストリームの周波数取得に失敗しました。(BASS_ChannelGetAttribute)[{0}]", Bass.LastError.ToString()));
			}
			this.nオリジナルの周波数 = (int)freq;

			// インスタンスリストに登録。

			CSound.SoundInstances.Add(this);
		}
		//-----------------

		//private int pos = 0;
		//private int CallbackPlayingXA( int handle, IntPtr buffer, int length, IntPtr user )
		//{
		//    int bytesread = ( pos + length > Convert.ToInt32( nBytes ) ) ? Convert.ToInt32( nBytes ) - pos : length;

		//    Marshal.Copy( byArrWAVファイルイメージ, pos, buffer, bytesread );
		//    pos += bytesread;
		//    if ( pos >= nBytes )
		//    {
		//        // set indicator flag
		//        bytesread |= (int) BASSStreamProc.BASS_STREAMPROC_END;
		//    }
		//    return bytesread;
		//}
		/// <summary>
		/// ストリームの終端まで再生したときに呼び出されるコールバック
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="channel"></param>
		/// <param name="data"></param>
		/// <param name="user"></param>
		private void CallbackEndofStream(int handle, int channel, int data, IntPtr user)    // #32248 2013.10.14 yyagi
		{
			// Trace.TraceInformation( "Callback!(remove): " + Path.GetFileName( this.strファイル名 ) );
			if (b演奏終了後も再生が続くチップである)         // 演奏終了後に再生終了するチップ音のミキサー削除は、再生終了のコールバックに引っ掛けて、自前で行う。
			{                                                   // そうでないものは、ミキサー削除予定時刻に削除する。
				RemoveBassSoundFromMixer(channel);
			}
		}

		// mixerからの削除

		public bool tRemoveSoundFromMixer() {
			return RemoveBassSoundFromMixer(this.hBassStream);
		}
		public bool RemoveBassSoundFromMixer(int channel) {
			bool b = BassMix.MixerRemoveChannel(channel);
			if (b) {
				Interlocked.Decrement(ref SoundManager.nMixing);
				//				Debug.WriteLine( "Removed: " + Path.GetFileName( this.strファイル名 ) + " (" + channel + ")" + " MixedStreams=" + CSound管理.nMixing );
			}
			return b;
		}


		// mixer への追加

		public bool AddBassSoundFromMixer() {
			if (BassMix.ChannelGetMixer(hBassStream) == 0) {
				BassFlags bf = BassFlags.SpeakerFront | BassFlags.MixerChanNoRampin | BassFlags.MixerChanPause;
				Interlocked.Increment(ref SoundManager.nMixing);

				// preloadされることを期待して、敢えてflagからはBASS_MIXER_PAUSEを外してAddChannelした上で、すぐにPAUSEする
				// -> ChannelUpdateでprebufferできることが分かったため、BASS_MIXER_PAUSEを使用することにした

				bool b1 = BassMix.MixerAddChannel(this.hMixer, this.hBassStream, bf);
				//bool b2 = BassMix.BASS_Mixer_ChannelPause( this.hBassStream );
				tSetPositonToBegin();   // StreamAddChannelの後で再生位置を戻さないとダメ。逆だと再生位置が変わらない。
										//Trace.TraceInformation( "Add Mixer: " + Path.GetFileName( this.strファイル名 ) + " (" + hBassStream + ")" + " MixedStreams=" + CSound管理.nMixing );
				Bass.ChannelUpdate(this.hBassStream, 0);    // pre-buffer
				return b1;  // &b2;
			}
			return true;
		}

		#endregion
	}
}

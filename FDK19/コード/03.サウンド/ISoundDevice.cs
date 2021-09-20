using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Un4seen.Bass;
using Un4seen.BassAsio;
using Un4seen.BassWasapi;
using Un4seen.Bass.AddOn.Mix;

namespace FDK
{
	internal interface ISoundDevice : IDisposable
	{
		ESoundDeviceType e出力デバイス { get; }
		int nMasterVolume { get; set; }
		long n実出力遅延ms { get; }
		long n実バッファサイズms { get; }
		long n経過時間ms { get; }
		long n経過時間を更新したシステム時刻ms { get; }
		CTimer tmシステムタイマ { get; }

		CSound tサウンドを作成する( string strファイル名, ESoundGroup soundGroup );
		void tサウンドを作成する( string strファイル名, CSound sound );
		void tサウンドを作成する( byte[] byArrWAVファイルイメージ, CSound sound );
	}
}

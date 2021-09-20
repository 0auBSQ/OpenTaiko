using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using DirectShowLib;
using SharpDX.Multimedia;

namespace FDK
{
	public class CDStoWAVFileImage
	{
		/// <summary>
		/// <para>指定された動画ファイルから音声のみをエンコードし、WAVファイルイメージを作成して返す。</para>
		/// </summary>
		public static void t変換( string fileName, out byte[] wavFileImage )
		{
			int hr = 0;

			IGraphBuilder graphBuilder = null;

			try
			{
				graphBuilder = (IGraphBuilder) new FilterGraph();

				#region [ オーディオ用サンプルグラバの作成と追加。]
				//-----------------
				ISampleGrabber sampleGrabber = null;
				try
				{
					sampleGrabber = (ISampleGrabber) new SampleGrabber();


					// サンプルグラバのメディアタイプの設定。

					var mediaType = new AMMediaType() {
						majorType = MediaType.Audio,
						subType = MediaSubType.PCM,
						formatType = FormatType.WaveEx,
					};
					try
					{
						hr = sampleGrabber.SetMediaType( mediaType );
						DsError.ThrowExceptionForHR( hr );
					}
					finally
					{
						if( mediaType != null )
							DsUtils.FreeAMMediaType( mediaType );
					}


					// サンプルグラバのバッファリングを有効にする。

					hr = sampleGrabber.SetBufferSamples( true );
					DsError.ThrowExceptionForHR( hr );


					// サンプルグラバにコールバックを追加する。

					sampleGrabberProc = new CSampleGrabberCallBack();
					hr = sampleGrabber.SetCallback( sampleGrabberProc, 1 );	// 1:コールバックの BufferCB() メソッドの方を呼び出す。


					// サンプルグラバをグラフに追加する。

					hr = graphBuilder.AddFilter( (IBaseFilter) sampleGrabber, "SampleGrabber for Audio/PCM" );
					DsError.ThrowExceptionForHR( hr );
				}
				finally
				{
					C共通.tCOMオブジェクトを解放する( ref sampleGrabber );
				}
				//-----------------
				#endregion

				var e = new DirectShowLib.DsROTEntry( graphBuilder );

				// fileName からグラフを自動生成。

				hr = graphBuilder.RenderFile( fileName, null );	// IMediaControl.RenderFile() は推奨されない
				DsError.ThrowExceptionForHR( hr );


				// ビデオレンダラを除去。

				CDirectShow.tビデオレンダラをグラフから除去する( graphBuilder );		// オーディオレンダラをNullに変えるより前に実行すること。（CDirectShow.tオーディオレンダラをNullレンダラに変えてフォーマットを取得する() の中で一度再生するので、そのときにActiveウィンドウが表示されてしまうため。）
	

				// オーディオレンダラを NullRenderer に置換。

				WaveFormat wfx;
				byte[] wfx拡張領域;
				CDirectShow.tオーディオレンダラをNullレンダラに変えてフォーマットを取得する( graphBuilder, out wfx, out wfx拡張領域 );


				// 基準クロックを NULL（最高速）に設定する。

				IMediaFilter mediaFilter = graphBuilder as IMediaFilter;
				mediaFilter.SetSyncSource( null );
				mediaFilter = null;


				// メモリストリームにデコードデータを出力する。

				sampleGrabberProc.MemoryStream = new MemoryStream();	// CDirectShow.tオーディオレンダラをNullレンダラに変えてフォーマットを取得する() で一度再生しているので、ストリームをクリアする。
				var ms = sampleGrabberProc.MemoryStream;
				var bw = new BinaryWriter( ms );
				bw.Write( new byte[] { 0x52, 0x49, 0x46, 0x46 } );		// 'RIFF'
				bw.Write( (UInt32) 0 );									// ファイルサイズ - 8 [byte]；今は不明なので後で上書きする。
				bw.Write( new byte[] { 0x57, 0x41, 0x56, 0x45 } );		// 'WAVE'
				bw.Write( new byte[] { 0x66, 0x6D, 0x74, 0x20 } );		// 'fmt '
				bw.Write( (UInt32) ( 16 + ( ( wfx拡張領域.Length > 0 ) ? ( 2/*sizeof(WAVEFORMATEX.cbSize)*/ + wfx拡張領域.Length ) : 0 ) ) );	// fmtチャンクのサイズ[byte]
				bw.Write( (UInt16) wfx.Encoding);						// フォーマットID（リニアPCMなら1）
				bw.Write( (UInt16) wfx.Channels );						// チャンネル数
				bw.Write( (UInt32) wfx.SampleRate);				// サンプリングレート
				bw.Write( (UInt32) wfx.AverageBytesPerSecond );			// データ速度
				bw.Write( (UInt16) wfx.BlockAlign);				// ブロックサイズ
				bw.Write( (UInt16) wfx.BitsPerSample );					// サンプルあたりのビット数
				if( wfx拡張領域.Length > 0 )
				{
					bw.Write( (UInt16) wfx拡張領域.Length );			// 拡張領域のサイズ[byte]
					bw.Write( wfx拡張領域 );							// 拡張データ
				}
				bw.Write( new byte[] { 0x64, 0x61, 0x74, 0x61 } );		// 'data'
				int nDATAチャンクサイズ位置 = (int) ms.Position;
				bw.Write( (UInt32) 0 );									// dataチャンクのサイズ[byte]；今は不明なので後で上書きする。

				#region [ 再生を開始し、終了を待つ。- 再生中、sampleGrabberProc.MemoryStream に PCM データが蓄積されていく。]
				//-----------------
				IMediaControl mediaControl = graphBuilder as IMediaControl;
				mediaControl.Run();						// 再生開始

				IMediaEvent mediaEvent = graphBuilder as IMediaEvent;
				EventCode eventCode;
				hr = mediaEvent.WaitForCompletion( -1, out eventCode );
				DsError.ThrowExceptionForHR( hr );
				if( eventCode != EventCode.Complete )
					throw new Exception( "再生待ちに失敗しました。" );

				mediaControl.Stop();
				mediaEvent = null;
				mediaControl = null;
				//-----------------
				#endregion

				bw.Seek( 4, SeekOrigin.Begin );
				bw.Write( (UInt32) ms.Length - 8 );					// ファイルサイズ - 8 [byte]

				bw.Seek( nDATAチャンクサイズ位置, SeekOrigin.Begin );
				bw.Write( (UInt32) ms.Length - ( nDATAチャンクサイズ位置 + 4 ) );	// dataチャンクサイズ [byte]


				// 出力その２を作成。

				wavFileImage = ms.ToArray();


				// 終了処理。

				bw.Close();
				sampleGrabberProc.Dispose();	// ms.Close()
			}
			finally
			{
				C共通.tCOMオブジェクトを解放する( ref graphBuilder );
			}
		}

		#region [ private ]
		//-----------------
		private class CSampleGrabberCallBack : ISampleGrabberCB, IDisposable
		{
			public MemoryStream MemoryStream = new MemoryStream();

			public int BufferCB( double SampleTime, IntPtr pBuffer, int BufferLen )
			{
				var bytes = new byte[ BufferLen ];
				Marshal.Copy( pBuffer, bytes, 0, BufferLen );		// unmanage → manage
				this.MemoryStream.Write( bytes, 0, BufferLen );		// byte[] → Stream
				return CWin32.S_OK;
			}
			public int SampleCB( double SampleTime, IMediaSample pSample )
			{
				throw new NotImplementedException( "実装されていません。" );
			}

			public void Dispose()
			{
				this.MemoryStream.Close();
			}
		}
		private static CSampleGrabberCallBack sampleGrabberProc = null;
		//-----------------
		#endregion
	}
}

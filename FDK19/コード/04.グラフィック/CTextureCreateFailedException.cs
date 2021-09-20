using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace FDK
{
	/// <summary>
	/// テクスチャの作成に失敗しました。
	/// </summary>
	public class CTextureCreateFailedException : Exception
	{
		public CTextureCreateFailedException()
		{
		}
		public CTextureCreateFailedException( string message )
			: base( message )
		{
		}
		public CTextureCreateFailedException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
		}
		public CTextureCreateFailedException( string message, Exception innerException )
			: base( message, innerException )
		{
		}
	}
}

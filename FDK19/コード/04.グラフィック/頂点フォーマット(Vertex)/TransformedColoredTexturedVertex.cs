using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;
using SlimDX;
using SlimDX.Direct3D9;

namespace FDK
{
	[StructLayout( LayoutKind.Sequential )]
	public struct TransformedColoredTexturedVertex : IEquatable<TransformedColoredTexturedVertex>
	{
		public Vector4	Position;
		public int		Color;
		public Vector2	TextureCoordinates;

		public static int SizeInBytes
		{
			get
			{
				return Marshal.SizeOf( typeof( TransformedColoredTexturedVertex ) );
			}
		}
		public static VertexFormat Format
		{
			get
			{
				return ( VertexFormat.Texture1 | VertexFormat.Diffuse | VertexFormat.PositionRhw );
			}
		}
		public TransformedColoredTexturedVertex( Vector4 position, int color, Vector2 textureCoordinates )
		{
			this = new TransformedColoredTexturedVertex();
			this.Position = position;
			this.Color = color;
			this.TextureCoordinates = textureCoordinates;
		}

		public static bool operator ==( TransformedColoredTexturedVertex left, TransformedColoredTexturedVertex right )
		{
			return left.Equals( right );
		}
		public static bool operator !=( TransformedColoredTexturedVertex left, TransformedColoredTexturedVertex right )
		{
			return !( left == right );
		}
		public override int GetHashCode()
		{
			return ( ( this.Position.GetHashCode() + this.Color.GetHashCode() ) + this.TextureCoordinates.GetHashCode() );
		}
		public override bool Equals( object obj )
		{
			if( obj == null )
			{
				return false;
			}
			if( base.GetType() != obj.GetType() )
			{
				return false;
			}
			return this.Equals( (TransformedColoredTexturedVertex) obj );
		}
		public bool Equals( TransformedColoredTexturedVertex other )
		{
			return ( ( ( this.Position == other.Position ) && ( this.Color == other.Color ) ) && ( this.TextureCoordinates == other.TextureCoordinates ) );
		}
		public override string ToString()
		{
			return string.Format( CultureInfo.CurrentCulture, "{0} ({1}, {2})", new object[] { this.Position.ToString(), System.Drawing.Color.FromArgb( this.Color ).ToString(), this.TextureCoordinates.ToString() } );
		}
	}
}

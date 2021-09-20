/*
* Copyright (c) 2007-2009 SlimDX Group
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using SlimDX;
using SlimDX.Direct3D9;

namespace SampleFramework
{
    /// <summary>
    /// Represents a single transformed, colored, and textured vertex.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TransformedColoredTexturedVertex : IEquatable<TransformedColoredTexturedVertex>
    {
		private Vector4 m_Position;
        /// <summary>
        /// Gets or sets the transformed position of the vertex.
        /// </summary>
        /// <value>The transformed position of the vertex.</value>
        [VertexElement(DeclarationType.Float4, DeclarationUsage.PositionTransformed)]
        public Vector4 Position
        {
			get { return m_Position; }
			set { m_Position = value; }
        }

		private int m_Color;
        /// <summary>
        /// Gets or sets the color of the vertex.
        /// </summary>
        /// <value>The color of the vertex.</value>
        [VertexElement(DeclarationType.Color, DeclarationUsage.Color)]
        public int Color
        {
			get { return m_Color; }
			set { m_Color = value; }
        }

		private Vector2 m_TextureCoordinates;
        /// <summary>
        /// Gets or sets the texture coordinates.
        /// </summary>
        /// <value>The texture coordinates.</value>
        [VertexElement(DeclarationType.Float2, DeclarationUsage.TextureCoordinate)]
        public Vector2 TextureCoordinates
        {
			get { return m_TextureCoordinates; }
			set { m_TextureCoordinates = value; }
        }

        /// <summary>
        /// Gets the size in bytes.
        /// </summary>
        /// <value>The size in bytes.</value>
        public static int SizeInBytes
        {
            get { return Marshal.SizeOf(typeof(TransformedColoredTexturedVertex)); }
        }

        /// <summary>
        /// Gets the format.
        /// </summary>
        /// <value>The format.</value>
        public static VertexFormat Format
        {
            get { return VertexFormat.PositionRhw | VertexFormat.Diffuse | VertexFormat.Texture1; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformedColoredTexturedVertex"/> struct.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="color">The color.</param>
        /// <param name="textureCoordinates">The texture coordinates.</param>
        public TransformedColoredTexturedVertex(Vector4 position, int color, Vector2 textureCoordinates)
            : this()
        {
            Position = position;
            Color = color;
            TextureCoordinates = textureCoordinates;
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left side of the operator.</param>
        /// <param name="right">The right side of the operator.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(TransformedColoredTexturedVertex left, TransformedColoredTexturedVertex right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left side of the operator.</param>
        /// <param name="right">The right side of the operator.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(TransformedColoredTexturedVertex left, TransformedColoredTexturedVertex right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        public override int GetHashCode()
        {
            return Position.GetHashCode() + Color.GetHashCode() + TextureCoordinates.GetHashCode();
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">Another object to compare to.</param>
        /// <returns>
        /// true if <paramref name="obj"/> and this instance are the same type and represent the same value; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (GetType() != obj.GetType())
                return false;

            return Equals((TransformedColoredTexturedVertex)obj);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(TransformedColoredTexturedVertex other)
        {
            return (Position == other.Position && Color == other.Color && TextureCoordinates == other.TextureCoordinates);
        }

        /// <summary>
        /// Returns a string representation of the current object.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> representing the vertex.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} ({1}, {2})", Position.ToString(), System.Drawing.Color.FromArgb(Color).ToString(), TextureCoordinates.ToString());
        }
    }
}

﻿/*
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
using SharpDX;
using SharpDX.Direct3D9;

namespace SampleFramework
{
    /// <summary>
    /// Represents a single transformed and colored vertex.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TransformedColoredVertex : IEquatable<TransformedColoredVertex>
    {
        /// <summary>
        /// Gets or sets the transformed position of the vertex.
        /// </summary>
        /// <value>The transformed position of the vertex.</value>
        [VertexElement(DeclarationType.Float4, DeclarationUsage.PositionTransformed)]
        public Vector4 Position
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the color of the vertex.
        /// </summary>
        /// <value>The color of the vertex.</value>
        [VertexElement(DeclarationType.Color, DeclarationUsage.Color)]
        public int Color
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the size in bytes.
        /// </summary>
        /// <value>The size in bytes.</value>
        public static int SizeInBytes
        {
            get { return Marshal.SizeOf(typeof(TransformedColoredVertex)); }
        }

        /// <summary>
        /// Gets the format.
        /// </summary>
        /// <value>The format.</value>
        public static VertexFormat Format
        {
            get { return VertexFormat.PositionRhw | VertexFormat.Diffuse; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformedColoredVertex"/> struct.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="color">The color.</param>
        public TransformedColoredVertex(Vector4 position, int color)
            : this()
        {
            Position = position;
            Color = color;
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left side of the operator.</param>
        /// <param name="right">The right side of the operator.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(TransformedColoredVertex left, TransformedColoredVertex right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left side of the operator.</param>
        /// <param name="right">The right side of the operator.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(TransformedColoredVertex left, TransformedColoredVertex right)
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
            return Position.GetHashCode() + Color.GetHashCode();
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

            return Equals((TransformedColoredVertex)obj);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(TransformedColoredVertex other)
        {
            return (Position == other.Position && Color == other.Color);
        }

        /// <summary>
        /// Returns a string representation of the current object.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> representing the vertex.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", Position.ToString(), System.Drawing.Color.FromArgb(Color).ToString());
        }
    }
}

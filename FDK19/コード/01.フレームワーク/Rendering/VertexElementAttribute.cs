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
using SlimDX.Direct3D9;

namespace SampleFramework
{
    /// <summary>
    /// Indicates that the target code element is part of a vertex declaration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class VertexElementAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the stream index.
        /// </summary>
        /// <value>The stream index.</value>
        public int Stream
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the tessellation method.
        /// </summary>
        /// <value>The tessellation method.</value>
        public DeclarationMethod Method
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the element usage.
        /// </summary>
        /// <value>The element usage.</value>
        public DeclarationUsage Usage
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the type of the data.
        /// </summary>
        /// <value>The type of the data.</value>
        public DeclarationType Type
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the offset.
        /// </summary>
        /// <value>The offset.</value>
        internal int Offset
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexElementAttribute"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="usage">The vertex element usage.</param>
        public VertexElementAttribute(DeclarationType type, DeclarationUsage usage)
        {
            Type = type;
            Usage = usage;
        }
    }
}

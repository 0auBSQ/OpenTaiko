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
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using SlimDX;
using SlimDX.Direct3D9;

namespace SampleFramework
{
    /// <summary>
    /// Manages aspects of the graphics device unique to Direct3D9.
    /// </summary>
    public class Direct3D9Manager
    {
        GraphicsDeviceManager manager;

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
#if TEST_Direct3D9Ex
		public DeviceEx Device							//yyagi
#else
		public DeviceCache Device
#endif
		{
            get;
            internal set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Direct3D9Manager"/> class.
        /// </summary>
        /// <param name="manager">The parent manager.</param>
        internal Direct3D9Manager(GraphicsDeviceManager manager)
        {
            this.manager = manager;
        }

        /// <summary>
        /// Creates a vertex declaration using the specified vertex type.
        /// </summary>
        /// <param name="vertexType">Type of the vertex.</param>
        /// <returns>The vertex declaration for the specified vertex type.</returns>
        [EnvironmentPermission(SecurityAction.LinkDemand)]
        public VertexDeclaration CreateVertexDeclaration(Type vertexType)
        {
            // ensure that we have a value type
            if (!vertexType.IsValueType)
                throw new InvalidOperationException("Vertex types must be value types.");

            // grab the list of elements in the vertex
            List<VertexElementAttribute> objectAttributes = new List<VertexElementAttribute>();
            FieldInfo[] fields = vertexType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                // check for the custom attribute
                VertexElementAttribute[] attributes = (VertexElementAttribute[])field.GetCustomAttributes(typeof(VertexElementAttribute), false);
                if (field.Name.Contains("<") && field.Name.Contains(">"))
                {
                    // look up the property matching this field to see if it has the attribute
                    int index1 = field.Name.IndexOf('<');
                    int index2 = field.Name.IndexOf('>');

                    // parse out the name
                    string propertyName = field.Name.Substring(index1 + 1, index2 - index1 - 1);
                    PropertyInfo property = vertexType.GetProperty(propertyName, field.FieldType);
                    if (property != null)
                        attributes = (VertexElementAttribute[])property.GetCustomAttributes(typeof(VertexElementAttribute), false);
                }
                if (attributes.Length == 1)
                {
                    // add the attribute to the list
                    attributes[0].Offset = Marshal.OffsetOf(vertexType, field.Name).ToInt32();
                    objectAttributes.Add(attributes[0]);
                }
            }

            // make sure we have at least one element
            if (objectAttributes.Count < 1)
                throw new InvalidOperationException("The vertex type must have at least one field or property marked with the VertexElement attribute.");

            // loop through the attributes and start building vertex elements
            List<VertexElement> elements = new List<VertexElement>();
            Dictionary<DeclarationUsage, int> usages = new Dictionary<DeclarationUsage, int>();
            foreach (VertexElementAttribute attribute in objectAttributes)
            {
                // check the current usage index
                if (!usages.ContainsKey(attribute.Usage))
                    usages.Add(attribute.Usage, 0);

                // advance the current usage count
                int index = usages[attribute.Usage];
                usages[attribute.Usage]++;

                // create the element
                elements.Add(new VertexElement((short)attribute.Stream, (short)attribute.Offset, attribute.Type,
                    attribute.Method, attribute.Usage, (byte)index));
            }

            elements.Add(VertexElement.VertexDeclarationEnd);
            return new VertexDeclaration(Device.UnderlyingDevice, elements.ToArray());
        }

        /// <summary>
        /// Creates a render target surface that is compatible with the current device settings.
        /// </summary>
        /// <param name="width">The width of the surface.</param>
        /// <param name="height">The height of the surface.</param>
        /// <returns>The newly created render target surface.</returns>
        public Texture CreateRenderTarget(int width, int height)
        {
            return new Texture(Device.UnderlyingDevice, width, height, 1, Usage.RenderTarget, manager.CurrentSettings.BackBufferFormat, Pool.Default);
        }

        /// <summary>
        /// Creates a resolve target for capturing the back buffer.
        /// </summary>
        /// <returns>The newly created resolve target.</returns>
        public Texture CreateResolveTarget()
        {
            return new Texture(Device.UnderlyingDevice, manager.ScreenWidth, manager.ScreenHeight, 1, Usage.RenderTarget, manager.CurrentSettings.BackBufferFormat, Pool.Default);
        }

        /// <summary>
        /// Resolves the current back buffer into a texture.
        /// </summary>
        /// <param name="target">The target texture.</param>
        /// <exception cref="InvalidOperationException">Thrown when the resolve process fails.</exception>
        public void ResolveBackBuffer(Texture target)
        {
            ResolveBackBuffer(target, 0);
        }

        /// <summary>
        /// Resolves the current back buffer into a texture.
        /// </summary>
        /// <param name="target">The target texture.</param>
        /// <param name="backBufferIndex">The index of the back buffer.</param>
        /// <exception cref="InvalidOperationException">Thrown when the resolve process fails.</exception>
        public void ResolveBackBuffer(Texture target, int backBufferIndex)
        {
            // disable exceptions for this method
            bool storedThrow = Configuration.ThrowOnError;
            Configuration.ThrowOnError = false;
            Surface destination = null;

            try
            {
                // grab the current back buffer
                Surface backBuffer = Device.GetBackBuffer(0, backBufferIndex);
                if (backBuffer == null || Result.Last.IsFailure)
                    throw new InvalidOperationException("Could not obtain back buffer surface.");

                // grab the destination surface
                destination = target.GetSurfaceLevel(0);
                if (destination == null || Result.Last.IsFailure)
                    throw new InvalidOperationException("Could not obtain resolve target surface.");

                // first try to copy using linear filtering
                if (Device.StretchRectangle(backBuffer, destination, TextureFilter.Linear).IsFailure)
                {
                    // that failed, so try with no filtering
                    if (Device.StretchRectangle(backBuffer, destination, TextureFilter.None).IsFailure)
                    {
                        // that failed as well, so the last thing we can try is a load surface call
                        if (Surface.FromSurface(destination, backBuffer, Filter.Default, 0).IsFailure)
                            throw new InvalidOperationException("Could not copy surfaces.");
                    }
                }
            }
            finally
            {
                if (destination != null)
                    destination.Dispose();
                Configuration.ThrowOnError = storedThrow;
            }
        }

        /// <summary>
        /// Resets the render target.
        /// </summary>
        public void ResetRenderTarget()
        {
            Surface backBuffer = Device.GetBackBuffer(0, 0);

            try
            {
                Device.SetRenderTarget(0, backBuffer);
            }
            finally
            {
                backBuffer.Dispose();
            }
        }
    }
}

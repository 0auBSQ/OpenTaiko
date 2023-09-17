
#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;

uniform mat4 mvp;
uniform vec4 textureRect;
uniform mat4 camera;

out vec2 texcoord;

void main()
{
    vec4 position = vec4(aPosition, 1.0);
    position = mvp * position * camera;
                    
    gl_Position = position;
    texcoord = textureRect.xy;
    texcoord.xy += aTexCoord.xy * textureRect.zw;
}
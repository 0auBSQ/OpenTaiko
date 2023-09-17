#version 330 core

in vec2 texcoord;
out vec4 out_color;
uniform sampler2D texture1;
uniform vec4 color;

void main()
{
    vec4 totalcolor = texture(texture1, texcoord);
    totalcolor *= color;
    out_color = totalcolor;
}
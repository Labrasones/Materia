#version 330 core
out vec4 FragColor;
in vec2 UV;

uniform sampler2D MainTex;
uniform float maxDistance = 0.2;

void main() {
    //note to self: the below can actually
    //be converted to work as an euclidean distance transform
    //simply change the >= to < 0.5
    //and remove 1.0 - on final dist calc
    vec2 offset = 1.0 / textureSize(MainTex, 0);
    float r = texture(MainTex, UV).r;

    if(r >= 0.5) {
        FragColor = vec4(vec3(r), 1);
        return;
    }

    float dist = 1;
    for(float y = -0.5; y <= 0.5; y+=offset.y) {
        for(float x = -0.5; x <= 0.5; x+=offset.x) {
            vec2 pos = UV + vec2(x,y);
            if(texture(MainTex, pos).r >= 0.5) {
                dist = min(dist, distance(pos,UV));
            }
        }
    }

    if(dist > maxDistance * maxDistance) {
        dist = 0;
    }
    else {
        dist = 1.0 - dist / (maxDistance * maxDistance);
    }

    FragColor = vec4(vec3(dist), 1);
}
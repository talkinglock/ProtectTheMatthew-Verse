shader_type spatial;

const vec3 baseColor = vec3(1,0.02,0.02);
const vec3 lowestColor = vec3(0.7,0.05,0.05);
uniform vec3 centerOffset = vec3(0.0,2.0,0.0);
const float pulsationAmplitude = 2.0f;

float hash(vec2 p) {
  return fract(sin(dot(p * 17.17, vec2(14.91, 67.31))) * 4791.9511);
}

float noise(vec2 x) {
  vec2 p = floor(x);
  vec2 f = fract(x);
  f = f * f * (3.0 - 2.0 * f);
  vec2 a = vec2(1.0, 0.0);
  return mix(mix(hash(p + a.yy), hash(p + a.xy), f.x),
         mix(hash(p + a.yx), hash(p + a.xx), f.x), f.y);
}

void vertex() {
	// Called for every vertex the material is visible on.
	vec3 relativeToCenter = centerOffset - VERTEX;
	vec3 directionToCenter = -relativeToCenter;
	float noiseValue = noise(VERTEX.xy * TIME * 4.0) * 0.2f;
	
	float minValue = -0.5;
	float maxValue = -0.2;
	
	float normalized = (sin(TIME * 10.0) + 1.0) / 2.0;
	
	float converted = minValue + normalized * (maxValue - minValue);
	VERTEX = noiseValue + VERTEX + 2.0f * converted * normalize(directionToCenter);
}

void fragment() {
	// Called for every pixel the material is visible on.
	//float colorNoise = noise(UV * 50.0f);
	float wave = (sin(TIME * 10.0));
	ALBEDO = baseColor * mix(lowestColor, baseColor, wave);
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}

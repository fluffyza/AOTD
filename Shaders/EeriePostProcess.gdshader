shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_disabled;

uniform sampler2D albedo_texture : source_color;
uniform bool use_texture = false;
uniform bool use_vertex_color = false;
uniform vec4 base_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

uniform float shade_tweak : hint_range(0.0, 1.0) = 0.45;
uniform float light_tweak : hint_range(0.0, 2.0) = 1.0;
uniform float sharpness : hint_range(0.0, 4.0) = 1.8;
uniform float scale : hint_range(1.0, 8.0) = 2.0;
uniform float ambient_floor : hint_range(0.0, 0.5) = 0.08;

const mat4 dither = mat4(
	vec4(0.0, 0.5333333333, 0.1333333333, 0.6666666667),
	vec4(0.8, 0.2666666667, 0.9333333333, 0.4),
	vec4(0.2, 0.7333333333, 0.06666666667, 0.6),
	vec4(1.0, 0.4666666667, 0.8666666667, 0.3333333333)
);

float sample_dither(vec2 coord, float alpha, float shade, float lit) {
	int x = int(mod(coord.x, 4.0));
	int y = int(mod(coord.y, 4.0));

	if (dither[y][x] >= alpha)
		return shade;

	return lit;
}

void fragment() {
	vec4 tex = vec4(1.0);

	if (use_texture) {
		tex = texture(albedo_texture, UV);
	}

	vec3 base = tex.rgb * base_color.rgb;

	if (use_vertex_color) {
		base *= COLOR.rgb;
	}

	ALBEDO = base;
	ALPHA = tex.a * base_color.a;

	EMISSION = ALBEDO * ambient_floor;
}

void light() {
	float ndl = max(dot(normalize(NORMAL), normalize(LIGHT)), 0.0);
	float alpha = clamp(ndl * sharpness * ATTENUATION, 0.0, 1.0);

	float a = sample_dither(
		FRAGCOORD.xy / scale,
		alpha,
		shade_tweak,
		light_tweak
	);

	DIFFUSE_LIGHT += ALBEDO * a * LIGHT_COLOR;
}
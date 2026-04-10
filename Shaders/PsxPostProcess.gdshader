shader_type spatial;
render_mode unshaded, fog_disabled, cull_disabled, depth_draw_never, depth_test_disabled;

uniform sampler2D depth_texture : hint_depth_texture, repeat_disable, filter_nearest;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

uniform bool enable_fog = true;
uniform vec3 fog_color : source_color = vec3(0.72, 0.82, 0.82);
uniform vec3 noise_color : source_color = vec3(0.12, 0.14, 0.18);
uniform float fog_distance : hint_range(1.0, 6000.0) = 2.0;
uniform float fog_fade_range : hint_range(1.0, 6000.0) = 10.0;

uniform bool enable_noise = true;
uniform float noise_time_fac : hint_range(0.1, 10.0) = 4.0;

uniform bool enable_color_limitation = true;
uniform int color_levels : hint_range(2, 256, 1) = 18;

uniform bool enable_dithering = true;
uniform float dither_strength : hint_range(0.0, 1.0) = 1;

float hash_old_12(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453 + (sin(TIME) / noise_time_fac));
}

vec3 color_mix(vec3 a, vec3 b, float f) {
	f = clamp(abs(f), 0.0, 1.0);
	float invf = 1.0 - f;
	return (a * f) + (b * invf);
}

vec3 quantize_color(vec3 color, int levels) {
	float quantizer = float(levels - 1);
	return floor(color * quantizer + 0.5) / quantizer;
}

float dither4x4(vec2 position, float brightness_unused) {
	int x = int(mod(position.x, 4.0));
	int y = int(mod(position.y, 4.0));
	int index = x + y * 4;

	float dithering[16] = float[](
		0.0,    0.5,    0.125,  0.625,
		0.75,   0.25,   0.875,  0.375,
		0.1875, 0.6875, 0.0625, 0.5625,
		0.9375, 0.4375, 1.0,    0.8125
	);

	return dithering[index];
}

void vertex() {
	POSITION = vec4(VERTEX.xy, 1.0, 1.0);
}

void fragment() {
	vec2 uv = SCREEN_UV;

	vec3 screen_color = texture(screen_texture, uv).rgb;

	vec2 screen_coords = uv * 2.0 - 1.0;
	float depth = texture(depth_texture, uv).x;
	vec3 ndc = vec3(screen_coords, depth);

	vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	view.xyz /= view.w;

	float linear_depth = -view.z;

	float depth_mask_inv = clamp(
		(linear_depth - (fog_distance - fog_fade_range)) / fog_fade_range,
		0.0,
		1.0
	);
	
	//float fog_factor = linear_depth / fog_distance;
	//fog_factor = clamp(fog_factor, 0.0, 1.0);
//
	//// Optional curve (makes fog come in sooner)
	//fog_factor = pow(fog_factor, 1.5);
//
	//float depth_mask_inv = fog_factor;
	//
	
	

	vec3 final_color = screen_color;

	if (enable_noise) {
		vec3 two_color_noise = color_mix(fog_color, noise_color, hash_old_12(screen_coords));
		final_color = color_mix(two_color_noise, final_color, depth_mask_inv);
	}

	if (enable_fog) {
		final_color = color_mix(fog_color, final_color, depth_mask_inv);
	}

	if (enable_color_limitation) {
		final_color = quantize_color(final_color, color_levels);
	}

	if (enable_dithering) {
		float brightness = dot(final_color, vec3(0.299, 0.587, 0.114));

		// Quantize brightness into visible bands first
		float band_steps = 6.0;
		float banded = floor(brightness * band_steps) / band_steps;

		// Bayer threshold from screen-space pixel position
		float threshold = dither4x4(FRAGCOORD.xy, banded);

		// Difference between smooth brightness and banded brightness
		float remainder = fract(brightness * band_steps);

		// Decide whether this pixel should step up into the next band
		float mix_up = step(threshold, remainder);

		float dithered_brightness = (floor(brightness * band_steps) + mix_up) / band_steps;

		// Preserve color but remap brightness
		float original_brightness = max(brightness, 0.0001);
		vec3 remapped = final_color * (dithered_brightness / original_brightness);

		final_color = mix(final_color, remapped, dither_strength);
	}

	ALBEDO = final_color;
	ALPHA = 1.0;
}
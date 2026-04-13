shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

uniform bool enable_color_limitation = true;
uniform int color_levels : hint_range(2, 256, 1) = 20;

uniform bool enable_dithering = true;
uniform float dither_strength : hint_range(0.0, 1.0) = 0.65;
//uniform float dither_strength : hint_range(0.0, 1.0) = 25;
uniform float band_steps : hint_range(2.0, 16.0) = 6.0;

uniform bool enable_screen_wobble = false;
uniform float wobble_strength : hint_range(0.0, 0.02) = 0.0025;
uniform float wobble_speed : hint_range(0.1, 20.0) = 4.0;

uniform bool enable_screen_jitter = false;
uniform float jitter_strength : hint_range(0.0, 5.0) = 1.0;

//uniform bool enable_fog = true;
//uniform vec3 fog_color : source_color = vec3(0.72, 0.76, 0.78);
//uniform float fog_strength : hint_range(0.0, 1.0) = 0.35;
//uniform float fog_vertical_start : hint_range(0.0, 1.0) = 0.35;
//uniform float fog_vertical_end : hint_range(0.0, 1.0) = 1.0;

uniform bool enable_noise = true;
uniform vec3 noise_color : source_color = vec3(0.18, 0.20, 0.24);
uniform float noise_strength : hint_range(0.0, 1.0) = 0.12;
uniform float noise_time_fac : hint_range(0.1, 10.0) = 4.0;

float hash_old_12(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453 + (sin(TIME) / noise_time_fac));
}

vec3 quantize_color(vec3 color, int levels) {
	float q = float(levels - 1);
	return floor(color * q + 0.5) / q;
}

float bayer4x4(vec2 position) {
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

void fragment() {
	vec2 uv = SCREEN_UV;

	if (enable_screen_wobble) {
		uv.x += sin(uv.y * 12.0 + TIME * wobble_speed) * wobble_strength;
		uv.y += cos(uv.x * 10.0 + TIME * wobble_speed * 0.8) * wobble_strength * 0.5;
	}

	if (enable_screen_jitter) {
		vec2 pixel_size = SCREEN_PIXEL_SIZE;
		float jitter_x = (fract(sin(TIME * 37.0) * 43758.5453) - 0.5) * jitter_strength;
		float jitter_y = (fract(cos(TIME * 29.0) * 24634.6345) - 0.5) * jitter_strength;
		uv += vec2(jitter_x * pixel_size.x, jitter_y * pixel_size.y);
	}
	vec3 final_color = texture(screen_texture, uv).rgb;

	//if (enable_fog) {
		//float fog_factor = smoothstep(fog_vertical_start, fog_vertical_end, uv.y);
		//fog_factor *= fog_strength;
		//final_color = mix(final_color, fog_color, fog_factor);
	//}

	if (enable_noise) {
		float n = hash_old_12(uv * 200.0);
		vec3 noise_mix = mix(final_color, noise_color, n * noise_strength);
		final_color = mix(final_color, noise_mix, noise_strength);
	}

	if (enable_color_limitation) {
		final_color = quantize_color(final_color, color_levels);
	}

	if (enable_dithering) {
		float brightness = dot(final_color, vec3(0.299, 0.587, 0.114));

		float scaled = brightness * band_steps;
		float base_band = floor(scaled);
		float remainder = fract(scaled);

		float threshold = bayer4x4(FRAGCOORD.xy);
		float mix_up = step(threshold, remainder);

		float dithered_brightness = (base_band + mix_up) / band_steps;

		float original_brightness = max(brightness, 0.0001);
		vec3 remapped = final_color * (dithered_brightness / original_brightness);

		final_color = mix(final_color, remapped, dither_strength);
	}

	COLOR = vec4(final_color, 1.0);
}
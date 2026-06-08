shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_prepass_alpha;

uniform sampler2D texture_albedo : source_color, filter_nearest;

uniform float base_wobble = 0.004;
uniform float base_diverge = 0.004;
uniform float burst_strength = 1.0;
uniform float burst_speed = 7.0;
uniform float band_frequency = 70.0;
uniform float static_amount = 0.08;
uniform float billboard_scale = 1;

float rand(float n) {
	return fract(sin(n) * 43758.5453123);
}

uniform float pixel_size = 1;

void vertex() {
	vec3 world_pos = MODEL_MATRIX[3].xyz;

	vec3 camera_forward = -INV_VIEW_MATRIX[2].xyz;
	camera_forward.y = 0.0;
	camera_forward = normalize(camera_forward);

	vec3 right = normalize(cross(vec3(0.0, 1.0, 0.0), camera_forward));
	vec3 up = vec3(0.0, 1.0, 0.0);

	vec3 world_vertex =
		world_pos +
		right * VERTEX.x * pixel_size +
		up * VERTEX.y * pixel_size;

	POSITION = PROJECTION_MATRIX * VIEW_MATRIX * vec4(world_vertex, 1.0);
}

void fragment() {
	vec2 uv = UV;

	float burst_time = floor(TIME * burst_speed);
	float burst_random = rand(burst_time);

	float burst = 0.0;
	if (burst_random > 0.86) {
		burst = (burst_random - 0.86) * 7.0 * burst_strength;
	}

	// Read alpha around this pixel to detect sprite edges
	float a_center = texture(texture_albedo, uv).a;
	float a_left = texture(texture_albedo, uv + vec2(-0.01, 0.0)).a;
	float a_right = texture(texture_albedo, uv + vec2(0.01, 0.0)).a;
	float a_up = texture(texture_albedo, uv + vec2(0.0, -0.01)).a;
	float a_down = texture(texture_albedo, uv + vec2(0.0, 0.01)).a;

	float edge = abs(a_center - a_left)
		+ abs(a_center - a_right)
		+ abs(a_center - a_up)
		+ abs(a_center - a_down);

	edge = clamp(edge * 3.0, 0.0, 1.0);

	// Constant dramatic edge tearing
	float line = floor(UV.y * band_frequency);
	float line_noise = rand(line + floor(TIME * 26.0));

	float small_shift = 0.0;
	if (line_noise > 0.25) {
		small_shift = (line_noise - 0.5) * 0.055 * edge;
	}

	// More frequent big line bursts
	float big_shift = 0.0;
	if (burst > 0.0 && line_noise > 0.48) {
		big_shift = (line_noise - 0.5) * 0.18 * edge * burst;
	}

	uv.x += small_shift + big_shift;

	// RGB split mostly on edges
	float split = base_diverge + edge * 0.008 + burst * 0.02;

	float r = texture(texture_albedo, uv + vec2(split, 0.0)).r;
	float g = texture(texture_albedo, uv).g;
	float b = texture(texture_albedo, uv - vec2(split, 0.0)).b;
	float a = texture(texture_albedo, uv).a;

	vec4 tex = vec4(r, g, b, a);
	
	// Extra horizontal glitch trails going in/out from the silhouette
	float trail_noise = rand(line * 3.1 + floor(TIME * 18.0));

	if (edge > 0.15 && trail_noise > 0.38) {
		float trail_dir = trail_noise > 0.69 ? 1.0 : -1.0;
		float trail_len = 0.025 + trail_noise * 0.09;

		vec4 trail = texture(texture_albedo, UV + vec2(trail_dir * trail_len, 0.0));

		tex.rgb = max(tex.rgb, trail.rgb * vec3(0.9, 1.0, 1.2));
		tex.a = max(tex.a, trail.a * 0.85);
	}

	// White/black speckly artifacting around edges
	//float speckle = rand(UV.x * 900.0 + UV.y * 1400.0 + floor(TIME * 30.0));
//
	//if (speckle > 0.965 && edge > 0.2) {
		//tex.rgb += vec3(0.8);
	//}
//
	//if (speckle < 0.025 && edge > 0.2) {
		//tex.rgb *= 0.25;
	//}

	ALBEDO = tex.rgb;
	ALPHA = tex.a;

	if (tex.a < 0.01) {
		discard;
	}
}
@tool

extends WorldEnvironment

# General settings for time of day, on/off for simulating day and night cycle, rate of time, and overall rotation of the sky
@export_category("GodotSky Control")
@export_range(0,2400,0.01) var timeOfDay : float = 1200.0
@export var simulateTime : bool = false
@export_range(0,10,0.01) var rateOfTime : float = 0.1
@export_range(0,360,0.1) var skyRotation : float = 0.0
@export_enum("Static", "2D Dynamic") var cloudType : String = "Static"
@export_range(0,1,0.001) var cloudCoverage : float = 0.5
#@export var staticClouds : bool = true *DEPRECATED*
@export var animateStaticClouds : bool = true
@export var animateStarMap : bool = true
@export var sunShadow : bool = true
@export var moonShadow : bool = true
@export_category("GodotSky Preset")
@export var skyPreset : SkyPreset = preload("res://GodotSky/Presets/sky_default.tres")

# Get required node references
@onready var sunMoonParent = $SunMoon
@onready var sunRoot : MeshInstance3D = $SunMoon/Sun
@onready var moonRoot : MeshInstance3D = $SunMoon/Moon
@onready var sun : DirectionalLight3D = $SunMoon/Sun/SunLight
@onready var moon : DirectionalLight3D = $SunMoon/Moon/MoonLight
@onready var sky : WorldEnvironment = $"."

var sunPosition : float = 0.0
var moonPosition : float = 0.0
var sunPosAlpha : float = 0.0

# Check if simulating day/night cycle, determine rate of time, and increase time
func simulateDay():
	if (simulateTime == true):
		timeOfDay += rateOfTime
		if (timeOfDay >= 2400.0):
			timeOfDay = 0.0

# Update sun and moon based on current time of day 
func updateLights():
	var moonStrength: float = clampf(moonRoot.global_position.y / 2.0 + 0.5, 0.0, 1.0)

	# Sun is blocked by The Veil.
	# Stronger red when moon is down, weaker red when moon is up.
	sun.light_energy = lerpf(0.08, 0.01, moonStrength)
	sun.light_color = Color(0.8, 0.12, 0.04)
	sun.shadow_enabled = false

	# Moon becomes the main world light as it rises.
	moon.light_color = Color(0.42, 0.46, 0.58)
	moon.shadow_enabled = moonShadow

	match cloudType:
		"Static":
			moon.light_energy = lerpf(0.02, 0.18, moonStrength)

		"2D Dynamic":
			moon.light_energy = lerpf(0.02, 0.18, moonStrength) * (1.0 - (cloudCoverage + 0.2))
			moon.light_energy = clamp(moon.light_energy, 0.0, 1.0)

# Update rotation of sun and moon
func updateRotation():
	var hourMapped = remap(timeOfDay, 0.0, 2400.0, 0.0, 1.0)

	# Do NOT rotate the whole SunMoon parent anymore
	sunMoonParent.rotation_degrees.y = skyRotation
	sunMoonParent.rotation_degrees.x = 0.0

	# Keep the sun fixed in the sky
	sunRoot.rotation_degrees.x = 110.0
	sunRoot.rotation_degrees.y = 45.0
	sunRoot.rotation_degrees.z = 0.0

	# Only the moon moves with time
	moonRoot.rotation_degrees.x = hourMapped * 360.0
	moonRoot.rotation_degrees.y = 0.0
	moonRoot.rotation_degrees.z = 0.0
	
# Update colors based on current time of day
func updateSky():
	var skyPosition := 0.0
	var eclipsePosition := 0.55

	var skyMaterial = self.environment.sky.get_material()
	var cloudColor = lerp(
		skyPreset.baseCloudColor.gradient.sample(skyPosition),
		skyPreset.overcastCloudColor.gradient.sample(skyPosition),
		cloudCoverage
	)
	
	skyMaterial.set_shader_parameter("cloudType", 0 if cloudType == "Static" else 1)
	skyMaterial.set_shader_parameter("cloudCoverage", cloudCoverage)
	skyMaterial.set_shader_parameter("cloudDensity", skyPreset.cloudDensity)
	skyMaterial.set_shader_parameter("cloudAlpha", 1.0)
	
	var moonStrength: float = clampf(moonRoot.global_position.y / 2.0 + 0.5, 0.0, 1.0)
	skyMaterial.set_shader_parameter("moonStrength", moonStrength)
	skyMaterial.set_shader_parameter("sunlightColor", Color(0.8, 0.12, 0.04))
	skyMaterial.set_shader_parameter("baseCloudColor", cloudColor)
	skyMaterial.set_shader_parameter("horizonFogColor", Color(0.015, 0.018, 0.035, 1.0))
	skyMaterial.set_shader_parameter("baseColor", Color(0.01, 0.012, 0.03, 1.0))

	# Keep sun/eclipse visible even though the world is dark
	skyMaterial.set_shader_parameter("sunDiscColor", Color(0.7, 0.08, 0.02))
	skyMaterial.set_shader_parameter("sunGlowColor", Color(0.8, 0.12, 0.03, 0.45))
	skyMaterial.set_shader_parameter("sunGlowIntensity", 0.8)

	# Moon can still use normal preset if wanted
	skyMaterial.set_shader_parameter("moonGlowColor", skyPreset.moonGlowColor.gradient.sample(eclipsePosition))
	skyMaterial.set_shader_parameter("moonLightColor", skyPreset.moonLightColor.gradient.sample(eclipsePosition))
	
# Called when the node enters the scene tree for the first time.
func _ready():
	pass

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta):
	simulateDay()
	updateRotation()
	updateSky()
	updateLights()

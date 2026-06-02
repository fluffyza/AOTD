using Godot;
using System.Collections.Generic;

public partial class MineManager : Node
{
	[Export] public NodePath WorldManagerPath;
	[Export] public NodePath BlockManagerPath;

	[Export] public int MinShaftDepth = 10;
	[Export] public int MaxShaftDepth = 20;

	[Export] public int ShaftOpeningWidth = 3;
	[Export] public int ShaftWidth = 3;

	[Export] public int CaveWallThickness = 1;

	[Export] public int MainWormStepsMin = 18;
	[Export] public int MainWormStepsMax = 28;

	[Export] public int SideWormStepsMin = 12;
	[Export] public int SideWormStepsMax = 22;

	[Export] public float WormBaseRadiusMin = 2.0f;
	[Export] public float WormBaseRadiusMax = 2.8f;

	[Export] public float WormTurnStrength = 0.28f;
	[Export] public float WormPitchStrength = 0.12f;

	[Export] public float RadiusVariationStrength = 0.35f;

	[Export] public int ChamberBlobCountMin = 4;
	[Export] public int ChamberBlobCountMax = 7;
	[Export] public float ChamberRadiusMin = 4.0f;
	[Export] public float ChamberRadiusMax = 6.5f;
	[Export] public float ChamberOffsetRadius = 3.5f;
	
	public HashSet<Vector3I> ProtectedEntranceCells { get; } = new();

	private WorldManager _worldManager;
	private BlockManager _blockManager;
	private readonly RandomNumberGenerator _rng = new();
	private FastNoiseLite _caveNoise;

	private class CaveWorm
	{
		public Vector3 Position;
		public Vector3 Direction;
		public int StepsRemaining;
		public float BaseRadius;
		public float NoiseCursor;
		public float NoiseChannel;
	}

	public override void _Ready()
	{
		_worldManager = GetNode<WorldManager>(WorldManagerPath);
		_blockManager = GetNode<BlockManager>(BlockManagerPath);

		_rng.Randomize();

		_caveNoise = new FastNoiseLite();
		_caveNoise.Seed = (int)_rng.Randi();
		_caveNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		_caveNoise.Frequency = 0.08f;
	}

	public void GenerateMine(Vector3I entranceCenter)
	{
		MarkProtectedMineEntrance(entranceCenter);

		int shaftDepth = _rng.RandiRange(MinShaftDepth, MaxShaftDepth);
		GenerateVerticalShaftMine(entranceCenter, shaftDepth);
	}

	private void GenerateVerticalShaftMine(Vector3I entranceCenter, int shaftDepth)
	{
		int shaftHalfNeg = ShaftWidth / 2;
		int shaftHalfPos = ShaftWidth - shaftHalfNeg - 1;

		Vector3I shaftBottomCenter = entranceCenter + new Vector3I(0, -shaftDepth, 0);

		GenerateShaft(entranceCenter, shaftDepth, shaftHalfNeg, shaftHalfPos);
		ClearSurfaceOpening(entranceCenter);

		GenerateCaveNetworkFromShaftBottom(shaftBottomCenter);
	}

	private void GenerateCaveNetworkFromShaftBottom(Vector3I shaftBottomCenter)
	{
		HashSet<Vector3I> caveInterior = new();
		HashSet<Vector3I> caveShell = new();
		HashSet<Vector3I> blockedShellCells = new();

		Vector3I chamberCenterCell = shaftBottomCenter + new Vector3I(0, -5, 0);
		Vector3 chamberCenter = new Vector3(
			chamberCenterCell.X,
			chamberCenterCell.Y,
			chamberCenterCell.Z
		);

		// 1. Make the main chamber first.
		MarkBlobChamber(chamberCenter, caveInterior);

		// 2. Force a clean shaft-to-chamber opening.
		MarkShaftToChamberConnectionInterior(shaftBottomCenter, chamberCenterCell, caveInterior);
		MarkProtectedShaftEntranceZone(shaftBottomCenter, blockedShellCells);

		// 3. Start worms from the chamber, not the shaft.
		Vector3 mainDir = RandomHorizontalDirection();
		Vector3 sideDirA = mainDir.Rotated(Vector3.Up, _rng.RandfRange(0.9f, 1.5f)).Normalized();
		Vector3 sideDirB = mainDir.Rotated(Vector3.Up, _rng.RandfRange(-1.5f, -0.9f)).Normalized();

		CaveWorm mainWorm = CreateWorm(
			chamberCenter + mainDir * 2.0f,
			mainDir,
			_rng.RandiRange(MainWormStepsMin, MainWormStepsMax),
			_rng.RandfRange(WormBaseRadiusMin, WormBaseRadiusMax),
			0f
		);

		CaveWorm sideWormA = CreateWorm(
			chamberCenter + sideDirA * 2.0f,
			sideDirA,
			_rng.RandiRange(SideWormStepsMin, SideWormStepsMax),
			_rng.RandfRange(WormBaseRadiusMin, WormBaseRadiusMax),
			100f
		);

		CaveWorm sideWormB = CreateWorm(
			chamberCenter + sideDirB * 2.0f,
			sideDirB,
			_rng.RandiRange(SideWormStepsMin, SideWormStepsMax),
			_rng.RandfRange(WormBaseRadiusMin, WormBaseRadiusMax),
			200f
		);

		RunWorm(mainWorm, caveInterior, createChamberAtEnd: true);
		RunWorm(sideWormA, caveInterior, createChamberAtEnd: false);
		RunWorm(sideWormB, caveInterior, createChamberAtEnd: false);

		BuildShellFromInterior(caveInterior, caveShell, blockedShellCells, CaveWallThickness);

		// Place only the unbreakable shell first.
		ApplyCaveShellToWorld(caveInterior, caveShell);

		// Do any clearing/opening BEFORE filling.
		OpenShaftBottomIntoChamber(shaftBottomCenter);

		// Fill the cave/worm/shaft interior LAST.
		FillCaveInteriorWithRandomBlocks(caveInterior);
	}
	
	private void ApplyCaveShellToWorld(HashSet<Vector3I> caveInterior, HashSet<Vector3I> caveShell)
	{
		foreach (Vector3I cell in caveShell)
		{
			if (caveInterior.Contains(cell))
				continue;

			PlaceMineBlock(cell, true);
		}
	}
	
	private void FillCaveInteriorWithRandomBlocks(HashSet<Vector3I> caveInterior)
	{
		foreach (Vector3I cell in caveInterior)
		{
			PlaceRandomMineBlock(cell);
		}
	}
	
	private void MarkProtectedShaftEntranceZone(Vector3I shaftBottomCenter, HashSet<Vector3I> blockedShellCells)
	{
		int shaftHalfNeg = ShaftWidth / 2;
		int shaftHalfPos = ShaftWidth - shaftHalfNeg - 1;

		// Protect the 3x3 shaft continuation plus a slightly wider mouth
		// where it enters the chamber, so the cave shell cannot form a roof here.
		for (int y = 0; y <= 6; y++)
		{
			for (int x = -shaftHalfNeg - 1; x <= shaftHalfPos + 1; x++)
			{
				for (int z = -shaftHalfNeg - 1; z <= shaftHalfPos + 1; z++)
				{
					Vector3I cell = new Vector3I(
						shaftBottomCenter.X + x,
						shaftBottomCenter.Y - y,
						shaftBottomCenter.Z + z
					);

					blockedShellCells.Add(cell);
				}
			}
		}
	}
	
	private void ForceShaftChamberOpening(Vector3I shaftBottomCenter, Vector3I chamberCenterCell)
	{
		int shaftHalfNeg = ShaftWidth / 2;
		int shaftHalfPos = ShaftWidth - shaftHalfNeg - 1;

		// Go well into the chamber, not just near its roof.
		int connectorBottomY = chamberCenterCell.Y + 1;

		// 1) Clear a straight 3x3 shaft continuation down into the chamber.
		for (int y = shaftBottomCenter.Y; y >= connectorBottomY; y--)
		{
			for (int x = -shaftHalfNeg; x <= shaftHalfPos; x++)
			{
				for (int z = -shaftHalfNeg; z <= shaftHalfPos; z++)
				{
					Vector3I cell = new Vector3I(
						shaftBottomCenter.X + x,
						y,
						shaftBottomCenter.Z + z
					);

					_worldManager.RemoveBlockIfExists(cell);
				}
			}
		}

		// 2) Clear a proper mouth where the shaft enters the chamber.
		// This is the important part that removes the "blocking layer".
		for (int y = connectorBottomY; y >= connectorBottomY - 2; y--)
		{
			for (int x = -shaftHalfNeg - 1; x <= shaftHalfPos + 1; x++)
			{
				for (int z = -shaftHalfNeg - 1; z <= shaftHalfPos + 1; z++)
				{
					Vector3I cell = new Vector3I(
						shaftBottomCenter.X + x,
						y,
						shaftBottomCenter.Z + z
					);

					_worldManager.RemoveBlockIfExists(cell);
				}
			}
		}
	}
	
	private void ClearEntranceSphere(Vector3 center, float radius)
	{
		int minX = Mathf.FloorToInt(center.X - radius);
		int maxX = Mathf.CeilToInt(center.X + radius);
		int minY = Mathf.FloorToInt(center.Y - radius);
		int maxY = Mathf.CeilToInt(center.Y + radius);
		int minZ = Mathf.FloorToInt(center.Z - radius);
		int maxZ = Mathf.CeilToInt(center.Z + radius);

		float radiusSq = radius * radius;

		for (int x = minX; x <= maxX; x++)
		{
			for (int y = minY; y <= maxY; y++)
			{
				for (int z = minZ; z <= maxZ; z++)
				{
					Vector3 sample = new Vector3(x, y, z);

					if (sample.DistanceSquaredTo(center) <= radiusSq)
					{
						_worldManager.RemoveBlockIfExists(new Vector3I(x, y, z));
					}
				}
			}
		}
	}
	
	private void OpenShaftBottomIntoChamber(Vector3I shaftBottomCenter)
	{
		int shaftHalfNeg = ShaftWidth / 2;
		int shaftHalfPos = ShaftWidth - shaftHalfNeg - 1;

		// 1. Clear the exact shaft footprint for a few layers around the bottom.
		for (int yOffset = -2; yOffset <= 2; yOffset++)
		{
			int y = shaftBottomCenter.Y + yOffset;

			for (int x = -shaftHalfNeg; x <= shaftHalfPos; x++)
			{
				for (int z = -shaftHalfNeg; z <= shaftHalfPos; z++)
				{
					_worldManager.RemoveBlockIfExists(new Vector3I(
						shaftBottomCenter.X + x,
						y,
						shaftBottomCenter.Z + z
					));
				}
			}
		}

		// 2. Clear downward below the shaft so it connects into the chamber.
		for (int depth = 0; depth <= 5; depth++)
		{
			int y = shaftBottomCenter.Y - depth;

			for (int x = -shaftHalfNeg; x <= shaftHalfPos; x++)
			{
				for (int z = -shaftHalfNeg; z <= shaftHalfPos; z++)
				{
					_worldManager.RemoveBlockIfExists(new Vector3I(
						shaftBottomCenter.X + x,
						y,
						shaftBottomCenter.Z + z
					));
				}
			}
		}

		// 3. Clear a slightly wider mouth right under the shaft
		// to remove uneven roof blocks from the cavern generation.
		for (int depth = 2; depth <= 5; depth++)
		{
			int y = shaftBottomCenter.Y - depth;

			for (int x = -shaftHalfNeg - 1; x <= shaftHalfPos + 1; x++)
			{
				for (int z = -shaftHalfNeg - 1; z <= shaftHalfPos + 1; z++)
				{
					_worldManager.RemoveBlockIfExists(new Vector3I(
						shaftBottomCenter.X + x,
						y,
						shaftBottomCenter.Z + z
					));
				}
			}
		}
	}
	
	
	private void MarkShaftToChamberConnectionInterior(
		Vector3I shaftBottomCenter,
		Vector3I chamberCenterCell,
		HashSet<Vector3I> caveInterior)
	{
		int shaftHalfNeg = ShaftWidth / 2;
		int shaftHalfPos = ShaftWidth - shaftHalfNeg - 1;

		int connectorBottomY = chamberCenterCell.Y + 1;

		// Straight 3x3 connector
		for (int y = shaftBottomCenter.Y; y >= connectorBottomY; y--)
		{
			for (int x = -shaftHalfNeg; x <= shaftHalfPos; x++)
			{
				for (int z = -shaftHalfNeg; z <= shaftHalfPos; z++)
				{
					Vector3I cell = new Vector3I(
						shaftBottomCenter.X + x,
						y,
						shaftBottomCenter.Z + z
					);

					caveInterior.Add(cell);
				}
			}
		}

		// Small chamber mouth so shell does not cap it off
		for (int y = connectorBottomY; y >= connectorBottomY - 2; y--)
		{
			for (int x = -shaftHalfNeg - 1; x <= shaftHalfPos + 1; x++)
			{
				for (int z = -shaftHalfNeg - 1; z <= shaftHalfPos + 1; z++)
				{
					Vector3I cell = new Vector3I(
						shaftBottomCenter.X + x,
						y,
						shaftBottomCenter.Z + z
					);

					caveInterior.Add(cell);
				}
			}
		}
	}
	
	private void ConnectShaftToNearestCaveInterior(Vector3I shaftBottomCenter, HashSet<Vector3I> caveInterior)
	{
		int shaftHalfNeg = ShaftWidth / 2;
		int shaftHalfPos = ShaftWidth - shaftHalfNeg - 1;

		// Search downward below the shaft for the first hollow cave cell.
		// We search a bit wider than the shaft itself so we can find a cave
		// that is slightly offset but still basically connected.
		Vector3I? foundInterior = null;

		for (int depth = 0; depth <= 20 && foundInterior == null; depth++)
		{
			int y = shaftBottomCenter.Y - depth;

			for (int x = -shaftHalfNeg - 2; x <= shaftHalfPos + 2; x++)
			{
				for (int z = -shaftHalfNeg - 2; z <= shaftHalfPos + 2; z++)
				{
					Vector3I cell = new Vector3I(
						shaftBottomCenter.X + x,
						y,
						shaftBottomCenter.Z + z
					);

					if (caveInterior.Contains(cell))
					{
						foundInterior = cell;
						break;
					}
				}

				if (foundInterior != null)
					break;
			}
		}

		// If no cave interior was found, just clear a deeper fallback opening.
		if (foundInterior == null)
		{
			for (int y = 0; y <= 8; y++)
			{
				for (int x = -shaftHalfNeg; x <= shaftHalfPos; x++)
				{
					for (int z = -shaftHalfNeg; z <= shaftHalfPos; z++)
					{
						Vector3I cell = new Vector3I(
							shaftBottomCenter.X + x,
							shaftBottomCenter.Y - y,
							shaftBottomCenter.Z + z
						);

						_worldManager.RemoveBlockIfExists(cell);
					}
				}
			}

			return;
		}

		Vector3I target = foundInterior.Value;

		// Clear a guaranteed vertical column from shaft bottom down to the found cave interior.
		int minY = Mathf.Min(shaftBottomCenter.Y, target.Y);
		int maxY = Mathf.Max(shaftBottomCenter.Y, target.Y);

		for (int y = minY; y <= maxY; y++)
		{
			for (int x = -shaftHalfNeg; x <= shaftHalfPos; x++)
			{
				for (int z = -shaftHalfNeg; z <= shaftHalfPos; z++)
				{
					Vector3I cell = new Vector3I(
						shaftBottomCenter.X + x,
						y,
						shaftBottomCenter.Z + z
					);

					_worldManager.RemoveBlockIfExists(cell);
				}
			}
		}

		// Clear a slightly wider opening at the connection point so it does not feel plugged.
		for (int x = -shaftHalfNeg - 1; x <= shaftHalfPos + 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				for (int z = -shaftHalfNeg - 1; z <= shaftHalfPos + 1; z++)
				{
					Vector3I cell = new Vector3I(
						target.X + x,
						target.Y + y,
						target.Z + z
					);

					_worldManager.RemoveBlockIfExists(cell);
				}
			}
		}
	}
	
	private void ClearShaftToCaveConnection(Vector3I shaftBottomCenter)
	{
		int shaftHalfNeg = ShaftWidth / 2;
		int shaftHalfPos = ShaftWidth - shaftHalfNeg - 1;

		// Clear a few blocks downward so the shaft definitely opens into the cave.
		// This removes any cave roof/shell that formed under the shaft.
		for (int y = 0; y <= 6; y++)
		{
			for (int x = -shaftHalfNeg; x <= shaftHalfPos; x++)
			{
				for (int z = -shaftHalfNeg; z <= shaftHalfPos; z++)
				{
					Vector3I cell = new Vector3I(
						shaftBottomCenter.X + x,
						shaftBottomCenter.Y - y,
						shaftBottomCenter.Z + z
					);

					_worldManager.RemoveBlockIfExists(cell);
				}
			}
		}

		// Optional: widen the opening just under the shaft slightly so it feels smoother.
		for (int x = -shaftHalfNeg - 1; x <= shaftHalfPos + 1; x++)
		{
			for (int z = -shaftHalfNeg - 1; z <= shaftHalfPos + 1; z++)
			{
				Vector3I cell = new Vector3I(
					shaftBottomCenter.X + x,
					shaftBottomCenter.Y - 2,
					shaftBottomCenter.Z + z
				);

				_worldManager.RemoveBlockIfExists(cell);
			}
		}
	}


	private CaveWorm CreateWorm(Vector3 startPosition, Vector3 startDirection, int steps, float baseRadius, float noiseChannel)
	{
		return new CaveWorm
		{
			Position = startPosition,
			Direction = startDirection.Normalized(),
			StepsRemaining = steps,
			BaseRadius = baseRadius,
			NoiseCursor = 0f,
			NoiseChannel = noiseChannel
		};
	}

	private void RunWorm(CaveWorm worm, HashSet<Vector3I> caveInterior, bool createChamberAtEnd)
	{
		Vector3 lastPosition = worm.Position;

		for (int step = 0; step < worm.StepsRemaining; step++)
		{
			float yawNoise = _caveNoise.GetNoise2D(worm.NoiseCursor, worm.NoiseChannel);
			float pitchNoise = _caveNoise.GetNoise2D(worm.NoiseCursor, worm.NoiseChannel + 37.0f);
			float radiusNoise = _caveNoise.GetNoise2D(worm.NoiseCursor, worm.NoiseChannel + 73.0f);

			float yaw = yawNoise * WormTurnStrength;
			float pitch = pitchNoise * WormPitchStrength;

			Vector3 dir = worm.Direction;

			// Yaw around world up.
			dir = (new Basis(Vector3.Up, yaw) * dir).Normalized();

			// Pitch around local right axis.
			Vector3 right = dir.Cross(Vector3.Up).Normalized();
			if (right.LengthSquared() < 0.0001f)
				right = Vector3.Right;

			dir = (new Basis(right, pitch) * dir).Normalized();

			// Keep caves mostly horizontal so they feel mine-like.
			dir.Y = Mathf.Clamp(dir.Y, -0.30f, 0.18f);
			dir = dir.Normalized();

			worm.Direction = dir;
			worm.Position += worm.Direction;
			worm.NoiseCursor += 1.0f;

			float currentRadius = worm.BaseRadius * (1.0f + radiusNoise * RadiusVariationStrength);
			currentRadius = Mathf.Max(1.75f, currentRadius);

			MarkInteriorSphere(worm.Position, currentRadius, caveInterior);

			// Small occasional bulges so tunnels do not feel too uniform.
			if (_rng.Randf() < 0.12f)
			{
				float bulgeRadius = currentRadius + _rng.RandfRange(0.4f, 1.0f);
				Vector3 bulgeOffset = new Vector3(
					_rng.RandfRange(-0.7f, 0.7f),
					_rng.RandfRange(-0.4f, 0.4f),
					_rng.RandfRange(-0.7f, 0.7f)
				);

				MarkInteriorSphere(worm.Position + bulgeOffset, bulgeRadius, caveInterior);
			}

			lastPosition = worm.Position;
		}

		if (createChamberAtEnd)
			MarkBlobChamber(lastPosition, caveInterior);
	}

	private void MarkBlobChamber(Vector3 center, HashSet<Vector3I> caveInterior)
	{
		int blobCount = _rng.RandiRange(ChamberBlobCountMin, ChamberBlobCountMax);

		for (int i = 0; i < blobCount; i++)
		{
			Vector3 offset = new Vector3(
				_rng.RandfRange(-ChamberOffsetRadius, ChamberOffsetRadius),
				_rng.RandfRange(-1.8f, 1.8f),
				_rng.RandfRange(-ChamberOffsetRadius, ChamberOffsetRadius)
			);

			float radius = _rng.RandfRange(ChamberRadiusMin, ChamberRadiusMax);
			MarkInteriorSphere(center + offset, radius, caveInterior);
		}
	}
	
	private void ForceShaftWormConnection(Vector3I shaftBottomCenter)
	{
		int shaftHalfNeg = ShaftWidth / 2;
		int shaftHalfPos = ShaftWidth - shaftHalfNeg - 1;

		// Clear a 3x3 tunnel straight down from the shaft bottom.
		// This guarantees the shaft opens into the worm area.
		for (int y = 0; y <= 4; y++)
		{
			for (int x = -shaftHalfNeg; x <= shaftHalfPos; x++)
			{
				for (int z = -shaftHalfNeg; z <= shaftHalfPos; z++)
				{
					Vector3I cell = new Vector3I(
						shaftBottomCenter.X + x,
						shaftBottomCenter.Y - y,
						shaftBottomCenter.Z + z
					);

					_worldManager.RemoveBlockIfExists(cell);
				}
			}
		}
	}

	private void MarkShaftConnectionInterior(Vector3I shaftBottomCenter, HashSet<Vector3I> caveInterior)
	{
		int shaftHalfNeg = ShaftWidth / 2;
		int shaftHalfPos = ShaftWidth - shaftHalfNeg - 1;

		// Only mark a short straight opening below the shaft.
		// Keep this tight so we do not accidentally open too much roof.
		for (int y = 0; y <= 2; y++)
		{
			for (int x = -shaftHalfNeg; x <= shaftHalfPos; x++)
			{
				for (int z = -shaftHalfNeg; z <= shaftHalfPos; z++)
				{
					Vector3I cell = new Vector3I(
						shaftBottomCenter.X + x,
						shaftBottomCenter.Y - y,
						shaftBottomCenter.Z + z
					);

					caveInterior.Add(cell);
				}
			}
		}
	}

	private void MarkInteriorSphere(Vector3 center, float radius, HashSet<Vector3I> caveInterior)
	{
		int minX = Mathf.FloorToInt(center.X - radius);
		int maxX = Mathf.CeilToInt(center.X + radius);
		int minY = Mathf.FloorToInt(center.Y - radius);
		int maxY = Mathf.CeilToInt(center.Y + radius);
		int minZ = Mathf.FloorToInt(center.Z - radius);
		int maxZ = Mathf.CeilToInt(center.Z + radius);

		float radiusSq = radius * radius;

		for (int x = minX; x <= maxX; x++)
		{
			for (int y = minY; y <= maxY; y++)
			{
				for (int z = minZ; z <= maxZ; z++)
				{
					Vector3 sample = new Vector3(x, y, z);
					if (sample.DistanceSquaredTo(center) <= radiusSq)
						caveInterior.Add(new Vector3I(x, y, z));
				}
			}
		}
	}

	private void BuildShellFromInterior(
		HashSet<Vector3I> caveInterior,
		HashSet<Vector3I> caveShell,
		HashSet<Vector3I> blockedShellCells,
		int wallThickness)
	{
		foreach (Vector3I cell in caveInterior)
		{
			for (int x = -wallThickness; x <= wallThickness; x++)
			{
				for (int y = -wallThickness; y <= wallThickness; y++)
				{
					for (int z = -wallThickness; z <= wallThickness; z++)
					{
						Vector3I neighbor = cell + new Vector3I(x, y, z);

						if (caveInterior.Contains(neighbor))
							continue;

						if (blockedShellCells.Contains(neighbor))
							continue;

						float dist = new Vector3(x, y, z).Length();
						if (dist <= wallThickness + 0.35f)
							caveShell.Add(neighbor);
					}
				}
			}
		}
	}

	private void ApplyCaveToWorld(HashSet<Vector3I> caveInterior, HashSet<Vector3I> caveShell)
	{
		// 1. Fill the inside of the worms/caverns with random breakable mine blocks.
		foreach (Vector3I cell in caveInterior)
		{
			PlaceRandomMineBlock(cell);
		}

		// 2. Place unbreakable shell around the worms/caverns.
		foreach (Vector3I cell in caveShell)
		{
			if (caveInterior.Contains(cell))
				continue;

			PlaceMineBlock(cell, true);
		}
	}
	
	private void PlaceRandomMineBlock(Vector3I cell)
	{
		_worldManager.RemoveBlockIfExists(cell);

		string blockId = GetRandomMineBlockId(cell.Y);

		var block = _blockManager.CreateMineBlock(
			GridUtils.CellToWorld(cell),
			false // random blocks are breakable
		);

		if (block == null)
			return;

		_worldManager.AddPlacedNode(cell, block);
	}
	
	private string GetRandomMineBlockId(int y)
	{
		float roll = GD.Randf();

		if (y > 0)
			return "dirt";
			
		if (roll < 0.2f)
			return "sulfur";

		if (roll < 0.6f)
			return "stone";
		if (roll < 0.8f)
			return "coal";

		return "iron_ore";
	}

	private Vector3 RandomHorizontalDirection()
	{
		float angle = _rng.RandfRange(0f, Mathf.Tau);
		return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).Normalized();
	}

	private void GenerateShaft(Vector3I entranceCenter, int shaftDepth, int shaftHalfNeg, int shaftHalfPos)
	{
		for (int y = 0; y <= shaftDepth; y++)
		{
			int worldY = entranceCenter.Y - y;

			for (int x = -shaftHalfNeg - 1; x <= shaftHalfPos + 1; x++)
			{
				for (int z = -shaftHalfNeg - 1; z <= shaftHalfPos + 1; z++)
				{
					Vector3I cell = new Vector3I(
						entranceCenter.X + x,
						worldY,
						entranceCenter.Z + z
					);

					bool insideShaft =
						x >= -shaftHalfNeg && x <= shaftHalfPos &&
						z >= -shaftHalfNeg && z <= shaftHalfPos;

					if (insideShaft)
					{
						PlaceRandomMineBlock(cell);
					}
					else
					{
						PlaceMineBlock(cell, true);
					}
				}
			}
		}
	}

	private void ClearSurfaceOpening(Vector3I entranceCenter)
	{
		int halfNeg = ShaftOpeningWidth / 2;
		int halfPos = ShaftOpeningWidth - halfNeg - 1;

		for (int x = -halfNeg; x <= halfPos; x++)
		{
			for (int z = -halfNeg; z <= halfPos; z++)
			{
				Vector3I cell = new Vector3I(
					entranceCenter.X + x,
					entranceCenter.Y,
					entranceCenter.Z + z
				);

				ProtectedEntranceCells.Add(cell);
				_worldManager.RemoveBlockIfExists(cell);
			}
		}
	}
	
	private void MarkProtectedMineEntrance(Vector3I entranceCenter)
	{
		int radius = 2; // 5x5

		for (int x = -radius; x <= radius; x++)
		{
			for (int z = -radius; z <= radius; z++)
			{
				ProtectedEntranceCells.Add(new Vector3I(
					entranceCenter.X + x,
					entranceCenter.Y,
					entranceCenter.Z + z
				));
			}
		}
	}

	private void PlaceMineBlock(Vector3I cell, bool unbreakable)
	{
		_worldManager.RemoveBlockIfExists(cell);

		var block = _blockManager.CreateMineBlock(
			GridUtils.CellToWorld(cell),
			unbreakable
		);

		if (block == null)
			return;

		_worldManager.AddPlacedNode(cell, block);
	}
}

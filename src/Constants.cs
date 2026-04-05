using Godot;

namespace BioFilter
{
    public static class Constants
    {
        public static class Colors
        {
            // ── Core palette — inspired by the bunker air filter room ──────────
            public static readonly Color Background    = new Color("#0d1208"); // Near-black with green tint
            public static readonly Color GridLine      = new Color("#1a2a1a"); // Subtle dark green grid
            public static readonly Color ScanlineAlt   = new Color("#121a12"); // Every 2nd row lighter

            // ── Tiles ──────────────────────────────────────────────────────────
            public static readonly Color Wall          = new Color("#2a3a2a"); // Dark military metal green
            public static readonly Color WallHighlight = new Color("#3a5a3a"); // Top-left edge highlight
            public static readonly Color WallShadow    = new Color("#1a2a1a"); // Bottom-right shadow
            public static readonly Color WallRivet     = new Color("#4a6a4a"); // Corner rivet dots
            public static readonly Color Spawn         = new Color("#8b0000"); // Deep red entry point
            public static readonly Color SpawnRing     = new Color("#ff2222"); // Pulsing spawn rings
            public static readonly Color Exit          = new Color("#1a4a2a"); // Dark green bunker intake
            public static readonly Color ExitScanLine  = new Color("#00ff41"); // Bright green scan line

            // ── Towers ─────────────────────────────────────────────────────────
            public static readonly Color BasicFilter        = new Color("#2d7a3a"); // Filter green
            public static readonly Color BasicFilterInner   = new Color("#0d2a0d"); // Inner dark green
            public static readonly Color BasicFilterBright  = new Color("#4caf50"); // Bright green cross
            public static readonly Color Electrostatic      = new Color("#1a5a6a"); // Cold teal-blue
            public static readonly Color ElectrostaticInner = new Color("#0a1a2a"); // Inner dark teal
            public static readonly Color UVSterilizer       = new Color("#4a2a6a"); // Dark purple UV
            public static readonly Color UVSterilizerInner  = new Color("#1a0a2a"); // Inner dark purple
            public static readonly Color VortexCyan         = new Color("#00bcd4"); // Vortex cyan
            public static readonly Color PowerGold          = new Color("#ffd700"); // Power core gold
            public static readonly Color BioNeutralPurple   = new Color("#9c27b0"); // Bio neutraliser
            public static readonly Color MagneticBrown      = new Color("#795548"); // Magnetic cage

            // ── Particles ──────────────────────────────────────────────────────
            public static readonly Color BioParticle   = new Color("#7fff3a"); // Toxic neon green
            public static readonly Color BunkerIntake  = new Color("#3aff8a"); // Glowing intake
            public static readonly Color ParticleSpawn = new Color("#cc2200"); // Danger red
            public static readonly Color SporeSpeckBright = new Color("#ddff44"); // SporeSpeck bright
            public static readonly Color RadiationOrange   = new Color("#ff8c00"); // Radiation blob
            public static readonly Color SwarmLime         = new Color("#88ff44"); // Bacterial swarm
            public static readonly Color CellDivisionPink  = new Color("#ff44aa"); // Cell division

            // ── UI accents ─────────────────────────────────────────────────────
            public static readonly Color HazardYellow  = new Color("#f5c518"); // Warning/hazard
            public static readonly Color FogMist       = new Color("#a0c4b8"); // Ambient mist/fog
            public static readonly Color MetalDark     = new Color("#1e2420"); // Panel backgrounds
            public static readonly Color GlowGreen     = new Color("#4a9e6a"); // Filter glass glow
            public static readonly Color TextPrimary   = new Color("#c8e6c0"); // Main readable text
            public static readonly Color TextDim       = new Color("#6a8a6a"); // Secondary text
            public static readonly Color CornerMarker  = new Color("#4caf50"); // Grid corner brackets
            public static readonly Color CorruptedPixel = new Color(0f, 1f, 0.2f, 0.15f); // Corrupted pixel flash
        }
    }
}

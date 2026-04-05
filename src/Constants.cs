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

            // ── Tiles ──────────────────────────────────────────────────────────
            public static readonly Color Wall          = new Color("#2a3a2a"); // Dark military metal green
            public static readonly Color Spawn         = new Color("#8b0000"); // Deep red entry point
            public static readonly Color Exit          = new Color("#1a4a2a"); // Dark green bunker intake

            // ── Towers ─────────────────────────────────────────────────────────
            public static readonly Color BasicFilter   = new Color("#2d7a3a"); // Filter green
            public static readonly Color Electrostatic = new Color("#1a5a6a"); // Cold teal-blue
            public static readonly Color UVSterilizer  = new Color("#4a2a6a"); // Dark purple UV

            // ── Particles ──────────────────────────────────────────────────────
            public static readonly Color BioParticle   = new Color("#7fff3a"); // Toxic neon green
            public static readonly Color BunkerIntake  = new Color("#3aff8a"); // Glowing intake
            public static readonly Color ParticleSpawn = new Color("#cc2200"); // Danger red

            // ── UI accents ─────────────────────────────────────────────────────
            public static readonly Color HazardYellow  = new Color("#f5c518"); // Warning/hazard
            public static readonly Color FogMist       = new Color("#a0c4b8"); // Ambient mist/fog
            public static readonly Color MetalDark     = new Color("#1e2420"); // Panel backgrounds
            public static readonly Color GlowGreen     = new Color("#4a9e6a"); // Filter glass glow
            public static readonly Color TextPrimary   = new Color("#c8e6c0"); // Main readable text
            public static readonly Color TextDim       = new Color("#6a8a6a"); // Secondary text
        }
    }
}

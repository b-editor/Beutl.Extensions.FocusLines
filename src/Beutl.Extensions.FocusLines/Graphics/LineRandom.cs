namespace Beutl.Extensions.FocusLines.Graphics;

/// <summary>
/// Random numbers that depend only on the seed, the drawing, the line and what the number is for, so any frame
/// draws the same lines whether it is reached by playback, seeking or export, on every platform.
/// </summary>
/// <param name="seed">The seed the user chose.</param>
/// <param name="generation">Which drawing of the lines this is; it moves on each time the lines are redrawn.</param>
internal readonly struct LineRandom(int seed, long generation)
{
    private readonly ulong _base = Mix(Mix((uint)seed) ^ (ulong)generation);

    /// <summary>A number in [0, 1) for line <paramref name="index"/>; each <paramref name="channel"/> is independent.</summary>
    public float Next(int index, int channel)
    {
        ulong hash = Mix(_base ^ (((ulong)(uint)index << 8) | (byte)channel));
        return (hash >> 40) * (1f / (1 << 24));
    }

    /// <summary>A number in [-1, 1) for line <paramref name="index"/>.</summary>
    public float NextSigned(int index, int channel) => Next(index, channel) * 2 - 1;

    // SplitMix64's finalizer: every input bit flips about half of the output bits.
    private static ulong Mix(ulong z)
    {
        z += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}

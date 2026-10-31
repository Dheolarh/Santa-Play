using System;
using UnityEngine;

// Central list of "something happened" moments. Gameplay scripts announce, your audio system listens.
public static class GameEvents
{
    public enum BombEnd { Detonated, Missed, Expired }

    // ---- Flakes ----
    public static event Action<Flake> FlakeSpawned;
    public static event Action<Flake> FlakeCaught;      // flake.Points / flake.AudioId tell you which sound
    public static event Action<Flake> FlakeMissed;      // fell past the bottom

    // ---- Net ----
    public static event Action NetFull;                 // last flake just landed
    public static event Action<int> MeltStarted;        // how many flakes are melting
    public static event Action MeltFinished;
    public static event Action<float> NetFlipped;       // +1 = now on the right, -1 = left

    // ---- Bomb ----
    public static event Action<Bomb> BombSpawned;       // start the fuse loop here
    public static event Action<Bomb, int> BombCaught;   // the explosion moment (int = score actually lost)
    public static event Action<Bomb, BombEnd> BombRemoved; // stop the fuse loop here (any reason)

    // ---- Shockwave ----
    public static event Action<Vector3> ShockwaveStarted;
    public static event Action<float> SantaShoved;      // signed push strength

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        FlakeSpawned = null; FlakeCaught = null; FlakeMissed = null;
        NetFull = null; MeltStarted = null; MeltFinished = null; NetFlipped = null;
        BombSpawned = null; BombCaught = null; BombRemoved = null;
        ShockwaveStarted = null; SantaShoved = null;
    }

    // Events can only be fired from inside this class, so gameplay scripts call these.
    public static void RaiseFlakeSpawned(Flake f) { FlakeSpawned?.Invoke(f); }
    public static void RaiseFlakeCaught(Flake f) { FlakeCaught?.Invoke(f); }
    public static void RaiseFlakeMissed(Flake f) { FlakeMissed?.Invoke(f); }
    public static void RaiseNetFull() { NetFull?.Invoke(); }
    public static void RaiseMeltStarted(int count) { MeltStarted?.Invoke(count); }
    public static void RaiseMeltFinished() { MeltFinished?.Invoke(); }
    public static void RaiseNetFlipped(float side) { NetFlipped?.Invoke(side); }
    public static void RaiseBombSpawned(Bomb b) { BombSpawned?.Invoke(b); }
    public static void RaiseBombCaught(Bomb b, int loss) { BombCaught?.Invoke(b, loss); }
    public static void RaiseBombRemoved(Bomb b, BombEnd why) { BombRemoved?.Invoke(b, why); }
    public static void RaiseShockwaveStarted(Vector3 p) { ShockwaveStarted?.Invoke(p); }
    public static void RaiseSantaShoved(float s) { SantaShoved?.Invoke(s); }
}
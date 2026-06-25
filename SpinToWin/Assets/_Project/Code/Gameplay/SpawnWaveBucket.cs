using CoreUtils.AssetBuckets;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     A CoreUtils bucket of <see cref="SpawnWave" /> assets - the same kind of bucket used for
    ///     socks, obstacles and paddles. Point its Sources at a folder of wave assets and it
    ///     auto-collects them (the asset dragnet), so a teammate adds a wave to the game just by
    ///     dropping a new Spawn Wave asset in that folder - no scene or code changes.
    ///
    ///     The TrackSpawner takes two of these: an INTRO bucket (played once, in bucket order, to teach
    ///     mechanics) and a POOL bucket (pulled at random afterwards). Sort the intro bucket to control
    ///     the teaching order.
    /// </summary>
    [CreateAssetMenu(menuName = "SpinToWin/Spawn Wave Bucket", fileName = "WaveBucket")]
    public class SpawnWaveBucket : GenericAssetBucket<SpawnWave> { }
}

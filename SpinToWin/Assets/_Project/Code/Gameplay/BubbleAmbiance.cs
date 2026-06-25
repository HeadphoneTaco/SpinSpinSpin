using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     A rough-in for the playtest note "bubbles on the inside of the machine": a gentle soap-bubble
    ///     particle haze drifting up through the drum. It configures a <see cref="ParticleSystem" /> in
    ///     code (adding one if the object doesn't have it) so the team gets a working ambient layer to
    ///     re-skin - swap the renderer's material for a real bubble sprite and it's art-final.
    ///
    ///     Put it on its own object in the gameplay scene, roughly centred in the drum. Set the Box Size
    ///     to the drum's interior so bubbles spawn across it. It's purely cosmetic - no colliders, no
    ///     gameplay hooks - so it's safe to leave running in every state.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class BubbleAmbiance : MonoBehaviour {
        [Header("Volume bubbles fill (drum interior)")]
        [Tooltip("Size of the spawn box, centred on this object. Match it to the drum's inside.")]
        [SerializeField] private Vector3 boxSize = new Vector3(5f, 4f, 8f);

        [Header("Flow")]
        [Tooltip("Bubbles spawned per second.")]
        [SerializeField] private float rate = 14f;
        [Tooltip("How fast bubbles drift upward.")]
        [SerializeField] private float riseSpeed = 0.6f;
        [Tooltip("Random sideways wobble, so they don't rise in straight lines.")]
        [SerializeField] private float drift = 0.25f;

        [Header("Look")]
        [Tooltip("Smallest / largest bubble diameter.")]
        [SerializeField] private float minSize = 0.08f;
        [SerializeField] private float maxSize = 0.5f;
        [Tooltip("How long a bubble lives before it pops/fades.")]
        [SerializeField] private float lifetime = 6f;
        [Tooltip("Peak opacity - bubbles fade in then out around this.")]
        [Range(0f, 1f)] [SerializeField] private float opacity = 0.35f;
        [Tooltip("Tint. Leave near-white for soap; the real bubble sprite goes on the renderer's material.")]
        [SerializeField] private Color tint = new Color(0.85f, 0.95f, 1f, 1f);

        private void Reset() {
            // Friendly defaults the moment the component is added in-editor.
            Configure();
        }

        private void OnEnable() {
            Configure();
        }

        /// <summary>Pushes all the fields above onto the ParticleSystem's modules.</summary>
        private void Configure() {
            ParticleSystem ps = GetComponent<ParticleSystem>();
            if (ps == null) {
                return;
            }

            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = lifetime;
            main.startSpeed = riseSpeed;
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = tint;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            main.playOnAwake = true;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rate;

            // Spawn across a box the size of the drum interior, biased upward.
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;

            // Drift sideways a touch so the rise isn't ruler-straight.
            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = drift > 0f;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-drift, drift);
            vel.z = new ParticleSystem.MinMaxCurve(-drift, drift);

            // Fade in then out so bubbles appear and pop softly instead of blinking.
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(opacity, 0.2f),
                    new GradientAlphaKey(opacity, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = g;

            // Slow size breathing so they feel buoyant.
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, 0.7f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.85f));
            size.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }
    }
}

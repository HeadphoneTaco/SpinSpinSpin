using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     The "C" path items trace inside the drum: they enter high (near the top, above the player),
    ///     sweep out and around the curved wall, and come down to the gremlin at the bottom. Items
    ///     advance along it by <b>angle</b>, not by Z, because a C doubles back on itself in Z. Side-to-
    ///     side lane (X) is handled separately by the spawner.
    ///
    ///     Geometry: a circle of <see cref="radius" /> whose bottom touches the gremlin at
    ///     (<see cref="playerZ" />, <see cref="groundY" />), with its height scaled by <see cref="squish" />
    ///     so the C can be flattened or exaggerated to match the drum's interior. Angle phi sweeps from
    ///     <see cref="StartAngle" /> (entry, high up) down to 0 (arrival, at the player).
    /// </summary>
    [System.Serializable]
    public struct ApproachCurve {
        [Tooltip("Height the items arrive at - the gremlin's run height.")]
        public float groundY;

        [Tooltip("Z the gremlin runs at; the bottom of the C sits here.")]
        public float playerZ;

        [Tooltip("Radius of the C. Bigger = wider sweep and more reaction time.")]
        public float radius;

        [Tooltip("How much of the circle to trace. 180 = a full C (semicircle); less = a gentler hook.")]
        public float sweepDegrees;

        [Tooltip("Vertical squish: 1 = round C, below 1 = flatter, above 1 = taller / more exaggerated.")]
        public float squish;

        /// <summary>Entry angle (radians) - where items spawn along the C.</summary>
        public float StartAngle => sweepDegrees * Mathf.Deg2Rad;

        /// <summary>How far (radians) an item advances along the C this frame for a given linear speed.</summary>
        public float AngularStep(float linearSpeed, float dt) {
            return radius > 0f ? linearSpeed / radius * dt : 0f;
        }

        /// <summary>World (z, y) at angle <paramref name="phi" /> along the C. phi = 0 is the player.</summary>
        public Vector2 PointAt(float phi) {
            float z = playerZ + radius * Mathf.Sin(phi);
            float y = groundY + radius * (1f - Mathf.Cos(phi)) * squish;
            return new Vector2(z, y);
        }
    }
}

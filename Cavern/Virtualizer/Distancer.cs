using System;
using System.Numerics;

using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Virtualizer {
    /// <summary>Simulates distance for objects when enabled and using virtualization.</summary>
    public class Distancer : Filter {
        /// <summary>The filtered source.</summary>
        readonly Source source;
		/// <summary>Convolution used for actual filtering.</summary>
        readonly SpikeConvolver filter;

        /// <summary>Create a distance simulation for a <see cref="Source"/>.</summary>
        public Distancer(Source source) {
            this.source = source;
			filter = new SpikeConvolver(new float[16], 0);
        }

        /// <summary>Apply distance simulation on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public override void Process(float[] samples, int channel, int channels) {
            // TODO: optimization by running this code once per object
            Vector3 sourceForward = source.listener.Rotation.PlaceInSphere();
            Vector3 dir = source.Position - source.listener.Position;
            float distance = dir.Length();
            float angle = Vector3.Dot(sourceForward, dir) / distance * VectorExtensions.Rad2Deg;

            // Find bounding angles with discrete impulses
            int smallerAngle = 0;
            while (smallerAngle < angles.Length - 1 && angles[smallerAngle] < angle)
                ++smallerAngle;
            int largerAngle = smallerAngle + 1;
            if (largerAngle == angles.Length)
                largerAngle = angles.Length - 1;
            float angleRatio = Math.Min(QMath.LerpInverse(angles[smallerAngle], angles[largerAngle], angle), 1);

            // Find bounding distances with discrete impulses
            int smallerDistance = 0;
            while (smallerDistance < distances.Length - 1 && distances[smallerDistance] < distance)
                ++smallerDistance;
            int largerDistance = smallerDistance + 1;
            if (largerDistance == distances.Length)
                largerDistance = distances.Length - 1;
            float distanceRatio = QMath.Clamp(QMath.LerpInverse(distances[smallerDistance], distances[largerDistance], distance), 0, 1);

            // Find impulse candidates and their weight
            float[][] candidates = new float[4][] {
                impulses[smallerAngle][smallerDistance],
                impulses[smallerAngle][largerDistance],
                impulses[largerAngle][smallerDistance],
                impulses[largerAngle][largerDistance]
            };
            float[] gains = new float[4] {
                (float)Math.Sqrt((1 - angleRatio) * (1 - distanceRatio)),
                (float)Math.Sqrt((1 - angleRatio) * distanceRatio),
                (float)Math.Sqrt(angleRatio * (1 - distanceRatio)),
                (float)Math.Sqrt(angleRatio * distanceRatio)
            };

            // Create and apply the filter
            int filterSize = Math.Max(
                Math.Max(candidates[0].Length, candidates[1].Length),
                Math.Max(candidates[2].Length, candidates[3].Length)
            );
            float[] filterImpulse = new float[filterSize];
            for (int candidate = 0; candidate < candidates.Length; ++candidate)
                WaveformUtils.Mix(candidates[candidate], filterImpulse, gains[candidate]);
            filter.Impulse = filterImpulse;
            // TODO: find why this breaks Unity audio completely, maybe spectrally corrected impulses will work
            filter.Process(samples, channel, channels);
		}

		/// <summary>All the angles that have their own impulse responses.</summary>
		static readonly float[] angles = new float[6] { 0, 15, 30, 45, 60, 75 };
		/// <summary>All the distances that have their own impulse responses for each angle.</summary>
		static readonly float[] distances = new float[4] { .1f, .5f, 1, 2 };

		/// <summary>Ear canal distortion impulse responses for given angles and distances. The first dimension is the angle,
		/// provided in <see cref="angles"/>, and the second dimension is the distance, provided in <see cref="distances"/>.</summary>
		static readonly float[][][] impulses = new float[6][][] {
			new float[4][] {
				new float[23] { 0f, 0f, 0f, 0f, 0f, 0f, 0.005060405f, 0.370165f, 0.7765994f, 0.963137f, 0.8465202f, 0.8024259f, 0.7807245f, 0.7166646f, 0.6186926f, 0.4751698f, 0.2959671f, 0.2170968f, 0.1505986f, 0.08992894f, 0.03626856f, 0.009816867f, 0.0009188529f },
				new float[167] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.1021404f, 0.2940448f, 0.4853287f, 0.4306912f, 0.3643141f, 0.3160195f, 0.3214442f, 0.2198477f, 0.1780103f, 0.2132602f, 0.1656031f, 0.1298992f, 0.117907f, 0.09417684f, 0.09399163f, 0.02347218f, 0.02343179f, 0.01169635f },
				new float[364] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.2555006f, 0.4465739f, 0.4672332f, 0.4241904f, 0.3601693f, 0.317319f, 0.2957991f, 0.2743649f, 0.2318606f, 0.06318209f, 0.1051346f, 0.147005f, 0.1468251f, 0.06285588f, 0.04186194f, 0.02089582f, 0.02086996f, 0.02084631f },
				new float[83] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.2254526f, 0.489379f, 0.5419078f, 0.4280191f, 0.384966f, 0.3561029f, 0.3694531f, 0.273842f, 0.2655759f, 0.2650507f, 0.215067f, 0.2208568f, 0.1741736f, 0.1184689f, 0.08905829f, 0.04599712f, 0.01989763f, 0.006106794f }
			},
			new float[4][] {
				new float[16] { 0f, 0f, 0f, 0f, 0f, 0.2540428f, 0.618826f, 0.7922781f, 0.5469493f, 0.3315574f, 0.199326f, 0.121843f, 0.1018899f, 0.06081806f, 0.01992492f, 0.002486186f },
				new float[134] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.1586088f, 0.4180621f, 0.5375387f, 0.4103165f, 0.4663895f, 0.408951f, 0.3579919f, 0.2946973f, 0.2628989f, 0.2311984f, 0.2120828f, 0.1431974f, 0.1616223f, 0.1117149f, 0.07432533f, 0.06183909f, 0.01852315f },
				new float[304] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.0460467f, 0.2530732f, 0.5514097f, 0.3900569f, 0.4812818f, 0.3661653f, 0.3656398f, 0.2054189f, 0.2963142f, 0.3186648f, 0.1818931f, 0.09080185f, 0.09070366f, 0.1584972f, 0.09046178f, 0.158059f, 0.06765252f, 0f, 0.02250617f },
				new float[66] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.09916351f, 0.2840413f, 0.4795852f, 0.544922f, 0.4341253f, 0.470324f, 0.4003018f, 0.4059214f, 0.2930336f, 0.2253603f, 0.1818558f, 0.1448934f, 0.1096459f, 0.07136548f, 0.03641209f, 0.02053734f, 0.003155366f }
			},
			new float[4][] {
				new float[13] { 0f, 0f, 0f, 0.1050854f, 0.5309059f, 0.8102948f, 0.7725348f, 0.3768995f, 0.1718067f, 0.1600976f, 0.1167709f, 0.04386054f, 0.01000423f },
				new float[100] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.0805884f, 0.40914f, 0.669571f, 0.6015406f, 0.5137377f, 0.4794751f, 0.3456331f, 0.2786785f, 0.3113221f, 0.310726f, 0.2969636f, 0.2041848f, 0.1709308f, 0.0853182f, 0.05896522f },
				new float[240] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.1761677f, 0.4523828f, 0.451845f, 0.5764098f, 0.4755346f, 0.4498033f, 0.4241526f, 0.2990268f, 0.3732281f, 0.3229832f, 0.1985206f, 0.1486002f, 0.1978573f, 0.172844f, 0.04932022f, 0.07387928f, 0f, 0.04910687f },
				new float[50] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.2106925f, 0.5591921f, 0.5947716f, 0.5670011f, 0.5475857f, 0.5051002f, 0.4016184f, 0.3265782f, 0.2304685f, 0.1905839f, 0.1377254f, 0.1031048f, 0.06696148f, 0.02119071f, 0.01139209f }
			},
			new float[4][] {
				new float[12] { 0f, 0.01470866f, 0.2668808f, 0.6709758f, 0.8696038f, 0.6238111f, 0.2982592f, 0.1819701f, 0.1314542f, 0.06732406f, 0.02134668f, 0.002193517f },
				new float[72] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.3047225f, 0.3182212f, 0.5082004f, 0.6551769f, 0.5977036f, 0.5544696f, 0.4903019f, 0.4404049f, 0.3140291f, 0.3691338f, 0.2502432f, 0.159578f, 0.04848383f, 0.02073469f, 0.006904373f },
				new float[175] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.1666749f, 0.3884387f, 0.2770164f, 0.5254929f, 0.7177687f, 0.3032225f, 0.5229709f, 0.1923689f, 0.5486845f, 0.2191968f, 0.2734259f, 0.2457759f, 0.354367f, 0.05441149f, 0.1086966f, 0.0813694f, 0.05420946f },
				new float[36] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.07025311f, 0.2891384f, 0.4695353f, 0.8894784f, 0.6768143f, 0.587148f, 0.4674066f, 0.3971567f, 0.3170993f, 0.1885206f, 0.1125487f, 0.04694382f, 0.005021037f }
			},
			new float[4][] {
				new float[12] { 0.009613713f, 0.1906747f, 0.6507455f, 0.8635021f, 0.6925584f, 0.3429887f, 0.2285819f, 0.1412279f, 0.06935507f, 0.03271831f, 0.01224353f, 0.00156687f },
				new float[50] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.02210077f, 0.2060175f, 0.4772561f, 0.6082825f, 0.8775923f, 1f, 0.8450313f, 0.6543332f, 0.4643365f, 0.3259043f, 0.1445349f, 0.07934242f, 0.007204685f },
				new float[120] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.1826799f, 0.4258f, 0.4857594f, 1f, 0.6956412f, 0.6643257f, 0.6630311f, 0.4513865f, 0.360483f, 0.4198555f, 0.1197096f, 0.1792739f, 0.11933f, 0f, 0f, 0.02969324f },
				new float[28] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.1610606f, 0.5619036f, 0.7142545f, 0.9916827f, 1f, 0.718495f, 0.448316f, 0.2595796f, 0.04602725f, 0.01360753f }
			},
			new float[4][] {
				new float[15] { 0.04263178f, 0.2895508f, 0.6382924f, 0.747534f, 0.6445036f, 0.4407095f, 0.2630998f, 0.157976f, 0.09709516f, 0.06700681f, 0.03422115f, 0.009398301f, 0.005941324f, 0.001247929f, 0.0006229539f },
				new float[54] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.08108839f, 0.2795981f, 0.3671861f, 0.3444276f, 0.526583f, 0.5328071f, 0.5682167f, 0.5524712f, 0.457112f, 0.3548413f, 0.3035131f, 0.1875393f, 0.2159518f, 0.107756f, 0.1147124f, 0.07157325f, 0.02855934f },
				new float[92] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.0959605f, 0.3513282f, 0.3825811f, 0.636355f, 0.3811103f, 0.6657124f, 0.6962913f, 0.7582012f, 0.504584f, 0.5665219f, 0.2827532f, 0.3136709f, 0.3130487f, 0.03126271f, 0.09355384f },
				new float[44] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.005174049f, 0.07228132f, 0.2130509f, 0.2674596f, 0.3507443f, 0.4132414f, 0.4498594f, 0.4217311f, 0.3818137f, 0.3624241f, 0.2856064f, 0.2833472f, 0.2490722f, 0.2032441f, 0.1558968f, 0.173982f, 0.1218763f, 0.1283033f, 0.06484295f, 0.05974399f, 0.03808018f, 0.03800479f, 0.01978673f, 0.02469312f }
			}
		};
	}
}
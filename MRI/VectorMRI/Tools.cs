using Corpus;

namespace MRI.VectorMRI;

public static class Tools {
	internal static float Norm(this IEnumerable<float> vector) =>
		(float)Math.Sqrt(vector.Select(t => Math.Pow(t, 2)).Sum());

	internal static float InverseProximity(this Corpus.Corpus corpus, Query query, string document) => 1 /
		(float)query.Proximity()
			.Select(proximitySet => corpus.Proximity(document, proximitySet, proximitySet.Count, PowGenerator(5, 13)))
			.Select(a => (int)Math.Log(a, 5)).Aggregate(1, (current, a) => current * a);

	private static IEnumerable<int> PowGenerator(int @base, int max) =>
		Enumerable.Range(0, max).Select(t => (int)Math.Pow(@base, t));
}
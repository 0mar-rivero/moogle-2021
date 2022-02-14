using Corpus;

namespace MRI.VectorMRI;

internal static class ProximityTools {
	internal static double Norm(this IEnumerable<double> vector) =>
		Math.Sqrt(vector.Select(t => Math.Pow(t, 2)).Sum());

	internal static double InverseProximity(this Corpus.Corpus corpus, Query query, string document) => 1 /
		(double)query.Proximity
			.Select(proximitySet => corpus.Proximity(document, proximitySet, proximitySet.Count, PowGenerator(2, 30)))
			.Select(a => (int)Math.Log(a, 2)).Aggregate(1, (current, a) => current * a);
			

	private static IEnumerable<int> PowGenerator(int @base, int max) =>
		Enumerable.Range(0, max).Select(t => (int)Math.Pow(@base, t));
}
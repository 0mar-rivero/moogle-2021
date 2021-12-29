using Corpus;

namespace MRI.VectorMRI;

public class VectorMri : MRI {
	public VectorMri(Corpus.Corpus corpus) : base(corpus) { }

	public override IEnumerable<(string document, float score)> Query(Query query) { //mejorar esta historia
		var docs = Corpus.Documents().Where(document => query.Inclusions().All(word => Corpus[document, word] != 0) &&
		                                                query.Exclusions().All(word => Corpus[document, word] == 0));
		var scores = docs.Select(document => (document, score: Similarity(query, document)));
		scores = scores.Select(t => (t.document, t.score * Corpus.InverseProximity(query, t.document)));
		return scores.ToList().OrderByDescending(t => t.score);
	}

	/// <summary>
	/// Calcula el peso de una palabra en un documento.
	/// </summary>
	/// <param name="word">Palabra</param>
	/// <param name="document">Documento</param>
	/// <returns>Peso</returns>
	private float CalculateW(string word, string document) => CalculateTf(word, document) * CalculateIdf(word);

	/// <summary>
	/// Calcula el peso de una palabra en la consulta.
	/// </summary>
	/// <param name="word">Palabra</param>
	/// <param name="query">Consulta</param>
	/// <param name="a">Suavizado</param>
	/// <returns>Peso</returns>
	private float CalculateWq(string word, Query query, float a = 0) =>
		CalculateIdf(word) * (a + (1 - a) * CalculateTf(word, query));

	/// <summary>
	/// Calcula la frecuencia normalizada de una palabra en un documento.
	/// </summary>
	/// <param name="word">Palabra</param>
	/// <param name="document">Documento</param>
	/// <returns></returns>
	private float CalculateTf(string word, string document) => (float)Freq(word, document) / MaxL(document);

	/// <summary>
	/// Calcula la frecuencia normalizada de una palabra en un la consulta.
	/// </summary>
	/// <param name="word">Palabra</param>
	/// <param name="query">Consulta</param>
	/// <returns>Frecuencia normalizada</returns>
	private float CalculateTf(string word, Query query) => (float)Freq(word, query) / MaxL(query);

	private float CalculateIdf(string word) =>
		Corpus[word] == 0 ? 0 : (float)Math.Log10((float)Corpus.DocsCount / Corpus[word]);

	private int MaxL(string document) => Corpus.MostRepeatedWordOccurrences(document);
	private static int MaxL(Query query) => query.MostRepeatedOccurrences;

	private int Freq(string word, string document) => Corpus[document, word];

	private static int Freq(string word, Query query) => query[word];

	private IEnumerable<float> Weights(string document) => Corpus.Words().Select(word => CalculateW(word, document));

	private IEnumerable<float> Weights(Query query) => Corpus.Words().Select(word => CalculateWq(word, query));

	private float Similarity(Query query, string document) {
		return Weights(query).Zip(Weights(document)).Select(t => t.First * t.Second).Sum() /
		       (Weights(query).Norm() * Weights(document).Norm());
	}
}
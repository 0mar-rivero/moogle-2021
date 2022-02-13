using Corpus;

namespace MRI.VectorMRI;

internal class QueryTfxIdf {
	private readonly Query _query;
	private readonly Corpus.Corpus _corpus;
	private readonly Dictionary<string, double> _weightsDictionary;
	public readonly double Norm;

	public QueryTfxIdf(Query query, Corpus.Corpus corpus) {
		_query = query;
		_corpus = corpus;
		_weightsDictionary = ProcessQuery();
		Norm = Math.Sqrt(_weightsDictionary.Values.Select(weight => Math.Pow(weight, 2)).Sum());
	}

	private Dictionary<string, double> ProcessQuery() => _corpus.Words().ToDictionary(word => word, CalculateWq);

	internal double this[string word] => _weightsDictionary.ContainsKey(word) ? _weightsDictionary[word] : 0;

	#region WeightsProcessing

	/// <summary>
	/// Calcula el peso de una palabra en la consulta.
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <returns>Peso.</returns>
	private double CalculateWq(string word) => CalculateTf(word) * CalculateIdf(word);

	/// <summary>
	/// Calcula la frecuencia normalizada de una palabra en un la consulta.
	/// </summary>
	/// <param name="word">Palabra</param>
	/// <returns>Frecuencia normalizada</returns>
	private double CalculateTf(string word) => (double)Freq(word) / _query.MostRepeatedOccurrences;

	private double CalculateIdf(string word) => Math.Log10((double)_corpus.DocsCount / _corpus[word]);

	private int Freq(string word) => _query[word];

	#endregion
}
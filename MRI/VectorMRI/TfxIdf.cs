using System.Text;
using System.Text.Json;

namespace MRI.VectorMRI;

public class TfxIdf {
	private readonly Dictionary<string, Dictionary<string, double>>? _weightsDictionary;
	private const string WeightsPath = "../Cache/TfxIdf.json";
	private const string NormsPath = "../Cache/Norms.json";
	private readonly Corpus.Corpus _corpus;
	private readonly Dictionary<string, double>? _norms;

	public TfxIdf(Corpus.Corpus corpus) {
		_corpus = corpus;
		try {
			if (corpus.Changed) throw new Exception();
			_weightsDictionary =
				JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, double>>>(
					File.ReadAllText(WeightsPath));
			_norms = JsonSerializer.Deserialize<Dictionary<string, double>>(File.ReadAllText(NormsPath));
		}
		catch {
			_weightsDictionary = ProcessTFxIdf();
			_norms = ProcessNorms();
			File.WriteAllText(WeightsPath,
				JsonSerializer.Serialize(_weightsDictionary));
			File.WriteAllText(NormsPath, JsonSerializer.Serialize(_norms));
		}
	}

	private Dictionary<string, Dictionary<string, double>> ProcessTFxIdf() {
		var tfxIdf = new Dictionary<string, Dictionary<string, double>>();
		foreach (var word in _corpus.Words) {
			tfxIdf[word] = new Dictionary<string, double>();
			foreach (var document in _corpus.GetDocuments(word))
				if (_corpus[document, word] != 0)
					tfxIdf[word][document] = CalculateW(word, document);
		}

		return tfxIdf;
	}

	private Dictionary<string, double> ProcessNorms() {
		var dic = _corpus.Documents.ToDictionary(document => document, _ => 0d);
		if (_weightsDictionary != null)
			foreach (var (_, documents) in _weightsDictionary) {
				foreach (var (document, score) in documents) {
					dic[document] += Math.Pow(score, 2);
				}
			}

		foreach (var (document, norm) in dic) {
			dic[document] = Math.Sqrt(norm);
		}

		return dic;
	}

	internal double this[string document, string word] =>
		_weightsDictionary != null && _weightsDictionary.ContainsKey(word) && _weightsDictionary[word].ContainsKey(document)
			? _weightsDictionary[word][document]
			: 0;

	/// <summary>
	/// Calcula el peso de una palabra en un documento.
	/// </summary>
	/// <param name="word">Palabra</param>
	/// <param name="document">Documento</param>
	/// <returns>Peso</returns>
	private double CalculateW(string word, string document) => CalculateTf(word, document) * CalculateIdf(word);

	/// <summary>
	/// Calcula la frecuencia normalizada de una palabra en un documento.
	/// </summary>
	/// <param name="word">Palabra</param>
	/// <param name="document">Documento</param>
	/// <returns></returns>
	private double CalculateTf(string word, string document) =>
		(double)Freq(word, document) / _corpus.MostRepeatedWordOccurrences(document);

	private double CalculateIdf(string word) => Math.Log10((double)_corpus.DocsCount / _corpus[word]);

	private int Freq(string word, string document) => _corpus[document, word];

	internal double Norm(string document) => _norms != null && _norms.ContainsKey(document) ? _norms[document] : 0;
}
using Corpus;
using Corpus.Tools;

namespace MRI.VectorMRI;

public class VectorMri : MRI {
	private readonly TfxIdf _tfxIdf;
	private QueryTfxIdf _queryTfxIdf = null!;

	public VectorMri(Corpus.Corpus corpus) : base(corpus) {
		_tfxIdf = new TfxIdf(corpus);
	}

	public override IEnumerable<(string document, double score)> Query(Query query) {
		_queryTfxIdf = new QueryTfxIdf(query, Corpus);
		return Corpus.Documents.Where(document => query.Inclusions.All(word => Corpus[document, word] is not 0) &&
		                                          query.Exclusions.All(word => Corpus[document, word] is 0))
			.Select(document => (document, score: Similarity(document) * Corpus.InverseProximity(query, document))).ToList()
			.OrderByDescending(t => t.score);
	}
	/// <summary>
	/// Sugiere una consulta nueva con buenos resultados de búsqueda.
	/// </summary>
	/// <param name="query">Consulta.</param>
	/// <returns>Una consulta con palabras cercanas a la consulta original con buenos resultados garantizados.</returns>
	public override string Suggestion(Query query) {
		var outPut = "";
		foreach (var (_, candidates) in query.SuggestionDictionary) {
			outPut += ' ';
			var max = double.MinValue;
			var bestWord = "";
			foreach (var (candidate, score) in candidates) {
				if (score * WordRelevance(candidate) <= max) continue;
				max = score * WordRelevance(candidate);
				bestWord = candidate;
			}

			outPut += bestWord;
		}

		return outPut.Trim();
	}
	/// <summary>
	/// Devuelve un valor que indica cúan bueno es buscar una palabra en el corpus mediante la duma de sus pesos en los documentos donde aparece.
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <returns>Suma de los pesos de la palabra en todos los documentos.</returns>
	private double WordRelevance(string word) => Corpus.GetDocuments(word).Sum(document => _tfxIdf[document, word]);

	/// <summary>
	/// Calcula el coseno del ángulo entre el vector del documento y el vector de la consulta.
	/// </summary>
	/// <param name="document">Documento.</param>
	/// <returns>Valor entre 0 y 1 que indica el grado de relevancia del documento para la consulta</returns>
	private double Similarity(string document) =>
		_queryTfxIdf.Weights.Select(word => word.weight * _tfxIdf[document, word.word]).Sum() /
		(_queryTfxIdf.Norm * _tfxIdf.Norm(document));
}
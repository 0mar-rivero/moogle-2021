using Corpus;

namespace MRI;
/// <summary>
/// Clase abstracta de la cuál deben heredar los modelos de recuperación de la información.
/// </summary>
public abstract class MRI {
	protected readonly Corpus.Corpus Corpus;

	protected MRI(Corpus.Corpus corpus) {
		Corpus = corpus;
	}
	public abstract IEnumerable<(string document, double score)> Query(Query query);

	public abstract string Suggestion(Query query);
}
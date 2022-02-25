using Corpus;

namespace MRI;

public abstract class MRI {
	protected readonly Corpus.Corpus Corpus;

	protected MRI(Corpus.Corpus corpus) {
		Corpus = corpus;
	}
	public abstract IEnumerable<(string document, double score)> Query(Query query);

	public abstract string Suggestion(Query query);
}
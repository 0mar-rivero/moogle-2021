using System.Collections;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace Corpus;

public abstract class Corpus {
	public int WordsCount;
	public int DocsCount;
	internal Dictionary<string, string> StemmerDictionary;
	internal HashSet<string> StopWords = new();

	protected Corpus() {
	}

	public abstract int this[string document, string word] { get; }
	public abstract int this[string word] { get; }
	protected abstract void ProcessCorpus();

	public abstract int MostRepeatedWordOccurrences(string document);

	public abstract IEnumerable<string> Words { get; }
	public abstract IEnumerable<string> Documents { get; }

	public abstract IEnumerable<string> GetDocuments(string word);

	public abstract int Proximity(string document, IEnumerable<string> words);

	public abstract string Snippet(string document, Query query);
}
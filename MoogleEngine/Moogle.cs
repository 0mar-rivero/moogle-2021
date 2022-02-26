using System.Runtime.InteropServices;
using System.Xml;
using Corpus;
using MRI;
using MRI.VectorMRI;

namespace MoogleEngine;

public static class Moogle {
	private static readonly Corpus.Corpus Corpus = new TestCorpus("../Content/");
	private static readonly VectorMri Mri = new(Corpus);
	public static SearchResult Query(string queryText) {
		var query = new Query(queryText, Corpus);
		var b = Mri.Query(query);
		var items = new List<SearchItem>();
		foreach (var (doc, ranking) in b.Take(10).Where(t=>t.Item2 is not (0 or double.NaN))) {
			items.Add(new SearchItem(new FileInfo(doc).Name ,Corpus.Snippet(doc, query), ranking));
		} 
		return new SearchResult(items.ToArray(), Mri.Suggestion(query));
	}
}
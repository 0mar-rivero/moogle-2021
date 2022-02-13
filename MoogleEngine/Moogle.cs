using System.Runtime.InteropServices;
using System.Xml;
using Corpus;
using MRI;
using MRI.VectorMRI;

namespace MoogleEngine;

public static class Moogle {
	private static readonly Corpus.Corpus Corpus = new TestCorpus("../Content/");
	private static readonly VectorMri Mri = new(Corpus);
	public static SearchResult Query(string query) {
		var b = Mri.Query(new Query(query, Corpus));
		var items = new List<SearchItem>();
		foreach (var (doc, ranking) in b.Take(10).Where(t=>t.Item2 is not (0 or double.NaN))) {
			items.Add(new SearchItem(new FileInfo(doc).Name ,"not implemented", ranking));
			Console.WriteLine(ranking);
		}

		Console.WriteLine();
		return new SearchResult(items.ToArray(), query);
	}
}
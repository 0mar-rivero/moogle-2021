﻿using Corpus;
using MRI.VectorMRI;

namespace MoogleEngine;

public static class Moogle {
	private static readonly Corpus.Corpus Corpus = new MoogleCorpus("../Content/");
	private static readonly VectorMri Mri = new(Corpus);
	public static SearchResult Query(string queryText) {
		var query = new Query(queryText, Corpus);
		var ranking = Mri.Query(query);
		var items = new List<SearchItem>();
		foreach (var (doc, score) in ranking.Take(10).Where(t=>t.Item2 is not (0 or double.NaN))) {
			items.Add(new SearchItem(new FileInfo(doc).Name[..^".txt".Length] ,Corpus.Snippet(doc, query), score));
		}
		return new SearchResult(items.ToArray(), Mri.Suggestion(query));
	}
}
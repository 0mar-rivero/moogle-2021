﻿using Corpus;
using Corpus.Tools;

namespace MRI.VectorMRI;

public class VectorMri : MRI {
	private readonly TfxIdf _tfxIdf;
	private QueryTfxIdf _queryTfxIdf;

	public VectorMri(Corpus.Corpus corpus) : base(corpus) {
		_tfxIdf = new TfxIdf(@"..\Cache\", corpus);
	}

	public override IEnumerable<(string document, double score)> Query(Query query) {
		_queryTfxIdf = new QueryTfxIdf(query, Corpus);
		return Corpus.Documents.Where(document => query.Inclusions.All(word => Corpus[document, word] != 0) &&
		                                          query.Exclusions.All(word => Corpus[document, word] == 0))
			.Select(document => (document, score: Similarity(document) * Corpus.InverseProximity(query, document))).ToList()
			.OrderByDescending(t => t.score);
	}

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

		return outPut;
	}

	private double WordRelevance(string word) => Corpus.GetDocuments(word).Sum(document => _tfxIdf[document, word]);

	private IEnumerable<double> DocWeights(string document) =>
		Corpus.Words.Select(word => _tfxIdf[document, word]);

	private IEnumerable<double> QueryWeights() => Corpus.Words.Select(word => _queryTfxIdf[word]);

	private double Similarity(string document) =>
		QueryWeights().Zip(DocWeights(document)).Select(t => t.First * t.Second).Sum() /
		(_queryTfxIdf.Norm * _tfxIdf.Norm(document));
}
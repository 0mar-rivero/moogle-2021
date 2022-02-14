using System.Collections;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Cryptography;
using Corpus.Tools;
using static Corpus.Tools.Tools;

namespace Corpus;

public class Query {
	private readonly Dictionary<string, double> _text;
	private readonly Dictionary<string, double> _expandedText;
	private readonly Corpus _corpus;
	public readonly HashSet<string> Exclusions;
	public readonly HashSet<string> Inclusions;
	public readonly HashSet<HashSet<string>> Proximity;
	public readonly double MostRepeatedOccurrences;

	public Query(string text, Corpus corpus) {
		_corpus = corpus;
		var rawText = text.ToLower().Split().ToList();
		Exclusions = new HashSet<string>();
		Inclusions = new HashSet<string>();
		Proximity = new HashSet<HashSet<string>>();
		_text = ProcessQuery(rawText);
		_expandedText = new Dictionary<string, double>();
		LevenshteinProcess();
		MostRepeatedOccurrences = _expandedText.Values.Max();
	}

	public double this[string word] {
		get => _expandedText.ContainsKey(word) ? _expandedText[word] : 0;
		private set => _expandedText[word] = value;
	}

	public IEnumerable<string> Words => _text.Keys;

	#region RawTextProcessing

	private Dictionary<string, double> ProcessQuery(List<string> rawText) {
		ProcessBinProximity(rawText);
		ProcessNonBinProximity(rawText);
		ProcessPriority(rawText);
		foreach (var word in rawText) {
			if (ToExclude(word)) Exclusions.Add(word.TrimPunctuation());
			if (ToInclude(word)) Inclusions.Add(word.TrimPunctuation());
		}
		
		var dic = new Dictionary<string, double>();
		foreach (var word in rawText.Select(Tools.Tools.TrimPunctuation).Where(word => !Exclusions.Contains(word) && word.Length > 1)) {
			if (!dic.ContainsKey(word)) dic[word] = 0;
			dic[word]++;
		}

		return dic;
	}

	private void ProcessBinProximity(IReadOnlyList<string> rawText) {
		for (var i = 1; i < rawText.Count - 1; i++) {
			if (rawText[i] != "~") continue;
			var previous = rawText[i - 1].TrimPunctuation();
			var next = rawText[i + 1].TrimPunctuation();
			if (previous is not "" && next is not "") Proximity.Add(new HashSet<string> { previous, next });
		}
	}

	private void ProcessNonBinProximity(IReadOnlyList<string> rawText) {
		var set = new HashSet<string>();
		for (var i = 1; i < rawText.Count - 1; i++) {
			if (rawText[i] is not "~~") continue;
			var previous = rawText[i - 1].TrimPunctuation();
			if (previous != "") set.Add(previous);
			for (var j = i; j < rawText.Count && rawText[j] is "~~"; j += 2) {
				i = j;
				var next = rawText[j + 1].TrimPunctuation();
				if (next is not "") set.Add(next);
			}

			if (set.Count > 1) Proximity.Add(set);
		}
	}

	private static void ProcessPriority(List<string> rawText) {
		var toAdd = new List<string>();
		foreach (var word in rawText) {
			for (var i = 0; i < word.Length; i++) {
				switch (word[i]) {
					case '^':
						continue;
					case '*':
						toAdd.Add(word.TrimPunctuation());
						continue;
				}

				break;
			}
		}

		rawText.InsertRange(rawText.Count, toAdd);
	}

	private static bool ToExclude(string word) => word.StartsWith('!');
	private static bool ToInclude(string word) => word.StartsWith('^');

	#endregion

	#region Turbio

	private void LevenshteinProcess() {
		foreach (var word1 in Words) {
			if (word1.Length <= 2 && _corpus.Words.Contains(word1)) {
				this[word1] = _text[word1];
				continue;
			}

			foreach (var word2 in _corpus.Words) {
				this[word2] += _text[word1] * LevenshteinFactor(word1, word2);
			}
		}
	}

	#endregion
}
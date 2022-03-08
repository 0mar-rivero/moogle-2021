using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Corpus.Tools;

namespace Corpus;

public class Query {
	private readonly Dictionary<string, double> _text;
	private readonly Dictionary<string, double> _expandedText;
	public readonly Dictionary<string, Dictionary<string, double>> SuggestionDictionary = new();
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
		StrongQueryProcess();
		MostRepeatedOccurrences = _expandedText.Count > 0 ? _expandedText.Values.Max() : 0;
	}

	public double this[string word] {
		get => _expandedText.ContainsKey(word) ? _expandedText[word] : 0;
		private set {
			if (value > double.Epsilon) _expandedText[word] = value;
		}
	}

	public IEnumerable<string> PrivateWords => _text.Keys;

	public IEnumerable<string> Words => _expandedText.Keys;


	#region RawTextProcessing

	private Dictionary<string, double> ProcessQuery(List<string> rawText) {
		foreach (var word in rawText.Where(word => word.TrimPunctuation() is not "")) {
			if (ToExclude(word)) Exclusions.Add(word.TrimPunctuation());
			if (ToInclude(word)) Inclusions.Add(word.TrimPunctuation());
		}
		ProcessBinProximity(rawText);
		ProcessNonBinProximity(rawText);

		var dic = new Dictionary<string, double>();
		foreach (var word in rawText.Select(Tools.Tools.TrimPunctuation)
			         .Where(word => word is not "" && !Exclusions.Contains(word))) {
			if (!dic.ContainsKey(word)) dic[word] = 0;
			dic[word]++;
		}

		ProcessPriority(rawText, dic);

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

	private static void ProcessPriority(List<string> rawText, IDictionary<string, double> text) {
		foreach (var word in rawText.Select(Tools.Tools.TrimPunctuation).Where(word => word is not ""))
			text[word] *= Math.Pow(Math.E, word.TakeWhile(t => t is '^' or '*').Count(t => t is '*'));
	}


	private static bool ToExclude(string word) => word.StartsWith('!');
	private static bool ToInclude(string word) => word.StartsWith('^');

	#endregion

	private void StrongQueryProcess() {
		foreach (var queryWord in PrivateWords) {
			SuggestionDictionary[queryWord] = new Dictionary<string, double>();
			if (_corpus.StopWords.Contains(queryWord)) {
				SuggestionDictionary[queryWord][queryWord] = 1;
				continue;
			}

			foreach (var corpusWord in _corpus.Words) {
				var proximity = Tools.Tools.WordProximity(queryWord, corpusWord, _corpus.StemmerDictionary);
				if (proximity is 0) continue;
				SuggestionDictionary[queryWord][corpusWord] = proximity;
				this[corpusWord] += _text[queryWord] * proximity;
			}
		}

		File.WriteAllText("../Cache/StemmerDictionary.json", JsonSerializer.Serialize(_corpus.StemmerDictionary));
	}
}
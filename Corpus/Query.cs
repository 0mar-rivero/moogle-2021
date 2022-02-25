using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Corpus.Tools;

namespace Corpus;

public class Query {
	private const double ThirdOfE = Math.E / 3;
	private const double TenthOfPi = Math.PI / 10;
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
		foreach (var word in rawText.Select(Tools.Tools.TrimPunctuation)
			         .Where(word => !Exclusions.Contains(word) && word.Length > 1)) {
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

	private void StrongQueryProcess() {
		foreach (var queryWord in Words) {
			SuggestionDictionary[queryWord] = new Dictionary<string, double>();
			foreach (var corpusWord in _corpus.Words) {
				var proximity = Tools.Tools.WordProximity(queryWord, corpusWord, _corpus.StemmerDictionary);
				if (proximity is 0) continue;
				SuggestionDictionary[queryWord][corpusWord] = proximity is 1? 1 : proximity;
				this[corpusWord] += _text[queryWord] * proximity;
			}
		}
		
		File.WriteAllText("../Cache/StemmerDictionary.json", JsonSerializer.Serialize(_corpus.StemmerDictionary));
	}

	#endregion
}
using System.Collections;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Cryptography;

namespace Corpus;

public class Query {
	private Dictionary<string, int> _text;
	private readonly HashSet<string> _exclusions;
	private readonly HashSet<string> _inclusions;
	private readonly HashSet<HashSet<string>> _proximity;
	public readonly int MostRepeatedOccurrences;

	public Query(string text) {
		var rawText = text.ToLower().Split().ToList();
		_exclusions = new HashSet<string>();
		_inclusions = new HashSet<string>();
		_proximity = new HashSet<HashSet<string>>();
		_text = new Dictionary<string, int>();
		ProcessQuery(rawText);
		MostRepeatedOccurrences = _text.Values.Max();
	}

	public int this[string word] => _text.ContainsKey(word) ? _text[word] : 0;
	public IEnumerable<string> Words() => _text.Keys;
	private void ProcessQuery(List<string> rawText) {
		ProcessBinProximity(rawText);
		ProcessNonBinProximity(rawText);
		ProcessPriority(rawText);
		foreach (var word in rawText) {
			if (ToExclude(word)) _exclusions.Add(word.TrimPunctuation());
			if (ToInclude(word)) _inclusions.Add(word.TrimPunctuation());
		}

		_text = rawText.Select(Tools.TrimPunctuation).Where(word => !Exclusions().Contains(word) && word.Length > 1)
			.ToDictionary(word => word, word => rawText.Count(i => i == word));
	}

	private void ProcessBinProximity(IReadOnlyList<string> rawText) {
		for (var i = 1; i < rawText.Count - 1; i++) {
			if (rawText[i] != "~") continue;
			var previous = rawText[i - 1].TrimPunctuation();
			var next = rawText[i + 1].TrimPunctuation();
			if (previous is not "" && next is not "") _proximity.Add(new HashSet<string> { previous, next });
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

			if (set.Count > 1) _proximity.Add(set);
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

	public IEnumerable<string> Inclusions() => _inclusions;
	public IEnumerable<string> Exclusions() => _exclusions;

	public IEnumerable<HashSet<string>> Proximity() => _proximity;
}
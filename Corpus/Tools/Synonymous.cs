using System.Text.Json;

namespace Corpus.Tools;

public static class Synonymous {
	private static Dictionary<string, string[]>? _synonymousMap;

	public static bool Syn(string word1, string word2) {
		if (_synonymousMap is null)
			try {
				_synonymousMap =
					JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText("../Data/Synonymous.json"));
			}
			catch {
				_synonymousMap = new Dictionary<string, string[]>();
			}
		if (!_synonymousMap!.ContainsKey(word1) || !_synonymousMap.ContainsKey(word2)) return false;
		return _synonymousMap[word1].Contains(word2);
	}
}
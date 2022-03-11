using System.Text.Json;

namespace Corpus.Tools;
/// <summary>
/// Clase encargada de determinar si dos palabras son sinónimos.
/// </summary>
public static class Synonymous {
	private static Dictionary<string, string[]>? _synonymousMap;

	/// <summary>
	/// Determina si dos palabras son sinónimos.
	/// </summary>
	/// <param name="word1">Palabra 1.</param>
	/// <param name="word2">Palabra 2.</param>
	/// <returns>true si la palabra 1 es sinónimo de la palabra 2, false en caso contrario.</returns>
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
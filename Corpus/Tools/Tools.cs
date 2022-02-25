namespace Corpus.Tools;

public static class Tools {
	private const double ThirdOfE = Math.E / 3;
	private const double TenthOfPi = Math.PI / 10;
	public static IEnumerable<(int index, T elem)> Enumerate<T>(this IEnumerable<T> collection) {
		var index = 0;
		foreach (var elem in collection)
			yield return (index++, elem);
	}

	#region Proximity

	/// <summary>
	/// Mezcla un diccionario de listas ordenadas de entero.
	/// </summary>
	/// <param name="indexDictionary">Diccionario de (palabra, lista)</param>
	/// <returns>Lista de tuplas ordenadas de la forma(word, index). Siendo word la palabra de ese índice.</returns>
	private static List<(string word, int index)> SortedMerge(this Dictionary<string, List<int>> indexDictionary) {
		var merged = new List<(string word, int index)>();
		var indexes = indexDictionary.Keys.ToDictionary(word => word, _ => 0);

		while (true) {
			var min = int.MaxValue;
			var minWord = "";
			foreach (var (word, index) in indexes) {
				if (index >= indexDictionary[word].Count) continue;
				if (indexDictionary[word][index] >= min) continue;
				min = indexDictionary[word][index];
				minWord = word;
			}

			if (min == int.MaxValue) break;
			merged.Add((minWord, min));
			indexes[minWord]++;
		}

		return merged;
	}

	public static int Proximity(Dictionary<string, List<int>> indexDictionary) {
		var indexes = indexDictionary.SortedMerge();
		indexes.TrimExcess();
		var count = indexDictionary.ToDictionary(t => t.Key, _ => 0);
		var left = 0;
		var right = -1;
		var tCount = 0;
		var min = int.MaxValue;
		var canMove = true;

		while (canMove) {
			canMove = false;
			while (tCount < count.Count && right < indexes.Count - 1) {
				canMove = true;
				right++;
				count[indexes[right].word]++;
				if (count[indexes[right].word] == 1) tCount++;
			}

			while (tCount == count.Count && left < right) {
				canMove = true;
				count[indexes[left].word]--;
				if (count[indexes[left].word] == 0) tCount--;
				min = Math.Min(indexes[right].index - indexes[left].index, min);
				left++;
			}
		}

		return min;
	}

	public static double InverseProximity(this Corpus corpus, Query query, string document) =>
		1 / (double)query.Proximity
			.Select(proximitySet => corpus.Proximity(document, proximitySet))
			.Select(a => (int)Math.Log(a, 5) + 1).Aggregate(1, (current, a) => current * a);

	#endregion

	#region Trimming

	/// <summary>
	/// Elimina de los extremos de una palabra todos los caracteres no alfanuméricos.
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <returns>Palabra con los extremos no alfanuméricos eliminados.</returns>
	public static string TrimPunctuation(this string word) {
		var first = 0;
		var last = word.Length;
		for (var i = 0; i < word.Length; i++) {
			if (!char.IsLetterOrDigit(word[i])) continue;
			first = i;
			break;
		}

		for (var i = word.Length - 1; i >= 0; i--) {
			if (!char.IsLetterOrDigit(word[i])) continue;
			last = i + 1;
			break;
		}

		return first < last ? word[first..last] : "";
	}

	/// <summary>
	/// Aplica la función TrimPunctuation a cada elemento de una colección.
	/// </summary>
	/// <param name="words">Colección de palabras sobre la cual aplicar TrimPunctuation.</param>
	/// <returns>Colección de palabras tras aplicar TrimPunctuation sobre ellas.</returns>
	public static IEnumerable<string> TrimPunctuation(this IEnumerable<string> words) => words.Select(TrimPunctuation);

	#endregion
	
	#region WordProximity

	public static double WordProximity(string word1, string word2, Dictionary<string, string> stemmer) {
		if (word1 == word2) return 1;
		if (word1.Stem(stemmer) == word2.Stem(stemmer)) return ThirdOfE;
		var levenshteinFactor = Levenshtein.LevenshteinFactor(word1, word2);
		if (levenshteinFactor > TenthOfPi) return levenshteinFactor;
		if (Synonymous.Syn(word1,word2)) return TenthOfPi;
		return 0;
	}

	#endregion
}
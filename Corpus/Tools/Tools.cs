﻿namespace Corpus.Tools;

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
		if (levenshteinFactor is not 0) return levenshteinFactor;
		if (Synonymous.Syn(word1, word2)) return TenthOfPi;
		return 0;
	}

	#endregion

	#region Snippet

	public static string Snippet(string document, Corpus corpus, Query query, int size) {
		var loadedDoc = File.ReadAllText(document).ToLower().Split().TrimPunctuation().ToArray();
		var processedDoc = PreProcessSnippet(loadedDoc, corpus, query);
		var acm = query.PrivateWords.Where(word => word.Length > 1 && !corpus.StopWords.Contains(word))
			.ToDictionary(word => word, _ => new Dictionary<double, int>());
		(int left, int right) best = (0, 0);
		var bestScore = double.MinValue;
		var canMove = true;
		var right = -1;
		var left = 0;
		while (canMove) {
			canMove = false;
			while (right < processedDoc.Count - 1 && processedDoc[right + 1].index - processedDoc[left].index <= size) {
				canMove = true;
				right++;
				foreach (var (word, score) in processedDoc[right].relevance) {
					if (!acm[word].ContainsKey(score)) acm[word][score] = 0;
					acm[word][score]++;
				}
			}

			if (canMove) {
				var current = (from relevance in acm.Values
					where relevance.Count != 0 && !relevance.All(t => t.Value is 0)
					select (from t in relevance where t.Value is not 0 select t.Key).Max()).Sum();
				if (current > bestScore) {
					bestScore = current;
					best = (processedDoc[left].index, processedDoc[right].index);
				}
			}

			if (left < right) {
				canMove = true;
				left++;
			}

			if (canMove || right >= processedDoc.Count - 1) continue;
			left = right;
			right++;
			canMove = true;
			foreach (var (_,dict) in acm) {
				foreach (var score in dict.Keys) {
					dict[score] = 0;
				}
			}
		}

		if (loadedDoc.Length < size) best = (0, loadedDoc.Length - 1);
		var toAdd = (size - (best.right - best.left)) / 2;
		best = (best.left - toAdd < 0 ? 0 : best.left - toAdd,
		best.right + toAdd >= loadedDoc.Length ? loadedDoc.Length - 1 : best.right + toAdd);
		return string.Join(" ", File.ReadAllText(document).Split()[best.left..(best.right + 1)]);
	}

	private static List<(int index, Dictionary<string, double> relevance)> PreProcessSnippet(string[] document,
		Corpus corpus, Query query) {
		var reloadedDoc = new List<(int index, Dictionary<string, double> relevance)>();
		for (var i = 0; i < document.Length; i++) {
			if (document[i].Length <= 1 || corpus.StopWords.Contains(document[i])) continue;
			foreach (var (word, candidates) in query.SuggestionDictionary) {
				if (!candidates.ContainsKey(document[i])) continue;
				if (reloadedDoc.Count == 0 || reloadedDoc[^1].index != i) {
					reloadedDoc.Add((i, new Dictionary<string, double>()));
				}

				reloadedDoc[^1].relevance[word] = candidates[document[i]] * (1 - corpus[word] / corpus.DocsCount);
			}
		}

		return reloadedDoc;
	}

	#endregion
}
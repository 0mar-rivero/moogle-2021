using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Runtime.Serialization;
using System.Text.Json;

namespace Corpus.Tools;

public static class Tools {
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
	public static List<(string word, int index)> SortedMerge(this Dictionary<string, List<int>> indexDictionary) {
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

	public static int Proximity(Dictionary<string, List<int>> indexDictionary, IEnumerable<int>? gaps, int minAmount) {
		var mask = indexDictionary.Keys.ToDictionary(word => word, _ => false);
		var indexes = indexDictionary.SortedMerge();
		indexes.TrimExcess();
		gaps ??= Enumerable.Range(0, indexes.Last().index);
		var end = indexes.Last().index;
		var minGap = int.MaxValue - end;
		var bestLength = int.MaxValue;
		var count = 0;
		for (var left = indexes.Count - minAmount + 1; left >= 0; left--) {
			for (var right = left;
			     right < indexes.Count && indexes[left].index + minGap > indexes[right].index;
			     right++) {
				var (word, index) = indexes[right];
				if (!mask[word]) count++;
				mask[word] = true;
				if (count != minAmount) continue;
				bestLength = index - indexes[left].index;
				foreach (var gap in gaps) {
					if (gap < bestLength) {
						minGap = gap;
						continue;
					}

					bestLength = gap;
					break;
				}

				break;
			}

			foreach (var wordKey in mask.Keys) mask[wordKey] = false;
			count = 0;
		}

		return bestLength;
	}

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

	#region Levenshtein

	private static double LevenshteinDistance(string word1, string word2, double max) {
		var matrix = new double[word2.Length + 1, word1.Length + 1];
		var mask = new bool[matrix.GetLength(0), matrix.GetLength(1)];
		mask[0, 0] = true;
		for (var col = 1; col < matrix.GetLength(1); col++) {
			var toPut = matrix[0, col - 1] + InCost(word1[col - 1]);
			if (toPut > max) break;
			matrix[0, col] = toPut;
			mask[0, col] = true;
		}

		for (var row = 1; row < matrix.GetLength(1); row++) {
			var toPut = matrix[row - 1, 0] + InCost(word2[row - 1]);
			if (toPut > max) break;
			matrix[row, 0] = toPut;
			mask[row, 0] = true;
		}

		Put(matrix, mask, word1, word2, 1, 1, max);
		Expand(matrix, mask, word1, word2, 0, 1, 1, max);

		return mask[word2.Length, word1.Length] ? matrix[word2.Length, word1.Length] : double.MaxValue;
	}

	private static void Expand(double[,] matrix, bool[,] mask, string word1, string word2, int dir, int row, int col,
		double maxDistance) {
		while (true) {
			if (dir <= 0 && row < matrix.GetLength(0) - 1) {
				Put(matrix, mask, word1, word2, row + 1, col, maxDistance);
				Expand(matrix, mask, word1, word2, -1, row + 1, col, maxDistance);
			}

			if (dir >= 0 && col < matrix.GetLength(1) - 1) {
				Put(matrix, mask, word1, word2, row, col + 1, maxDistance);
				Expand(matrix, mask, word1, word2, 1, row, col + 1, maxDistance);
			}

			if (dir == 0 && row < matrix.GetLength(0) - 1 && col < matrix.GetLength(1) - 1) {
				Put(matrix, mask, word1, word2, row + 1, col + 1, maxDistance);
				dir = 0;
				row += 1;
				col += 1;
				continue;
			}

			break;
		}
	}

	private static void Put(double[,] matrix, bool[,] mask, string word1, string word2, int row, int col,
		double max) {
		var replacing = mask[row - 1, col - 1]
			? matrix[row - 1, col - 1] + SusCost(word1[col - 1], word2[row - 1])
			: double.MaxValue;
		var deleting = mask[row, col - 1] ? matrix[row, col - 1] + InCost(word1[col - 1]) : double.MaxValue;
		var inserting = mask[row - 1, col] ? matrix[row - 1, col] + InCost(word2[row - 1]) : double.MaxValue;
		var min = Math.Min(replacing, Math.Min(deleting, inserting));
		if (min > max) return;
		matrix[row, col] = min;
		mask[row, col] = true;
	}

	private static double InCost(char character) {
		return 1;
	}

	private static double SusCost(char character1, char character2) {
		return character1 == character2 ? 0 : 1;
	}

	#endregion
}
using System.Text;

namespace Corpus.Tools; 

public class Levenshtein {
	public static double LevenshteinFactor(string word1, string word2) {
		if (word1.Length * 4d / 3 + .5 < word2.Length || word1.Length * 2d / 3 - .5 > word2.Length) return 0;
		if (!Levenshteable(word1, word2)) return 0;
		var levenshteinDistance = LevenshteinDistance(word1, word2, word1.Length / 3d);
		return levenshteinDistance is not double.NaN ? 1 - levenshteinDistance / word1.Length : 0;
	}

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

		for (var row = 1; row < matrix.GetLength(0); row++) {
			var toPut = matrix[row - 1, 0] + InCost(word2[row - 1]);
			if (toPut > max) break;
			matrix[row, 0] = toPut;
			mask[row, 0] = true;
		}

		var possible = true;
		for (var i = 1; i < Math.Min(matrix.GetLength(0), matrix.GetLength(1)) && possible; i++) {
			for (var j = i; j < matrix.GetLength(1); j++) {
				if (Put(matrix, mask, word1, word2, i, j, max)) possible = true;
			}

			for (var j = i + 1; j < matrix.GetLength(0); j++) {
				if (Put(matrix, mask, word1, word2, j, i, max)) possible = true;
			}
		}

		return mask[word2.Length, word1.Length] ? matrix[word2.Length, word1.Length] : double.NaN;
	}

	private static bool Put(double[,] matrix, bool[,] mask, string word1, string word2, int row, int col,
		double max) {
		var replacing = mask[row - 1, col - 1]
			? matrix[row - 1, col - 1] + SusCost(word1[col - 1], word2[row - 1])
			: double.MaxValue;
		var deleting = mask[row, col - 1] ? matrix[row, col - 1] + InCost(word1[col - 1]) : double.MaxValue;
		var inserting = mask[row - 1, col] ? matrix[row - 1, col] + InCost(word2[row - 1]) : double.MaxValue;
		var min = Math.Min(replacing, Math.Min(deleting, inserting));
		if (min > max) return false;
		matrix[row, col] = min;
		mask[row, col] = true;
		return true;
	}

	private static double InCost(char character) =>
		character switch {
			'h' => 0.5,
			_ => 1
		};

	private static double SusCost(char character1, char character2) {
		if (character1 == character2) return 0;
		character1 = character1.ToString().Normalize(NormalizationForm.FormD)[0];
		character2 = character2.ToString().Normalize(NormalizationForm.FormD)[0];
		if (character1 == character2) return 0.25;
		if (Set05.Contains((character1, character2)) || Set05.Contains((character2, character1))) return 0.5;
		if (Set075.Contains((character1, character2)) || Set075.Contains((character2, character1))) return 0.75;
		return 1;
	}

	private static bool Levenshteable(string word1, string word2) {
		var a = (word1[0] is 'h' ? word1[1] : word1[0]).ToString().Normalize(NormalizationForm.FormD)[0];
		var b = (word2[0] is 'h' ? word2[1] : word2[0]).ToString().Normalize(NormalizationForm.FormD)[0];
		return SusCost(a, b) <= 0.5;
	}

	private static readonly HashSet<(char, char)> Set05 = new()
		{ ('b', 'v'), ('c', 'k'), ('c', 's'), ('c', 'z'), ('c', 'q'), ('g', 'j'), ('k', 'q'), ('s', 'z') };

	private static readonly HashSet<(char, char)> Set075 = new() {
		('i', 'y'), ('r', 'l'), ('m', 'n'), ('a', 'e'), ('a', 'i'), ('a', 'o'), ('a', 'u'), ('e', 'i'), ('e', 'o'),
		('e', 'u'), ('i', 'o'), ('i', 'u'), ('o', 'u')
	};
}
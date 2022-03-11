using System.Text;

namespace Corpus.Tools; 
/// <summary>
/// Clase estática encargada del cálculo de la distancia de Levenshtein entre dos palabras.
/// </summary>
public static class Levenshtein {
	/// <summary>
	/// Cálcula la cercanía gráfica entre dos palabras.
	/// </summary>
	/// <param name="word1">Palabra 1.</param>
	/// <param name="word2">Palabra 2.</param>
	/// <returns>Número real entre 0 y 1 que reperesenta el grado de cercanía de las palabras.</returns>
	public static double LevenshteinFactor(string word1, string word2) {
		if (word1.Length <= 2 || word2.Length <= 2) return 0;
		if (word1.Length * 4d / 3 + .5 < word2.Length || word1.Length * 2d / 3 - .5 > word2.Length) return 0;
		if (!Levenshteable(word1, word2)) return 0;
		var levenshteinDistance = LevenshteinDistance(word1, word2, word1.Length / 3d);
		return levenshteinDistance is not double.NaN ? 1 - levenshteinDistance / word1.Length : 0;
	}
	/// <summary>
	/// Cálcula la distancia de Levenshetein entre dos palabras.
	/// </summary>
	/// <param name="word1">Palabra 1.</param>
	/// <param name="word2">Palabra 2.</param>
	/// <param name="max">Cota superior de la distancia.</param>
	/// <returns>La distancia de Levenshtein si esta es menor que max, en caso contrario 0.</returns>
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
	/// <summary>
	/// Rellena una casilla en la matriz de Levenshtein.
	/// </summary>
	/// <param name="matrix">Matriz de Levenshtein.</param>
	/// <param name="mask">Máscara del tamaño de la matriz de Levenshetin que indica cuales celdas fueron rellenadas.</param>
	/// <param name="word1">Palabra 1.</param>
	/// <param name="word2">Palabra 2.</param>
	/// <param name="row">Fila de la celda a rellenar.</param>
	/// <param name="col">Columna de la celda a rellenar.</param>
	/// <param name="max">Cota superior de la distancia.</param>
	/// <returns>true si fue posible rellenar la celda con un valor menor que max, false en caso contrario.</returns>
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

	/// <summary>
	/// Calcula el costo de insertar un caracter.
	/// </summary>
	/// <param name="character">Caracter a insertar.</param>
	/// <returns>Costo de insertar un caracter.</returns>
	private static double InCost(char character) =>
		character switch {
			'h' => 0.5,
			_ => 1
		};
	/// <summary>
	/// Calcula el costo de sustituir un caracter por otro.
	/// </summary>
	/// <param name="character1">Caracter a sustituir.</param>
	/// <param name="character2">Caracter por el cual sustituir.</param>
	/// <returns>Costo de realizar la sustitución.</returns>
	private static double SusCost(char character1, char character2) {
		if (character1 == character2) return 0;
		character1 = character1.ToString().Normalize(NormalizationForm.FormD)[0];
		character2 = character2.ToString().Normalize(NormalizationForm.FormD)[0];
		if (character1 == character2) return 0.25;
		if (Set05.Contains((character1, character2)) || Set05.Contains((character2, character1))) return 0.5;
		if (Set075.Contains((character1, character2)) || Set075.Contains((character2, character1))) return 0.75;
		return 1;
	}
	/// <summary>
	/// Deterermina si vale la pena calcular la distancia de Levenshtein entre dos palabras.
	/// </summary>
	/// <param name="word1">Palabra 1.</param>
	/// <param name="word2">Palabra 2.</param>
	/// <returns>true si vale la pena calcular la distancia de Levenshtein, false en caso contrario.</returns>
	private static bool Levenshteable(string word1, string word2) {
		var a = (word1[0] is 'h' ? word1[1] : word1[0]).ToString().Normalize(NormalizationForm.FormD)[0];
		var b = (word2[0] is 'h' ? word2[1] : word2[0]).ToString().Normalize(NormalizationForm.FormD)[0];
		return SusCost(a, b) <= 0.5;
	}
	/// <summary>
	/// Conjunto de sustituciones con costo 0.5.
	/// </summary>
	private static readonly HashSet<(char, char)> Set05 = new()
		{ ('b', 'v'), ('c', 'k'), ('c', 's'), ('c', 'z'), ('c', 'q'), ('g', 'j'), ('k', 'q'), ('s', 'z') };
	/// <summary>
	/// Conjunto de sustituciones con costo 0.75.
	/// </summary>
	private static readonly HashSet<(char, char)> Set075 = new() {
		('i', 'y'), ('r', 'l'), ('m', 'n'), ('a', 'e'), ('a', 'i'), ('a', 'o'), ('a', 'u'), ('e', 'i'), ('e', 'o'),
		('e', 'u'), ('i', 'o'), ('i', 'u'), ('o', 'u')
	};
}
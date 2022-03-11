using System.Text;
using SuffixDic = System.Collections.Generic.Dictionary<int, System.Collections.Generic.HashSet<string>>;

namespace Corpus.Tools;
/// <summary>
/// Clase encargada de extraer las raíces de las palabras.
/// </summary>
public static class Stemmer {
	/// <summary>
	/// Extrae la raíz de una palbra.
	/// </summary>
	/// <param name="word">Palabra a la cual se le va a extraer la raíz.</param>
	/// <param name="stemmerDictionary">Diccionario con las palabras previamente stemmiadas para no volver a hallar las raíces de ellas.</param>
	/// <returns>Raíz de la palabtra.</returns>
	public static string Stem(this string word, Dictionary<string, string> stemmerDictionary) {
		if (stemmerDictionary.ContainsKey(word)) return stemmerDictionary[word];
		var r1 = R1(word);
		var r2 = R2(word, r1);
		var rV = Rv(word);
		stemmerDictionary[word] = word.DeletePronoun(rV).VerbSuffixDeleting(rV).NonVerbSuffixDeleting(r1, r2).ResidualDeleting(rV)
			.AcuteAccentsDeleting();
		return stemmerDictionary[word];
	}
	/// <summary>
	/// Conjunto de las vocales en español.
	/// </summary>
	private static readonly HashSet<char> VowelList = new() { 'a', 'e', 'i', 'o', 'u', 'á', 'é', 'í', 'ó', 'ú', 'ü' };
	/// <summary>
	/// Conjunto de los pronombres enclíticos.
	/// </summary>
	private static readonly HashSet<string> Pronouns = new()
		{ "me", "se", "sela", "selo", "selas", "selos", "le", "les", "nos", "te", "la", "lo", "las", "los" };
	/// <summary>
	/// Conjunto de las terminaciones verbales.
	/// </summary>
	private static readonly HashSet<(string, string)> PrePronouns = new()
		{ ("iéndo", "iendo"), ("ándo", "ando"), ("ár", "ar"), ("ér", "er"), ("ír", "ir") };
	/// <summary>
	/// Determina la región R1 de la palabra.
	/// </summary>
	/// <param name="word">Palabra a hallarle la región.</param>
	/// <returns>Índice donde comienza la región R1.</returns>
	private static int R1(string word) {
		for (var i = 1; i < word.Length; i++) {
			if (word[i].IsVowel()) continue;
			if (!word[i - 1].IsVowel()) continue;
			return i;
		}

		return word.Length;
	}
	/// <summary>
	/// Determina la región R2 de la palabra.
	/// </summary>
	/// <param name="word">Palabra a hallarle la región.</param>
	/// <param name="r1">Región r1 de la palabra.</param>>
	/// <returns>Índice donde comienza la región R2.</returns>
	private static int R2(string word, int r1) {
		for (var i = r1 + 1; i < word.Length; i++) {
			if (word[i].IsVowel()) continue;
			if (!word[i - 1].IsVowel()) continue;
			return i;
		}

		return word.Length;
	}
	/// <summary>
	/// Halla la region RV de terminación verbal de una palabra.
	/// </summary>
	/// <param name="word">Palabra a hallarle la región de terminación verbal.</param>
	/// <returns>Índice donde comienza la terminación verbal de la palabra.</returns>
	private static int Rv(string word) {
		if (word.Length <= 3) return word.Length;

		if (!word[1].IsVowel())
			for (var i = 2; i < word.Length; i++)
				if (word[i].IsVowel())
					return i + 1;

		if (word[0].IsVowel() && word[1].IsVowel())
			for (var i = 2; i < word.Length; i++)
				if (!word[i].IsVowel())
					return i + 1;

		return 3;
	}
	/// <summary>
	/// Elimina los pronombres enclíticos.
	/// </summary>
	/// <param name="word">Palabra a eliminiarle el pronombre enclítico.</param>
	/// <param name="rv"></param>
	/// <returns>true si eliminó algún pronombre del final de la palabra.</returns>
	private static string DeletePronoun(this string word, int rv) {
		foreach (var pronoun in Pronouns.Where(pronoun =>
			         word.Length - pronoun.Length >= rv && word[^pronoun.Length..] == pronoun)) {
			foreach (var (prePronoun1, prePronoun2) in PrePronouns) {
				if (word.Length - pronoun.Length - prePronoun1.Length < rv) continue;
				var temp = word[^(pronoun.Length + prePronoun1.Length)..^pronoun.Length];
				if (temp != prePronoun1 && temp != prePronoun2) continue;
				return word[..^(pronoun.Length + prePronoun1.Length)] + prePronoun2;
			}

			if (word.Length - pronoun.Length - "yendo".Length >= rv &&
			    word[^(pronoun.Length + "yendo".Length)..^pronoun.Length] is "yendo" &&
			    word[^(pronoun.Length + "yendo".Length + 1)] is 'u')
				return word[..^pronoun.Length];
		}

		return word;
	}
	/// <summary>
	/// Elimina los morfemas no verbales de la palabra.
	/// </summary>
	/// <param name="word">Palabra a encontrarle la raíz.</param>
	/// <param name="r1">Región r1 de la palabra.</param>
	/// <param name="r2">Región r2 de la palabra.</param>
	/// <returns>Palabra con los sufijos no verbales eliminados.</returns>
	private static string NonVerbSuffixDeleting(this string word, int r1, int r2) => word.DeleteFirstSuffix(r2)
		.DeleteSecondSuffix(r2).DeleteLogiaSuffix(r2).DeleteUcionSuffix(r2).DeleteCionSuffix(r2)
		.DeleteEnciaSuffix(r2).DeleteAMenteSuffix(r1, r2).DeleteMenteSuffix(r2).DeleteIdadSuffix(r2).DeleteIvoSuffix(r2);
	/// <summary>
	/// Conjunto de sufijos nos verbales de primer nivel.
	/// </summary>
	private static readonly SuffixDic FirstLayerSuffixes =
		new() {
			{ 8, new() { "amientos", "imientos" } },
			{ 7, new() { "amiento", "imiento" } },
			{ 5, new() { "anzas", "ismos", "ables", "ibles", "istas" } },
			{ 4, new() { "anza", "icos", "icas", "ismo", "able", "ible", "ista", "osos", "osas" } },
			{ 3, new() { "ico", "ica", "oso", "osa" } },
		};
	/// <summary>
	/// Elimina los sufijos de primer nivel.
	/// </summary>
	/// <param name="word">Palabra a eliminarle el sufijo.</param>
	/// <param name="r2">Región r2 de la palabra.</param>
	/// <returns>Palabra con los sufijos de primer nivel eliminados.</returns>
	private static string DeleteFirstSuffix(this string word, int r2) =>
		TryDelete(ref word, r2, FirstLayerSuffixes) ? word : word;
	/// <summary>
	/// Conjunto de sufijos de segundo nivel.
	/// </summary>
	private static readonly SuffixDic SecondLayerSuffixes =
		new() {
			{ 7, new() { "aciones" } },
			{ 6, new() { "adoras", "adores", "ancias", "idores", "idoras" } },
			{ 5, new() { "adora", "ación", "antes", "ancia", "idora", "acion" } },
			{ 4, new() { "ador", "ante", "idor" } }
		};
	/// <summary>
	/// Elimina los sufijos de segundo nivel de una palabra.
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <param name="r2">Índice de inicio de la región r2 de la palabra.</param>
	/// <returns>Palabra con los sufijos de segundo nivel eliminados.</returns>
	private static string DeleteSecondSuffix(this string word, int r2) {
		return TryDelete(ref word, r2, SecondLayerSuffixes)
			? word[..^(word.Length - 2 >= r2 && word[^2..] is "ic" ? 2 : 0)]
			: word;
	}

	/// <summary>
	/// Conjunto de terminaciones "Logia"
	/// </summary>
	private static readonly SuffixDic LogiaSuffixes = new() {
		{ 6, new() { "logías", "logias" } },
		{ 5, new() { "logía", "logia" } }
	};
	/// <summary>
	/// Elimina las terminaciones del tipo "logia" del final de la palabra.
	/// </summary>
	/// <param name="word">Palabra</param>
	/// <param name="r2">Inicio de la región r2 en la palabra.</param>
	/// <returns>Palabra con el sufijo logia eliminado.</returns>
	private static string DeleteLogiaSuffix(this string word, int r2) =>
		TryDelete(ref word, r2, LogiaSuffixes) ? word + "log" : word;
	
	/// <summary>
	/// Conjunto de terminaciones del tipo "ucion"
	/// </summary>
	private static readonly SuffixDic UcionSuffixes = new() {
		{ 7, new() { "uciones" } },
		{ 5, new() { "ución", "ucion" } }
	};
	/// <summary>
	/// Sustituye la terminación del tipo "ucion" palabra por "u".
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <param name="r2">Inicio de la región r2 en la palabra.</param>
	/// <returns>Palabra con la terminación de tipo "ución" sustituida por "u"</returns>
	private static string DeleteUcionSuffix(this string word, int r2) =>
		TryDelete(ref word, r2, UcionSuffixes) ? word + 'u' : word;
	/// <summary>
	/// Conjunto con las terminaciones de tipo "cion"
	/// </summary>
	private static readonly SuffixDic CionSuffixes = new() {
		{ 5, new() { "ccion", "cción" } },
		{ 4, new() { "cion", "ción", "sion", "sión" } }
	};
	/// <summary>
	/// Elimina los sufijos de tipo "cion" del final de la palabra.
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <param name="r2">Inicio de la región r2 en la palabra.</param>
	/// <returns>Palabra con el sufijo "ción eliminado del final de esta."</returns>
	private static string DeleteCionSuffix(this string word, int r2) =>
		TryDelete(ref word, r2, CionSuffixes) ? word : word;
	/// <summary>
	/// Conjunto con las terminaciones de tipo "encia".
	/// </summary>
	private static readonly SuffixDic EnciaSuffixes = new() {
		{ 6, new() { "encias" } },
		{ 5, new() { "encia", "entes" } },
		{ 4, new() { "ente" } }
	};
	/// <summary>
	/// Elimina los sufijos de tipo "encia" del final de la palabra.
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <param name="r2">Inicio de la región r2 en la palabra.</param>
	/// <returns>Palabra con el prefijo de tipo "encia" eliminado.</returns>
	private static string DeleteEnciaSuffix(this string word, int r2) =>
		TryDelete(ref word, r2, EnciaSuffixes) ? word : word;
	/// <summary>
	/// Elimina el sufijo "amente" del final de una palabra. De poder eliminar los presufijos "iv", "ivat", "os", "ic" o "ad", también se eliminan
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <param name="r1">Inicio de la región r1 en la palabra.</param>
	/// <param name="r2">Inicio de la región r2 en la palabra.</param>
	/// <returns>Palabra con la terminación "amente" y sus posibles presufijos eliminados.</returns>
	private static string DeleteAMenteSuffix(this string word, int r1, int r2) =>
		word.Length - 6 >= r1 && word[^6..] is "amente"
			? word.Length - 8 >= r2 && word[^8..^2] is "iv"
				? word[..^(word.Length - 10 >= r2 && word[^10..^8] is "at" ? 10 : 8)]
				: word[..^(word[^8..^2] is "os" or "ic" or "ad" ? 8 : 6)]
			: word;
	/// <summary>
	/// Elimina el sufijo "mente" del final de una palabra. De ser posible eliminar los presufijos "ante", "able", "ible".
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <param name="r2">Inicio de la región r2 en la palabra.</param>
	/// <returns>Palabra con la terminación "mente" y sus posibles presufijos eliminados.</returns>
	private static string DeleteMenteSuffix(this string word, int r2) =>
		word.Length - 5 >= r2 && word[^5..] is "mente"
			? word[..^(word.Length - 9 >= r2 && word[^9..^5] is "ante" or "able" or "ible" ? 9 : 5)]
			: word;
	/// <summary>
	/// Conjunto con los sufijos de tipo "idad"
	/// </summary>
	private static readonly SuffixDic IdadSuffixes = new() {
		{ 6, new() { "idades" } },
		{ 4, new() { "idad" } }
	};
	/// <summary>
	/// Elimina el sufijo de tipo "idad". De ser posible eliminar los presufijos "abil", "ic" o "iv" también los elimina.
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <param name="r2">Inicio de la región r2 en la palabra.</param>
	/// <returns>La palabra con los sufijos de tipo "idad" y sus posibles presufijos del final de la palabra.</returns>
	private static string DeleteIdadSuffix(this string word, int r2) {
		if (!TryDelete(ref word, r2, IdadSuffixes)) return word;
		if (word.Length - 4 >= r2 && word[^4..] is "abil") return word[..^4];
		if (word.Length - 2 >= r2 && word[^2..] is "iv" or "ic") return word[..^2];

		return word;
	}
	/// <summary>
	/// Conjunto con los sufijos de tipo "ivo".
	/// </summary>
	private static readonly SuffixDic IvoSuffixes = new() {
		{ 4, new() { "ivas", "ivos" } },
		{ 3, new() { "iva", "ivo" } }
	};
	
	/// <summary>
	/// Elimina los sufijos de tipo "ivo" y de ser posible el presufijo "at".
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <param name="r2">Inicio de la región r2 en la palabra.</param>
	/// <returns>Palabra con el sufijo de tipo "ivo" y, de ser porible, el presufijo "at" eliminado.</returns>
	private static string DeleteIvoSuffix(this string word, int r2) =>
		TryDelete(ref word, r2, IvoSuffixes)
			? word[..^(word.Length - 2 >= r2 && word[^2..] is "at" ? 2 : 0)]
			: word;
	/// <summary>
	/// Elimina las terminaciones verbales del final de la palabra.
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <param name="rV">Inicio de la terminación verbal de la palabra.</param>
	/// <returns>Palabra con los morfemas verbales eliminados.</returns>
	private static string VerbSuffixDeleting(this string word, int rV) {
		return word.DeleteYVerbSuffix(rV).DeleteVerbSuffix(rV);
	}
	/// <summary>
	/// Conjunto de morfemas comenzados por "y".
	/// </summary>
	private static readonly SuffixDic YVerbSuffix = new() {
		{ 5, new() { "yeron", "yendo", "yamos" } },
		{ 4, new() { "yais", "yáis" } },
		{ 3, new() { "yan", "yen", "yas", "yes" } },
		{ 2, new() { "ya", "ye", "yo", "yó" } }
	};
	/// <summary>
	/// Conjunto de posibles morfemas precedidos por g.
	/// </summary>
	private static readonly SuffixDic GVerbSuffix = new() {
		{ 4, new() { "emos" } },
		{ 3, new() { "éis" } },
		{ 2, new() { "en", "es" } }
	};

	/// <summary>
	/// Conjunto de terminaciones verbales.
	/// </summary>
	private static readonly SuffixDic VerbSuffix = new() {
		{ 7, new() { "aríamos", "eríamos", "iríamos", "iéramos", "iésemos" } }, {
			6, new() {
				"aríais", "aremos", "asteis", "ábamos", "áramos", "ásemos", "eríais", "eremos", "iríais", "iremos", "ierais",
				"ieseis", "isteis"
			}
		}, {
			5, new() {
				"arían", "arías", "abais", "arais", "aseis", "erían", "erías", "eréis", "irían", "irías", "iréis", "ieran",
				"iesen", "ieron", "iendo", "ieras", "ieses", "íamos"
			}
		}, {
			4, new() {
				"arán", "arás", "aban", "aran", "asen", "aron", "aste", "ando", "abas", "adas", "aras", "ases", "ados", "amos",
				"erán", "erás", "ería", "irán", "irás", "iría", "iera", "iese", "iste", "idas", "íais", "idos", "imos"
			}
		}, {
			3, new() {
				"ará", "aré", "aba", "ada", "ara", "ase", "ado", "áis", "erá", "eré", "irá", "iré", "ida", "ían", "ido", "ías"
			}
		},
		{ 2, new() { "ad", "an", "ar", "as", "ed", "er", "ía", "id", "ió", "ir", "ís" } }
	};

	/// <summary>
	/// Elimina los sufijos verbales comenzados por "y" si están precedidos por "u".
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <param name="rV">Inicio de la terminación verbal de la palabra.</param>
	/// <returns>Palabra con el sufijo verbal comenzado por "y" eliminado en caso de estar precedido por "u".</returns>
	private static string DeleteYVerbSuffix(this string word, int rV) {
		foreach (var (length, suffixes) in YVerbSuffix) {
			if (word.Length - length < rV) continue;
			var temp = word[^length..];
			if (suffixes.All(suffix => temp != suffix)) continue;
			if (word[^(length + 1)] is 'u') return word[..^length];
			break;
		}

		return word;
	}
	/// <summary>
	/// ELimina los morfemas verbales del final de las palabras.
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <param name="rV">Inicio de la terminación verbal de la palabra.</param>
	/// <returns>Palabra con los morfemas verbales eliminados.</returns>
	private static string DeleteVerbSuffix(this string word, int rV) {
		if (TryDelete(ref word, rV, GVerbSuffix) && word.Length - 1 >= rV && word[^2..] is "gu") {
			word = word[..^1];
		}

		TryDelete(ref word, rV, VerbSuffix);
		return word;
	}
	/// <summary>
	/// Conjunto de morfemas vocálicos.
	/// </summary>
	private static readonly SuffixDic VowelSuffix = new() {
		{ 2, new() { "os", "al" } },
		{ 1, new() { "a", "o", "á", "í", "ó", "i" } }
	};

	/// <summary>
	/// Conjunto de morfemas vocálicos de tipo "e".
	/// </summary>
	private static readonly SuffixDic Vowel2Suffix = new() {
		{ 1, new() { "e", "é" } }
	};

	/// <summary>
	/// Elimina los morfemas vocálicos residuales de la palabra. En caso de este morfema sea de "e" o "é" se elimina en caso de ser posible el submorfema "gu".
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <param name="rV">Inicio de la terminación verbal de la palabra.</param>
	/// <returns>Palabra con los morfemas vocálicos residuales elminados.</returns>
	private static string ResidualDeleting(this string word, int rV) {
		TryDelete(ref word, rV, VowelSuffix);
		return !TryDelete(ref word, rV, Vowel2Suffix)
			? word
			: word[..^(word.Length - 1 >= rV && word[^2..] is "gu" ? 1 : 0)];
	}

	/// <summary>
	/// Elimina los acentos de la palabra.
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <returns>Palabra con los acentos eliminados.</returns>
	private static string AcuteAccentsDeleting(this string word) =>
		string.Join("", word.Normalize(NormalizationForm.FormD).Replace("n~", "ñ").Where(char.IsLetterOrDigit));

	/// <summary>
	/// Determina si un caracter es vocal.
	/// </summary>
	/// <param name="character">Caracter.</param>
	/// <returns>true si el caracter es vocálico, false en caso contrario.</returns>
	private static bool IsVowel(this char character) => VowelList.Contains(character);

	/// <summary>
	/// Intenta eliminar alguno de los sufijos en el diccionario de sufijos de la palabra.
	/// </summary>
	/// <param name="word">Palabra.</param>
	/// <param name="region">Región admisible para elminar sufijos.</param>
	/// <param name="suffixDic">Diccionario de sufijos a eliminar.</param>
	/// <returns>true si logró eliminar alguno de los sufijos del diccionario, false en caso contrario.</returns>
	private static bool TryDelete(ref string word, int region, SuffixDic suffixDic) {
		foreach (var (length, suffixes) in suffixDic) {
			if (word.Length - length < region) continue;
			var temp = word[^length..];
			if (suffixes.All(suffix => temp != suffix)) continue;
			word = word[..^length];
			return true;
		}

		return false;
	}
}
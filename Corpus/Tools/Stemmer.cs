using System.Text;
using SuffixDic = System.Collections.Generic.Dictionary<int, System.Collections.Generic.HashSet<string>>;

namespace Corpus.Tools;

public static class Stemmer {
	public static string Stem(this string word, Dictionary<string, string> stemmerDictionary) {
		if (stemmerDictionary.ContainsKey(word)) return stemmerDictionary[word];
		var r1 = R1(word);
		var r2 = R2(word, r1);
		var rV = Rv(word);
		stemmerDictionary[word] = word.DeletePronoun(rV).VerbSuffixDeleting(rV).NonVerbSuffixDeleting(r1, r2).ResidualDeleting(rV)
			.AcuteAccentsDeleting();
		return stemmerDictionary[word];
	}

	private static readonly HashSet<char> VowelList = new() { 'a', 'e', 'i', 'o', 'u', 'á', 'é', 'í', 'ó', 'ú', 'ü' };

	private static readonly HashSet<string> Pronouns = new()
		{ "me", "se", "sela", "selo", "selas", "selos", "le", "les", "nos", "te", "la", "lo", "las", "los" };

	private static readonly HashSet<(string, string)> PrePronouns = new()
		{ ("iéndo", "iendo"), ("ándo", "ando"), ("ár", "ar"), ("ér", "er"), ("ír", "ir") };

	private static int R1(string word) {
		for (var i = 1; i < word.Length; i++) {
			if (word[i].IsVowel()) continue;
			if (!word[i - 1].IsVowel()) continue;
			return i;
		}

		return word.Length;
	}

	private static int R2(string word, int r1) {
		for (var i = r1 + 1; i < word.Length; i++) {
			if (word[i].IsVowel()) continue;
			if (!word[i - 1].IsVowel()) continue;
			return i;
		}

		return word.Length;
	}

	private static int Rv(string word) {
		if (word.Length <= 3) return word.Length;

		if (word[1].IsNotVowel())
			for (var i = 2; i < word.Length; i++)
				if (word[i].IsVowel())
					return i + 1;

		if (word[0].IsVowel() && word[1].IsVowel())
			for (var i = 2; i < word.Length; i++)
				if (word[i].IsNotVowel())
					return i + 1;

		return 3;
	}

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

	private static string NonVerbSuffixDeleting(this string word, int r1, int r2) => word.DeleteFirstSuffix(r2)
		.DeleteSecondSuffix(r2).DeleteLogiaSuffix(r2).DeleteUcionSuffix(r2).DeleteCionSuffix(r2)
		.DeleteEnciaSuffix(r2).DeleteAMenteSuffix(r1, r2).DeleteMenteSuffix(r2).DeleteIdadSuffix(r2).DeleteIvoSuffix(r2);

	private static readonly SuffixDic FirstLayerSuffixes =
		new() {
			{ 8, new() { "amientos", "imientos" } },
			{ 7, new() { "amiento", "imiento" } },
			{ 5, new() { "anzas", "ismos", "ables", "ibles", "istas" } },
			{ 4, new() { "anza", "icos", "icas", "ismo", "able", "ible", "ista", "osos", "osas" } },
			{ 3, new() { "ico", "ica", "oso", "osa" } },
		};

	private static string DeleteFirstSuffix(this string word, int r2) =>
		TryDelete(ref word, r2, FirstLayerSuffixes) ? word : word;

	private static readonly SuffixDic SecondLayerSuffixes =
		new() {
			{ 7, new() { "aciones" } },
			{ 6, new() { "adoras", "adores", "ancias", "idores", "idoras" } },
			{ 5, new() { "adora", "ación", "antes", "ancia", "idora", "acion" } },
			{ 4, new() { "ador", "ante", "idor" } }
		};

	private static string DeleteSecondSuffix(this string word, int r2) {
		return TryDelete(ref word, r2, SecondLayerSuffixes)
			? word[..^(word.Length - 2 >= r2 && word[^2..] is "ic" ? 2 : 0)]
			: word;
	}


	private static readonly SuffixDic LogiaSuffixes = new() {
		{ 6, new() { "logías", "logias" } },
		{ 5, new() { "logía", "logia" } }
	};

	private static string DeleteLogiaSuffix(this string word, int r2) =>
		TryDelete(ref word, r2, LogiaSuffixes) ? word + "log" : word;

	private static readonly SuffixDic UcionSuffixes = new() {
		{ 7, new() { "uciones" } },
		{ 5, new() { "ución", "ucion" } }
	};

	private static string DeleteUcionSuffix(this string word, int r2) =>
		TryDelete(ref word, r2, UcionSuffixes) ? word + 'u' : word;

	private static readonly SuffixDic CionSuffixes = new() {
		{ 5, new() { "ccion", "cción" } },
		{ 4, new() { "cion", "ción", "sion", "sión" } }
	};

	private static string DeleteCionSuffix(this string word, int r2) =>
		TryDelete(ref word, r2, CionSuffixes) ? word : word;

	private static readonly SuffixDic EnciaSuffixes = new() {
		{ 6, new() { "encias" } },
		{ 5, new() { "encia", "entes" } },
		{ 4, new() { "ente" } }
	};

	private static string DeleteEnciaSuffix(this string word, int r2) =>
		TryDelete(ref word, r2, EnciaSuffixes) ? word : word;

	private static string DeleteAMenteSuffix(this string word, int r1, int r2) =>
		word.Length - 6 >= r1 && word[^6..] is "amente"
			? word.Length - 8 >= r2 && word[^8..^2] is "iv"
				? word[..^(word.Length - 10 >= r2 && word[^10..^8] is "at" ? 10 : 8)]
				: word[..^(word[^8..^2] is "os" or "ic" or "ad" ? 8 : 6)]
			: word;

	private static string DeleteMenteSuffix(this string word, int r2) =>
		word.Length - 5 >= r2 && word[^5..] is "mente"
			? word[..^(word.Length - 9 >= r2 && word[^9..^5] is "ante" or "able" or "ible" ? 9 : 5)]
			: word;

	private static readonly SuffixDic IdadSuffixes = new() {
		{ 6, new() { "idades" } },
		{ 4, new() { "idad" } }
	};

	private static string DeleteIdadSuffix(this string word, int r2) {
		if (!TryDelete(ref word, r2, IdadSuffixes)) return word;
		if (word.Length - 4 >= r2 && word[^4..] is "abil") return word[..^4];
		if (word.Length - 2 >= r2 && word[^2..] is "iv" or "ic") return word[..^2];

		return word;
	}

	private static readonly SuffixDic IvoSuffixes = new() {
		{ 4, new() { "ivas", "ivos" } },
		{ 3, new() { "iva", "ivo" } }
	};

	private static string DeleteIvoSuffix(this string word, int r2) =>
		TryDelete(ref word, r2, IvoSuffixes)
			? word[..^(word.Length - 2 >= r2 && word[^2..] is "at" ? 2 : 0)]
			: word;

	private static string VerbSuffixDeleting(this string word, int rV) {
		return word.DeleteYVerbSuffix(rV).DeleteVerbSuffix(rV);
	}

	private static readonly SuffixDic YVerbSuffix = new() {
		{ 5, new() { "yeron", "yendo", "yamos" } },
		{ 4, new() { "yais", "yáis" } },
		{ 3, new() { "yan", "yen", "yas", "yes" } },
		{ 2, new() { "ya", "ye", "yo", "yó" } }
	};

	private static readonly SuffixDic GVerbSuffix = new() {
		{ 4, new() { "emos" } },
		{ 3, new() { "éis" } },
		{ 2, new() { "en", "es" } }
	};

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

	private static string DeleteYVerbSuffix(this string word, int rV) {
		foreach (var (length, suffixes) in YVerbSuffix) {
			if (word.Length - length < rV) continue;
			var temp = word[^length..];
			if (suffixes.All(suffix => temp != suffix)) continue;
			if (word[^(length + 1)] == 'u') return word[..^length];
			break;
		}

		return word;
	}

	private static string DeleteVerbSuffix(this string word, int rV) {
		if (TryDelete(ref word, rV, GVerbSuffix) && word.Length - 1 >= rV && word[^2..] is "gu") {
			word = word[..^1];
		}

		TryDelete(ref word, rV, VerbSuffix);
		return word;
	}

	private static readonly SuffixDic VowelSuffix = new() {
		{ 2, new() { "os", "al" } },
		{ 1, new() { "a", "o", "á", "í", "ó", "i" } }
	};

	private static readonly SuffixDic Vowel2Suffix = new() {
		{ 1, new() { "e", "é" } }
	};

	private static string ResidualDeleting(this string word, int rV) {
		TryDelete(ref word, rV, VowelSuffix);
		return !TryDelete(ref word, rV, Vowel2Suffix)
			? word
			: word[..^(word.Length - 1 >= rV && word[^2..] is "gu" ? 1 : 0)];
	}

	private static string AcuteAccentsDeleting(this string word) =>
		string.Join("", word.Normalize(NormalizationForm.FormD).Replace("n~", "ñ").Where(char.IsLetterOrDigit));

	private static bool IsVowel(this char character) => VowelList.Contains(character);

	private static bool IsNotVowel(this char character) => !VowelList.Contains(character);

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
namespace Corpus;
/// <summary>
/// Clase abstracta de la cual deben heredar las clases encargadas de procesar los documentos en un corpus.
/// </summary>
public abstract class Corpus {
	public int WordsCount;
	public int DocsCount;
	internal Dictionary<string, string> StemmerDictionary = new();
	internal readonly HashSet<string> StopWords = new();
	public bool Changed = false;
	/// <summary>
	/// Cantidad de veces que aparece una palabra en un documento.
	/// </summary>
	/// <param name="document">Documento.</param>
	/// <param name="word">Palabra.</param>
	public abstract int this[string document, string word] { get; }
	/// <summary>
	/// Cantidad de documentos en los que aparece una palabra.
	/// </summary>
	/// <param name="word">Palabra.</param>
	public abstract int this[string word] { get; }
	protected abstract void ProcessCorpus();
	/// <summary>
	/// Calcula la cantidad de veces que se repite la palabra que más aparece en un documento.
	/// </summary>
	/// <param name="document">Documento.</param>
	/// <returns>Cantidad de veces que se repite la palabra de más ocurrencias.</returns>
	public abstract int MostRepeatedWordOccurrences(string document);
	/// <summary>
	/// Todas las palabras procesadas.
	/// </summary>
	public abstract IEnumerable<string> Words { get; }
	/// <summary>
	/// Todos los documentos procesados.
	/// </summary>
	public abstract IEnumerable<string> Documents { get; }
	/// <summary>
	/// Retorna todos los documentos en los que aparece una palabra.
	/// </summary>
	/// <param name="word">Palabra</param>
	/// <returns>Todos los documentos en los que aparece la palabra.</returns>
	public abstract IEnumerable<string> GetDocuments(string word);
	/// <summary>
	/// Calcula la longitud del menor intervalo que contiene un grupo de palabras en un documento dado.
	/// </summary>
	/// <param name="document">Documento.</param>
	/// <param name="words">Colección de palabras que deben estar contenidas en el intervalo.</param>
	/// <returns>Longitud del menor intervalo que contiene todas las palabras de la colección.</returns>
	public abstract int Proximity(string document, IEnumerable<string> words);
	/// <summary>
	/// Encuentra un fragmento del documento de longitud máxima 50 palabras relevante para la consulta.
	/// </summary>
	/// <param name="document">Documento.</param>
	/// <param name="query">Consulta.</param>
	/// <returns>Un string con un fragmento de texto relevante para la consulta en el documento.</returns>
	public abstract string Snippet(string document, Query query);
}
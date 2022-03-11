# Moogle!

> Proyecto de Programación I. Facultad de Matemática y Computación. Universidad de La Habana. Curso 2021.

> Omar Rivero Gómez-Wangüemert. C-111

Moogle! es un motor de búsqueda diseñado para la recuperación de información relacionada con una consulta en un conjunto de documentos. Para su funcionamiento fue seleccionado el modelo vectorial de recuperación de información por su capacidad para ofrecer, no solo resultados relevantes, sino evaluados cuantitativamente en dependencia de su grado de similitud con la consulta. Esta evaluación permite la disposición de los resultados obtenidos en orden de relevancia.

Entre sus funcionalidades Moogle incluye para cada consulta una sugerencia, un fragmento de texto relevante por cada documento devuelto y la posibilidad de aumentar la precisión de la búsqueda mediante el uso de operadores.

### Operadores

- `~`: Los documentos en los que las palabras adyacentes al operador aparezcan más cercanas deben ser priorizados.

- `~~`: Los documentos en los que las palabras consecutivas separadas por el operador aparezcan más cercanas deben ser priorizados.

- `*`: La cantidad de apariciones de las palabra precedidas por *n* `*`s debe considerase *eⁿ*

- `^`: Los documentos que resulten en la búsqueda deben contener las palabras precedidas por `^`.

- `!`: No puede resultar de la búsqueda ningún documento que contenga una palabra precedidad por `!`.


## Funcionamiento del modelo vectorial

Para evaluar cada uno de los documentos y producir un valor que represente su relevancia para la consulta se le asigna a cada uno un vector del primer cuadrante del espacio euclídeo *n*-dimensional, donde *n* es la cantidad de palabras diferentes en el sistema de documentos. Se reserva una dimensión para cada una de las palabras y en la componente de cada vector relativa a cada palabra se asigna un número real positivo que represente la importancia de dicha palabra en el documento que repesenta dicho vector. Luego, tratando el texto de la consulta como otro documento se le asigna a esta un vector del espacio. Finalmente, y aprovechando la monotonía decreciente del coseno en el primer cuadrante, se le asigna a cada documento el coseno del ángulo que forman su vector y el vector consulta, de modo que los documentos con mejor puntuación en la búsqueda sean aquellos cuyos vectores estén mas cercanos angularmente al vector de la consulta.

### Cálculo de relevancia de una palabra en un documento (TFxIDF)

Con el objetivo de determinar el peso de una palabra en un documento se halla el producto de su relevancia en el documento y su relevancia en sistema de documentos. La relevancia en el documento está determinada por el cociente de su frecuencia y la del vocablo más común. La relevancia en el sistema de documentos está dada por el logaritmo decimal del cociente de la cantidad de documentos totales y la cantidad de documentos que contienen la palabra; de modo que la rareza de las palabras en el sistema sea bonificada.

## Funcionamiento e implementación
Una vez comienza la ejecución se revisa si el caché de ejecuciones anteriores se corresponde con los documentos actuales y de ser así se carga desde ahi la información relativa a los documentos. En caso contrario comienza el procesamiento de los documentos. Para esto se le asigna a cada par documento-palabra una lista con los índices de la palabra en el documento. También, se guarda para cada documento el número de veces que se repite la palabra que más se repite. Luego, a cada uno de estos pares se les asigna su correspondiente peso o TFxIDF a la vez que se calcula la norma de cada uno de los vectores documento.

Una vez es introducida la consulta esta es procesada para aplicar y eliminar los operadores; enriquecerla para mejorar los resultados que ofrezca y generar información útil para la posterior selección de la sugerencias y del fragmento de texto relevante por cada documento. Posteriormente, se halla TFxIDF a cada una de las palabras de la consulta enriquecida.

Luego se obtiene la puntuación de cada documento mediante el cálculo del coseno y se modifica según el operador de cercanía. Acorde a esta puntuación los documentos son ordenados y dispuestos en la página, obviando aquellos que no contengan las palabras forzadas por el operador de inclusión o incluyan palabras marcadas con el operador de exclusión.
Por cada documento se muestra su nombre y un fragmento o snippet relevante a la búsqueda. Para seleccionar este se busca en el documento la ventana de 50 palabras que más "cercana" esté a la consulta. Además, se realiza la sugerencia de una posible consulta con palabras del sistema de documentos cercanas a las introducidas por el usuario.

### Procesamiento del sistema de documentos

El procesamiento de los documento es llevado a cabo por la clase `MoogleCorpus : Corpus`. Su constructor recibe la dirección de un directorio y llama al método void `ProcessCorpus()`. Este recorre cada uno de los archivos con extensión `.txt` en la carpeta y rellena un diccionario `Dictionary<string, Dictionary<string, List<int>>>` donde a cada palabra se le asigna un diccionario cuyas llaves son los documentos en donde aparece; y sus valores, los índices de la palabra en el documento. Una vez recorridos todos los documentos, se calcula para cada uno de estos cual es la cantidad de veces que aparece la palabra que más ocurre, sin tener en cuenta aquellas de una sola letra o que aparezcan en casi todos los documentos.

Los objetos de esta clase son indizables de modo que `Corpus[string word, string document]` retorna el número de ocurrencias de la palabra `word` en el documento `document` y `Corpus[string word]` la cantidad de documentos en los que aparece la palabra `word`.

### Cálculo de TFxIDF

El cálculo del TFxIDF es llevado a cabo por la clase `TfxIdf`. Su constructor recibe una instancia de la clase `Corpus` y llama al método `ProcessTFxIdf()` que devuelve un diccionario `Dictionary<string, Dictionary<string, double>>` donde a cada palabra se le asigna un diccionario con los documentos en los que aparece y su peso en estos. También se encarga de crear y almacenar un diccionario que para cada documento almacena la norma de su vector.

```c#
/// <summary>
/// Recorre todo el corpus y crea un diccionario que guarda para cada palabra un diccionario con sus pesos en los documentos.
/// </summary>
/// <returns>Diccionario con los pesos de cada palabra en cada documento</returns>
private Dictionary<string, Dictionary<string, double>> ProcessTFxIdf() {
	var tfxIdf = new Dictionary<string, Dictionary<string, double>>();
	foreach (var word in _corpus.Words) {
		tfxIdf[word] = new Dictionary<string, double>();
		foreach (var document in _corpus.GetDocuments(word))
			if (_corpus[document, word] != 0)
				tfxIdf[word][document] = CalculateW(word, document);
	}

	return tfxIdf;
}
```

Los objetos de la clase `TfxIdf` son indizables y `TdxIdf[string word, string document]` retorna el peso de la palabra `word` en el documento `document`.

### Procesamiento de la consulta

El procesamiento de la consulta es llevado a cabo por la clase `Query`. Esta se construye con los parámetros `text`(texto introducido por el usuario en la búsqueda) y `corpus`(sistema de documentos previamente procesados). La primera étapa del análisis consiste en convertir todo el texto a minúsculas y guardarlo en una lista de palabras. Seguidamamente se realiza el primer procesado de la consulta. Para este se busca la presencia de los operadores y de existir estos se aplican.

Para los operadores `!` y `^` se buscan las palabras precedidas de los mismos y se incluyen en los `HashSet<string>` de las propiedades `Exclusions` e `Inclusions` respectivamente. En el caso de `*` se buscan las palabras que comiencen con este y en vez de contar esa aparición de las mismas como una, se cuenta como *eⁿ* donde *n* es la cantidad de veces que se usó el operador. Para los operadores `~` y `~~` se incluyen las palabras que deban estar cercanas en un mismo `HashSet<string>` y luego todos estos conjuntos son agrupados en el `HashSet<HashSet<string>>` de la propiedad `Proximity`.

Luego, se genera un diccionario donde a cada palabra de la consulta se le asigna la cantidad de veces que apareció en la misma y se reprocesa la consulta, añadiendo palabras de los documentos que sean "similares" a la palabras originales de la misma (estas palabras son añadidas con una frecuencia que depende del grado de similaridad), al mismo tiempo que estas palabras "similares" son guardadas en un diccionario para su psterior uso en la sugerencia y el snippet.

La clase `Query` es indizable y `Query[string word]` retorna el número de apariciones de `word` en la consulta enriquecida (este no es necesariamente natural).

```c#
/// <summary>
/// Rellena la consulta con las palabras del corpus cercanas a cada una de las palabras de la consulta original. 
/// </summary>
private void StrongQueryProcess() {
	foreach (var queryWord in _text.Keys) {
		SuggestionDictionary[queryWord] = new Dictionary<string, double>();
		if (_corpus.StopWords.Contains(queryWord)) {
			SuggestionDictionary[queryWord][queryWord] = 1;
			continue;
		}

		foreach (var corpusWord in _corpus.Words) {
			var proximity = Tools.Tools.WordProximity(queryWord, corpusWord, _corpus.StemmerDictionary);
			if (proximity is 0) continue;
			SuggestionDictionary[queryWord][corpusWord] = proximity;
			this[corpusWord] += _text[queryWord] * proximity;
		}
	}

	File.WriteAllText("../Cache/StemmerDictionary.json", JsonSerializer.Serialize(_corpus.StemmerDictionary));
}
```

#### Similaridad entre palabras

La similaridad entre dos palabras es un número real que pertenece al intervalo de [0,1] tal que 0 significa que las palabras no tienen relación relevante y 1 que son la misma palabra.
Para su cálculo se tienen en cuenta tres factores: su pertenencia a la misma familia de palabras, la semejanza de sus grafías y la sinonimia. Si las palabras pertenecen a la misma familia de palabras su similaridad es de *e/3*, si sus grafías son semejantes su similaridad coincide con el grado de semejanza y si son sinónimos su similaridad es de *π/10*.

- ##### Familia de palabras (raíz común)

Se considera que dos palabras pertenecen a la misma familia de palabras si sus raíces coinciden. Para la extracción de la raíz de una palabra en **Moogle!** se escogió el algoritmo Snowball que separa la palabra en regiones y busca morfemas comunes del lenguajes para eliminarlos en busca de la raíz en caso de enontrarse estos en una región eliminable.

- ##### Semejanza de grafías (corrección ortográfica)

Para contemplar los posibles errores ortográficos en la consulta se escogió la distancia de Levenshtein para hallar la semejanza entre las grafías de dos palabras. Esta distancia solo se considera en palabras de longitudes e inicios similares para no penalizar la velocidad de la búsqueda. En la misma se consideran menos significativos aquellos errores comunes como el cambio de *v* por *b* o los de acentuación. Finalmente, el grado de semejanza se calcula restandole a 1 el cociente de la distancia obtenida y la longitud de la primera palabra.

- ##### Sinonimia

Se considera que dos palabras son sinónimos si una está en la lista de sinónimos de la otra en un diccionario de sinónimos.

### Cálculo de TfxIdf de la consulta

Este es llevado a cabo por la clase `QueryTfxIdf` que recibe un objeto `Query` y un objeto `Corpus` en su constructor. Le asigna a cada una de las palabras de la consulta su peso en la misma, además de calcular la norma del vector que forman estos pesos.

La propiedad `Weights` retorna un `IEnumerable<(string word, double weight)>` con tuplas de palabra y peso de esa palabras.

### Cálculo del coseno y devolución de los documentos ordenados

Este es llevado a cabo por una instancia de la clase `VectorMri : MRI` que recibe como constructor un objeto de tipo `Corpus` y genera para él un objeto de tipo `TfxIdf`. Su método `Query` recibe una instancia  de `Query` y retorna un `IEnumerable<(string document, double score)>` con las tuplas ordenas de los documentos y sus puntuaciones. Las puntuaciones retornadas son el resultado del cálculo del coseno de los vectores con el vector consulta multiplicado por la proximidad inversa. Esta última es el producto de los recíprocos de los logaritmos en base 5 de las longitudes de los mejores intervalos que contienen todas las palabras de cada uno de los conjuntos creados por los operadores de cercanía de la consulta.

```c#
/// <summary>
/// Realiza el càlculo de la similitud entre la consulta y cada uno de los documentos, elimina aquellos resultados que no cumplen con los requisitos de inclusión y exclusión, divide la puntuación de cada uno entre la proximidad inversa y los ordena descendentemente.
/// </summary>
/// <param name="query">Consulta.</param>
/// <returns>Enumerable de tuplas ordenadas descendentemente de documentos y puntuación. Donde la puntuación indica cuan bueno es cada documento para la consulta.</returns>
public override IEnumerable<(string document, double score)> Query(Query query) {
	_queryTfxIdf = new QueryTfxIdf(query, Corpus);
	return Corpus.Documents.Where(document => query.Inclusions.All(word => Corpus[document, word] is not 0) &&
	                                          query.Exclusions.All(word => Corpus[document, word] is 0))
		.Select(document => (document, score: Similarity(document) * Corpus.InverseProximity(query, document))).ToList()
		.OrderByDescending(t => t.score);
}
```

#### Cálculo del coseno y de la proximidad inversa

Para el cálculo del coseno entre un vector documento y el vector consulta se divide el producto de estos por el producto de la norma de estos.

```c#
/// <summary>
/// Calcula el coseno del ángulo entre el vector del documento y el vector de la consulta.
/// </summary>
/// <param name="document">Documento.</param>
/// <returns>Valor entre 0 y 1 que indica el grado de relevancia del documento para la consulta</returns>
private double Similarity(string document) =>
		_queryTfxIdf.Weights.Select(word => word.weight * _tfxIdf[document, word.word]).Sum() /
		(_queryTfxIdf.Norm * _tfxIdf.Norm(document));
```

Para el cálculo del mejor intervalo que contiene todas las palabras de un conjunto en un documento se mezclan ordenadamente las listas de los índices de las palabras del conjunto en el documento y luego se recorre la lista resultante en busca del intervalo de menor distancia que contenga cada uno de los vocablos.

```c#
/// <summary>
/// Encuentra la longitud del menor intervalo que contiene a las palabras que son llaves del diccionario "indexDictionary".
/// </summary>
/// <param name="indexDictionary">Diccionario donde las llaves son palabras y el valor una lista con los índices de cada palabra en un documento.</param>
/// <returns>Longitud del intervalo más pequeño que contiene todas las palabras que son llaves de "indexDictionary".</returns>
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
        //mientras haya alguna palabra que no esté contenida en el intervalo se desplaza el extremo derecho para la derecha
        //tratando de encontrar un intervalo que las contenga a todas
        canMove = false;
        while (tCount < count.Count && right < indexes.Count - 1) {
            canMove = true;
            right++;
            count[indexes[right].word]++;
            if (count[indexes[right].word] == 1) tCount++;
        }
        //una vez se logra que el intervalo contenga todas las palabras se comienza a desplazar el extremo izquierdo todo
        //lo posible para minimizar la longitud del mismo
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
```

### Ubicación del snippet o fragmento relevante

Para determinar cúal es el mejor snippet se mezclan ordenamente los índices de las palabras "cercanas" a las palabras de la búsqueda en el documento y luego se busca en este cúal es el intervalo de 50 palabras que mejor resultado ofrece. EL valor de un intervalo está determinado por la suma de la representación de cada palabra de la consulta original en el mismo. La representación de una palabra en el intervalo está dada por el maximo del valor que resulta de multiplicar la similaridad de cada palabra por (1- la frecuencia de aparición de esa palabra en el corpus).

### Sugerencia

Para determinar la sugerencia dada una consulta se revisa para cada una de las palabras originales de la consulta cúal es la palabra "cercana" a esta que mejor resultado ofrece para el corpus. Para esto se halla la suma del `TfxIdf` en los documentos de cada una de las palabras "cercanas" y se multiplica por la similaridad con la palabra original. Una vez se tiene el mejor sustituto(puede ser la misma palabra) para cada palabra se concatena y devuelve.

```c#
/// <summary>
/// Sugiere una consulta nueva con buenos resultados de búsqueda.
/// </summary>
/// <param name="query">Consulta.</param>
/// <returns>Una consulta con palabras cercanas a la consulta original con buenos resultados garantizados.</returns>
public override string Suggestion(Query query) {
    var outPut = "";
    foreach (var (_, candidates) in query.SuggestionDictionary) {
        outPut += ' ';
        var max = double.MinValue;
        var bestWord = "";
        foreach (var (candidate, score) in candidates) {
            if (score * WordRelevance(candidate) <= max) continue;
            max = score * WordRelevance(candidate);
            bestWord = candidate;
        }

        outPut += bestWord;
    }

    return outPut.Trim();
}
/// <summary>
/// Devuelve un valor que indica cúan bueno es buscar una palabra en el corpus mediante la duma de sus pesos en los documentos donde aparece.
/// </summary>
/// <param name="word">Palabra.</param>
/// <returns>Suma de los pesos de la palabra en todos los documentos.</returns>
private double WordRelevance(string word) => Corpus.GetDocuments(word).Sum(document => _tfxIdf[document, word])/Corpus[word];
```

### Métodos útiles

#### Trimming de puntuación

Utilizado para el almacenamiento de todas las palabras para eliminar los caracteres no alfanuméricos de los extremos de la palabra.

```c#
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
```

### Eliminación de sufijo en el stemmer

Utilizado para eliminar ciertas terminaciones de una region dada de una palabra. Recibe un diccionario de `int` contra conjuntos de terminaciones de esa longitud. Revisa si la palabra termina en en una de esas terminaciones y de ser asi la elimina y retorna `true`.

```c#
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
```

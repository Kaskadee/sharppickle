# sharppickle

Parser for serialized python pickles written in C#/.NET Core 

### Why?
---

This repository should be seen as a proof-of-concept. **DO NOT USE** Python's `pickle` module as a way to communicate between your python script and your application. [Especially since the `pickle` module is unsecure](https://docs.python.org/3/library/pickle.html ) as it allows to execute arbitrary code during unpickling!

To exchange data use a language-independant serialization format such as [JSON](https://en.wikipedia.org/wiki/JSON ) or [XML](https://en.wikipedia.org/wiki/XML ).

### How to use
---

```csharp
// Initialize PickleReader with pickle data.
using(var reader = new PickleReader(new FileInfo("data.pickle")) {
    // Deserialize data.
    object[] deserialized = reader.Unpickle();
    // How to parse the data is up to you!
}
```

### Example
---

Please visit the [rpaextract](https://git.kaskadee.eu/Kaskadee/rpaextract ) repository for an example application using `sharppickle`. Usage can be found in [Archive.cs](https://git.kaskadee.eu/Kaskadee/rpaextract/src/branch/master/csharp/rpaextract/Archive.cs#L138 ).

### License
---

sharppickle is licensed under the [European Union Public Licence v1.2](https://git.kaskadee.eu/Kaskadee/sharppickle/src/branch/master/LICENSE )
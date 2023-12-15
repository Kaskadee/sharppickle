# sharppickle

Parser for serialized python pickles written in C#/.NET Core 

### Why?
---

This repository should be seen as a proof-of-concept. 

**Do not unpickle data received from an untrusted or unauthenticated source.**  
[The `pickle` module allows to execute arbitrary code during unpickling](https://docs.python.org/3/library/pickle.html )

To exchange data use a language-independent serialization format such as [JSON](https://en.wikipedia.org/wiki/JSON ) or [XML](https://en.wikipedia.org/wiki/XML ).

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

Please visit the [rpaextract](https://github.com/Kaskadee/rpaextract ) repository for an example application using `sharppickle`. Usage can be found in [Archive.cs](https://github.com/Kaskadee/rpaextract/blob/master/src/rpaextract/API/RenpyArchiveReader.cs#L72 ).

### License
---

sharppickle is licensed under the [European Union Public Licence v1.2](https://github.com/Kaskadee/sharppickle/blob/master/LICENSE )
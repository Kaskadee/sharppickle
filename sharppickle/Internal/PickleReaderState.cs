// rpaextract - PickleReaderState.cs
// Copyright (C) 2023 Fabian Creutz.
// 
// Licensed under the EUPL, Version 1.2 or – as soon they will be approved by the
// European Commission - subsequent versions of the EUPL (the "Licence");
// 
// You may not use this work except in compliance with the Licence.
// You may obtain a copy of the Licence at:
// 
// https://joinup.ec.europa.eu/software/page/eupl
// 
// Unless required by applicable law or agreed to in writing, software distributed under the Licence is distributed on an "AS IS" basis,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the Licence for the specific language governing permissions and limitations under the Licence.

using System.Collections;
using System.Text;

namespace sharppickle.Internal;

/// <summary>
/// Provides an record that represents the current state of a <see cref="PickleReader"/> instance.
/// </summary>
/// <param name="ProtocolVersion">The protocol version of the pickle that is being deserialized.</param>
/// <param name="Stream">The <see cref="Stream"/> to read the pickle data from.</param>
/// <param name="Stack">The current <see cref="Stack"/> to push/pop data to/from.</param>
/// <param name="Memo">The current state of the memo as a <see cref="Dictionary{TKey,TValue}"/>.</param>
/// <param name="Encoding">The current <see cref="Encoding"/> to use for decoding strings.</param>
/// <param name="Reader">The <see cref="PickleReader"/> instance.</param>
internal sealed record PickleReaderState(int ProtocolVersion, Stream Stream, Stack Stack, Dictionary<int, object?> Memo, Encoding? Encoding, PickleReader Reader);
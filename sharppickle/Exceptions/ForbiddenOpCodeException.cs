// rpaextract - ForbiddenOpCodeException.cs
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

namespace sharppickle.Exceptions;

/// <summary>
/// Provides an exception that is raised when a deprecated op-code has been encountered in a newer protocol version.
/// </summary>
public sealed class ForbiddenOpCodeException : PickleException {
    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenOpCodeException" />.
    /// </summary>
    /// <param name="message">A short description which describes the occured error.</param>
    public ForbiddenOpCodeException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenOpCodeException" /> class which wraps another exception.
    /// </summary>
    /// <param name="message">A short description which describes the occured error.</param>
    /// <param name="innerException">The exception to wrap with this exception object.</param>
    public ForbiddenOpCodeException(string message, Exception innerException) : base(message, innerException) { }
}

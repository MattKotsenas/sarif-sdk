﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Sarif
{
    public interface IAnalysisContext : IDisposable
    {
        Uri TargetUri { get; set; }

        string MimeType { get; set; }

        HashData Hashes { get; set; }

        Exception TargetLoadException { get; set; }

        bool IsValidAnalysisTarget { get; }

        ReportingDescriptor Rule { get; set; }

        PropertiesDictionary Policy { get; set; }

        IAnalysisLogger Logger { get; set; }

        RuntimeConditions RuntimeErrors { get; set; }

        bool AnalysisComplete { get; set; }

        ISet<string> Traces { get; set; }

        long MaxFileSizeInKilobytes { get; set; }
    }
}

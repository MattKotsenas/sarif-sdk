// Copyright (c) Microsoft.  All Rights Reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Sarif
{
    /// <summary>
    /// Defines methods to support the comparison of objects of type MultiformatMessageString for sorting.
    /// </summary>
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "2.3.0.0")]
    internal sealed class MultiformatMessageStringComparer : IComparer<MultiformatMessageString>
    {
        internal static readonly MultiformatMessageStringComparer Instance = new MultiformatMessageStringComparer();

        public int Compare(MultiformatMessageString left, MultiformatMessageString right)
        {
            int compareResult = 0;

            // TryReferenceCompares is an autogenerated extension method
            // that will properly handle the case when 'left' is null.
            if (left.TryReferenceCompares(right, out compareResult))
            {
                return compareResult;
            }

            compareResult = string.Compare(left.Text, right.Text);
            if (compareResult != 0)
            {
                return compareResult;
            }

            compareResult = string.Compare(left.Markdown, right.Markdown);
            if (compareResult != 0)
            {
                return compareResult;
            }

            compareResult = left.Properties.DictionaryCompares(right.Properties, SerializedPropertyInfoComparer.Instance);
            if (compareResult != 0)
            {
                return compareResult;
            }

            return compareResult;
        }
    }
}
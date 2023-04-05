// Copyright (c) Microsoft.  All Rights Reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Sarif
{
    /// <summary>
    /// Defines methods to support the comparison of objects of type RunAutomationDetails for sorting.
    /// </summary>
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "2.3.0.0")]
    internal sealed class RunAutomationDetailsComparer : IComparer<RunAutomationDetails>
    {
        internal static readonly RunAutomationDetailsComparer Instance = new RunAutomationDetailsComparer();

        public int Compare(RunAutomationDetails left, RunAutomationDetails right)
        {
            int compareResult = 0;

            // TryReferenceCompares is an autogenerated extension method
            // that will properly handle the case when 'left' is null.
            if (left.TryReferenceCompares(right, out compareResult))
            {
                return compareResult;
            }

            compareResult = MessageComparer.Instance.Compare(left.Description, right.Description);
            if (compareResult != 0)
            {
                return compareResult;
            }

            compareResult = string.Compare(left.Id, right.Id);
            if (compareResult != 0)
            {
                return compareResult;
            }

            compareResult = left.Guid.CompareTo(right.Guid);
            if (compareResult != 0)
            {
                return compareResult;
            }

            compareResult = left.CorrelationGuid.CompareTo(right.CorrelationGuid);
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
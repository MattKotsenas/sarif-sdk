// Copyright (c) Microsoft.  All Rights Reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Sarif
{
    /// <summary>
    /// Defines methods to support the comparison of objects of type ReportingDescriptorReference for sorting.
    /// </summary>
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "1.1.4.0")]
    internal sealed class ReportingDescriptorReferenceComparer : IComparer<ReportingDescriptorReference>
    {
        internal static readonly ReportingDescriptorReferenceComparer Instance = new ReportingDescriptorReferenceComparer();

        public int Compare(ReportingDescriptorReference left, ReportingDescriptorReference right)
        {
            int compareResult = 0;

            // TryReferenceCompares is an autogenerated extension method
            // that will properly handle the case when 'left' is null.
            if (left.TryReferenceCompares(right, out compareResult))
            {
                return compareResult;
            }

            compareResult = string.Compare(left.Id, right.Id);
            if (compareResult != 0)
            {
                return compareResult;
            }

            compareResult = left.Index.CompareTo(right.Index);
            if (compareResult != 0)
            {
                return compareResult;
            }

            compareResult = string.Compare(left.Guid, right.Guid);
            if (compareResult != 0)
            {
                return compareResult;
            }

            compareResult = ToolComponentReferenceComparer.Instance.Compare(left.ToolComponent, right.ToolComponent);
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
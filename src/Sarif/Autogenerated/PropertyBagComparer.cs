// Copyright (c) Microsoft.  All Rights Reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Sarif
{
    /// <summary>
    /// Defines methods to support the comparison of objects of type PropertyBag for sorting.
    /// </summary>
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "2.3.0.0")]
    internal sealed class PropertyBagComparer : IComparer<PropertyBag>
    {
        internal static readonly PropertyBagComparer Instance = new PropertyBagComparer();

        public int Compare(PropertyBag left, PropertyBag right)
        {
            int compareResult = 0;

            // TryReferenceCompares is an autogenerated extension method
            // that will properly handle the case when 'left' is null.
            if (left.TryReferenceCompares(right, out compareResult))
            {
                return compareResult;
            }

            compareResult = left.Tags.ListCompares(right.Tags);
            if (compareResult != 0)
            {
                return compareResult;
            }

            return compareResult;
        }
    }
}
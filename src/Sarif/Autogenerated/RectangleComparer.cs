// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Sarif
{
    /// <summary>
    /// Defines methods to support the comparison of objects of type Rectangle for sorting.
    /// </summary>
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "2.1.0.0")]
    internal sealed class RectangleComparer : IComparer<Rectangle>
    {
        internal static readonly RectangleComparer Instance = new RectangleComparer();

        public int Compare(Rectangle left, Rectangle right)
        {
            int compareResult = 0;

            // TryReferenceCompares is an autogenerated extension method
            // that will properly handle the case when 'left' is null.
            if (left.TryReferenceCompares(right, out compareResult))
            {
                return compareResult;
            }

            compareResult = left.Top.CompareTo(right.Top);
            if (compareResult != 0)
            {
                return compareResult;
            }

            compareResult = left.Left.CompareTo(right.Left);
            if (compareResult != 0)
            {
                return compareResult;
            }

            compareResult = left.Bottom.CompareTo(right.Bottom);
            if (compareResult != 0)
            {
                return compareResult;
            }

            compareResult = left.Right.CompareTo(right.Right);
            if (compareResult != 0)
            {
                return compareResult;
            }

            compareResult = MessageComparer.Instance.Compare(left.Message, right.Message);
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

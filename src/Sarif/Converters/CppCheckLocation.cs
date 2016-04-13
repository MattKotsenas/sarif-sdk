﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Xml;
using Microsoft.CodeAnalysis.Sarif.Driver;
using Microsoft.CodeAnalysis.Sarif.Sdk;

namespace Microsoft.CodeAnalysis.Sarif.Converters
{
    /// <summary>A "location" structure from a CppCheck file.</summary>
    internal struct CppCheckLocation : IEquatable<CppCheckLocation>
    {
        /// <summary>The file name from a CppCheck location node.</summary>
        public readonly string File;
        /// <summary>The line number from a CppCheck location node.</summary>
        public readonly int Line;

        /// <summary>Initializes a new instance of the <see cref="CppCheckLocation"/> struct.</summary>
        /// <exception cref="ArgumentException">Thrown if <paramref name="file"/> is null or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="line"/> is negative.</exception>
        /// <param name="file">The file value from the location. This must be a non-null non-whitespace value.</param>
        /// <param name="line">The line value from the location. This value must be positive.</param>
        public CppCheckLocation(string file, int line)
        {
            if (String.IsNullOrWhiteSpace(file))
            {
                throw new ArgumentException(SarifResources.CppCheckLocationNameEmpty, "file");
            }

            if (line < 0)
            {
                throw new ArgumentOutOfRangeException("line", SarifResources.CppCheckLocationNegativeLine);
            }

            this.File = file;
            this.Line = line;
        }

        /// <summary>
        /// Parses a "location" node from the supplied instance of <see cref="XmlReader"/>.
        /// </summary>
        /// <exception cref="XmlException">Thrown if <paramref name="reader"/> points to XML of an
        /// incorrect format.</exception>
        /// <param name="reader">The reader from which XML will be parsed. Upon entry to this method, this
        /// XML reader must be positioned on the location node to parse. Upon completion of this method,
        /// the reader will be positioned on the node following the location node.</param>
        /// <param name="strings">Strings used to parse the CppCheck log.</param>
        /// <returns>
        /// A <see cref="CppCheckLocation"/> instance containing data from the current node of
        /// <paramref name="reader"/>.
        /// </returns>
        public static CppCheckLocation Parse(XmlReader reader, CppCheckStrings strings)
        {
            if (!reader.IsStartElement(strings.Location))
            {
                throw reader.CreateException(SarifResources.CppCheckLocationElementNameIncorrect);
            }

            string file = null;
            string lineText = null;

            while (reader.MoveToNextAttribute())
            {
                string name = reader.LocalName;
                if (Ref.Equal(name, strings.File))
                {
                    file = reader.Value;
                }
                else if (Ref.Equal(name, strings.Line))
                {
                    lineText = reader.Value;
                }
            }

            if (file == null)
            {
                throw reader.CreateException(SarifResources.CppCheckLocationMissingName);
            }

            if (lineText == null)
            {
                throw reader.CreateException(SarifResources.CppCheckLocationMissingLine);
            }

            int line = XmlConvert.ToInt32(lineText);
            reader.MoveToElement();
            reader.Skip();

            return new CppCheckLocation(file, line);
        }

        /// <summary>Converts this instance to a <see cref="PhysicalLocation"/>.</summary>
        /// <returns>This instance as a <see cref="PhysicalLocation"/>.</returns>
        public PhysicalLocation ToSarifPhysicalLocation()
        {
            return new PhysicalLocation
            {
                Uri = new Uri(this.File, UriKind.RelativeOrAbsolute),
                Region = Extensions.CreateRegion(this.Line)
            };
        }

        /// <summary>Tests if this <see cref="CppCheckLocation"/> is considered equal to another.</summary>
        /// <param name="obj">Another object to compare to.</param>
        /// <returns>true if the objects are considered equal, false if they are not.</returns>
        /// <seealso cref="M:System.ValueType.Equals(object)"/>
        public override bool Equals(object obj)
        {
            var asLoc = obj as CppCheckLocation?;
            if (asLoc == null)
            {
                return false;
            }

            return this.Equals(asLoc.Value);
        }

        /// <summary>Tests if this <see cref="CppCheckLocation"/> is considered equal to another.</summary>
        /// <param name="other">The CppCheck location to compare to this instance.</param>
        /// <returns>true if the objects are considered equal, false if they are not.</returns>
        public bool Equals(CppCheckLocation other)
        {
            return Object.Equals(this.File, other.File)
                && this.Line == other.Line;
        }

        /// <summary>Returns a hash code for this instance.</summary>
        /// <returns>A hash code for this instance.</returns>
        /// <seealso cref="M:System.ValueType.GetHashCode()"/>
        public override int GetHashCode()
        {
            var hash = new MultiplyByPrimesHash();
            hash.Add(this.File);
            hash.Add(this.Line);
            return hash.GetHashCode();
        }

        /// <summary>Equality operator.</summary>
        /// <param name="lhs">The left hand side.</param>
        /// <param name="rhs">The right hand side.</param>
        /// <returns>The result of the operation.</returns>
        public static bool operator ==(CppCheckLocation lhs, CppCheckLocation rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>Inequality operator.</summary>
        /// <param name="lhs">The left hand side.</param>
        /// <param name="rhs">The right hand side.</param>
        /// <returns>The result of the operation.</returns>
        public static bool operator !=(CppCheckLocation lhs, CppCheckLocation rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>Convert this instance into a string representation.</summary>
        /// <returns>A string that represents this instance.</returns>
        /// <seealso cref="M:System.ValueType.ToString()"/>
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, SarifResources.CppCheckLocationFormat,
                this.File, this.Line);
        }
    }
}

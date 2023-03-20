// Copyright (c) Microsoft.  All Rights Reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Sarif
{
    /// <summary>
    /// A physical or virtual address, or a range of addresses, in an 'addressable region' (memory or a binary file).
    /// </summary>
    [DataContract]
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "2.3.0.0")]
    public partial class Address : PropertyBagHolder, ISarifNode
    {
        public static IEqualityComparer<Address> ValueComparer => AddressEqualityComparer.Instance;

        public bool ValueEquals(Address other) => ValueComparer.Equals(this, other);
        public int ValueGetHashCode() => ValueComparer.GetHashCode(this);

        public static IComparer<Address> Comparer => AddressComparer.Instance;

        /// <summary>
        /// Gets a value indicating the type of object implementing <see cref="ISarifNode" />.
        /// </summary>
        public virtual SarifNodeKind SarifNodeKind
        {
            get
            {
                return SarifNodeKind.Address;
            }
        }

        /// <summary>
        /// The address expressed as a byte offset from the start of the addressable region.
        /// </summary>
        [DataMember(Name = "absoluteAddress", IsRequired = false, EmitDefaultValue = false)]
        [DefaultValue(-1)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public virtual int AbsoluteAddress { get; set; }

        /// <summary>
        /// The address expressed as a byte offset from the absolute address of the top-most parent object.
        /// </summary>
        [DataMember(Name = "relativeAddress", IsRequired = false, EmitDefaultValue = false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public virtual int? RelativeAddress { get; set; }

        /// <summary>
        /// The number of bytes in this range of addresses.
        /// </summary>
        [DataMember(Name = "length", IsRequired = false, EmitDefaultValue = false)]
        public virtual int? Length { get; set; }

        /// <summary>
        /// An open-ended string that identifies the address kind. 'data', 'function', 'header','instruction', 'module', 'page', 'section', 'segment', 'stack', 'stackFrame', 'table' are well-known values.
        /// </summary>
        [DataMember(Name = "kind", IsRequired = false, EmitDefaultValue = false)]
        public virtual string Kind { get; set; }

        /// <summary>
        /// A name that is associated with the address, e.g., '.text'.
        /// </summary>
        [DataMember(Name = "name", IsRequired = false, EmitDefaultValue = false)]
        public virtual string Name { get; set; }

        /// <summary>
        /// A human-readable fully qualified name that is associated with the address.
        /// </summary>
        [DataMember(Name = "fullyQualifiedName", IsRequired = false, EmitDefaultValue = false)]
        public virtual string FullyQualifiedName { get; set; }

        /// <summary>
        /// The byte offset of this address from the absolute or relative address of the parent object.
        /// </summary>
        [DataMember(Name = "offsetFromParent", IsRequired = false, EmitDefaultValue = false)]
        public virtual int? OffsetFromParent { get; set; }

        /// <summary>
        /// The index within run.addresses of the cached object for this address.
        /// </summary>
        [DataMember(Name = "index", IsRequired = false, EmitDefaultValue = false)]
        [DefaultValue(-1)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public virtual int Index { get; set; }

        /// <summary>
        /// The index within run.addresses of the parent object.
        /// </summary>
        [DataMember(Name = "parentIndex", IsRequired = false, EmitDefaultValue = false)]
        [DefaultValue(-1)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public virtual int ParentIndex { get; set; }

        /// <summary>
        /// Key/value pairs that provide additional information about the address.
        /// </summary>
        [DataMember(Name = "properties", IsRequired = false, EmitDefaultValue = false)]
        internal override IDictionary<string, SerializedPropertyInfo> Properties { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Address" /> class.
        /// </summary>
        public Address()
        {
            AbsoluteAddress = -1;
            Index = -1;
            ParentIndex = -1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Address" /> class from the supplied values.
        /// </summary>
        /// <param name="absoluteAddress">
        /// An initialization value for the <see cref="P:AbsoluteAddress" /> property.
        /// </param>
        /// <param name="relativeAddress">
        /// An initialization value for the <see cref="P:RelativeAddress" /> property.
        /// </param>
        /// <param name="length">
        /// An initialization value for the <see cref="P:Length" /> property.
        /// </param>
        /// <param name="kind">
        /// An initialization value for the <see cref="P:Kind" /> property.
        /// </param>
        /// <param name="name">
        /// An initialization value for the <see cref="P:Name" /> property.
        /// </param>
        /// <param name="fullyQualifiedName">
        /// An initialization value for the <see cref="P:FullyQualifiedName" /> property.
        /// </param>
        /// <param name="offsetFromParent">
        /// An initialization value for the <see cref="P:OffsetFromParent" /> property.
        /// </param>
        /// <param name="index">
        /// An initialization value for the <see cref="P:Index" /> property.
        /// </param>
        /// <param name="parentIndex">
        /// An initialization value for the <see cref="P:ParentIndex" /> property.
        /// </param>
        /// <param name="properties">
        /// An initialization value for the <see cref="P:Properties" /> property.
        /// </param>
        public Address(int absoluteAddress, int? relativeAddress, int? length, string kind, string name, string fullyQualifiedName, int? offsetFromParent, int index, int parentIndex, IDictionary<string, SerializedPropertyInfo> properties)
        {
            Init(absoluteAddress, relativeAddress, length, kind, name, fullyQualifiedName, offsetFromParent, index, parentIndex, properties);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Address" /> class from the specified instance.
        /// </summary>
        /// <param name="other">
        /// The instance from which the new instance is to be initialized.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="other" /> is null.
        /// </exception>
        public Address(Address other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            Init(other.AbsoluteAddress, other.RelativeAddress, other.Length, other.Kind, other.Name, other.FullyQualifiedName, other.OffsetFromParent, other.Index, other.ParentIndex, other.Properties);
        }

        ISarifNode ISarifNode.DeepClone()
        {
            return DeepCloneCore();
        }

        /// <summary>
        /// Creates a deep copy of this instance.
        /// </summary>
        public virtual Address DeepClone()
        {
            return (Address)DeepCloneCore();
        }

        private ISarifNode DeepCloneCore()
        {
            return new Address(this);
        }

        protected virtual void Init(int absoluteAddress, int? relativeAddress, int? length, string kind, string name, string fullyQualifiedName, int? offsetFromParent, int index, int parentIndex, IDictionary<string, SerializedPropertyInfo> properties)
        {
            AbsoluteAddress = absoluteAddress;
            RelativeAddress = relativeAddress;
            Length = length;
            Kind = kind;
            Name = name;
            FullyQualifiedName = fullyQualifiedName;
            OffsetFromParent = offsetFromParent;
            Index = index;
            ParentIndex = parentIndex;
            if (properties != null)
            {
                Properties = new Dictionary<string, SerializedPropertyInfo>(properties);
            }
        }
    }
}

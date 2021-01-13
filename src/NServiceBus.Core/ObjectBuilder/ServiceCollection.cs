// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See NOTICE.txt in the project root for license information.

namespace MicrosoftExtensionsDependencyInjection
{
    /*
     * This file has been modified from its original form in the following ways:
     * - The namespace has been changed
     * - The access modifier has been changed
     * - The license file name in the copyright notice has been changed to match the license location in this repository
     */

    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Default implementation of <see cref="IServiceCollection"/>.
    /// </summary>
    internal class ServiceCollection : IServiceCollection
    {
        private readonly List<ServiceDescriptor> descriptors = new List<ServiceDescriptor>();

        /// <inheritdoc />
        public int Count => descriptors.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public ServiceDescriptor this[int index]
        {
            get
            {
                return descriptors[index];
            }
            set
            {
                descriptors[index] = value;
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            descriptors.Clear();
        }

        /// <inheritdoc />
        public bool Contains(ServiceDescriptor item)
        {
            return descriptors.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
        {
            descriptors.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public bool Remove(ServiceDescriptor item)
        {
            return descriptors.Remove(item);
        }

        /// <inheritdoc />
        public IEnumerator<ServiceDescriptor> GetEnumerator()
        {
            return descriptors.GetEnumerator();
        }

        void ICollection<ServiceDescriptor>.Add(ServiceDescriptor item)
        {
            descriptors.Add(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public int IndexOf(ServiceDescriptor item)
        {
            return descriptors.IndexOf(item);
        }

        /// <inheritdoc />
        public void Insert(int index, ServiceDescriptor item)
        {
            descriptors.Insert(index, item);
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            descriptors.RemoveAt(index);
        }
    }
}
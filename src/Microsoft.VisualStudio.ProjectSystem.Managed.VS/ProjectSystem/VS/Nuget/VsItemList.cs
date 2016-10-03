// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Nuget
{
    /// <summary>
    /// Abstract list with Item method for getting members by index or name
    /// </summary>
    internal abstract class VsItemList<T> : List<T>
    {
        public VsItemList() : base() { }

        public VsItemList(IEnumerable<T> collection): base(collection) { }

        public abstract string GetName(T value);

        public T Item(Object index)
        {
            if (index is string)
            {
                return this.FirstOrDefault(v => GetName(v) == (string)index);
            }
            else if (index is int)
            {
                return this[(int)index];
            }
            else
            {
                return default(T);
            }
        }
    }
}

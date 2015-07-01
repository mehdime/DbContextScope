/* 
 * Copyright (C) 2014 Mehdi El Gueddari
 * http://mehdi.me
 *
 * This software may be modified and distributed under the terms
 * of the MIT license.  See the LICENSE file for details.
 */
using System;

namespace Mehdime.Entity
{
    /// <summary>
    /// A read-only DbContextScope. Refer to the comments for IDbContextScope
    /// for more details.
    /// </summary>
    public interface IDbContextReadOnlyScope : IDisposable
    {
        /// <summary>
        /// The DbContext instances that this DbContextScope manages.
        /// </summary>
        IDbContextCollection DbContexts { get; }
    }
}
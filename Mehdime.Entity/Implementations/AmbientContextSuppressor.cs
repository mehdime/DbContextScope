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
    public class AmbientContextSuppressor : IDisposable
    {
        private DbContextScope _savedScope;
        private bool _disposed;

        public AmbientContextSuppressor()
        {
            _savedScope = DbContextScope.GetAmbientScope();

            // We're hiding the ambient scope but not removing its instance
            // altogether. This is to be tolerant to some programming errors. 
            // 
            // Suppose we removed the ambient scope instance here. If someone
            // was to start a parallel task without suppressing
            // the ambient context and then tried to suppress the ambient
            // context within the parallel task while the original flow
            // of execution was still ongoing (a strange thing to do, I know,
            // but I'm sure this is going to happen), we would end up 
            // removing the ambient context instance of the original flow 
            // of execution from within the parallel flow of execution!
            // 
            // As a result, any code in the original flow of execution
            // that would attempt to access the ambient scope would end up 
            // with a null value since we removed the instance.
            //
            // It would be a fairly nasty bug to track down. So don't let
            // that happen. Hiding the ambient scope (i.e. clearing the CallContext
            // in our execution flow but leaving the ambient scope instance untouched)
            // is safe.
            DbContextScope.HideAmbientScope();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_savedScope != null)
            {
                DbContextScope.SetAmbientScope(_savedScope);
                _savedScope = null;
            }

            _disposed = true;
        }
    }
}

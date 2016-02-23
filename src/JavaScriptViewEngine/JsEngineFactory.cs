﻿using System;
using System.Collections.Concurrent;
using System.Threading;

namespace JavaScriptViewEngine
{
    public class JsEngineFactory : IJsEngineFactory
    {
        private readonly IJsEngineInitializer _engineInitializer;
        protected readonly ConcurrentDictionary<int, IJsEngine> Engines = new ConcurrentDictionary<int, IJsEngine>();
        private bool _disposed;
        private readonly object _lock = new object();

        public JsEngineFactory(IJsEngineInitializer engineInitializer)
        {
            _engineInitializer = engineInitializer;
        }
        
        public virtual IJsEngine GetEngineForCurrentThread()
        {
            EnsureValidState();
            return Engines.GetOrAdd(Thread.CurrentThread.ManagedThreadId, id =>
            {
                var engine = new VroomJsEngine();
                _engineInitializer.Initialize(engine);
                return engine;
            });
        }
        
        public virtual void DisposeEngineForCurrentThread()
        {
            IJsEngine engine;
            if (Engines.TryRemove(Thread.CurrentThread.ManagedThreadId, out engine))
                engine?.Dispose();
        }
        
        public void EnsureValidState()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public void Dispose()
        {
            if (_disposed) return;

            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
            }
            
            foreach (var engine in Engines)
                if (engine.Value != null)
                    engine.Value.Dispose();
        }
    }
}

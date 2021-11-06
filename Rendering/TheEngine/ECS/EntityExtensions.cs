using System;
using System.Threading;

namespace TheEngine.ECS
{
    public static class EntityExtensions
    {
        public static void ForEach<T>(this Archetype archetype, Action<IChunkDataIterator, int, int, ComponentDataAccess<T>> process) where T : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                var access = itr.DataAccess<T>();
                process(itr, 0, itr.Length, access);
            }
        }
        
        public static void ForEach<T1, T2>(this Archetype archetype, 
            System.Action<IChunkDataIterator, int, int, ComponentDataAccess<T1>, ComponentDataAccess<T2>> process)
            where T1 : unmanaged, IComponentData
            where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                process(itr, 0, itr.Length, itr.DataAccess<T1>(), itr.DataAccess<T2>());
            }
        }

        public static void ParallelForEach<T1, T2>(this Archetype archetype, 
            System.Action<IChunkDataIterator, int, int, ComponentDataAccess<T1>, ComponentDataAccess<T2>> process)
            where T1 : unmanaged, IComponentData
            where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                var a1 = itr.DataAccess<T1>();
                var a2 = itr.DataAccess<T2>();

                int total = itr.Length;
                int threads = 4;
                int perThread = total / threads;
                int start = 0;

                Thread[] alloc = new Thread[threads];
                for (int i = 0; i < threads; ++i)
                {
                    if (i == threads - 1)
                        perThread = total - start;
                    
                    var start1 = start;
                    var end = start + perThread;
                    alloc[i] = new Thread(() =>
                    {
                        process(itr, start1, end, a1, a2);
                    });
                    alloc[i].Start();
                    start += perThread;
                }

                foreach (var t in alloc)
                    t.Join();
            }
        }
    }
}
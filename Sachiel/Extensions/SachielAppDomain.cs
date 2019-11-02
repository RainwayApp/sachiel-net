using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if !NET_45
using Microsoft.Extensions.DependencyModel;
#endif

namespace Sachiel.Extensions
{
    public class SachielAppDomain
    {
        static SachielAppDomain()
        {
            CurrentDomain = new SachielAppDomain();
        }

        public static SachielAppDomain CurrentDomain { get; }


        /// <summary>
        ///     A replacement for the AppDomain.GetAssemblies function.
        /// </summary>
        /// <returns></returns>
        public Assembly[] GetAssemblies()
        {
            Assembly[] ass = null;
#if NET_CORE
            ass = GetNetCoreAssemblies();
#else
            ass = GetFrameAssemblies();
#endif

            return ass;
        }

        private Assembly[] GetFrameAssemblies()
        {
            return GetReferencingAssemblies(Assembly.GetEntryAssembly());
        }

        private Assembly[] GetNetCoreAssemblies()
        {
            var dependencies = DependencyContext.Default.RuntimeLibraries;
            return (from library in dependencies
                where IsCandidateCompilationLibrary(library)
                select Assembly.Load(new AssemblyName(library.Name))).ToArray();
        }

        private static Assembly[] GetReferencingAssemblies(Assembly assembly)
        {
            var assemblies = new List<Assembly> {assembly};
            foreach (var library in assembly.GetReferencedAssemblies())
                try
                {
                    assemblies.Add(Assembly.Load(new AssemblyName(library.FullName)));
                }
                catch (Exception)
                {
                    // ignored
                }

            return assemblies.Distinct().ToArray();
        }

        private static bool IsCandidateCompilationLibrary(RuntimeLibrary compilationLibrary)
        {
            return compilationLibrary.Name == "Specify"
                   || compilationLibrary.Dependencies.Any(d => d.Name.StartsWith("Specify"));
        }
    }
}
using System.Reflection;

namespace CollectionApp.Infrastructure.Utils
{
    public static class AssemblyTools
    {
        /// <summary>
        /// Get the loaded assemblies
        /// </summary>
        /// <returns></returns>
        public static List<Assembly> GetAssemblies()
        {
            return Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory)
                .Where(file => Path.GetExtension(file) == ".dll" && Path.GetFileName(file).StartsWith("CollectionApp"))
                .ToList()
                .Select(file => Assembly.LoadFrom(file))
                .ToList();
        }
    }
}

namespace Njord.Core.FileManagement
{
    public static class FileManager
    {
        private static string SolutionName = "Njord";
        private static string[] ExcludedDirectories = new string[]
        {
            "bin", "obj", ".git", ".vs", ".idea", ".vscode", ".ci", ".doc"
        };

        public static string FindFile(string filename)
        {
            var directories = new List<string> { FindSolutionRoot() };
            while (directories.Any())
            {
                var tmpDirectories = new List<string>();
                foreach(var directory in directories)
                {
                    var file = Directory.GetFiles(directory)
                                        .Select(f => (f, Path.GetFileName(f)))
                                        .FirstOrDefault(f => f.Item2 == filename);

                    if (file != default)
                        return file.Item1;

                    var localDirectories = Directory.GetDirectories(directory).AsEnumerable();
                    localDirectories = localDirectories.Except(localDirectories.Where(d => ExcludedDirectories.Contains(Path.GetFileName(d))));
                    tmpDirectories.AddRange(localDirectories);
                }
                directories = tmpDirectories;
            }

            throw new FileNotFoundException(filename);
        }

        private static string FindSolutionRoot()
        {
            var solution = SolutionName + ".slnx";
            var current = Directory.GetCurrentDirectory();
            while (current is not null
                && current.Length != 0
                && !Directory.GetFiles(current).Select(f => Path.GetFileName(f)).Contains(solution))
                current = Directory.GetParent(current)?.FullName;

            if (current is null)
                throw new FileNotFoundException(solution);

            return current;
        }
    }
}

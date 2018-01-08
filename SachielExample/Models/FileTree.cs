#region

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;
using Sachiel.Messages;

#endregion

namespace SachielExample.Models
{
    [ProtoContract]
    public class File : Message
    {
        public File(string path, long size)
        {
            Path = path;
            FileSize = size;
        }

        public File()
        {
            
        }

        [ProtoMember(1)]
        public string Path { get; set; }

        [ProtoMember(2)]
        public long FileSize { get; set; }
    }

    [ProtoContract]
    public class Folder : Message
    {
        public Folder(string name, File[] files, Folder[] childFolders)
        {
            Name = name;
            Files = files;
            ChildFolders = childFolders;
        }

        public Folder(string name)
        {
            Name = name;
        }

        public Folder()
        {
            
        }

        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public File[] Files { get; set; }

        [ProtoMember(3)]
        public Folder[] ChildFolders { get; set; }

        public long Size { get; set; }

        public void AddFiles(File[] files)
        {
            Files = files;
        }

        public void AddChildFolders(Folder[] folders)
        {
            ChildFolders = folders;
        }
    }

    [ProtoContract]
    public class FileTree : Message
    {
        public bool DeepWalk;

        public FileTree()
        {
            
        }
        public FileTree(string rootPath, bool deepWalk = false)
        {
            RootFolder = new Folder(rootPath);
            DeepWalk = deepWalk;
            ConstructTreeDfs(RootFolder);
        }

        [ProtoMember(1)]
        public Folder RootFolder { get; set; }

        public void ConstructTreeDfs(Folder dir)
        {
            var directory = new DirectoryInfo(dir.Name);
            if (directory.Exists)
            {
                DirectoryInfo[] childDirs;
                try
                {
                    childDirs = directory.GetDirectories();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }

                var childFolders = new Folder[childDirs.Length];
                for (var i = 0; i < childDirs.Length; i++)
                    childFolders[i] = new Folder(childDirs[i].FullName);
                dir.AddChildFolders(childFolders);
                var files = directory.GetFiles();
                var f = new File[files.Length];
                for (var i = 0; i < files.Length; i++)
                    try
                    {
                        f[i] = new File(files[i].FullName, files[i].Length);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                dir.AddFiles(f);
                if (DeepWalk)
                    foreach (var item in childFolders)
                        ConstructTreeDfs(item);
            }
        }

        private static long DirSize(string sourceDir, bool recurse)
        {
            long size = 0;
            var fileEntries = Directory.GetFiles(sourceDir);

            foreach (var fileName in fileEntries)
                Interlocked.Add(ref size, new FileInfo(fileName).Length);

            if (recurse)
            {
                var subdirEntries = Directory.GetDirectories(sourceDir);

                Parallel.For<long>(0, subdirEntries.Length, () => 0, (i, loop, subtotal) =>
                    {
                        if ((System.IO.File.GetAttributes(subdirEntries[i]) & FileAttributes.ReparsePoint) !=
                            FileAttributes.ReparsePoint)
                        {
                            subtotal += DirSize(subdirEntries[i], true);
                            return subtotal;
                        }
                        return 0;
                    },
                    x => Interlocked.Add(ref size, x)
                );
            }
            return size;
        }

        public long CalculateFilesSizesDfs(Folder startFolder, string searchForFolder, bool isFound)
        {
            long sizeInBytes = 0;
            if (startFolder.Name == searchForFolder)
                isFound = true;
            if (!isFound)
            {
                foreach (var item in startFolder.ChildFolders.Where(item => item.Name == searchForFolder))
                {
                    isFound = true;
                    sizeInBytes += CalculateFilesSizesDfs(item, searchForFolder, true);
                    break;
                }
                return sizeInBytes;
            }
            if (startFolder.Files != null)
                sizeInBytes += startFolder.Files.Sum(item => item.FileSize);
            if (startFolder.ChildFolders == null) return sizeInBytes;
            {
                sizeInBytes +=
                    startFolder.ChildFolders.Sum(item => CalculateFilesSizesDfs(item, searchForFolder, true));
            }
            return sizeInBytes;
        }
    }
}
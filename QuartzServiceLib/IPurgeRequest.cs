using System;
namespace QuartzServiceLib
{
    public interface IPurgeRequest
    {
        string Name { get; set; }
        int? DaysBack { get; set; }
        bool FakePurge { get; set; }
        string Folder { get; set; }
        void Purge();
        void PurgeFiles(string Folder);
        void PurgeFolders(string Folder);
        bool Recurse { get; set; }
        bool RemoveEmptySubFolders { get; set; }
        PurgeRequest.PurgeType Type { get; set; }
        string WildCard { get; set; }
    }
}

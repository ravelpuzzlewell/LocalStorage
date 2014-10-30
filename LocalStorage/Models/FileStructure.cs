namespace LocalStorage.Models
{
    using System.Collections.Generic;

    public class FileStructure
    {
        public Dictionary<string, FileStructureItem> Entities { get; set; }
    }
}

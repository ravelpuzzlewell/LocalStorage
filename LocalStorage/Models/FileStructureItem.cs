namespace LocalStorage.Models
{
    using LocalStorage.Models.Enums;

    public class FileStructureItem
    {
        public FileStructureItemType Type { get; set; }
        
        public string Path { get; set; }
    }
}

namespace LocalStorage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    using LocalStorage.Models;
    using LocalStorage.Models.Enums;

    public sealed class Store
    {
        private static readonly Lazy<Store> instance =
            new Lazy<Store>(() => new Store { }, LazyThreadSafetyMode.ExecutionAndPublication);

        private readonly IsolatedStorageScope scope =
            IsolatedStorageScope.User | IsolatedStorageScope.Assembly;

        private IsolatedStorageFile store;

        private FileStructure structure;

        private Store()
        {
            // десереализуем конфигурационный файл с описанием требуемой файловой структуры

            var assembly = Assembly.GetExecutingAssembly();

            var setupData = String.Empty;

            using (var stream = assembly.GetManifestResourceStream(this.GetResourceName("setup.json")))
                using (var reader = new StreamReader(stream))
                    setupData = reader.ReadToEnd();

            this.structure = JsonConvert.DeserializeObject<FileStructure>(setupData.ToString());

            this.store = IsolatedStorageFile.GetStore(scope, null, null);

#if DEBUG
            var path =
                this.store.GetType()
                          .GetField("m_RootDir", BindingFlags.NonPublic | BindingFlags.Instance)
                          .GetValue(store)
                          .ToString();

            System.Diagnostics.Debug.WriteLine(
                "Путь {1}", this.store.AssemblyIdentity, path);
#endif

            // создаем структуру каталогов

            foreach (var entity in
                this.structure.Entities.Where(x => x.Value.Type == FileStructureItemType.Directory))
                if (!this.store.DirectoryExists(entity.Value.Path))
                     this.store.CreateDirectory(entity.Value.Path);

            // создаем необходимые файлы, в случае если имеется одноименный embedded ресурс, копируем его содержание

            var resources = assembly.GetManifestResourceNames();

            var asyncJobs = new List<Task>();

            foreach (var entity in
                this.structure.Entities.Where(x => x.Value.Type == FileStructureItemType.File))
                if (!this.store.FileExists(entity.Value.Path))
                    if (resources.Contains(this.GetResourceName(entity.Value.Path)))
                        asyncJobs.Add(this.CopyEmbeddedData(entity.Value.Path));
                            else asyncJobs.Add(Task.Factory.StartNew(
                                () => { using(this.store.CreateFile(entity.Value.Path)); }));

            Task.WaitAll(asyncJobs.ToArray());

#if DEBUG// && CleanOnCreate

            ((Action<DirectoryInfo>)(dir =>
                {
                    dir.GetDirectories().ToList().ForEach(x => x.Delete(true));
                    dir.GetFiles().ToList().ForEach(x => x.Delete());
                }))(new DirectoryInfo(path));

            Console.WriteLine("DONE!");
#endif
        }

        private string GetResourceName(string name) {
            return String.Format("Ravel.Perpetuum.Layers.DataProviders.LocalStorage.Embedded.{0}", name);
        }

        private async Task CopyEmbeddedData(string name) {

            var assembly = Assembly.GetExecutingAssembly();

            using (var embedded = assembly.GetManifestResourceStream(this.GetResourceName(name))) {
                using(var stream =  this.store.CreateFile(name)) {

                    await embedded.CopyToAsync(stream);
                }
            }
        }

        public static Store Instance { get { return instance.Value; } }

        public Stream GetSteram()
        {
            return null;
        }
    }
}

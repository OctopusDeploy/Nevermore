using System;
using System.Linq;

namespace Nevermore
{
    public class RelationalStoreFactory : IRelationalStoreFactory
    {
        readonly string connectionString;
        readonly IMasterKeyEncryption masterKey;
        readonly Lazy<RelationalStore> relationalStore;

        public RelationalStoreFactory(string connectionString, IMasterKeyEncryption masterKey)
        {
            this.connectionString = connectionString;
            this.masterKey = masterKey;

            relationalStore = new Lazy<RelationalStore>(InitializeRelationalStore);
        }

        public static RelationalMappings CreateMappings()
        {
            var mappings = new RelationalMappings();

            var mappers = (
                from type in typeof(DeploymentEnvironment).Assembly.GetTypes()
                where typeof(DocumentMap).IsAssignableFrom(type)
                where type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters
                select Activator.CreateInstance(type) as DocumentMap).ToList();
            
            mappings.Install(mappers);
            
            mappings.Install(new DocumentMap[]
            {
                new ConfigurationMapping<UpgradeAvailability>(),
                new ConfigurationMapping<License>(),
                new ConfigurationMapping<BuiltInRepositoryConfiguration>(),
                new ConfigurationMapping<ActivityLogStorageConfiguration>(),
                new ConfigurationMapping<SmtpConfiguration>(),
                new ConfigurationMapping<MaintenanceConfiguration>(),
                new ConfigurationMapping<ArtifactStorageConfiguration>()
            });

            return mappings;
        }

        RelationalStore InitializeRelationalStore()
        {
            return new RelationalStore(connectionString, CreateMappings(), masterKey);
        }

        public RelationalStore RelationalStore { get { return relationalStore.Value; } }
    }
}
using System.Linq;
using System.Text;
using Nevermore.Advanced.Hooks;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    [TestFixture]
    public class HooksFixture : FixtureWithRelationalStore
    {
        [Test]
        public void ShouldCallHooks()
        {
            var log = new StringBuilder();
            Configuration.HookRegistry.Register(new AuditHook(log));

            using var transaction = Store.BeginTransaction();

            var customer = new Customer {Id = "C131", FirstName = "Fred", LastName = "Freddy"};
            transaction.Insert(customer);
            AssertLogged(log, "BeforeInsert", "AfterInsert");
            
            transaction.Update(customer);
            AssertLogged(log, "BeforeUpdate", "AfterUpdate");

            transaction.Delete(customer);
            AssertLogged(log, "BeforeDelete", "AfterDelete");
            
            transaction.Commit();
            AssertLogged(log, "BeforeCommit", "AfterCommit");
        }

        void AssertLogged(StringBuilder log, params string[] entries)
        {
            var lines = log.ToString().Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            if (lines.Count != entries.Length)
            {
                Assert.Fail($"Expected {entries.Length} lines but got {lines.Count}. Log:" + log.ToString());
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] != entries[i])
                {
                    Assert.Fail($"Expected line[{i}] to be '{entries[i]}' lines but got '{lines[i]}'. Log:" + log.ToString());
                }
            }

            log.Clear();
        }

        class AuditHook : IHook
        {
            readonly StringBuilder log;

            public AuditHook(StringBuilder log)
            {
                this.log = log;
            }
            
            public void BeforeInsert(object document, DocumentMap map, IWriteTransaction transaction) => log.AppendLine(nameof(BeforeInsert));
            public void AfterInsert(object document, DocumentMap map, IWriteTransaction transaction) => log.AppendLine(nameof(AfterInsert));
            public void BeforeUpdate(object document, DocumentMap map, IWriteTransaction transaction) => log.AppendLine(nameof(BeforeUpdate));
            public void AfterUpdate(object document, DocumentMap map, IWriteTransaction transaction) => log.AppendLine(nameof(AfterUpdate));
            public void BeforeDelete(string id, DocumentMap map, IWriteTransaction transaction) => log.AppendLine(nameof(BeforeDelete));
            public void AfterDelete(string id, DocumentMap map, IWriteTransaction transaction) => log.AppendLine(nameof(AfterDelete));
            public void BeforeCommit(IWriteTransaction transaction) => log.AppendLine(nameof(BeforeCommit));
            public void AfterCommit(IWriteTransaction transaction) => log.AppendLine(nameof(AfterCommit));
        }
        
        
    }
}
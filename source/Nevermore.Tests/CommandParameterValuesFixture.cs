using System;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;

namespace Nevermore.Tests
{
    public class CommandParameterValuesFixture
    {
        [Test]
        public void ShouldReplaceParametersWithExactMatch()
        {
            var parameters = new CommandParameterValues();
            parameters.Add("someId", new[] { "id1", "id2" });
            parameters.Add("someIdentifier", "value");
            var command = new SqlCommand("SELECT * FROM [Table] WHERE [Id] IN @someId AND [OtherId] = @someIdentifier");

            parameters.ContributeTo(command, new RelationalStoreConfiguration());

            command.CommandText.Should().Be("SELECT * FROM [Table] WHERE [Id] IN (@someId_0, @someId_1) AND [OtherId] = @someIdentifier");
            command.Parameters["someId_0"].Value.Should().Be("id1");
            command.Parameters["someId_1"].Value.Should().Be("id2");
            command.Parameters["someIdentifier"].Value.Should().Be("value");
        }
        
        [Test]
        public void ShouldReplaceParametersWithExactMatchEndOfQuery()
        {
            var parameters = new CommandParameterValues();
            parameters.Add("someId", "value");
            parameters.Add("someIdentifier", new[] { "id1", "id2" });
            var command = new SqlCommand("SELECT * FROM [Table] WHERE [Id] = @someId AND [OtherId] IN @someIdentifier");

            parameters.ContributeTo(command, new RelationalStoreConfiguration());

            command.CommandText.Should().Be("SELECT * FROM [Table] WHERE [Id] = @someId AND [OtherId] IN (@someIdentifier_0, @someIdentifier_1)");
            command.Parameters["someId"].Value.Should().Be("value");
            command.Parameters["someIdentifier_0"].Value.Should().Be("id1");
            command.Parameters["someIdentifier_1"].Value.Should().Be("id2");
        }

        [TestCase("someId@entifier")]
        [TestCase("someId#entifier")]
        [TestCase("someId_entifier")]
        [TestCase("someId$entifier")]
        public void ShouldReplaceParametersWithExactMatchSpecialCharacters(string paramName)
        {
            var parameters = new CommandParameterValues();
            parameters.Add("someId", new[] { "id1", "id2" });
            parameters.Add(paramName, "value");
            var command = new SqlCommand($"SELECT * FROM [Table] WHERE [Id] IN @someId AND [OtherId] = @{paramName}");

            parameters.ContributeTo(command, new RelationalStoreConfiguration());

            command.CommandText.Should().Be($"SELECT * FROM [Table] WHERE [Id] IN (@someId_0, @someId_1) AND [OtherId] = @{paramName}");
            command.Parameters["someId_0"].Value.Should().Be("id1");
            command.Parameters["someId_1"].Value.Should().Be("id2");
            command.Parameters[paramName].Value.Should().Be("value");
        }

        [Test]
        public void ShouldReplaceParametersAcrossLineBreaks()
        {
            var parameters = new CommandParameterValues();
            parameters.Add("someId", new[] { "id1", "id2" });
            parameters.Add("someIdentifier", "value");
            var command = new SqlCommand($"SELECT * FROM [Table]{Environment.NewLine}WHERE [Id] IN @someId{Environment.NewLine}AND [OtherId] = @someIdentifier");

            parameters.ContributeTo(command, new RelationalStoreConfiguration());

            command.CommandText.Should().Be($"SELECT * FROM [Table]{Environment.NewLine}WHERE [Id] IN (@someId_0, @someId_1){Environment.NewLine}AND [OtherId] = @someIdentifier");
            command.Parameters["someId_0"].Value.Should().Be("id1");
            command.Parameters["someId_1"].Value.Should().Be("id2");
            command.Parameters["someIdentifier"].Value.Should().Be("value");
        }
    }
}
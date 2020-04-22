using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Nevermore.Advanced.TypeHandlers;
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

            parameters.ContributeTo(command, new TypeHandlerRegistry());

            command.CommandText.Should().Be("SELECT * FROM [Table] WHERE [Id] IN (@someId_1, @someId_2) AND [OtherId] = @someIdentifier");
            command.Parameters["someId_1"].Value.Should().Be("id1");
            command.Parameters["someId_2"].Value.Should().Be("id2");
            command.Parameters["someIdentifier"].Value.Should().Be("value");
        }
        
        [Test]
        public void ShouldReplaceParametersWithExactMatchEndOfQuery()
        {
            var parameters = new CommandParameterValues();
            parameters.Add("someId", "value");
            parameters.Add("someIdentifier", new[] { "id1", "id2" });
            var command = new SqlCommand("SELECT * FROM [Table] WHERE [Id] = @someId AND [OtherId] IN @someIdentifier");

            parameters.ContributeTo(command, new TypeHandlerRegistry());

            command.CommandText.Should().Be("SELECT * FROM [Table] WHERE [Id] = @someId AND [OtherId] IN (@someIdentifier_1, @someIdentifier_2)");
            command.Parameters["someId"].Value.Should().Be("value");
            command.Parameters["someIdentifier_1"].Value.Should().Be("id1");
            command.Parameters["someIdentifier_2"].Value.Should().Be("id2");
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

            parameters.ContributeTo(command, new TypeHandlerRegistry());

            command.CommandText.Should().Be($"SELECT * FROM [Table] WHERE [Id] IN (@someId_1, @someId_2) AND [OtherId] = @{paramName}");
            command.Parameters["someId_1"].Value.Should().Be("id1");
            command.Parameters["someId_2"].Value.Should().Be("id2");
            command.Parameters[paramName].Value.Should().Be("value");
        }

        [Test]
        public void ShouldReplaceParametersAcrossLineBreaks()
        {
            var parameters = new CommandParameterValues();
            parameters.Add("someId", new[] { "id1", "id2" });
            parameters.Add("someIdentifier", "value");
            var command = new SqlCommand($"SELECT * FROM [Table]{Environment.NewLine}WHERE [Id] IN @someId{Environment.NewLine}AND [OtherId] = @someIdentifier");

            parameters.ContributeTo(command, new TypeHandlerRegistry());

            command.CommandText.Should().Be($"SELECT * FROM [Table]{Environment.NewLine}WHERE [Id] IN (@someId_1, @someId_2){Environment.NewLine}AND [OtherId] = @someIdentifier");
            command.Parameters["someId_1"].Value.Should().Be("id1");
            command.Parameters["someId_2"].Value.Should().Be("id2");
            command.Parameters["someIdentifier"].Value.Should().Be("value");
        }

        [Test]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(4, 4)]
        [TestCase(5, 5)]
        [TestCase(6, 10)]
        [TestCase(9, 10)]
        [TestCase(10, 10)]
        [TestCase(11, 15)]
        [TestCase(14, 15)]
        [TestCase(15, 15)]
        [TestCase(16, 20)]
        [TestCase(21, 30)]
        [TestCase(21, 30)]
        public void ShouldPadInParameters(int parameterCount, int expectedPaddingTo)
        {
            var parameters = new CommandParameterValues();
            parameters.Add("ids", Enumerable.Range(0, parameterCount).Select(i => "A"));
            var command = new SqlCommand($"SELECT * FROM [Table] WHERE [Id] IN @ids");
            parameters.ContributeTo(command, new TypeHandlerRegistry());

            var parameterListString = string.Join(", ", Enumerable.Range(0, expectedPaddingTo).Select(i => "@ids_" + (i + 1)));
            command.CommandText.Should().Be($"SELECT * FROM [Table] WHERE [Id] IN ({parameterListString})");
        }
    }
}
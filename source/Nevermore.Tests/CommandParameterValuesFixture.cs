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

            parameters.ContributeTo(command);

            command.CommandText.Should().Be("SELECT * FROM [Table] WHERE [Id] IN (@someId_0, @someId_1) AND [OtherId] = @someIdentifier");
        }

        [Test]
        public void ShouldReplaceParametersAcrossLineBreaks()
        {
            var parameters = new CommandParameterValues();
            parameters.Add("someId", new[] { "id1", "id2" });
            parameters.Add("someIdentifier", "value");
            var command = new SqlCommand($"SELECT * FROM [Table]{Environment.NewLine}WHERE [Id] IN @someId{Environment.NewLine}AND [OtherId] = @someIdentifier");

            parameters.ContributeTo(command);

            command.CommandText.Should().Be($"SELECT * FROM [Table]{Environment.NewLine}WHERE [Id] IN (@someId_0, @someId_1){Environment.NewLine}AND [OtherId] = @someIdentifier");
        }
    }
}
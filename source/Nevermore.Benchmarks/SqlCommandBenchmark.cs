using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;
using Nevermore.Benchmarks.Model;
using Nevermore.Benchmarks.SetUp;

namespace Nevermore.Benchmarks
{
    /// <summary>
    /// This benchmark is a sister benchmark to <see cref="NevermoreBenchmark"/>. It helps to show the overhead that
    /// Nevermore adds compared to if we hand-coded it ourselves using SqlCommand.
    /// </summary>
    public class SqlCommandBenchmark : BenchmarkBase
    {
        SqlConnection connection;
        SqlTransaction sqlTransaction;

        public override void SetUp()
        {
            base.SetUp();
            connection = new SqlConnection(ConnectionString);
            connection.Open();
            sqlTransaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        }
        
        [Benchmark]
        public List<Post> List100Posts()
        {
            using var postCommand = new SqlCommand("select Top 100 * from Posts", connection, sqlTransaction);

            var results = new List<Post>();
            using var reader = postCommand.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult);
            while (reader.Read())
            {
                var post = new Post
                {
                    Id = reader.IsDBNull(0) ? null : reader.GetString(0),
                    Text = reader.IsDBNull(1) ? null : reader.GetString(1),
                    CreationDate = reader.GetDateTime(2),
                    LastChangeDate = reader.GetDateTime(3),

                    Counter1 = reader.IsDBNull(4) ? null : (int?)reader.GetInt32(4),
                    Counter2 = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5),
                    Counter3 = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6),
                    Counter4 = reader.IsDBNull(7) ? null : (int?)reader.GetInt32(7),
                    Counter5 = reader.IsDBNull(8) ? null : (int?)reader.GetInt32(8),
                    Counter6 = reader.IsDBNull(9) ? null : (int?)reader.GetInt32(9),
                    Counter7 = reader.IsDBNull(10) ? null : (int?)reader.GetInt32(10),
                    Counter8 = reader.IsDBNull(11) ? null : (int?)reader.GetInt32(11),
                    Counter9 = reader.IsDBNull(12) ? null : (int?)reader.GetInt32(12)
                };
                
                results.Add(post);
            }

            if (results.Count != 100)
                throw new Exception("Incorrect results");
            return results;
        }
        
        [Benchmark]
        public List<(string Id, long PostLength)> List50Tuples()
        {
            using var postCommand = new SqlCommand("select Top 50 Id, Len([Text]) as PostLength from Posts", connection, sqlTransaction);

            var results = new List<(string Id, long PostLength)>();
            using var reader = postCommand.ExecuteReader();
            while (reader.Read())
            {
                (string Id, long PostLength) post = (
                    reader.GetString(0),
                    reader.GetInt64(1)
                );
                
                results.Add(post);
            }

            if (results.Count != 50)
                throw new Exception("Incorrect results");
            return results;
        }
        
        [Benchmark]
        public List<long?> List100Primitives()
        {
            using var postCommand = new SqlCommand("select Top 100 Len([Text]) as PostLength from Posts", connection, sqlTransaction);

            var results = new List<long?>();
            using var reader = postCommand.ExecuteReader();
            while (reader.Read())
            {
                results.Add(reader.IsDBNull(0) ? null : (long?)reader.GetInt64(0));
            }

            if (results.Count != 100)
                throw new Exception("Incorrect results");
            return results;
        }
    }
}
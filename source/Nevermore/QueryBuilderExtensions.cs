using Nevermore.Advanced;
using Nevermore.Querying.AST;

namespace Nevermore
{
    public static class QueryBuilderExtensions
    {
        /// <summary>
        /// Converts a normal query into a create stored procedure query.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="storedProcedureName">The name of the stored procedure</param>
        /// <param name="schemaName">The schema name of the stored procedure</param>
        /// <returns>A plain SQL string representing a create stored procedure query</returns>
        public static string AsStoredProcedure<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string storedProcedureName, string schemaName = null) where TRecord : class
        {
            var select = queryBuilder.GetSelectBuilder().GenerateSelect();
            schemaName ??= select.Schema;
            return new StoredProcedure(select, queryBuilder.Parameters, queryBuilder.ParameterDefaults, storedProcedureName, schemaName).GenerateSql();
        }

        /// <summary>
        /// Converts a normal query into a create view query.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="viewName">The name of the view</param>
        /// <param name="schemaName">The schema name of the view</param>
        /// <returns>A plain SQL string representing a create view query</returns>
        public static string AsView<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string viewName, string schemaName = null) where TRecord : class
        {
            var select = queryBuilder.GetSelectBuilder().GenerateSelect();
            schemaName ??= select.Schema;
            return new View(select, viewName, schemaName).GenerateSql();
        }

        /// <summary>
        /// Converts a normal query into a create function query.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="functionName">The name of the function</param>
        /// <param name="schemaName">The schema name of the function</param>
        /// <returns>A plain SQL string representing a create function query</returns>
        public static string AsFunction<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string functionName, string schemaName = null) where TRecord : class
        {
            var select = queryBuilder.GetSelectBuilder().GenerateSelect();
            schemaName ??= select.Schema;
            return new Function(select, queryBuilder.Parameters, queryBuilder.ParameterDefaults, functionName, schemaName).GenerateSql();
        }
    }
}
using Nevermore.AST;

namespace Nevermore
{
    public static class QueryBuilderExtensions
    {
        public static string AsStoredProcedure<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string storedProcedureName) where TRecord : class
        {
            return new StoredProcedure(queryBuilder.GetSelectBuilder().GenerateSelect(), queryBuilder.Parameters, queryBuilder.ParameterDefaults, storedProcedureName).GenerateSql();
        }

        public static string AsView<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string viewName) where TRecord : class
        {
            return new View(queryBuilder.GetSelectBuilder().GenerateSelect(), viewName).GenerateSql();
        }

        public static string AsFunction<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string functionName) where TRecord : class
        {
            return new Function(queryBuilder.GetSelectBuilder().GenerateSelect(), queryBuilder.Parameters, queryBuilder.ParameterDefaults, functionName).GenerateSql();
        }
    }
}
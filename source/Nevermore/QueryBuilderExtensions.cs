using Nevermore.AST;

namespace Nevermore
{
    public static class QueryBuilderExtensions
    {
        public static string AsStoredProcedure<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string storedProcedureName)
        {
            return new StoredProcedure(queryBuilder.GetSelectBuilder().GenerateSelect(), queryBuilder.Parameters, storedProcedureName).GenerateSql();
        }

        public static string AsView<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string viewName)
        {
            return new View(queryBuilder.GetSelectBuilder().GenerateSelect(), viewName).GenerateSql();
        }

        public static string AsFunction<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string functionName)
        {
            return new Function(queryBuilder.GetSelectBuilder().GenerateSelect(), queryBuilder.Parameters, functionName).GenerateSql();
        }
    }
}
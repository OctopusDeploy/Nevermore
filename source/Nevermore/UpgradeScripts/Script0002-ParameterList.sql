IF NOT EXISTS (SELECT NULL FROM sys.table_types WHERE name = 'ParameterList')
    CREATE TYPE dbo.[ParameterList] as TABLE([ParameterValue] nvarchar(300))

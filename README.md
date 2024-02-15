# Npgsql.BugRepro

`dotnet test` Should be enough to show the problem.

```
  Determining projects to restore...
  All projects are up-to-date for restore.
  Npgsql.BugRepro -> /persist/code/Npgsql.BugRepro/bin/Debug/net8.0/Npgsql.BugRepro.dll
Test run for /persist/code/Npgsql.BugRepro/bin/Debug/net8.0/Npgsql.BugRepro.dll (.NETCoreApp,Version=v8.0)
Microsoft (R) Test Execution Command Line Tool Version 17.9.0 (x64)
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
  Failed TypeFilter_SplitQuery_ReturnsSingle [171 ms]
  Error Message:
   Npgsql.PostgresException : 42601: a column definition list is required for functions returning "record"

POSITION: 204
Data:
  Severity: ERROR
  InvariantSeverity: ERROR
  SqlState: 42601
  MessageText: a column definition list is required for functions returning "record"
  Position: 204
  File: parse_relation.c
  Line: 1800
  Routine: addRangeTableEntryForFunction
  Stack Trace:
     at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)
   at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)
   at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
   at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
   at Npgsql.NpgsqlDataReader.NextResult()
   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)
   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)
   at Npgsql.NpgsqlCommand.ExecuteReader(CommandBehavior behavior)
   at Npgsql.NpgsqlCommand.ExecuteDbDataReader(CommandBehavior behavior)
   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReader(RelationalCommandParameterObject parameterObject)
   at Microsoft.EntityFrameworkCore.Query.RelationalShapedQueryCompilingExpressionVisitor.ShaperProcessingExpressionVisitor.<PopulateSplitIncludeCollection>g__InitializeReader|25_1[TIncludingEntity,TIncludedEntity](RelationalQueryContext queryContext, RelationalCommandCache relationalCommandCache, IReadOnlyList`1 readerColumns, Boolean detailedErrorsEnabled)
   at Microsoft.EntityFrameworkCore.Query.RelationalShapedQueryCompilingExpressionVisitor.ShaperProcessingExpressionVisitor.<>c__25`2.<PopulateSplitIncludeCollection>b__25_0(ValueTuple`4 tup)
   at Microsoft.EntityFrameworkCore.ExecutionStrategyExtensions.<>c__DisplayClass12_0`2.<Execute>b__0(DbContext _, TState s)
   at Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.NpgsqlExecutionStrategy.Execute[TState,TResult](TState state, Func`3 operation, Func`3 verifySucceeded)
   at Microsoft.EntityFrameworkCore.ExecutionStrategyExtensions.Execute[TState,TResult](IExecutionStrategy strategy, TState state, Func`2 operation, Func`2 verifySucceeded)
   at Microsoft.EntityFrameworkCore.Query.RelationalShapedQueryCompilingExpressionVisitor.ShaperProcessingExpressionVisitor.PopulateSplitIncludeCollection[TIncludingEntity,TIncludedEntity](Int32 collectionId, RelationalQueryContext queryContext, IExecutionStrategy executionStrategy, RelationalCommandCache relationalCommandCache, IReadOnlyList`1 readerColumns, Boolean detailedErrorsEnabled, SplitQueryResultCoordinator resultCoordinator, Func`3 childIdentifier, IReadOnlyList`1 identifierValueComparers, Func`5 innerShaper, Action`3 relatedDataLoaders, INavigationBase inverseNavigation, Action`2 fixup, Boolean trackingQuery)
   at lambda_method714(Closure, QueryContext, IExecutionStrategy, SplitQueryResultCoordinator)
   at Microsoft.EntityFrameworkCore.Query.Internal.SplitQueryingEnumerable`1.Enumerator.MoveNext()
   at System.Collections.Generic.List`1..ctor(IEnumerable`1 collection)
   at System.Linq.Enumerable.ToList[TSource](IEnumerable`1 source)
   at Npgsql.BugRepro.ServiceMetaDataTests.TypeFilter_SplitQuery_ReturnsSingle() in /persist/code/Npgsql.BugRepro/ServiceMetaDataTests.cs:line 120
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)


Failed!  - Failed:     1, Passed:     4, Skipped:     0, Total:     5, Duration: 2 s - Npgsql.BugRepro.dll (net8.0)
```

If you look in the test output you will see:

```
Error: Failed executing DbCommand (2ms) [Parameters=[@__name_0='ServiceType', @__value_1='Bar'], CommandType='Text', CommandTimeout='30']
SELECT s0."Id", s0."ServiceMetadataId", s0."ServiceName", s0."Attributes", s."Id"
FROM "Services" AS s
INNER JOIN "Services" AS s0 ON s."Id" = s0."ServiceMetadataId"
WHERE EXISTS (
    SELECT 1
    FROM jsonb_to_recordset(s."Attributes") AS a
    WHERE a."Name" = @__name_0 AND a."Value" = @__value_1)
ORDER BY s."Id"
Error: An exception occurred while iterating over the results of a query for context type 'Npgsql.BugRepro.DataAccess.ServiceDbContext'.
Npgsql.PostgresException (0x80004005): 42601: a column definition list is required for functions returning "record"
```

## Manual tests

If I run SQL like that myself I get the same error...

```
SELECT s0."Id", s0."ServiceMetadataId", s0."ServiceName", s0."Attributes", s."Id"
FROM "Services" AS s
INNER JOIN "Services" AS s0 ON s."Id" = s0."ServiceMetadataId"
WHERE EXISTS (
    SELECT 1
    FROM jsonb_to_recordset(s."Attributes") AS a
    WHERE a."Name" = 'ServiceType' AND a."Value" = 'Bar')
ORDER BY s."Id"
```

But this works:

```
SELECT s0."Id", s0."ServiceMetadataId", s0."ServiceName", s0."Attributes", s."Id"
FROM "Services" AS s
INNER JOIN "Services" AS s0 ON s."Id" = s0."ServiceMetadataId"
WHERE EXISTS (
    SELECT 1
    FROM jsonb_to_recordset(s."Attributes") AS a("Name" text, "Value" text)
    WHERE a."Name" = 'ServiceType' AND a."Value" = 'Bar')
ORDER BY s."Id"
```

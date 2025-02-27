﻿using System;
using System.Data;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.ServiceModel
{
	using Linq;
	using SqlProvider;

	public abstract partial class RemoteDataContextBase
	{
		IQueryRunner IDataContext.GetQueryRunner(Query query, int queryNumber, Expression expression, object[] parameters)
		{
			ThrowOnDisposed();
			return new QueryRunner(query, queryNumber, this, expression, parameters);
		}

		class QueryRunner : QueryRunnerBase
		{
			public QueryRunner(Query query, int queryNumber, RemoteDataContextBase dataContext, Expression expression, object[] parameters)
				: base(query, queryNumber, dataContext, expression, parameters)
			{
				_dataContext = dataContext;
			}

			readonly RemoteDataContextBase _dataContext;

			ILinqClient _client;

			public override Expression MapperExpression { get; set; }

			protected override void SetQuery()
			{
			}

			#region GetSqlText

			public override string GetSqlText()
			{
				SetCommand(false);

				var query      = Query.Queries[QueryNumber];
				var sqlBuilder = DataContext.CreateSqlProvider();
				var sb         = new StringBuilder();

				sb
					.Append("-- ")
					.Append("ServiceModel")
					.Append(' ')
					.Append(DataContext.ContextID)
					.Append(' ')
					.Append(sqlBuilder.Name)
					.AppendLine();

				if (query.Statement.Parameters != null && query.Statement.Parameters.Count > 0)
				{
					foreach (var p in query.Statement.Parameters)
					{
						var value = p.Value;

						sb
							.Append("-- DECLARE ")
							.Append(p.Name)
							.Append(' ')
							.Append(value == null ? p.SystemType.ToString() : value.GetType().Name)
							.AppendLine();
					}

					sb.AppendLine();

					foreach (var p in query.Statement.Parameters)
					{
						var value = p.Value;

						if (value is string || value is char)
							value = "'" + value.ToString().Replace("'", "''") + "'";

						sb
							.Append("-- SET ")
							.Append(p.Name)
							.Append(" = ")
							.Append(value)
							.AppendLine();
					}

					sb.AppendLine();
				}

				var cc = sqlBuilder.CommandCount(query.Statement);

				for (var i = 0; i < cc; i++)
				{
					sqlBuilder.BuildSql(i, query.Statement, sb);

					if (i == 0 && query.QueryHints != null && query.QueryHints.Count > 0)
					{
						var sql = sb.ToString();

						sql = sqlBuilder.ApplyQueryHints(sql, query.QueryHints);

						sb = new StringBuilder(sql);
					}
				}

				return sb.ToString();
			}

			#endregion

			public override void Dispose()
			{
				var disposable = _client as IDisposable;
				if (disposable != null)
					disposable.Dispose();

				base.Dispose();
			}

			public override int ExecuteNonQuery()
			{
				SetCommand(true);

				var queryContext = Query.Queries[QueryNumber];

				var q = _dataContext.GetSqlOptimizer().OptimizeStatement(queryContext.Statement, _dataContext.MappingSchema);

				var data = LinqServiceSerializer.Serialize(
					q,
					q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(),
					QueryHints);

				if (_dataContext._batchCounter > 0)
				{
					_dataContext._queryBatch.Add(data);
					return -1;
				}

				_client = _dataContext.GetClient();

				return _client.ExecuteNonQuery(_dataContext.Configuration, data);
			}

			public override object ExecuteScalar()
			{
				SetCommand(true);

				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				var queryContext = Query.Queries[QueryNumber];

				_client = _dataContext.GetClient();

				var q = _dataContext.GetSqlOptimizer().OptimizeStatement(queryContext.Statement, _dataContext.MappingSchema);

				return _client.ExecuteScalar(
					_dataContext.Configuration,
					LinqServiceSerializer.Serialize(
						q,
						q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(), QueryHints));
			}

			public override IDataReader ExecuteReader()
			{
				_dataContext.ThrowOnDisposed();

				SetCommand(true);

				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				var queryContext = Query.Queries[QueryNumber];

				_client = _dataContext.GetClient();

				var q   = _dataContext.GetSqlOptimizer().OptimizeStatement(queryContext.Statement, _dataContext.MappingSchema);
				var ret = _client.ExecuteReader(
					_dataContext.Configuration,
					LinqServiceSerializer.Serialize(
						q,
						q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(),
						QueryHints));

				var result = LinqServiceSerializer.DeserializeResult(ret);

				return new ServiceModelDataReader(_dataContext.MappingSchema, result);
			}

			class DataReaderAsync : IDataReaderAsync
			{
				public DataReaderAsync(ServiceModelDataReader dataReader)
				{
					DataReader = dataReader;
				}

				public IDataReader DataReader { get; }

				static Task<bool> _trueTask;
				static Task<bool> _falseTask;

				static Task<bool> TrueTask  => _trueTask  ?? (_trueTask  = Task.FromResult(true));
				static Task<bool> FalseTask => _falseTask ?? (_falseTask = Task.FromResult(false));

				public Task<bool> ReadAsync(CancellationToken cancellationToken)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						var task = new TaskCompletionSource<bool>();
						task.SetCanceled();
						return task.Task;
					}

					try
					{
						return DataReader.Read() ? TrueTask : FalseTask;
					}
					catch (Exception ex)
					{
						var task = new TaskCompletionSource<bool>();
						task.SetException(ex);
						return task.Task;
					}
				}

				public void Dispose()
				{
					DataReader.Dispose();
				}
			}

			public override async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
			{
				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				SetCommand(true);

				var queryContext = Query.Queries[QueryNumber];

				_client = _dataContext.GetClient();

				var q   = _dataContext.GetSqlOptimizer().OptimizeStatement(queryContext.Statement, _dataContext.MappingSchema);
				var ret = await _client.ExecuteReaderAsync(
					_dataContext.Configuration,
					LinqServiceSerializer.Serialize(
						q,
						q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(),
						QueryHints)).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				var result = LinqServiceSerializer.DeserializeResult(ret);
				var reader = new ServiceModelDataReader(_dataContext.MappingSchema, result);

				return new DataReaderAsync(reader);
			}

			public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
			{
				SetCommand(true);

				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				var queryContext = Query.Queries[QueryNumber];

				_client = _dataContext.GetClient();

				var q = _dataContext.GetSqlOptimizer().OptimizeStatement(queryContext.Statement, _dataContext.MappingSchema);

				return await _client.ExecuteScalarAsync(
					_dataContext.Configuration,
					LinqServiceSerializer.Serialize(
						q,
						q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(), QueryHints)).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}

			public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
			{
				SetCommand(true);

				var queryContext = Query.Queries[QueryNumber];

				var q    = _dataContext.GetSqlOptimizer().OptimizeStatement(queryContext.Statement, _dataContext.MappingSchema);
				var data = LinqServiceSerializer.Serialize(
					q,
					q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters(),
					QueryHints);

				if (_dataContext._batchCounter > 0)
				{
					_dataContext._queryBatch.Add(data);
					return -1;
				}

				_client = _dataContext.GetClient();

				return await _client.ExecuteNonQueryAsync(_dataContext.Configuration, data).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
		}
	}
}

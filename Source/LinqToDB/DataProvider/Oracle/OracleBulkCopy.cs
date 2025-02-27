﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace LinqToDB.DataProvider.Oracle
{
	using Configuration;
	using Data;
	using SqlProvider;
	using Extensions;

	class OracleBulkCopy : BasicBulkCopy
	{
		public OracleBulkCopy(OracleDataProvider dataProvider, Type connectionType)
		{
			_dataProvider   = dataProvider;
			_connectionType = connectionType;
		}

		readonly OracleDataProvider _dataProvider;
		readonly Type               _connectionType;

		Func<IDbConnection,int,IDisposable> _bulkCopyCreator;
		Func<int,string,object>             _columnMappingCreator;
		Action<object,Action<object>>       _bulkCopySubscriber;

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			[JetBrains.Annotations.NotNull] ITable<T> table,
			BulkCopyOptions options,
			IEnumerable<T>  source)
		{
			var dataConnection = table?.DataContext as DataConnection;

			if (dataConnection == null) throw new ArgumentNullException(nameof(dataConnection));

			var sqlBuilder = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema);
			var descriptor = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var tableName  = GetTableName(sqlBuilder, options, table);

			if (dataConnection.Transaction == null)
			{
				if (_bulkCopyCreator == null)
				{
					var clientNamespace    = ((OracleDataProvider)dataConnection.DataProvider).AssemblyName + ".Client.";
					var bulkCopyType       = _connectionType.AssemblyEx().GetType(clientNamespace + "OracleBulkCopy",              false);
					var bulkCopyOptionType = _connectionType.AssemblyEx().GetType(clientNamespace + "OracleBulkCopyOptions",       false);
					var columnMappingType  = _connectionType.AssemblyEx().GetType(clientNamespace + "OracleBulkCopyColumnMapping", false);

					if (bulkCopyType != null)
					{
						_bulkCopyCreator      = CreateBulkCopyCreator(_connectionType, bulkCopyType, bulkCopyOptionType);
						_columnMappingCreator = CreateColumnMappingCreator(columnMappingType);
					}
				}

				if (_bulkCopyCreator != null)
				{
					var columns = descriptor.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
					var rd      = new BulkCopyReader(_dataProvider, dataConnection.MappingSchema, columns, source);
					var rc      = new BulkCopyRowsCopied();

					var bcOptions = 0; // Default

					if (options.UseInternalTransaction == true) bcOptions |= 1; // UseInternalTransaction = 1,

					using (var bc = _bulkCopyCreator(Proxy.GetUnderlyingObject((DbConnection)dataConnection.Connection), bcOptions))
					{
						dynamic dbc = bc;

						var notifyAfter = options.NotifyAfter == 0 && options.MaxBatchSize.HasValue ?
							options.MaxBatchSize.Value : options.NotifyAfter;

						if (notifyAfter != 0 && options.RowsCopiedCallback != null)
						{
							if (_bulkCopySubscriber == null)
								_bulkCopySubscriber = CreateBulkCopySubscriber(bc, "OracleRowsCopied");

							dbc.NotifyAfter = notifyAfter;

							_bulkCopySubscriber(bc, arg =>
							{
								dynamic darg = arg;
								rc.RowsCopied = darg.RowsCopied;
								options.RowsCopiedCallback(rc);
								if (rc.Abort)
									darg.Abort = true;
							});
						}

						if (options.MaxBatchSize.   HasValue) dbc.BatchSize       = options.MaxBatchSize.   Value;
						if (options.BulkCopyTimeout.HasValue) dbc.BulkCopyTimeout = options.BulkCopyTimeout.Value;

						dbc.DestinationTableName = tableName;

						for (var i = 0; i < columns.Count; i++)
							dbc.ColumnMappings.Add((dynamic)_columnMappingCreator(i, columns[i].ColumnName));

						TraceAction(
							dataConnection,
							() => "INSERT BULK " + tableName + Environment.NewLine,
							() => { dbc.WriteToServer(rd); return rd.Count; });
					}

					if (rc.RowsCopied != rd.Count)
					{
						rc.RowsCopied = rd.Count;

						if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
							options.RowsCopiedCallback(rc);
					}

					return rc;
				}
			}

			//options.BulkCopyType = BulkCopyType.MultipleRows;

			return MultipleRowsCopy(table, options, source);
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			switch (OracleTools.UseAlternativeBulkCopy)
			{
				case AlternativeBulkCopy.InsertInto: return OracleMultipleRowsCopy2(new MultipleRowsHelper<T>(table, options), source);
				case AlternativeBulkCopy.InsertDual: return OracleMultipleRowsCopy3(new MultipleRowsHelper<T>(table, options), source);
				default                            : return OracleMultipleRowsCopy1(new MultipleRowsHelper<T>(table, options), source);
			}
		}

		static BulkCopyRowsCopied OracleMultipleRowsCopy1(MultipleRowsHelper helper, IEnumerable source)
		{
			helper.StringBuilder.AppendLine("INSERT ALL");
			helper.SetHeader();

			foreach (var item in source)
			{
				helper.StringBuilder.AppendFormat("\tINTO {0} (", helper.TableName);

				foreach (var column in helper.Columns)
					helper.StringBuilder
						.Append(helper.SqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField))
						.Append(", ");

				helper.StringBuilder.Length -= 2;

				helper.StringBuilder.Append(") VALUES (");
				helper.BuildColumns(item, _ => _.DataType == DataType.Text || _.DataType == DataType.NText);
				helper.StringBuilder.AppendLine(")");

				helper.RowsCopied.RowsCopied++;
				helper.CurrentCount++;

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > 10000 || helper.StringBuilder.Length > 100000)
				{
					helper.StringBuilder.AppendLine("SELECT * FROM dual");
					if (!helper.Execute())
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				helper.StringBuilder.AppendLine("SELECT * FROM dual");
				helper.Execute();
			}

			return helper.RowsCopied;
		}

		static BulkCopyRowsCopied OracleMultipleRowsCopy2(MultipleRowsHelper helper, IEnumerable source)
		{
			helper.StringBuilder.AppendFormat("INSERT INTO {0} (", helper.TableName);

			foreach (var column in helper.Columns)
				helper.StringBuilder
					.Append(helper.SqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField))
					.Append(", ");

			helper.StringBuilder.Length -= 2;

			helper.StringBuilder.Append(") VALUES (");

			for (var i = 0; i < helper.Columns.Length; i++)
				helper.StringBuilder.Append(":p" + ( i + 1)).Append(", ");

			helper.StringBuilder.Length -= 2;

			helper.StringBuilder.AppendLine(")");
			helper.SetHeader();

			var list = new List<object>(31);

			foreach (var item in source)
			{
				list.Add(item);

				helper.RowsCopied.RowsCopied++;
				helper.CurrentCount++;

				if (helper.CurrentCount >= helper.BatchSize)
				{
					if (!Execute(helper, list))
						return helper.RowsCopied;

					list.Clear();
				}
			}

			if (helper.CurrentCount > 0)
			{
				Execute(helper, list);
			}

			return helper.RowsCopied;
		}

		static BulkCopyRowsCopied OracleMultipleRowsCopy3(MultipleRowsHelper helper, IEnumerable source)
		{
			helper.StringBuilder
				.AppendFormat("INSERT INTO {0}", helper.TableName).AppendLine()
				.Append("(");

			foreach (var column in helper.Columns)
				helper.StringBuilder
					.AppendLine()
					.Append("\t")
					.Append(helper.SqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField))
					.Append(",");

			helper.StringBuilder.Length--;
			helper.StringBuilder
				.AppendLine()
				.AppendLine(")")
				;

			helper.SetHeader();

			foreach (var item in source)
			{
				helper.StringBuilder
					.AppendLine()
					.Append("\tSELECT ");
				helper.BuildColumns(item, _ => _.DataType == DataType.Text || _.DataType == DataType.NText);
				helper.StringBuilder.Append(" FROM DUAL ");
				helper.StringBuilder.Append(" UNION ALL");

				helper.RowsCopied.RowsCopied++;
				helper.CurrentCount++;

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > 10000 || helper.StringBuilder.Length > 100000)
				{
					helper.StringBuilder.Length -= " UNION ALL".Length;
					helper.StringBuilder
						.AppendLine()
						;
					if (!helper.Execute())
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				helper.StringBuilder.Length -= " UNION ALL".Length;
				helper.StringBuilder
					.AppendLine()
					;
				helper.Execute();
			}

			return helper.RowsCopied;
		}

		static bool Execute(MultipleRowsHelper helper, List<object> list)
		{
			for (var i = 0; i < helper.Columns.Length; i++)
			{
				var column   = helper.Columns[i];
				var dataType = column.DataType == DataType.Undefined
					? helper.DataConnection.MappingSchema.GetDataType(column.MemberType).DataType
					: column.DataType;
				//var type     = dataConnection.DataProvider.ConvertParameterType(column.MemberType, dataType);

				helper.Parameters.Add(new DataParameter(":p" + (i + 1), list.Select(o => column.GetValue(helper.DataConnection.MappingSchema, o)).ToArray(), dataType, column.DbType)
				{
					Direction = ParameterDirection.Input,
					IsArray   = true,
				});
			}

			return helper.Execute();
		}
	}
}

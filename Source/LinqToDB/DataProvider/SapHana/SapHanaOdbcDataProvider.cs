﻿using System;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Linq;

namespace LinqToDB.DataProvider.SapHana
{
	using Configuration;
	using Common;
	using Data;
	using Extensions;
	using Mapping;
	using SqlProvider;

	public class SapHanaOdbcDataProvider : DataProviderBase
	{
		public SapHanaOdbcDataProvider()
			: this(ProviderName.SapHana, new SapHanaMappingSchema())
		{
		}

		protected SapHanaOdbcDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			//supported flags
			SqlProviderFlags.IsCountSubQuerySupported = true;

			//Exception: Sap.Data.Hana.HanaException
			//Message: single-row query returns more than one row
			//when expression returns more than 1 row
			//mark this as supported, it's better to throw exception
			//then replace with left join, in which case returns incorrect data
			SqlProviderFlags.IsSubQueryColumnSupported = true;
			SqlProviderFlags.IsTakeSupported           = true;

			//testing

			//not supported flags
			SqlProviderFlags.IsSubQueryTakeSupported   = false;
			SqlProviderFlags.IsApplyJoinSupported      = false;
			SqlProviderFlags.IsInsertOrUpdateSupported = false;

			_sqlOptimizer = new SapHanaSqlOptimizer(SqlProviderFlags);
		}

		public override string ConnectionNamespace => typeof(OdbcConnection).Namespace;
		public override Type   DataReaderType      => typeof(OdbcDataReader);

		public override bool IsCompatibleConnection(IDbConnection connection)
		{
			return typeof(OdbcConnection).IsSameOrParentOf(Proxy.GetUnderlyingObject((DbConnection)connection).GetType());
		}

		protected override IDbConnection CreateConnectionInternal(string connectionString)
		{
			return new OdbcConnection(connectionString);
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new SapHanaOdbcSchemaProvider();
		}

		public override void InitCommand(DataConnection dataConnection, CommandType commandType, string commandText, DataParameter[] parameters, bool withParameters)
		{
			if (commandType == CommandType.StoredProcedure)
			{
				commandText = $"{{ CALL {commandText} ({string.Join(",", parameters.Select(x => "?"))}) }}";
				commandType = CommandType.Text;
			}

			base.InitCommand(dataConnection, commandType, commandText, parameters, withParameters);
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new SapHanaOdbcSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, mappingSchema.ValueToSqlConverter);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override Type ConvertParameterType(Type type, DbDataType dataType)
		{
			if (type.IsNullable())
				type = type.ToUnderlying();

			switch (dataType.DataType)
			{
				case DataType.Boolean: if (type == typeof(bool)) return typeof(byte);   break;
				case DataType.Guid   : if (type == typeof(Guid)) return typeof(string); break;
			}

			return base.ConvertParameterType(type, dataType);
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DbDataType dataType, object value)
		{
			switch (dataType.DataType)
			{
				case DataType.Boolean:
					dataType = dataType.WithDataType(DataType.Byte);
					if (value is bool)
						value = (bool)value ? (byte)1 : (byte)0;
					break;
				case DataType.Guid:
					if (value != null)
						value = value.ToString();
					dataType = dataType.WithDataType(DataType.Char);
					parameter.Size = 36;
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DbDataType dataType)
		{
			if (parameter is BulkCopyReader.Parameter)
				return;
			switch (dataType.DataType)
			{
				case DataType.Boolean:
					parameter.DbType = DbType.Byte;
					return;
				case DataType.Date:
					((OdbcParameter)parameter).OdbcType = OdbcType.Date;
					return;
				case DataType.DateTime2: ((OdbcParameter)parameter).OdbcType = OdbcType.DateTime;
					return;
			}
			base.SetParameterType(parameter, dataType);
		}
	}
}

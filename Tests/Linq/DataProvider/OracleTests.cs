﻿using System;
using System.Data;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Mapping;
using LinqToDB.Tools;

using NUnit.Framework;

using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Tests.DataProvider
{
	using LinqToDB.Linq;
	using Model;

	[TestFixture]
	public class OracleTests : TestBase
	{
		string _pathThroughSql = "SELECT :p FROM sys.dual";
		string  PathThroughSql
		{
			get
			{
				_pathThroughSql += " ";
				return _pathThroughSql;
			}
		}

		[Test]
		public void TestParameters([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.VarBinary("p", null)), Is.EqualTo(null));
				Assert.That(conn.Execute<char>  (PathThroughSql, DataParameter.Char     ("p", '1')),  Is.EqualTo('1'));

				Assert.That(conn.Execute<string>(PathThroughSql,                   new { p =  1  }), Is.EqualTo("1"));
				Assert.That(conn.Execute<string>(PathThroughSql,                   new { p = "1" }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT :p FROM sys.dual",        new { p =  new DataParameter { Value = 1   } }), Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT :p1 FROM sys.dual",       new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT :p1 + :p2 FROM sys.dual", new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
				Assert.That(conn.Execute<int>   ("SELECT :p2 + :p1 FROM sys.dual", new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
			}
		}

		static void TestType<T>(
			DataConnection connection, 
			string dataTypeName, 
			T value, 
			string tableName = "AllTypes", 
			bool convertToString = false, 
			bool throwException = false)
		{
			Assert.That(connection.Execute<T>($"SELECT {dataTypeName} FROM {tableName} WHERE ID = 1"),
				Is.EqualTo(connection.MappingSchema.GetDefaultValue(typeof(T))));

			object actualValue   = connection.Execute<T>($"SELECT {dataTypeName} FROM {tableName} WHERE ID = 2");
			object expectedValue = value;

			if (convertToString)
			{
				actualValue   = actualValue.  ToString();
				expectedValue = expectedValue.ToString();
			}

			if (throwException)
			{
				if (!EqualityComparer<T>.Default.Equals((T)actualValue, (T)expectedValue))
					throw new Exception($"Expected: {expectedValue} But was: {actualValue}");
			}
			else
			{
				Assert.That(actualValue, Is.EqualTo(expectedValue));
			}
		}

		/* If this test fails for you with

		 "ORA-22288: file or LOB operation FILEOPEN failed
		 The system cannot find the path specified."

			Copy file Data\Oracle\bfile.txt to C:\DataFiles on machine with oracle server
			(of course only if it is Windows machine)

		*/
		[Test]
		public void TestDataTypes([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				TestType(conn, "bigintDataType",         1000000L);
				TestType(conn, "numericDataType",        9999999m);
				TestType(conn, "bitDataType",            true);
				TestType(conn, "smallintDataType",       (short)25555);
				TestType(conn, "decimalDataType",        2222222m);
				TestType(conn, "smallmoneyDataType",     100000m);
				TestType(conn, "intDataType",            7777777);
				TestType(conn, "tinyintDataType",        (sbyte)100);
				TestType(conn, "moneyDataType",          100000m);
				TestType(conn, "floatDataType",          20.31d);
				TestType(conn, "realDataType",           16.2f);

				TestType(conn, "datetimeDataType",       new DateTime(2012, 12, 12, 12, 12, 12));
				TestType(conn, "datetime2DataType",      new DateTime(2012, 12, 12, 12, 12, 12, 012));
				TestType(conn, "datetimeoffsetDataType", new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, new TimeSpan(-5, 0, 0)));

				try
				{
					var dt = new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, TimeSpan.Zero);
					TestType(conn, "localZoneDataType", new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, TimeZoneInfo.Local.GetUtcOffset(dt) /* new TimeSpan(-4, 0, 0)*/), throwException:true);
				}
				catch (Exception ex)
					when (
						ex.Message.Replace(" ", "") == "Expected: 2012-12-12 12:12:12.012-05:00 But was: 2012-12-12 12:12:12.012-04:00".Replace(" ", "") ||
						ex.Message.Replace(" ", "") == "Expected: 12/12/2012 12:12:12 PM -05:00 But was: 12/12/2012 12:12:12 PM -04:00".Replace(" ", ""))
				{
				}

				TestType(conn, "charDataType",           '1');
				TestType(conn, "varcharDataType",        "234");
				TestType(conn, "textDataType",           "567");
				TestType(conn, "ncharDataType",          "23233");
				TestType(conn, "nvarcharDataType",       "3323");
				TestType(conn, "ntextDataType",          "111");

				TestType(conn, "binaryDataType",         new byte[] { 0, 170 });
				TestType(conn, "bfileDataType",          new byte[] { 49, 50, 51, 52, 53 });

				if (((OracleDataProvider)conn.DataProvider).IsXmlTypeSupported)
				{
					var res = "<root><element strattr=\"strvalue\" intattr=\"12345\"/></root>";

					TestType(conn, "XMLSERIALIZE(DOCUMENT xmlDataType AS CLOB NO INDENT)", res);
				}
			}
		}

		void TestNumeric<T>(DataConnection conn, T expectedValue, DataType dataType, string skip = "")
		{
			var skipTypes = skip.Split(' ');

			foreach (var sqlType in new[]
				{
					"number",
					"number(10,0)",
					"number(20,0)",
					"binary_float",
					"binary_double"
				}.Except(skipTypes))
			{
				var sqlValue = expectedValue is bool ? (bool)(object)expectedValue? 1 : 0 : (object)expectedValue;

				var sql = string.Format(CultureInfo.InvariantCulture, "SELECT Cast({0} as {1}) FROM sys.dual", sqlValue ?? "NULL", sqlType);

				Debug.WriteLine(sql + " -> " + typeof(T));

				Assert.That(conn.Execute<T>(sql), Is.EqualTo(expectedValue));
			}

			Debug.WriteLine("{0} -> DataType.{1}",  typeof(T), dataType);
			Assert.That(conn.Execute<T>(PathThroughSql, new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> auto", typeof(T));
			Assert.That(conn.Execute<T>(PathThroughSql, new DataParameter { Name = "p", Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> new",  typeof(T));
			Assert.That(conn.Execute<T>(PathThroughSql, new { p = expectedValue }), Is.EqualTo(expectedValue));
		}

		void TestSimple<T>(DataConnection conn, T expectedValue, DataType dataType)
			where T : struct
		{
			TestNumeric<T> (conn, expectedValue, dataType);
			TestNumeric<T?>(conn, expectedValue, dataType);
			TestNumeric<T?>(conn, (T?)null,      dataType);
		}

		[Test]
		public void TestNumerics([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				TestSimple<bool>   (conn, true, DataType.Boolean);
				TestSimple<sbyte>  (conn, 1,    DataType.SByte);
				TestSimple<short>  (conn, 1,    DataType.Int16);
				TestSimple<int>    (conn, 1,    DataType.Int32);
				TestSimple<long>   (conn, 1L,   DataType.Int64);
				TestSimple<byte>   (conn, 1,    DataType.Byte);
				TestSimple<ushort> (conn, 1,    DataType.UInt16);
				TestSimple<uint>   (conn, 1u,   DataType.UInt32);
				TestSimple<ulong>  (conn, 1ul,  DataType.UInt64);
				TestSimple<float>  (conn, 1,    DataType.Single);
				TestSimple<double> (conn, 1d,   DataType.Double);
				TestSimple<decimal>(conn, 1m,   DataType.Decimal);
				TestSimple<decimal>(conn, 1m,   DataType.VarNumeric);
				TestSimple<decimal>(conn, 1m,   DataType.Money);
				TestSimple<decimal>(conn, 1m,   DataType.SmallMoney);

				TestNumeric(conn, sbyte.MinValue,    DataType.SByte);
				TestNumeric(conn, sbyte.MaxValue,    DataType.SByte);
				TestNumeric(conn, short.MinValue,    DataType.Int16);
				TestNumeric(conn, short.MaxValue,    DataType.Int16);
				TestNumeric(conn, int.MinValue,      DataType.Int32);
				TestNumeric(conn, int.MaxValue,      DataType.Int32,      "binary_float");
				TestNumeric(conn, long.MinValue,     DataType.Int64,      "number(10,0)");
				TestNumeric(conn, long.MaxValue,     DataType.Int64,      "number(10,0) binary_float binary_double");

				TestNumeric(conn, byte.MaxValue,     DataType.Byte,       "");
				TestNumeric(conn, ushort.MaxValue,   DataType.UInt16,     "");
				TestNumeric(conn, uint.MaxValue,     DataType.UInt32,     "binary_float");
				TestNumeric(conn, ulong.MaxValue,    DataType.UInt64,     "number(10,0) binary_float binary_double");

				TestNumeric(conn, -3.4E+28f,         DataType.Single,     "number number(10,0) number(20,0)");
				TestNumeric(conn, +3.4E+28f,         DataType.Single,     "number number(10,0) number(20,0)");
				TestNumeric(conn, decimal.MinValue,  DataType.Decimal,    "number(10,0) number(20,0) binary_float binary_double");
				TestNumeric(conn, decimal.MaxValue,  DataType.Decimal,    "number(10,0) number(20,0) binary_float binary_double");
				TestNumeric(conn, decimal.MinValue,  DataType.VarNumeric, "number(10,0) number(20,0) binary_float binary_double");
				TestNumeric(conn, decimal.MaxValue,  DataType.VarNumeric, "number(10,0) number(20,0) binary_float binary_double");
				TestNumeric(conn, -922337203685477m, DataType.Money,      "number(10,0) binary_float");
				TestNumeric(conn, +922337203685477m, DataType.Money,      "number(10,0) binary_float");
				TestNumeric(conn, -214748m,          DataType.SmallMoney);
				TestNumeric(conn, +214748m,          DataType.SmallMoney);
			}
		}

		[Test]
		public void TestDate([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12);

				Assert.That(conn.Execute<DateTime> (PathThroughSql, DataParameter.Date("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>(PathThroughSql, new DataParameter("p", dateTime, DataType.Date)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestSmallDateTime([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 00);

				Assert.That(conn.Execute<DateTime> (PathThroughSql, DataParameter.SmallDateTime("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>(PathThroughSql, new DataParameter("p", dateTime, DataType.SmallDateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestDateTime([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT to_date('2012-12-12 12:12:12', 'YYYY-MM-DD HH:MI:SS') FROM sys.dual"), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT to_date('2012-12-12 12:12:12', 'YYYY-MM-DD HH:MI:SS') FROM sys.dual"), Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime> (PathThroughSql, DataParameter.DateTime("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>(PathThroughSql, new DataParameter("p", dateTime)),                    Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>(PathThroughSql, new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestDateTime2([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime1 = new DateTime(2012, 12, 12, 12, 12, 12);
				var dateTime2 = new DateTime(2012, 12, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime?>("SELECT timestamp '2012-12-12 12:12:12.012' FROM sys.dual"), Is.EqualTo(dateTime2));

				Assert.That(conn.Execute<DateTime> (PathThroughSql, DataParameter.DateTime2("p", dateTime2)),               Is.EqualTo(dateTime2));
				Assert.That(conn.Execute<DateTime> (PathThroughSql, DataParameter.Create   ("p", dateTime2)),               Is.EqualTo(dateTime2));
				Assert.That(conn.Execute<DateTime?>(PathThroughSql, new DataParameter("p", dateTime2, DataType.DateTime2)), Is.EqualTo(dateTime2));
			}
		}

		[Test]
		public void TestDateTimeOffset([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dto = new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, new TimeSpan(5, 0, 0));

				Assert.That(conn.Execute<DateTimeOffset>(
					"SELECT timestamp '2012-12-12 12:12:12.012' FROM sys.dual"),
					Is.EqualTo(new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2012, 12, 12, 12, 12, 12)))));

				Assert.That(conn.Execute<DateTimeOffset?>(
					"SELECT timestamp '2012-12-12 12:12:12.012' FROM sys.dual"),
					Is.EqualTo(new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2012, 12, 12, 12, 12, 12)))));

				Assert.That(conn.Execute<DateTime>(
					"SELECT timestamp '2012-12-12 12:12:12.012 -04:00' FROM sys.dual"),
					Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));

				Assert.That(conn.Execute<DateTime?>(
					"SELECT timestamp '2012-12-12 12:12:12.012 -04:00' FROM sys.dual"),
					Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));

				Assert.That(conn.Execute<DateTimeOffset>(
					"SELECT timestamp '2012-12-12 12:12:12.012 +05:00' FROM sys.dual"),
					Is.EqualTo(dto));

				Assert.That(conn.Execute<DateTimeOffset?>(
					"SELECT timestamp '2012-12-12 12:12:12.012 +05:00' FROM sys.dual"),
					Is.EqualTo(dto));

				Assert.That(conn.Execute<DateTime> ("SELECT datetimeoffsetDataType FROM AllTypes WHERE ID = 1"), Is.EqualTo(default(DateTime)));
				Assert.That(conn.Execute<DateTime?>("SELECT datetimeoffsetDataType FROM AllTypes WHERE ID = 1"), Is.EqualTo(default(DateTime?)));

				Assert.That(conn.Execute<DateTimeOffset?>(PathThroughSql, new DataParameter("p", dto)).                         ToString(), Is.EqualTo(dto.ToString()));
				Assert.That(conn.Execute<DateTimeOffset?>(PathThroughSql, new DataParameter("p", dto, DataType.DateTimeOffset)).ToString(), Is.EqualTo(dto.ToString()));
			}
		}

		[Test]
		public void TestChar([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char)    FROM sys.dual"),       Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char)    FROM sys.dual"),       Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char(1)) FROM sys.dual"),       Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char(1)) FROM sys.dual"),       Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast('1' as varchar2(20)) FROM sys.dual"),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar2(20)) FROM sys.dual"),  Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast('1' as nchar)     FROM sys.dual"),     Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nchar)     FROM sys.dual"),     Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as nchar(20)) FROM sys.dual"),     Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nchar(20)) FROM sys.dual"),     Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast('1' as nvarchar2(20)) FROM sys.dual"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nvarchar2(20)) FROM sys.dual"), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> (PathThroughSql, DataParameter.Char    ("p", '1')),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, DataParameter.Char    ("p", '1')),  Is.EqualTo('1'));

				Assert.That(conn.Execute<char> (PathThroughSql, DataParameter.VarChar ("p", '1')),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, DataParameter.VarChar ("p", '1')),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char> (PathThroughSql, DataParameter.NChar   ("p", '1')),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, DataParameter.NChar   ("p", '1')),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char> (PathThroughSql, DataParameter.NVarChar("p", '1')),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, DataParameter.NVarChar("p", '1')),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char> (PathThroughSql, DataParameter.Create  ("p", '1')),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, DataParameter.Create  ("p", '1')),  Is.EqualTo('1'));

				Assert.That(conn.Execute<char> (PathThroughSql, new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
			}
		}

		[Test]
		public void TestString([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20)) FROM sys.dual"),     Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as char(20)) FROM sys.dual"),     Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar2(20)) FROM sys.dual"),  Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as varchar2(20)) FROM sys.dual"),  Is.Null);

				Assert.That(conn.Execute<string>("SELECT textDataType FROM AllTypes WHERE ID = 2"),      Is.EqualTo("567"));
				Assert.That(conn.Execute<string>("SELECT textDataType FROM AllTypes WHERE ID = 1"),      Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nchar(20)) FROM sys.dual"),     Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as nchar(20)) FROM sys.dual"),     Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nvarchar2(20)) FROM sys.dual"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as nvarchar2(20)) FROM sys.dual"), Is.Null);

				Assert.That(conn.Execute<string>("SELECT ntextDataType FROM AllTypes WHERE ID = 2"),     Is.EqualTo("111"));
				Assert.That(conn.Execute<string>("SELECT ntextDataType FROM AllTypes WHERE ID = 1"),     Is.Null);

				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.Char    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.VarChar ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.Text    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.NChar   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.NText   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.Create  ("p", "123")), Is.EqualTo("123"));

				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.Create("p", (string)null)), Is.EqualTo(null));
				Assert.That(conn.Execute<string>(PathThroughSql, new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestBinary([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var arr1 = new byte[] {       0x30, 0x39 };
			var arr2 = new byte[] { 0, 0, 0x30, 0x39 };

			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<byte[]>("SELECT to_blob('3039')     FROM sys.dual"), Is.EqualTo(           arr1));
				Assert.That(conn.Execute<Binary>("SELECT to_blob('00003039') FROM sys.dual"), Is.EqualTo(new Binary(arr2)));

				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.VarBinary("p", null)), Is.EqualTo(null));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.Binary   ("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.VarBinary("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.Create   ("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.VarBinary("p", new byte[0])), Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.Image    ("p", new byte[0])), Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.Create   ("p", new Binary(arr1))), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, new DataParameter("p", new Binary(arr1))), Is.EqualTo(arr1));
			}
		}

		[Test]
		public void TestOracleManagedTypes([IncludeDataSources(ProviderName.OracleManaged)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var arr = new byte[] { 0x30, 0x39 };

				Assert.That(conn.Execute<Oracle.ManagedDataAccess.Types.OracleBinary>   ("SELECT to_blob('3039')          FROM sys.dual").      Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<Oracle.ManagedDataAccess.Types.OracleBlob>     ("SELECT to_blob('3039')          FROM sys.dual").      Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<Oracle.ManagedDataAccess.Types.OracleDecimal>  ("SELECT Cast(1       as decimal) FROM sys.dual").      Value, Is.EqualTo(1));
				Assert.That(conn.Execute<Oracle.ManagedDataAccess.Types.OracleString>   ("SELECT Cast('12345' as char(6)) FROM sys.dual").      Value, Is.EqualTo("12345 "));
				Assert.That(conn.Execute<Oracle.ManagedDataAccess.Types.OracleClob>     ("SELECT ntextDataType     FROM AllTypes WHERE ID = 2").Value, Is.EqualTo("111"));
				Assert.That(conn.Execute<Oracle.ManagedDataAccess.Types.OracleDate>     ("SELECT datetimeDataType  FROM AllTypes WHERE ID = 2").Value, Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(conn.Execute<Oracle.ManagedDataAccess.Types.OracleTimeStamp>("SELECT datetime2DataType FROM AllTypes WHERE ID = 2").Value, Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));
			}
		}

#if NET46

		[Test]
		public void TestOracleNativeTypes([IncludeDataSources(ProviderName.OracleNative)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var arr = new byte[] { 0x30, 0x39 };

				Assert.That(conn.Execute<Oracle.DataAccess.Types.OracleBinary>   ("SELECT to_blob('3039')          FROM sys.dual").      Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<Oracle.DataAccess.Types.OracleBlob>     ("SELECT to_blob('3039')          FROM sys.dual").      Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<Oracle.DataAccess.Types.OracleDecimal>  ("SELECT Cast(1       as decimal) FROM sys.dual").      Value, Is.EqualTo(1));
				Assert.That(conn.Execute<Oracle.DataAccess.Types.OracleString>   ("SELECT Cast('12345' as char(6)) FROM sys.dual").      Value, Is.EqualTo("12345 "));
				Assert.That(conn.Execute<Oracle.DataAccess.Types.OracleClob>     ("SELECT ntextDataType     FROM AllTypes WHERE ID = 2").Value, Is.EqualTo("111"));
				Assert.That(conn.Execute<Oracle.DataAccess.Types.OracleDate>     ("SELECT datetimeDataType  FROM AllTypes WHERE ID = 2").Value, Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(conn.Execute<Oracle.DataAccess.Types.OracleTimeStamp>("SELECT datetime2DataType FROM AllTypes WHERE ID = 2").Value, Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));
			}
		}

#endif

		[Test]
		public void TestGuid([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var guid = conn.Execute<Guid>("SELECT guidDataType FROM AllTypes WHERE ID = 2");

				Assert.That(conn.Execute<Guid?>("SELECT guidDataType FROM AllTypes WHERE ID = 1"), Is.EqualTo(null));
				Assert.That(conn.Execute<Guid?>("SELECT guidDataType FROM AllTypes WHERE ID = 2"), Is.EqualTo(guid));

				Assert.That(conn.Execute<Guid>(PathThroughSql, DataParameter.Create("p", guid)),                Is.EqualTo(guid));
				Assert.That(conn.Execute<Guid>(PathThroughSql, new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test]
		public void TestXml([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			if (OracleTools.IsXmlTypeSupported)
			{
				using (var conn = new DataConnection(context))
				{
					Assert.That(conn.Execute<string>     ("SELECT XMLTYPE('<xml/>') FROM sys.dual").TrimEnd(),  Is.EqualTo("<xml/>"));
					Assert.That(conn.Execute<XDocument>  ("SELECT XMLTYPE('<xml/>') FROM sys.dual").ToString(), Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XmlDocument>("SELECT XMLTYPE('<xml/>') FROM sys.dual").InnerXml,   Is.EqualTo("<xml />"));

					var xdoc = XDocument.Parse("<xml/>");
					var xml  = Convert<string,XmlDocument>.Lambda("<xml/>");

					Assert.That(conn.Execute<string>     (PathThroughSql, DataParameter.Xml("p", "<xml/>")),        Is.EqualTo("<xml/>"));
					Assert.That(conn.Execute<XDocument>  (PathThroughSql, DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XmlDocument>(PathThroughSql, DataParameter.Xml("p", xml)). InnerXml,   Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XDocument>  (PathThroughSql, new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XDocument>  (PathThroughSql, new DataParameter("p", xml)). ToString(), Is.EqualTo("<xml />"));
				}
			}
		}

		enum TestEnum
		{
			[MapValue("A")] AA,
			[MapValue("B")] BB,
		}

		[Test]
		public void TestEnum1([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<TestEnum> ("SELECT 'A' FROM sys.dual"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'A' FROM sys.dual"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum> ("SELECT 'B' FROM sys.dual"), Is.EqualTo(TestEnum.BB));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'B' FROM sys.dual"), Is.EqualTo(TestEnum.BB));
			}
		}

		[Test]
		public void TestEnum2([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>(PathThroughSql, new { p = TestEnum.AA }),            Is.EqualTo("A"));
				Assert.That(conn.Execute<string>(PathThroughSql, new { p = (TestEnum?)TestEnum.BB }), Is.EqualTo("B"));

				Assert.That(conn.Execute<string>(PathThroughSql, new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>(PathThroughSql, new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>(PathThroughSql, new { p = conn.MappingSchema.GetConverter<TestEnum?,string>()(TestEnum.AA) }), Is.EqualTo("A"));
			}
		}

		[Test]
		public void TestTreatEmptyStringsAsNulls([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var table    = db.GetTable<OracleSpecific.StringTest>();
				var expected = table.Where(_ => _.KeyValue == "NullValues").ToList();


				AreEqual(expected, table.Where(_ => string.IsNullOrEmpty(_.StringValue1)));
				AreEqual(expected, table.Where(_ => string.IsNullOrEmpty(_.StringValue2)));

				AreEqual(expected, table.Where(_ => _.StringValue1 == ""));
				AreEqual(expected, table.Where(_ => _.StringValue2 == ""));

				AreEqual(expected, table.Where(_ => _.StringValue1 == null));
				AreEqual(expected, table.Where(_ => _.StringValue2 == null));

				string emptyString = string.Empty;
				string nullString  = null;

				AreEqual(expected, table.Where(_ => _.StringValue1 == emptyString));
				AreEqual(expected, table.Where(_ => _.StringValue2 == emptyString));

				AreEqual(expected, table.Where(_ => _.StringValue1 == nullString));
				AreEqual(expected, table.Where(_ => _.StringValue2 == nullString));

				AreEqual(expected, GetStringTest1(db, emptyString));
				AreEqual(expected, GetStringTest1(db, emptyString));

				AreEqual(expected, GetStringTest2(db, emptyString));
				AreEqual(expected, GetStringTest2(db, emptyString));

				AreEqual(expected, GetStringTest1(db, nullString));
				AreEqual(expected, GetStringTest1(db, nullString));

				AreEqual(expected, GetStringTest2(db, nullString));
				AreEqual(expected, GetStringTest2(db, nullString));
			}
		}

		private IEnumerable<OracleSpecific.StringTest> GetStringTest1(IDataContext db, string value)
		{
			return db.GetTable<OracleSpecific.StringTest>()
				.Where(_ => value == _.StringValue1);
		}

		private IEnumerable<OracleSpecific.StringTest> GetStringTest2(IDataContext db, string value)
		{
			return db.GetTable<OracleSpecific.StringTest>()
				.Where(_ => value == _.StringValue2);
		}

#region DateTime Tests

		[Table(Name="ALLTYPES")]
		public partial class ALLTYPE
		{
			[Column(DataType=DataType.Decimal,        Length=22, Scale=0),               PrimaryKey,  NotNull] public decimal         ID                     { get; set; } // NUMBER
			[Column(DataType=DataType.Decimal,        Length=22, Precision=20, Scale=0),    Nullable         ] public decimal?        BIGINTDATATYPE         { get; set; } // NUMBER (20,0)
			[Column(DataType=DataType.Decimal,        Length=22, Scale=0),                  Nullable         ] public decimal?        NUMERICDATATYPE        { get; set; } // NUMBER
			[Column(DataType=DataType.Decimal,        Length=22, Precision=1, Scale=0),     Nullable         ] public sbyte?          BITDATATYPE            { get; set; } // NUMBER (1,0)
			[Column(DataType=DataType.Decimal,        Length=22, Precision=5, Scale=0),     Nullable         ] public int?            SMALLINTDATATYPE       { get; set; } // NUMBER (5,0)
			[Column(DataType=DataType.Decimal,        Length=22, Scale=6),                  Nullable         ] public decimal?        DECIMALDATATYPE        { get; set; } // NUMBER
			[Column(DataType=DataType.Decimal,        Length=22, Precision=10, Scale=4),    Nullable         ] public decimal?        SMALLMONEYDATATYPE     { get; set; } // NUMBER (10,4)
			[Column(DataType=DataType.Decimal,        Length=22, Precision=10, Scale=0),    Nullable         ] public long?           INTDATATYPE            { get; set; } // NUMBER (10,0)
			[Column(DataType=DataType.Decimal,        Length=22, Precision=3, Scale=0),     Nullable         ] public short?          TINYINTDATATYPE        { get; set; } // NUMBER (3,0)
			[Column(DataType=DataType.Decimal,        Length=22),                           Nullable         ] public decimal?        MONEYDATATYPE          { get; set; } // NUMBER
			[Column(DataType=DataType.Double,         Length=8),                            Nullable         ] public double?         FLOATDATATYPE          { get; set; } // BINARY_DOUBLE
			[Column(DataType=DataType.Single,         Length=4),                            Nullable         ] public float?          REALDATATYPE           { get; set; } // BINARY_FLOAT
			[Column(DataType=DataType.Date),                                                Nullable         ] public DateTime?       DATETIMEDATATYPE       { get; set; } // DATE
			[Column(DataType=DataType.DateTime2,      Length=11, Scale=6),                  Nullable         ] public DateTime?       DATETIME2DATATYPE      { get; set; } // TIMESTAMP(6)
			[Column(DataType=DataType.DateTimeOffset, Length=13, Scale=6),                  Nullable         ] public DateTimeOffset? DATETIMEOFFSETDATATYPE { get; set; } // TIMESTAMP(6) WITH TIME ZONE
			[Column(DataType=DataType.DateTimeOffset, Length=11, Scale=6),                  Nullable         ] public DateTimeOffset? LOCALZONEDATATYPE      { get; set; } // TIMESTAMP(6) WITH LOCAL TIME ZONE
			[Column(DataType=DataType.Char,           Length=1),                            Nullable         ] public char?           CHARDATATYPE           { get; set; } // CHAR(1)
			[Column(DataType=DataType.VarChar,        Length=20),                           Nullable         ] public string          VARCHARDATATYPE        { get; set; } // VARCHAR2(20)
			[Column(DataType=DataType.Text,           Length=4000),                         Nullable         ] public string          TEXTDATATYPE           { get; set; } // CLOB
			[Column(DataType=DataType.NChar,          Length=40),                           Nullable         ] public string          NCHARDATATYPE          { get; set; } // NCHAR(40)
			[Column(DataType=DataType.NVarChar,       Length=40),                           Nullable         ] public string          NVARCHARDATATYPE       { get; set; } // NVARCHAR2(40)
			[Column(DataType=DataType.NText,          Length=4000),                         Nullable         ] public string          NTEXTDATATYPE          { get; set; } // NCLOB
			[Column(DataType=DataType.Blob,           Length=4000),                         Nullable         ] public byte[]          BINARYDATATYPE         { get; set; } // BLOB
			[Column(DataType=DataType.VarBinary,      Length=530),                          Nullable         ] public byte[]          BFILEDATATYPE          { get; set; } // BFILE
			[Column(DataType=DataType.Binary,         Length=16),                           Nullable         ] public byte[]          GUIDDATATYPE           { get; set; } // RAW(16)
			[Column(DataType=DataType.Undefined,      Length=256),                          Nullable         ] public object          URIDATATYPE            { get; set; } // URITYPE
			[Column(DataType=DataType.Xml,            Length=2000),                         Nullable         ] public string          XMLDATATYPE            { get; set; } // XMLTYPE
		}

		[Table("t_entity")]
		public sealed class Entity
		{
			[PrimaryKey, Identity]
			[NotNull, Column("entity_id")] public long Id           { get; set; }
			[NotNull, Column("time")]      public DateTime Time     { get; set; }
			[NotNull, Column("duration")]  public TimeSpan Duration { get; set; }
		}

		[Test]
		public void TestTimeSpan([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = new DataConnection(context))
			{
				db.BeginTransaction();

				long id = 1;

				db.GetTable<Entity>().Insert(() => new Entity { Id = id + 1, Duration = TimeSpan.FromHours(1) });
			}
		}

		[Test]
		public void DateTimeTest1([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = new DataConnection(context))
			{
				db.GetTable<ALLTYPE>().Delete(t => t.ID >= 1000);

				db.BeginTransaction();

				db.MultipleRowsCopy(new[]
				{
					new ALLTYPE
					{
						ID                = 1000,
						DATETIMEDATATYPE  = DateTime.Now,
						DATETIME2DATATYPE = DateTime.Now
					}
				});
			}
		}

		[Test]
		public void NVarchar2InsertTest([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = new DataConnection(context))
			using (db.BeginTransaction())
			{
				db.InlineParameters = false;

				var value   = "致我们最爱的母亲";

				var id = db.GetTable<ALLTYPE>()
					.InsertWithInt32Identity(() => new ALLTYPE
					{
						NVARCHARDATATYPE = value
					});

				var query = from p in db.GetTable<ALLTYPE>()
							where p.ID == id
							select new { p.NVARCHARDATATYPE };

				var res = query.Single();
				Assert.That(res.NVARCHARDATATYPE, Is.EqualTo(value));
			}
		}

		[Test]
		public void NVarchar2UpdateTest([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = new DataConnection(context))
			using (db.BeginTransaction())
			{
				db.InlineParameters = false;

				var value = "致我们最爱的母亲";

				var id = db.GetTable<ALLTYPE>()
					.InsertWithInt32Identity(() => new ALLTYPE
					{
						INTDATATYPE = 123
					});

				db.GetTable<ALLTYPE>()
					.Set(e => e.NVARCHARDATATYPE, () => value)
					.Update();

				var query = from p in db.GetTable<ALLTYPE>()
							where p.ID == id
							select new { p.NVARCHARDATATYPE };

				var res = query.Single();
				Assert.That(res.NVARCHARDATATYPE, Is.EqualTo(value));
			}
		}

		[Test]
		public void SelectDateTime([IncludeDataSources(ProviderName.OracleNative)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var ms = new MappingSchema();

				// Set custom DateTime to SQL converter.
				//
				ms.SetValueToSqlConverter(
					typeof(DateTime),
					(stringBuilder, dataType, val) =>
					{
						var value = (DateTime)val;
						Assert.That(dataType.DataType, Is.Not.EqualTo(DataType.Undefined));

						var format =
							dataType.DataType == DataType.DateTime2 ?
								"TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')" :
								"TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.fffffff}', 'YYYY-MM-DD HH24:MI:SS.FF7')";

						stringBuilder.AppendFormat(format, value);
					});

				db.AddMappingSchema(ms);

				var res = (db.GetTable<ALLTYPE>().Where(e => e.DATETIME2DATATYPE == DateTime.Now)).ToList();
				Debug.WriteLine(res.Count);
			}
		}

		[Test]
		public void DateTimeTest2([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			// Set custom DateTime to SQL converter.
			//
			OracleTools.GetDataProvider().MappingSchema.SetValueToSqlConverter(
				typeof(DateTime),
				(stringBuilder,dataType,val) =>
				{
					var value  = (DateTime)val;
					var format =
						dataType.DataType == DataType.DateTime ?
							"TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')" :
							"TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.fffffff}', 'YYYY-MM-DD HH24:MI:SS.FF7')";

					stringBuilder.AppendFormat(format, value);
				});

			using (var db = new DataConnection(context))
			{
				db.GetTable<ALLTYPE>().Delete(t => t.ID >= 1000);

				db.BeginTransaction();

				db.MultipleRowsCopy(new[]
				{
					new ALLTYPE
					{
						ID                = 1000,
						DATETIMEDATATYPE  = DateTime.Now,
						DATETIME2DATATYPE = DateTime.Now
					}
				});
			}

			// Reset converter to default.
			//
			OracleTools.GetDataProvider().MappingSchema.SetValueToSqlConverter(
				typeof(DateTime),
				(stringBuilder,dataType,val) =>
				{
					var value  = (DateTime)val;
					var format =
						dataType.DataType == DataType.DateTime ?
							"TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')" :
							"TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.fffffff}', 'YYYY-MM-DD HH24:MI:SS.FF7')";

					if (value.Millisecond == 0)
					{
						format = value.Hour == 0 && value.Minute == 0 && value.Second == 0 ?
							"TO_DATE('{0:yyyy-MM-dd}', 'YYYY-MM-DD')" :
							"TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')";
					}

					stringBuilder.AppendFormat(format, value);
				});
		}

		[Test]
		public void ClauseDateTimeWithoutJointure([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var date = DateTime.Today;
			using (var db = new DataConnection(context))
			{
				var query = from a in db.GetTable<ALLTYPE>()
							where a.DATETIMEDATATYPE == date
							select a;

				query.FirstOrDefault();

				Assert.That(db.Command.Parameters.Count, Is.EqualTo(1));

				var parm = (IDbDataParameter)db.Command.Parameters[0];
				Assert.That(parm.DbType, Is.EqualTo(DbType.Date));
			}
		}

		[Test]
		public void ClauseDateTimeWithJointure([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var date = DateTime.Today;
			using (var db = new DataConnection(context))
			{
				var query = from a in db.GetTable<ALLTYPE>()
							join b in db.GetTable<ALLTYPE>() on a.ID equals b.ID
							where a.DATETIMEDATATYPE == date
							select a;

				query.FirstOrDefault();

				Assert.That(db.Command.Parameters.Count, Is.EqualTo(1));

				var parm = (IDbDataParameter)db.Command.Parameters[0];
				Assert.That(parm.DbType, Is.EqualTo(DbType.Date));
			}
		}

#endregion

#region Sequence

		[Test]
		public void SequenceInsert([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<OracleSpecific.SequenceTest>().Where(_ => _.Value == "SeqValue").Delete();
				db.Insert(new OracleSpecific.SequenceTest { Value = "SeqValue" });

				var id = db.GetTable<OracleSpecific.SequenceTest>().Single(_ => _.Value == "SeqValue").ID;

				db.GetTable<OracleSpecific.SequenceTest>().Where(_ => _.ID == id).Delete();

				Assert.AreEqual(0, db.GetTable<OracleSpecific.SequenceTest>().Count(_ => _.Value == "SeqValue"));
			}
		}

		[Test]
		public void SequenceInsertWithIdentity([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<OracleSpecific.SequenceTest>().Where(_ => _.Value == "SeqValue").Delete();

				var id1 = Convert.ToInt32(db.InsertWithIdentity(new OracleSpecific.SequenceTest { Value = "SeqValue" }));
				var id2 = db.GetTable<OracleSpecific.SequenceTest>().Single(_ => _.Value == "SeqValue").ID;

				Assert.AreEqual(id1, id2);

				db.GetTable<OracleSpecific.SequenceTest>().Where(_ => _.ID == id1).Delete();

				Assert.AreEqual(0, db.GetTable<OracleSpecific.SequenceTest>().Count(_ => _.Value == "SeqValue"));
			}
		}

#endregion

#region BulkCopy

		static void BulkCopyLinqTypes(string context, BulkCopyType bulkCopyType)
		{
			using (var db = new DataConnection(context))
			{
				if (bulkCopyType == BulkCopyType.ProviderSpecific)
				{
					var ms = new MappingSchema();

					ms.GetFluentMappingBuilder()
						.Entity<LinqDataTypes>()
							.Property(e => e.GuidValue)
								.IsNotColumn()
						;

					db.AddMappingSchema(ms);
				}

				db.BulkCopy(
					new BulkCopyOptions { BulkCopyType = bulkCopyType },
					Enumerable.Range(0, 10).Select(n =>
						new LinqDataTypes
						{
							ID            = 4000 + n,
							MoneyValue    = 1000m + n,
							DateTimeValue = new DateTime(2001,  1,  11,  1, 11, 21, 100),
							BoolValue     = true,
							GuidValue     = Guid.NewGuid(),
							SmallIntValue = (short)n
						}
					));

				db.GetTable<LinqDataTypes>().Delete(p => p.ID >= 4000);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void BulkCopyLinqTypesMultipleRows(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(
				AlternativeBulkCopy.InsertAll,
				AlternativeBulkCopy.InsertDual,
				AlternativeBulkCopy.InsertInto)]
			AlternativeBulkCopy useAlternativeBulkCopy)
		{
			try
			{
				OracleTools.UseAlternativeBulkCopy = useAlternativeBulkCopy;

				BulkCopyLinqTypes(context, BulkCopyType.MultipleRows);
			}
			finally
			{
				OracleTools.UseAlternativeBulkCopy = AlternativeBulkCopy.InsertAll;
			}
		}

		[Test]
		public void BulkCopyLinqTypesProviderSpecific(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(
				AlternativeBulkCopy.InsertAll,
				AlternativeBulkCopy.InsertDual,
				AlternativeBulkCopy.InsertInto)]
			AlternativeBulkCopy useAlternativeBulkCopy)
		{
			try
			{
				OracleTools.UseAlternativeBulkCopy = useAlternativeBulkCopy;

				BulkCopyLinqTypes(context, BulkCopyType.ProviderSpecific);
			}
			finally
			{
				OracleTools.UseAlternativeBulkCopy = AlternativeBulkCopy.InsertAll;
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void BulkCopyRetrieveSequencesProviderSpecific(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(
				AlternativeBulkCopy.InsertAll,
				AlternativeBulkCopy.InsertDual,
				AlternativeBulkCopy.InsertInto)]
			AlternativeBulkCopy useAlternativeBulkCopy)
		{
			try
			{
				OracleTools.UseAlternativeBulkCopy = useAlternativeBulkCopy;

				BulkCopyRetrieveSequence(context, BulkCopyType.ProviderSpecific);
			}
			finally
			{
				OracleTools.UseAlternativeBulkCopy = AlternativeBulkCopy.InsertAll;
			}
		}

		[Test]
		public void BulkCopyRetrieveSequencesMultipleRows(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(
				AlternativeBulkCopy.InsertAll,
				AlternativeBulkCopy.InsertDual,
				AlternativeBulkCopy.InsertInto)]
			AlternativeBulkCopy useAlternativeBulkCopy)
		{
			try
			{
				OracleTools.UseAlternativeBulkCopy = useAlternativeBulkCopy;

				BulkCopyRetrieveSequence(context, BulkCopyType.MultipleRows);
			}
			finally
			{
				OracleTools.UseAlternativeBulkCopy = AlternativeBulkCopy.InsertAll;
			}
		}

		[Test]
		public void BulkCopyRetrieveSequencesRowByRow(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(
				AlternativeBulkCopy.InsertAll,
				AlternativeBulkCopy.InsertDual,
				AlternativeBulkCopy.InsertInto)]
			AlternativeBulkCopy useAlternativeBulkCopy)
		{
			try
			{
				OracleTools.UseAlternativeBulkCopy = useAlternativeBulkCopy;

				BulkCopyRetrieveSequence(context, BulkCopyType.RowByRow);
			}
			finally
			{
				OracleTools.UseAlternativeBulkCopy = AlternativeBulkCopy.InsertAll;
			}
		}

		static void BulkCopyRetrieveSequence(string context, BulkCopyType bulkCopyType)
		{
			var data = new[]
			{
				new OracleSpecific.SequenceTest { Value = "Value"},
				new OracleSpecific.SequenceTest { Value = "Value"},
				new OracleSpecific.SequenceTest { Value = "Value"},
				new OracleSpecific.SequenceTest { Value = "Value"},
			};

			using (var db = new TestDataConnection(context))
			{
				db.GetTable<OracleSpecific.SequenceTest>().Where(_ => _.Value == "SeqValue").Delete();

				var options = new BulkCopyOptions
				{
					MaxBatchSize       = 5,
					//RetrieveSequence   = true,
					KeepIdentity       = bulkCopyType != BulkCopyType.RowByRow,
					BulkCopyType       = bulkCopyType,
					NotifyAfter        = 3,
					RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied)
				};

				db.BulkCopy(options, data.RetrieveIdentity(db));

				foreach (var d in data)
				{
					Assert.That(d.ID, Is.GreaterThan(0));
				}

				//Assert.That(options.BulkCopyType, Is.EqualTo(bulkCopyType));
			}
		}

		[Table(Name = "stg_trade_information")]
		public class Trade
		{
			[Column("STG_TRADE_ID")]          public int       ID             { get; set; }
			[Column("STG_TRADE_VERSION")]     public int       Version        { get; set; }
			[Column("INFORMATION_TYPE_ID")]   public int       TypeID         { get; set; }
			[Column("INFORMATION_TYPE_NAME")] public string    TypeName       { get; set; }
			[Column("value")]                 public string    Value          { get; set; }
			[Column("value_as_integer")]      public int?      ValueAsInteger { get; set; }
			[Column("value_as_date")]         public DateTime? ValueAsDate    { get; set; }
		}

		static void BulkCopy1(string context, BulkCopyType bulkCopyType)
		{
			var data = new[]
			{
				new Trade { ID = 375, Version = 1, TypeID = 20224, TypeName = "Gas Month",     },
				new Trade { ID = 328, Version = 1, TypeID = 20224, TypeName = "Gas Month",     },
				new Trade { ID = 348, Version = 1, TypeID = 20224, TypeName = "Gas Month",     },
				new Trade { ID = 357, Version = 1, TypeID = 20224, TypeName = "Gas Month",     },
				new Trade { ID = 371, Version = 1, TypeID = 20224, TypeName = "Gas Month",     },
				new Trade { ID = 333, Version = 1, TypeID = 20224, TypeName = "Gas Month",     ValueAsInteger = 1,          ValueAsDate = new DateTime(2011, 1, 5) },
				new Trade { ID = 353, Version = 1, TypeID = 20224, TypeName = "Gas Month",     ValueAsInteger = 1000000000,                                        },
				new Trade { ID = 973, Version = 1, TypeID = 20160, TypeName = "EU Allowances", },
			};

			using (var db = new TestDataConnection(context))
			{
				var options = new BulkCopyOptions
				{
					MaxBatchSize = 5,
					BulkCopyType = bulkCopyType,
					NotifyAfter  = 3,
					RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied)
				};

				db.BulkCopy(options, data);

				//Assert.That(options.BulkCopyType, Is.EqualTo(bulkCopyType));
			}
		}

		[Test]
		public void BulkCopy1MultipleRows(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(
				AlternativeBulkCopy.InsertAll,
				AlternativeBulkCopy.InsertDual,
				AlternativeBulkCopy.InsertInto)]
			AlternativeBulkCopy useAlternativeBulkCopy)
		{
			try
			{
				OracleTools.UseAlternativeBulkCopy = useAlternativeBulkCopy;

				BulkCopy1(context, BulkCopyType.MultipleRows);
			}
			finally
			{
				OracleTools.UseAlternativeBulkCopy = AlternativeBulkCopy.InsertAll;
			}
		}

		[Test]
		public void BulkCopy1ProviderSpecific(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(
				AlternativeBulkCopy.InsertAll,
				AlternativeBulkCopy.InsertDual,
				AlternativeBulkCopy.InsertInto)]
			AlternativeBulkCopy useAlternativeBulkCopy)
		{
			try
			{
				OracleTools.UseAlternativeBulkCopy = useAlternativeBulkCopy;

				BulkCopy1(context, BulkCopyType.ProviderSpecific);
			}
			finally
			{
				OracleTools.UseAlternativeBulkCopy = AlternativeBulkCopy.InsertAll;
			}
		}

		static void BulkCopy21(string context, BulkCopyType bulkCopyType)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Types2.Delete(_ => _.ID > 1000);

				if (context == ProviderName.OracleNative && bulkCopyType == BulkCopyType.ProviderSpecific)
				{
					var ms = new MappingSchema();

					db.AddMappingSchema(ms);

					ms.GetFluentMappingBuilder()
						.Entity<LinqDataTypes2>()
							.Property(e => e.GuidValue)
								.IsNotColumn()
						;
				}

				db.BulkCopy(
					new BulkCopyOptions { MaxBatchSize = 2, BulkCopyType = bulkCopyType },
					new[]
					{
						new LinqDataTypes2 { ID = 1003, MoneyValue = 0m, DateTimeValue = null,         BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue = null, IntValue = null    },
						new LinqDataTypes2 { ID = 1004, MoneyValue = 0m, DateTimeValue = DateTime.Now, BoolValue = false, GuidValue = null,                                             SmallIntValue = 2,    IntValue = 1532334 },
						new LinqDataTypes2 { ID = 1005, MoneyValue = 1m, DateTimeValue = DateTime.Now, BoolValue = false, GuidValue = null,                                             SmallIntValue = 5,    IntValue = null    },
						new LinqDataTypes2 { ID = 1006, MoneyValue = 2m, DateTimeValue = DateTime.Now, BoolValue = false, GuidValue = null,                                             SmallIntValue = 6,    IntValue = 153     }
					});

				db.Types2.Delete(_ => _.ID > 1000);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void BulkCopy21MultipleRows(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(
				AlternativeBulkCopy.InsertAll,
				AlternativeBulkCopy.InsertDual,
				AlternativeBulkCopy.InsertInto)]
			AlternativeBulkCopy useAlternativeBulkCopy)
		{
			try
			{
				OracleTools.UseAlternativeBulkCopy = useAlternativeBulkCopy;

				BulkCopy21(context, BulkCopyType.MultipleRows);
			}
			finally
			{
				OracleTools.UseAlternativeBulkCopy = AlternativeBulkCopy.InsertAll;
			}
		}

		[Test]
		public void BulkCopy21ProviderSpecific(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(
				AlternativeBulkCopy.InsertAll,
				AlternativeBulkCopy.InsertDual,
				AlternativeBulkCopy.InsertInto)]
			AlternativeBulkCopy useAlternativeBulkCopy)
		{
			try
			{
				OracleTools.UseAlternativeBulkCopy = useAlternativeBulkCopy;

				BulkCopy21(context, BulkCopyType.ProviderSpecific);
			}
			finally
			{
				OracleTools.UseAlternativeBulkCopy = AlternativeBulkCopy.InsertAll;
			}
		}

		static void BulkCopy22(string context, BulkCopyType bulkCopyType)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Types2.Delete(_ => _.ID > 1000);

				var ms = new MappingSchema();

				db.AddMappingSchema(ms);

				ms.GetFluentMappingBuilder()
					.Entity<LinqDataTypes2>()
						.Property(e => e.GuidValue)
							.IsNotColumn()
					;

				db.BulkCopy(
					new BulkCopyOptions { MaxBatchSize = 2, BulkCopyType = bulkCopyType },
					new[]
					{
						new LinqDataTypes2 { ID = 1003, MoneyValue = 0m, DateTimeValue = DateTime.Now, BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue = null, IntValue = null    },
						new LinqDataTypes2 { ID = 1004, MoneyValue = 0m, DateTimeValue = null,         BoolValue = false, GuidValue = null,                                             SmallIntValue = 2,    IntValue = 1532334 },
						new LinqDataTypes2 { ID = 1005, MoneyValue = 1m, DateTimeValue = DateTime.Now, BoolValue = false, GuidValue = null,                                             SmallIntValue = 5,    IntValue = null    },
						new LinqDataTypes2 { ID = 1006, MoneyValue = 2m, DateTimeValue = DateTime.Now, BoolValue = false, GuidValue = null,                                             SmallIntValue = 6,    IntValue = 153     }
					});

				db.Types2.Delete(_ => _.ID > 1000);
			}
		}

		[Test]
		public void BulkCopy22MultipleRows(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(
				AlternativeBulkCopy.InsertAll,
				AlternativeBulkCopy.InsertDual,
				AlternativeBulkCopy.InsertInto)]
			AlternativeBulkCopy useAlternativeBulkCopy)
		{
			try
			{
				OracleTools.UseAlternativeBulkCopy = useAlternativeBulkCopy;

				BulkCopy22(context, BulkCopyType.MultipleRows);
			}
			finally
			{
				OracleTools.UseAlternativeBulkCopy = AlternativeBulkCopy.InsertAll;
			}
		}

		[Test]
		public void BulkCopy22ProviderSpecific(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(
				AlternativeBulkCopy.InsertAll,
				AlternativeBulkCopy.InsertDual,
				AlternativeBulkCopy.InsertInto)]
			AlternativeBulkCopy useAlternativeBulkCopy)
		{
			try
			{
				OracleTools.UseAlternativeBulkCopy = useAlternativeBulkCopy;

				BulkCopy22(context, BulkCopyType.ProviderSpecific);
			}
			finally
			{
				OracleTools.UseAlternativeBulkCopy = AlternativeBulkCopy.InsertAll;
			}
		}

#endregion

#region CreateTest

		[Table]
		class TempTestTable
		{
			// column name length = 30 char (maximum for Oracle)
			[Column(Name = "AAAAAAAAAAAAAAAAAAAAAAAAAAAABC")]
			public long Id { get; set; }
		}

		[Test]
		public void LongAliasTest([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = new DataConnection(context))
			{
				try { db.DropTable<TempTestTable>(); } catch {}

				var table = db.CreateTable<TempTestTable>();

				var query =
				(
					from t in table.Distinct()
					select new { t.Id }
				).ToList();

				db.DropTable<TempTestTable>();
			}
		}

#endregion

#region XmlTable

		[Test]
		public void XmlTableTest1([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.OracleXmlTable(new[]
					{
						new { field1 = 1, field2 = "11" },
						new { field1 = 2, field2 = "22" },
					})
					.Select(t => new { t.field1, t.field2 })
					.ToList();

				Assert.That(list.Count, Is.EqualTo(2));
				Assert.That(list[0].field1, Is.EqualTo(1));
				Assert.That(list[1].field1, Is.EqualTo(2));
				Assert.That(list[0].field2, Is.EqualTo("11"));
				Assert.That(list[1].field2, Is.EqualTo("22"));
			}
		}

		[Test]
		public void XmlTableTest2([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataContext(context))
			{
				var list =
				(
					from t1 in conn.Parent
					join t2 in conn.OracleXmlTable(new[]
					{
						new { field1 = 1, field2 = "11" },
						new { field1 = 2, field2 = "22" },
					})
					on t1.ParentID equals t2.field1
					select new { t2.field1, t2.field2 }
				).ToList();

				Assert.That(list.Count, Is.EqualTo(2));
				Assert.That(list[0].field1, Is.EqualTo(1));
				Assert.That(list[1].field1, Is.EqualTo(2));
				Assert.That(list[0].field2, Is.EqualTo("11"));
				Assert.That(list[1].field2, Is.EqualTo("22"));
			}
		}

		[Test]
		public void XmlTableTest3([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var data = new[]
				{
					new { field1 = 1, field2 = "11" },
					new { field1 = 2, field2 = "22" },
				};

				var list = conn.OracleXmlTable(data)
					.Select(t => new { t.field1, t.field2 })
					.ToList();

				Assert.That(list.Count, Is.EqualTo(2));
				Assert.That(list[0].field1, Is.EqualTo(1));
				Assert.That(list[1].field1, Is.EqualTo(2));
				Assert.That(list[0].field2, Is.EqualTo("11"));
				Assert.That(list[1].field2, Is.EqualTo("22"));
			}
		}

		class XmlData
		{
			public int    Field1;
			[Column(Length = 2)]
			public string Field2;
		}

		[Test]
		public void XmlTableTest4([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.OracleXmlTable<XmlData>("<t><r><c0>1</c0><c1>11</c1></r><r><c0>2</c0><c1>22</c1></r></t>")
					.Select(t => new { t.Field1, t.Field2 })
					.ToList();

				Assert.That(list.Count, Is.EqualTo(2));
				Assert.That(list[0].Field1, Is.EqualTo(1));
				Assert.That(list[1].Field1, Is.EqualTo(2));
				Assert.That(list[0].Field2, Is.EqualTo("11"));
				Assert.That(list[1].Field2, Is.EqualTo("22"));
			}
		}

		static string _data;

		[Test]
		public void XmlTableTest5([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataContext(context))
			{
				_data = "<t><r><c0>1</c0><c1>11</c1></r><r><c0>2</c0><c1>22</c1></r></t>";

				var list = conn.OracleXmlTable<XmlData>(_data)
					.Select(t => new { t.Field1, t.Field2 })
					.ToList();

				Assert.That(list.Count, Is.EqualTo(2));
				Assert.That(list[0].Field1, Is.EqualTo(1));
				Assert.That(list[1].Field1, Is.EqualTo(2));
				Assert.That(list[0].Field2, Is.EqualTo("11"));
				Assert.That(list[1].Field2, Is.EqualTo("22"));

				_data = "<t><r><c0>1</c0><c1>11</c1></r></t>";

				list =
				(
					from t1 in conn.Parent
					join t2 in conn.OracleXmlTable<XmlData>(_data)
					on t1.ParentID equals t2.Field1
					select new { t2.Field1, t2.Field2 }
				).ToList();

				Assert.That(list.Count, Is.EqualTo(1));
				Assert.That(list[0].Field1, Is.EqualTo(1));
				Assert.That(list[0].Field2, Is.EqualTo("11"));
			}
		}

		[Test]
		public void XmlTableTest6([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var data = new[]
				{
					new { field1 = 1, field2 = "11" },
					new { field1 = 2, field2 = "22" },
				};

				var xmlData = OracleTools.GetXmlData(conn.MappingSchema, data);

				var list = conn.OracleXmlTable<XmlData>(xmlData)
					.Select(t => new { t.Field1, t.Field2 })
					.ToList();

				Assert.That(list.Count, Is.EqualTo(2));
				Assert.That(list[0].Field1, Is.EqualTo(1));
				Assert.That(list[1].Field1, Is.EqualTo(2));
				Assert.That(list[0].Field2, Is.EqualTo("11"));
				Assert.That(list[1].Field2, Is.EqualTo("22"));
			}
		}

		[Test]
		public void XmlTableTest7([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataContext(context))
			{
				var data = new[]
				{
					new { field1 = 1, field2 = "11" },
					new { field1 = 2, field2 = "22" },
				};

				var xmlData = OracleTools.GetXmlData(conn.MappingSchema, data);

				var list = conn.OracleXmlTable<XmlData>(() => xmlData)
					.Select(t => new { t.Field1, t.Field2 })
					.ToList();

				Assert.That(list.Count, Is.EqualTo(2));
				Assert.That(list[0].Field1, Is.EqualTo(1));
				Assert.That(list[1].Field1, Is.EqualTo(2));
				Assert.That(list[0].Field2, Is.EqualTo("11"));
				Assert.That(list[1].Field2, Is.EqualTo("22"));

				xmlData = "<t><r><c0>1</c0><c1>11</c1></r></t>";

				list = conn.OracleXmlTable<XmlData>(() => xmlData)
					.Select(t => new { t.Field1, t.Field2 })
					.ToList();

				Assert.That(list.Count, Is.EqualTo(1));
				Assert.That(list[0].Field1, Is.EqualTo(1));
				Assert.That(list[0].Field2, Is.EqualTo("11"));
			}
		}

		[Test]
		public void XmlTableTest8([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataContext(context))
			{
				var data = "<t><r><c0>1</c0><c1>11</c1></r></t>";

				var list =
				(
					from p in conn.Parent
					where conn.OracleXmlTable<XmlData>(data).Count(t => t.Field1 == p.ParentID) > 0
					select p
				).ToList();

				Assert.That(list[0].ParentID, Is.EqualTo(1));

				data = "<t><r><c0>2</c0><c1>22</c1></r></t>";

				list =
				(
					from p in conn.Parent
					where conn.OracleXmlTable<XmlData>(data).Count(t => t.Field1 == p.ParentID) > 0
					select p
				).ToList();

				Assert.That(list[0].ParentID, Is.EqualTo(2));
			}
		}

		[Test]
		public void XmlTableTest9([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataContext(context))
			{
				var data = "<t><r><c0>1</c0><c1>11</c1></r></t>";

				var list =
				(
					from p in conn.Parent
					where conn.OracleXmlTable<XmlData>(() => data).Count(t => t.Field1 == p.ParentID) > 0
					select p
				).ToList();

				Assert.That(list[0].ParentID, Is.EqualTo(1));

				data = "<t><r><c0>2</c0><c1>22</c1></r></t>";

				list =
				(
					from p in conn.Parent
					where conn.OracleXmlTable<XmlData>(() => data).Count(t => t.Field1 == p.ParentID) > 0
					select p
				).ToList();

				Assert.That(list[0].ParentID, Is.EqualTo(2));
			}
		}

#endregion

		[Test]
		public void TestOrderByFirst1([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var q =
					from x in db.Parent
					where x.Value1 == 1
					orderby x.ParentID descending
					select x;

				var row = q.First();

				var start = 0;
				var n     = 0;

				while ((start = db.LastQuery.IndexOf("FROM", start) + 1) > 0)
					n++;

				Assert.That(n, Is.EqualTo(2));
			}
		}

		[Test]
		public void TestOrderByFirst2([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var q =
					from x in db.Parent
					where x.Value1 == 1
					select x;

				var row = q.First();

				var start = 0;
				var n     = 0;

				while ((start = db.LastQuery.IndexOf("FROM", start) + 1) > 0)
					n++;

				Assert.That(n, Is.EqualTo(1));
			}
		}

		[Test]
		public void TestOrderByFirst3([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var q =
					from x in db.Parent
					where x.Value1 == 1
					orderby x.ParentID descending
					select x;

				var row = q.Skip(1).First();

				var start = 0;
				var n     = 0;

				while ((start = db.LastQuery.IndexOf("FROM", start) + 1) > 0)
					n++;

				Assert.That(n, Is.EqualTo(3));
			}
		}

		[Table("DecimalOverflow")]
		class DecimalOverflow
		{
			[Column] public decimal Decimal1;
			[Column] public decimal Decimal2;
			[Column] public decimal Decimal3;
		}

		[Test]
		public void OverflowTest([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var func = OracleTools.DataReaderGetDecimal;
			try
			{
				OracleTools.DataReaderGetDecimal = GetDecimal;

				using (var db = new DataConnection(context))
				{
					var list = db.GetTable<DecimalOverflow>().ToList();
				}
			}
			finally
			{
				OracleTools.DataReaderGetDecimal = func;
			}
		}

		const int ClrPrecision = 29;

		static decimal GetDecimal(IDataReader rd, int idx)
		{
			if (rd is Oracle.ManagedDataAccess.Client.OracleDataReader)
			{
				var value  = ((Oracle.ManagedDataAccess.Client.OracleDataReader)rd).GetOracleDecimal(idx);
				var newval = Oracle.ManagedDataAccess.Types.OracleDecimal.SetPrecision(value, value > 0 ? ClrPrecision : (ClrPrecision - 1));

				return newval.Value;
			}
			else
			{
				var value  = ((OracleDataReader)rd).GetOracleDecimal(idx);
				var newval = OracleDecimal.SetPrecision(value, value > 0 ? ClrPrecision : (ClrPrecision - 1));

				return newval.Value;
			}
		}

		[Table("DecimalOverflow")]
		class DecimalOverflow2
		{
			[Column] public OracleDecimal Decimal1;
			[Column] public OracleDecimal Decimal2;
			[Column] public OracleDecimal Decimal3;
		}

		[Test]
		public void OverflowTest2([IncludeDataSources(ProviderName.OracleManaged)] string context)
		{
			var func = OracleTools.DataReaderGetDecimal;
			try
			{

				OracleTools.DataReaderGetDecimal = (rd, idx) => { throw new Exception(); };

				using (var db = new DataConnection(context))
				{
					var list = db.GetTable<DecimalOverflow2>().ToList();
				}
			}
			finally
			{
				OracleTools.DataReaderGetDecimal = func;
			}
		}

		public class UseAlternativeBulkCopy
		{
			public int Id;
			public int Value;

			public override int GetHashCode()
			{
				return Id;
			}

			public override bool Equals(object obj)
			{
				var e = (UseAlternativeBulkCopy) obj;

				return e.Id == Id && e.Value == Value;
			}
		}

		[Test]
		public void UseAlternativeBulkCopyInsertIntoTest([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var data = new List<UseAlternativeBulkCopy>(100);
			for (var i = 0; i < 100; i++)
				data.Add(new UseAlternativeBulkCopy() {Id = i, Value = i});

			using (var db = new DataConnection(context))
			{
				OracleTools.UseAlternativeBulkCopy = AlternativeBulkCopy.InsertInto;
				db.CreateTable<UseAlternativeBulkCopy>();
				try
				{
					db.BulkCopy(25, data);

					var selected = db.GetTable<UseAlternativeBulkCopy>().ToList();
					AreEqual(data, selected);
				}
				finally
				{
					OracleTools.UseAlternativeBulkCopy = AlternativeBulkCopy.InsertAll;
					db.DropTable<UseAlternativeBulkCopy>();
				}
			}

		}

		[Test]
		public void UseAlternativeBulkCopyInsertDualTest([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var data = new List<UseAlternativeBulkCopy>(100);
			for (var i = 0; i < 100; i++)
				data.Add(new UseAlternativeBulkCopy() { Id = i, Value = i });

			using (var db = new DataConnection(context))
			{
				OracleTools.UseAlternativeBulkCopy = AlternativeBulkCopy.InsertDual;
				db.CreateTable<UseAlternativeBulkCopy>();
				try
				{
					db.BulkCopy(25, data);

					var selected = db.GetTable<UseAlternativeBulkCopy>().ToList();
					AreEqual(data, selected);
				}
				finally
				{
					OracleTools.UseAlternativeBulkCopy = AlternativeBulkCopy.InsertAll;
					db.DropTable<UseAlternativeBulkCopy>();
				}
			}

		}

		public class ClobEntity
		{
			public ClobEntity()
			{ }

			public ClobEntity(int id)
			{
				Id         = id;
				ClobValue  = "Clob" .PadRight(4001, id.ToString()[0]);
				NClobValue = "NClob".PadRight(4001, id.ToString()[0]);
			}
			public int Id;

			[Column(DataType = DataType.Text)]
			public string ClobValue;

			[Column(DataType = DataType.NText)]
			public string NClobValue;

			public override int GetHashCode()
			{
				return Id;
			}

			public override bool Equals(object obj)
			{
				var clob = (ClobEntity) obj;
				return    clob.Id         == Id
					   && clob.ClobValue  == ClobValue
					   && clob.NClobValue == NClobValue;
			}
		}

		[Test]
		public void ClobTest1([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = new DataConnection(context))
			{
				try
				{
					db.CreateTable<ClobEntity>();
					var obj = new ClobEntity(1);
					db.Insert(obj);

					var selected = db.GetTable<ClobEntity>().First(_ => _.Id == 1);
					Assert.AreEqual(obj, selected);
				}
				finally
				{
					db.DropTable<ClobEntity>();
				}
			}
		}

		[Test]
		public void ClobBulkCopyTest(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(
				AlternativeBulkCopy.InsertAll,
				AlternativeBulkCopy.InsertDual,
				AlternativeBulkCopy.InsertInto)]
			AlternativeBulkCopy useAlternativeBulkCopy)
		{
			var data = new List<ClobEntity>(new[] {new ClobEntity(1), new ClobEntity(2)});

			using (var db = new DataConnection(context))
			{
				OracleTools.UseAlternativeBulkCopy = useAlternativeBulkCopy;

				try
				{
					db.CreateTable<ClobEntity>();
					db.BulkCopy(data);

					var selected = db.GetTable<ClobEntity>().ToList();
					AreEqual(data, selected);
				}
				finally
				{
					OracleTools.UseAlternativeBulkCopy = AlternativeBulkCopy.InsertAll;
					db.DropTable<ClobEntity>();
				}

			}
		}

		[Table(IsColumnAttributeRequired = false)]
		public class DateTimeOffsetTable
		{
			public DateTimeOffset DateTimeOffsetValue;
		}

		[Test]
		public void Issue515Test([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var now = new DateTimeOffset(2000, 1, 1, 10, 11, 12, TimeSpan.FromHours(5));
					db.CreateTable<DateTimeOffsetTable>();
					db.Insert(new DateTimeOffsetTable() {DateTimeOffsetValue = now});
					Assert.AreEqual(now, db.GetTable<DateTimeOffsetTable>().Select(_ => _.DateTimeOffsetValue).Single());
				}
				finally
				{
					db.DropTable<DateTimeOffsetTable>();
				}
			}

		}

		[Test]
		public void Issue612Test([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					// initialize with ticks with default oracle timestamp presicion (6 fractional seconds)
					var expected = new DateTimeOffset(636264847785126550, TimeSpan.FromHours(3));

					db.CreateTable<DateTimeOffsetTable>();

					db.Insert(new DateTimeOffsetTable { DateTimeOffsetValue = expected });

					var actual = db.GetTable<DateTimeOffsetTable>().Select(x => x.DateTimeOffsetValue).Single();

					Assert.That(actual, Is.EqualTo(expected));
				}
				finally
				{
					db.DropTable<DateTimeOffsetTable>();
				}
			}

		}

		[Test]
		public void Issue612TestDefaultTSTZPrecisonCanDiffersOfUpTo9Ticks([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					// initialize with ticks with default DateTimeOffset presicion (7 fractional seconds for Oracle TSTZ)
					var expected = new DateTimeOffset(636264847785126559, TimeSpan.FromHours(3));

					db.CreateTable<DateTimeOffsetTable>();

					db.Insert(new DateTimeOffsetTable { DateTimeOffsetValue = expected });

					var actual = db.GetTable<DateTimeOffsetTable>().Select(x => x.DateTimeOffsetValue).Single();

					Assert.That(actual, Is.EqualTo(expected).Within(9).Ticks);
				}
				finally
				{
					db.DropTable<DateTimeOffsetTable>();
				}
			}

		}

		public static IEnumerable<Person> PersonSelectByKey(DataConnection dataConnection, int id)
		{
			return dataConnection.QueryProc<Person>("Person_SelectByKey",
				new DataParameter("pID", @id),
				new DataParameter { Name = "retCursor", DataType = DataType.Cursor, Direction = ParameterDirection.ReturnValue });
		}

		[Test]
		public void PersonSelectByKey([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				AreEqual(Person.Where(_ => _.ID == 1), PersonSelectByKey(db, 1));
			}
		}

		[Table(Name = "ALLTYPES")]
		public partial class ALLTYPE2
		{
			[Column, PrimaryKey, Identity] public decimal ID             { get; set; } // NUMBER
			[Column,             Nullable] public byte[]  BINARYDATATYPE { get; set; } // BLOB
			[Column,             Nullable] public byte[]  BFILEDATATYPE  { get; set; } // BFILE
			[Column,             Nullable] public byte[]  GUIDDATATYPE   { get; set; } // RAW(16)
		}

		[Test]
		public void Issue539([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var n = 0;
				try
				{
					var val = new byte[] { 1, 2, 3 };

					n = Convert.ToInt32(db.GetTable<ALLTYPE2>()
						.InsertWithIdentity(() => new ALLTYPE2 { ID = 1000, BINARYDATATYPE = val, GUIDDATATYPE = val }));

					var qry = db.GetTable<ALLTYPE2>().Where(_ => _.ID == 1000 && _.GUIDDATATYPE == val);

					var data = db.GetTable<ALLTYPE2>()
						.Where(_ => _.ID == n)
						.Select(_ => new
						{
							_.BINARYDATATYPE,
							Count = qry.Count()
						}).First();

					AreEqual(val, data.BINARYDATATYPE);

				}
				finally
				{
					db.GetTable<ALLTYPE2>().Delete(_ => _.ID == n);
				}
			}
		}

		public class Issue723Table
		{
			[PrimaryKey, Identity, NotNull]
			public int Id;

			public string StringValue;
		}

		[Test]
		public void Issue723Test1([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var ms = new MappingSchema();
			using (var db = (DataConnection)GetDataContext(context, ms))
			{
				var currentUser = db.Execute<string>("SELECT user FROM dual");
				db.Execute("GRANT CREATE ANY TRIGGER TO " + currentUser);
				db.Execute("GRANT CREATE ANY SEQUENCE TO " + currentUser);
				db.Execute("GRANT DROP ANY TRIGGER TO " + currentUser);
				db.Execute("GRANT DROP ANY SEQUENCE TO " + currentUser);

				try {db.Execute("DROP USER Issue723Schema CASCADE");} catch { }

				db.Execute("CREATE USER Issue723Schema IDENTIFIED BY password");

				try
				{

					var tableSpace = db.Execute<string>("SELECT default_tablespace FROM sys.dba_users WHERE username = 'ISSUE723SCHEMA'");
					db.Execute($"ALTER USER Issue723Schema quota unlimited on {tableSpace}");

					db.CreateTable<Issue723Table>(schemaName: "Issue723Schema");
					Assert.That(db.LastQuery.Contains("Issue723Schema.Issue723Table"));

					try
					{

						db.MappingSchema.GetFluentMappingBuilder()
							.Entity<Issue723Table>()
							.HasSchemaName("Issue723Schema");

						for (var i = 1; i < 3; i++)
						{
							var id = Convert.ToInt32(db.InsertWithIdentity(new Issue723Table() { StringValue = i.ToString() }));
							Assert.AreEqual(i, id);
						}
						Assert.That(db.LastQuery.Contains("Issue723Schema.Issue723Table"));
					}
					finally
					{
						db.DropTable<Issue723Table>(schemaName: "Issue723Schema");
					}
				}
				finally
				{
					db.Execute("DROP USER Issue723Schema CASCADE");
				}
			}
		}

		[Test]
		public void Issue723Test2([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Issue723Table>())
			{
				Assert.True(true);
			}
		}

		public class Issue731Table
		{
			public int    Id;
			public Guid   Guid;
			[Column(DataType = DataType.Binary)]
			public Guid   BinaryGuid;
			public byte[] BlobValue;
			[Column(Length = 5)]
			public byte[] RawValue;

		}

		[Test]
		public void Issue731Test([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Issue731Table>())
			{
				var origin = new Issue731Table()
				{
					Id         = 1,
					Guid       = Guid.NewGuid(),
					BinaryGuid = Guid.NewGuid(),
					BlobValue  = new byte[] { 1, 2, 3 },
					RawValue   = new byte[] { 4, 5, 6 }
				};

				db.Insert(origin);

				var result = db.GetTable<Issue731Table>().First(_ => _.Id == 1);

				Assert.AreEqual(origin.Id,         result.Id);
				Assert.AreEqual(origin.Guid,       result.Guid);
				Assert.AreEqual(origin.BinaryGuid, result.BinaryGuid);
				Assert.AreEqual(origin.BlobValue,  result.BlobValue);
				Assert.AreEqual(origin.RawValue,   result.RawValue);
			}
		}

		class MyDate
		{
			public int    Year;
			public int    Month;
			public int    Day;
			public int    Hour;
			public int    Minute;
			public int    Second;
			public int    Nanosecond;
			public string TimeZone;
		}

		static MyDate OracleTimeStampTZToMyDate(OracleTimeStampTZ tz)
		{
			return new MyDate
			{
				Year       = tz.Year,
				Month      = tz.Month,
				Day        = tz.Day,
				Hour       = tz.Hour,
				Minute     = tz.Minute,
				Second     = tz.Second,
				Nanosecond = tz.Nanosecond,
				TimeZone   = tz.TimeZone,
			};
		}

		static OracleTimeStampTZ MyDateToOracleTimeStampTZ(MyDate dt)
		{
			return dt == null ?
				OracleTimeStampTZ.Null :
				new OracleTimeStampTZ(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Nanosecond, dt.TimeZone);
		}

		[Table("AllTypes")]
		class MappingTest
		{
			[Column] public int    ID;
			[Column("datetimeoffsetDataType")] public MyDate MyDate;
		}

		[Test]
		public void CustomMappingNonstandardTypeTest([IncludeDataSources(ProviderName.OracleManaged)] string context)
		{
			var dataProvider = (DataProviderBase)DataConnection.GetDataProvider(context);

			// Expression to read column value from data reader.
			//
			dataProvider.ReaderExpressions[new ReaderInfo
			{
				ToType            = typeof(MyDate),
				ProviderFieldType = typeof(OracleTimeStampTZ),
			}] = (Expression<Func<OracleDataReader,int,MyDate>>)((rd, idx) => OracleTimeStampTZToMyDate(rd.GetOracleTimeStampTZ(idx)));

			// Converts object property value to data reader parameter.
			//
			dataProvider.MappingSchema.SetConverter<MyDate,DataParameter>(
				dt => new DataParameter { Value = MyDateToOracleTimeStampTZ(dt) });

			// Converts object property value to SQL.
			//
			dataProvider.MappingSchema.SetValueToSqlConverter(typeof(MyDate), (sb,tp,v) =>
			{
				var value = v as MyDate;
				if (value == null) sb.Append("NULL");
				else               sb.Append($"DATE '{value.Year}-{value.Month}-{value.Day}'");
			});

			// Converts object property value to SQL.
			//
			dataProvider.MappingSchema.SetValueToSqlConverter(typeof(OracleTimeStampTZ), (sb,tp,v) =>
			{
				var value = (OracleTimeStampTZ)v;
				if (value.IsNull) sb.Append("NULL");
				else              sb.Append($"DATE '{value.Year}-{value.Month}-{value.Day}'");
			});

			// Maps OracleTimeStampTZ to MyDate and the other way around.
			//
			dataProvider.MappingSchema.SetConverter<OracleTimeStampTZ,MyDate>(OracleTimeStampTZToMyDate);
			dataProvider.MappingSchema.SetConverter<MyDate,OracleTimeStampTZ>(MyDateToOracleTimeStampTZ);

			using (var db = GetDataContext(context))
			{
				var table = db.GetTable<MappingTest>();
				var list  = table.ToList();

				table.Update(
					mt => mt.ID == list[0].ID,
					mt => new MappingTest
					{
						MyDate = list[0].MyDate
					});

				db.InlineParameters = true;

				table.Update(
					mt => mt.ID == list[0].ID,
					mt => new MappingTest
					{
						MyDate = list[0].MyDate
					});
			}
		}

		class BooleanMapping
		{
			private sealed class EqualityComparer : IEqualityComparer<BooleanMapping>
			{
				public bool Equals(BooleanMapping x, BooleanMapping y)
				{
					if (ReferenceEquals(x, y)) return true;
					if (ReferenceEquals(x, null)) return false;
					if (ReferenceEquals(y, null)) return false;
					if (x.GetType() != y.GetType()) return false;
					return x.Id == y.Id && x.BoolProp == y.BoolProp && x.NullableBoolProp == y.NullableBoolProp;
				}

				public int GetHashCode(BooleanMapping obj)
				{
					unchecked
					{
						var hashCode = obj.Id;
						hashCode = (hashCode * 397) ^ obj.BoolProp.GetHashCode();
						hashCode = (hashCode * 397) ^ obj.NullableBoolProp.GetHashCode();
						return hashCode;
					}
				}
			}

			public static IEqualityComparer<BooleanMapping> Comparer { get; } = new EqualityComparer();

			[PrimaryKey]
			public int Id { get; set; }
			[Column]
			public bool BoolProp { get; set; }
			[Column]
			public bool? NullableBoolProp { get; set; }
		}

		[Test]
		public void BooleanMappingTests([IncludeDataSources(ProviderName.OracleManaged)] string context)
		{
			var ms = new MappingSchema();

			ms.SetConvertExpression<bool?, DataParameter>(_ =>
				_ != null
					? DataParameter.Char(null, _.HasValue && _.Value ? 'Y' : 'N')
					: new DataParameter(null, DBNull.Value));

			var testData = new[]
			{
				new BooleanMapping { Id = 1, BoolProp = true,  NullableBoolProp = true  },
				new BooleanMapping { Id = 2, BoolProp = false, NullableBoolProp = false },
				new BooleanMapping { Id = 3, BoolProp = true,  NullableBoolProp = null  }
			};

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<BooleanMapping>())
			{
				table.BulkCopy(testData);
				var values = table.ToArray();

				AreEqual(testData, values, BooleanMapping.Comparer);
			}
		}

		[Table("AllTypes")]
		public class TestIdentifiersTable1
		{
			[Column]
			public int Id { get; set; }
		}

		[Table("ALLTYPES")]
		public class TestIdentifiersTable2
		{
			[Column("ID")]
			public int Id { get; set; }
		}

		[Test]
		public void TestLowercaseIdentifiersQuotation([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var initial = OracleTools.DontEscapeLowercaseIdentifiers;
				try
				{
					OracleTools.DontEscapeLowercaseIdentifiers = true;
					db.GetTable<TestIdentifiersTable1>().ToList();
					db.GetTable<TestIdentifiersTable2>().ToList();

					Query.ClearCaches();
					OracleTools.DontEscapeLowercaseIdentifiers = false;

					// no specific exception type as it differ for managed and native providers
					Assert.That(() => db.GetTable<TestIdentifiersTable1>().ToList(), Throws.Exception.With.Message.Contains("ORA-00942"));

					db.GetTable<TestIdentifiersTable2>().ToList();
				}
				finally
				{
					OracleTools.DontEscapeLowercaseIdentifiers = initial;
				}
			}
		}
	}
}

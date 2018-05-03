﻿using System.Threading;
using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;
	using LinqToDB.Mapping;

	public class Issue1128Tests : TestBase
	{
		private static int _cnt;

		class Issue1128Table
		{
			public int Id { get; set; }
		}

		class Issue1128TableDerived : Issue1128Table
		{
		}


		MappingSchema SetMappings()
		{
			// counter added to fix this issue with tests in Firebird
			// https://stackoverflow.com/questions/44353607
			// it was only working solution for Firebird3
			var cnt = Interlocked.Increment(ref _cnt).ToString();

			var ms = new MappingSchema(cnt);
			
			var tableName = "Issue1128Table" + cnt;

			var mappingBuilder = ms.GetFluentMappingBuilder();
			mappingBuilder.Entity<Issue1128Table>()
				.HasTableName(tableName)
				.Property(x => x.Id).IsColumn().IsNullable(false).HasColumnName("Id").IsPrimaryKey();

			return ms;
		}

		[Test, DataContextSource]
		public void TestED(string configuration)
		{
			var ms = SetMappings();

			var ed1 = ms.GetEntityDescriptor(typeof(Issue1128Table));
			var ed2 = ms.GetEntityDescriptor(typeof(Issue1128TableDerived));

			Assert.AreEqual(ed1.TableName, ed2.TableName);
		}

		[Test, DataContextSource]
		public void Test(string configuration)
		{
			var ms = SetMappings();

			using (var db = GetDataContext(configuration, ms))
			{
				try
				{
					db.CreateTable<Issue1128TableDerived>();
				}
				catch
				{
					db.DropTable<Issue1128TableDerived>(throwExceptionIfNotExists: false);
					db.CreateTable<Issue1128TableDerived>();
				}

				try
				{
					db.Insert(new Issue1128TableDerived { Id = 1 });
				}
				finally
				{
					db.DropTable<Issue1128TableDerived>();
				}
			}
		}
	}
}

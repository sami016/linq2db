﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;
	using System.Text.RegularExpressions;

	[TestFixture]
	public class WhereTests : TestBase
	{
		[Test]
		public void MakeSubQuery([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					select new { PersonID = p.ID + 1, p.FirstName } into p
					where p.PersonID == 2
					select new Person(p.PersonID - 1) { FirstName = p.FirstName });
		}

		[Test]
		public void MakeSubQueryWithParam([DataSources] string context)
		{
			var n = 1;

			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					select new { PersonID = p.ID + n, p.FirstName } into p
					where p.PersonID == 2
					select new Person(p.PersonID - 1) { FirstName = p.FirstName });
		}

		[Test]
		public void DoNotMakeSubQuery([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p1 in db.Person
					select new { p1.ID, Name = p1.FirstName + "\r\r\r" } into p2
					where p2.ID == 1
					select new Person(p2.ID) { FirstName = p2.Name.TrimEnd('\r') });
		}

		[Test]
		public void EqualsConst([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 select p);
		}

		[Test]
		public void EqualsConsts([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && p.FirstName == "John" select p);
		}

		[Test]
		public void EqualsConsts2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					where (p.FirstName == "John" || p.FirstName == "John's") && p.ID > 0 && p.ID < 2 && p.LastName != "123"
					select p);
		}

		[Test]
		public void EqualsParam([DataSources] string context)
		{
			var id = 1;
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == id select p);
		}

		[Test]
		public void EqualsParams([DataSources] string context)
		{
			var id   = 1;
			var name = "John";
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == id && p.FirstName == name select p);
		}

		[Test]
		public void NullParam1([DataSources] string context)
		{
			var    id   = 1;
			string name = null;
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == id && p.MiddleName == name select p);
		}

		[Test]
		public void NullParam2([DataSources] string context)
		{
			var    id   = 1;
			string name = null;

			using (var db = GetDataContext(context))
			{
				       (from p in db.Person where p.ID == id && p.MiddleName == name select p).ToList();
				var q = from p in db.Person where p.ID == id && p.MiddleName == name select p;

				TestOneJohn(q);
			}
		}

		int TestMethod()
		{
			return 1;
		}

		[Test]
		public void MethodParam([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == TestMethod() select p);
		}

		static int StaticTestMethod()
		{
			return 1;
		}

		[Test]
		public void StaticMethodParam([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == StaticTestMethod() select p);
		}

		class TestMethodClass
		{
			private readonly int _n;

			public TestMethodClass(int n)
			{
				_n = n;
			}

			public int TestMethod()
			{
				return _n;
			}
		}

		public void MethodParam(int n, string context)
		{
			var t = new TestMethodClass(n);

			using (var db = GetDataContext(context))
			{
				var id = (from p in db.Person where p.ID == t.TestMethod() select new { p.ID }).ToList().First();
				Assert.AreEqual(n, id.ID);
			}
		}

		[Test]
		public void MethodParam2([DataSources] string context)
		{
			MethodParam(1, context);
			MethodParam(2, context);
		}

		static IQueryable<Person> TestDirectParam(ITestDataContext db, int id)
		{
			var name = "John";
			return from p in db.Person where p.ID == id && p.FirstName == name select p;
		}

		[Test]
		public void DirectParams([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(TestDirectParam(db, 1));
		}

		[Test]
		public void BinaryAdd([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID + 1 == 2 select p);
		}

		[Test]
		public void BinaryDivide([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where (p.ID + 9) / 10 == 1 && p.ID == 1 select p);
		}

		[Test]
		public void BinaryModulo([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID % 2 == 1 && p.ID == 1 select p);
		}

		[Test]
		public void BinaryMultiply([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID * 10 - 9 == 1 select p);
		}

		[Test]
		public void BinaryXor([DataSources(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where (p.ID ^ 2) == 3 select p);
		}

		[Test]
		public void BinaryAnd([DataSources(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where (p.ID & 3) == 1 select p);
		}

		[Test]
		public void BinaryOr([DataSources(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Person.Where(p => (p.ID | 2) == 3),
					db.Person.Where(p => (p.ID | 2) == 3));
			}
		}

		[Test]
		public void BinarySubtract([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID - 1 == 0 select p);
		}

		[Test]
		public void EqualsNull([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && p.MiddleName == null select p);
		}

		[Test]
		public void EqualsNull2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && null == p.MiddleName select p);
		}

		[Test]
		public void NotEqualNull([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && p.FirstName != null select p);
		}

		[Test]
		public void NotEqualNull2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && null != p.FirstName select p);
		}

		[Test]
		public void ComparisionNullCheckOn1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.Value1 != 1),
					db.Parent.Where(p => p.Value1 != 1));
		}

		[Test]
		public void ComparisionNullCheckOn2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => 1 != p.Value1),
					db.Parent.Where(p => 1 != p.Value1));
		}

		[Test]
		public void ComparisionNullCheckOff([DataSources] string context)
		{
			using (new WithoutComparisonNullCheck())
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.Value1 != 1 && p.Value1 != null),
					db.Parent.Where(p => p.Value1 != 1));
		}

		[Test]
		public void NotTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && !(p.MiddleName != null) select p);
		}

		[Test]
		public void NotTest2([DataSources] string context)
		{
			var n = 2;
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && !(p.MiddleName != null && p.ID == n) select p);
		}

		[Test]
		public void Coalesce1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					where
						p.ID == 1 &&
						(p.MiddleName ?? "None") == "None" &&
						(p.FirstName  ?? "None") == "John"
					select p);
		}

		[Test]
		public void Coalesce2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, (from p in db.Parent where p.ParentID == 1 ? true : false select p).ToList().Count);
		}

		[Test]
		public void Coalesce3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, (from p in db.Parent where p.ParentID != 1 ? false : true select p).ToList().Count);
		}

		[Test]
		public void Coalesce4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.ParentID == 1 ? false: true select p,
					from p in db.Parent where p.ParentID == 1 ? false: true select p);
		}

		[Test]
		public void Coalesce5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(2, (from p in db.Parent where (p.Value1 == 1 ? 10 : 20) == 10 select p).ToList().Count);
		}

		[Test]
		public void Coalesce6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where (p.Value1 == 1 ? 10 : 20) == 20 select p,
					from p in db.Parent where (p.Value1 == 1 ? 10 : 20) == 20 select p);
		}

		[Test]
		public void Coalesce7([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where (p.ParentID == 1 ? 10 : 20) == 20 select p,
					from p in db.Parent where (p.ParentID == 1 ? 10 : 20) == 20 select p);
		}

		[Test]
		public void Conditional([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					where
						p.ID == 1 &&
						(p.MiddleName == null ? 1 : 2) == 1 &&
						(p.FirstName  != null ? 1 : 2) == 1
					select p);
		}

		[Test]
		public void Conditional2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					where
						p.ID == 1 &&
						(p.MiddleName != null ? 3 : p.MiddleName == null? 1 : 2) == 1 &&
						(p.FirstName  == null ? 3 : p.FirstName  != null? 1 : 2) == 1
					select p);
		}

		[Test]
		public void Conditional3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					where
						p.ID == 1 &&
						(p.MiddleName != null ? 3 : p.ID == 2 ? 2 : p.MiddleName != null ? 0 : 1) == 1 &&
						(p.FirstName  == null ? 3 : p.ID == 2 ? 2 : p.FirstName  == null ? 0 : 1) == 1
					select p);
		}

		[Test]
		public void MultipleQuery1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 1;
				var q  = from p in db.Person where p.ID == id select p;

				var list = q.ToList();
				Assert.AreEqual(1, list[0].ID);

				id = 2;
				list = q.ToList();
				Assert.AreEqual(2, list[0].ID);
			}
		}

		[Test]
		public void MultipleQuery2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				string str = null;
				var    q   = from p in db.Person where p.MiddleName == str select p;

				var list = q.ToList();
				Assert.AreNotEqual(0, list.Count);

				str  = "123";
				list = q.ToList();
				Assert.AreEqual(0, list.Count);
			}
		}

		[Test]
		public void HasValue1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.Value1.HasValue select p,
					from p in db.Parent where p.Value1.HasValue select p);
		}

		[Test]
		public void HasValue2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(2, (from p in db.Parent where !p.Value1.HasValue select p).ToList().Count);
		}

		[Test]
		public void Value([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(2, (from p in db.Parent where p.Value1.Value == 1 select p).ToList().Count);
		}

		[Test]
		public void CompareNullable1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(2, (from p in db.Parent where p.Value1 == 1 select p).ToList().Count);
		}

		[Test]
		public void CompareNullable2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, (from p in db.Parent where p.ParentID == p.Value1 && p.Value1 == 1 select p).ToList().Count);
		}

		[Test]
		public void CompareNullable3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, (from p in db.Parent where p.Value1 == p.ParentID && p.Value1 == 1 select p).ToList().Count);
		}

		[Test]
		public void SubQuery([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in
						from ch in Child
						select ch.ParentID * 1000
					where t > 2000
					select t / 1000,
					from t in
						from ch in db.Child
						select ch.ParentID * 1000
					where t > 2000
					select t / 1000);
		}

		[Test]
		public void AnonymousEqual1([DataSources] string context)
		{
			var child = new { ParentID = 2, ChildID = 21 };

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					where ch.ParentID == child.ParentID && ch.ChildID == child.ChildID
					select ch
					,
					from ch in db.Child
					where new { ch.ParentID, ch.ChildID } == child
					select ch);
		}

		[Test]
		public void AnonymousEqual2([DataSources] string context)
		{
			var child = new { ParentID = 2, ChildID = 21 };

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					where !(ch.ParentID == child.ParentID && ch.ChildID == child.ChildID) && ch.ParentID > 0
					select ch
					,
					from ch in db.Child
					where child != new { ch.ParentID, ch.ChildID } && ch.ParentID > 0
					select ch);
		}

		[Test]
		public void AnonymousEqual31([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					where ch.ParentID == 2 && ch.ChildID == 21
					select ch
					,
					from ch in db.Child
					where new { ch.ParentID, ch.ChildID } == new { ParentID = 2, ChildID = 21 }
					select ch);
		}

		[Test]
		public void AnonymousEqual32([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					where ch.ParentID == 2 && ch.ChildID == 21
					select ch
					,
					from ch in db.Child
					where new { ParentID = 2, ChildID = 21 } == new { ch.ParentID, ch.ChildID }
					select ch);
		}

		[Test]
		public void AnonymousEqual4([DataSources] string context)
		{
			var parent = new { ParentID = 2, Value1 = (int?)null };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID == parent.ParentID && p.Value1 == parent.Value1
					select p
					,
					from p in db.Parent
					where new { p.ParentID, p.Value1 } == parent
					select p);
		}

		[Test]
		public void AnonymousEqual5([DataSources] string context)
		{
			var parent = new { ParentID = 3, Value1 = (int?)3 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID == parent.ParentID && p.Value1 == parent.Value1
					select p
					,
					from p in db.Parent
					where new { p.ParentID, p.Value1 } == parent
					select p);
		}

		[Test]
		public void CheckLeftJoin1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
						join ch in Child on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch == null
					select p
					,
					from p in db.Parent
						join ch in db.Child on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch == null
					select p);
		}

		[Test]
		public void CheckLeftJoin2([DataSources] string context)
		{
			using (var data = GetDataContext(context))
				AreEqual(
					from p in Parent
						join ch in Child on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch != null
					select p
					,
					CompiledQuery.Compile<ITestDataContext,IQueryable<Parent>>(db =>
						from p in db.Parent
							join ch in db.Child on p.ParentID equals ch.ParentID into lj1
							from ch in lj1.DefaultIfEmpty()
						where null != ch
						select p)(data));
		}

		[Test]
		public void CheckLeftJoin3([DataSources(ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
						join ch in
							from c in GrandChild
							where c.ParentID > 0
							select new { ParentID = 1 + c.ParentID, c.ChildID }
						on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch == null && ch == null
					select p
					,
					from p in db.Parent
						join ch in
							from c in db.GrandChild
							where c.ParentID > 0
							select new { ParentID = 1 + c.ParentID, c.ChildID }
						on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch == null && ch == null
					select p);
		}

		[Test]
		public void CheckLeftJoin4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
						join ch in
							from c in Child
							where c.ParentID > 0
							select new { c.ParentID, c.ChildID }
						on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch == null
					select p
					,
					from p in db.Parent
						join ch in
							from c in db.Child
							where c.ParentID > 0
							select new { c.ParentID, c.ChildID }
						on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch == null
					select p);
		}

		[Test]
		public void CheckNull1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p != null select p,
					from p in db.Parent where p != null select p);
		}

		[Test]
		public void CheckNull2([DataSources] string context)
		{
			int? n = null;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where n != null || p.ParentID > 1 select p,
					from p in db.Parent where n != null || p.ParentID > 1 select p);
		}

		[Test]
		public void CheckNull3([DataSources(ProviderName.SqlCe)] string context)
		{
			int? n = 1;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where n != null || p.ParentID > 1 select p,
					from p in db.Parent where n != null || p.ParentID > 1 select p);
		}

		[Test]
		public void CheckCondition1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID == 1 && p.Value1 == 1 || p.ParentID == 2 && p.Value1.HasValue
					select p
					,
					from p in db.Parent
					where p.ParentID == 1 && p.Value1 == 1 || p.ParentID == 2 && p.Value1.HasValue
					select p);
		}

		[Test]
		public void CheckCondition2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID == 1 && p.Value1 == 1 || p.ParentID == 2 && (p.ParentID != 3 || p.ParentID == 4) && p.Value1.HasValue
					select p
					,
					from p in db.Parent
					where p.ParentID == 1 && p.Value1 == 1 || p.ParentID == 2 && (p.ParentID != 3 || p.ParentID == 4) && p.Value1.HasValue
					select p);
		}

		[Test]
		public void CompareObject1([DataSources] string context)
		{
			var child = (from ch in Child where ch.ParentID == 2 select ch).First();

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in    Child where ch == child select ch,
					from ch in db.Child where ch == child select ch);
		}

		[Test]
		public void CompareObject2([DataSources] string context)
		{
			var parent = (from p in Parent where p.ParentID == 2 select p).First();

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where parent == p select p,
					from p in db.Parent where parent == p select p);
		}

		[Test]
		public void CompareObject3([DataSources] string context)
		{
			var child = (from ch in Child where ch.ParentID == 2 select ch).First();

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in    Child where ch != child select ch,
					from ch in db.Child where ch != child select ch);
		}

		[Test]
		public void OrAnd([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in Child
					where (c.ParentID == 2 || c.ParentID == 3) && c.ChildID != 21
					select c
					,
					from c in db.Child
					where (c.ParentID == 2 || c.ParentID == 3) && c.ChildID != 21
					select c);
		}

		[Test]
		public void NotOrAnd([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in Child
					where !(c.ParentID == 2 || c.ParentID == 3) && c.ChildID != 44
					select c
					,
					from c in db.Child
					where !(c.ParentID == 2 || c.ParentID == 3) && c.ChildID != 44
					select c);
		}

		[Test]
		public void AndOr([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID == 1 || (p.ParentID == 2 || p.ParentID == 3) && (p.ParentID == 3 || p.ParentID == 1)
					select p,
					from p in db.Parent
					where p.ParentID == 1 || (p.ParentID == 2 || p.ParentID == 3) && (p.ParentID == 3 || p.ParentID == 1)
					select p);
		}

		[Test]
		public void Contains1([DataSources] string context)
		{
			var words = new [] { "John", "Pupkin" };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Person
					where words.Contains(p.FirstName) || words.Contains(p.LastName)
					select p
					,
					from p in db.Person
					where words.Contains(p.FirstName) || words.Contains(p.LastName)
					select p);
		}

		[Test]
		public void Contains2([DataSources] string context)
		{
			IEnumerable<int> ids = new [] { 2, 3 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where ids.Contains(p.ParentID) select p,
					from p in db.Parent where ids.Contains(p.ParentID) select p);
		}

		static IEnumerable<int> GetIds()
		{
			yield return 1;
			yield return 2;
		}

		[Test]
		public void Contains3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where GetIds().Contains(p.ParentID) select p,
					from p in db.Parent where GetIds().Contains(p.ParentID) select p);
		}

		static IEnumerable<int> GetIds(int start, int n)
		{
			for (int i = 0; i < n; i++)
				yield return start + i;
		}

		[Test]
		public void Contains4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where GetIds(1, 2).Contains(p.ParentID) || GetIds(3, 0).Contains(p.ParentID) select p,
					from p in db.Parent where GetIds(1, 2).Contains(p.ParentID) || GetIds(3, 0).Contains(p.ParentID) select p);
		}

		[Test]
		public void Contains5([DataSources] string context)
		{
			IEnumerable<int> ids = new int[0];

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where !ids.Contains(p.ParentID) select p,
					from p in db.Parent where !ids.Contains(p.ParentID) select p);
		}

		[Test]
		public void AliasTest1([DataSources] string context)
		{
			int user = 3;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.ParentID == user select p,
					from p in db.Parent where p.ParentID == user select p);
		}

		[Test]
		public void AliasTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(_ => _.ParentID == 3),
					db.Parent.Where(_ => _.ParentID == 3));
		}

		[Test]
		public void AliasTest3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(_p => _p.ParentID == 3),
					db.Parent.Where(_p => _p.ParentID == 3));
		}

		[Test]
		public void AliasTest4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(тбл => тбл.ParentID == 3),
					db.Parent.Where(тбл => тбл.ParentID == 3));
		}

		[Test]
		public void AliasTest5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p_ => p_.ParentID == 3),
					db.Parent.Where(p_ => p_.ParentID == 3));
		}

		[Test]
		public void SelectNestedCalculatedTest([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					from r in from o in dd.Order select o.Freight * 1000 where r > 100000 select r / 1000,
					from r in from o in db.Order select o.Freight * 1000 where r > 100000 select r / 1000);
			}
		}

		[Test]
		public void CheckField1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					select new { p } into p
					where p.p.ParentID == 1
					select p.p
					,
					from p in db.Parent
					select new { p } into p
					where p.p.ParentID == 1
					select p.p);
		}

		[Test]
		public void CheckField2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					select new { p } into p
					where p.p.ParentID == 1
					select new { p.p.Value1, p },
					from p in db.Parent
					select new { p } into p
					where p.p.ParentID == 1
					select new { p.p.Value1, p });
		}

		[Test]
		public void CheckField3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					select new { p } into p
					where p.p.ParentID == 1
					select new { p.p.Value1, p.p },
					from p in db.Parent
					select new { p } into p
					where p.p.ParentID == 1
					select new { p.p.Value1, p.p });
		}

		[Test]
		public void CheckField4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(p => new { p }).Where(p => p.p.ParentID == 1),
					db.Parent.Select(p => new { p }).Where(p => p.p.ParentID == 1));
		}

		[Test]
		public void CheckField5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(p => new { Value = p.Value1 + 1, p }).Where(p => p.Value == 2 && p.p.ParentID == 1),
					db.Parent.Select(p => new { Value = p.Value1 + 1, p }).Where(p => p.Value == 2 && p.p.ParentID == 1));
		}

		[Test]
		public void CheckField6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent
					select new { p, Value = p.Value1 * 100 } into p
					where p.p.ParentID == 1 && p.Value > 0 select new { p.p.Value1, p.Value, p.p, p1 = p },
					from p in db.Parent
					select new { p, Value = p.Value1 * 100 } into p
					where p.p.ParentID == 1 && p.Value > 0 select new { p.p.Value1, p.Value, p.p, p1 = p });
		}

		[Test]
		public void SubQuery1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Types
					select new { Value = Math.Round(p.MoneyValue, 2) } into pp
					where pp.Value != 0 && pp.Value != 7
					select pp.Value
					,
					from p in db.Types
					select new { Value = Math.Round(p.MoneyValue, 2) } into pp
					where pp.Value != 0 && pp.Value != 7
					select pp.Value);
		}

		[Test]
		public void SearchCondition1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types
					where !t.BoolValue && t.MoneyValue > 1 && (t.SmallIntValue == 5 || t.SmallIntValue == 7 || t.SmallIntValue == 8)
					select t,
					from t in db.Types
					where !t.BoolValue && t.MoneyValue > 1 && (t.SmallIntValue == 5 || t.SmallIntValue == 7 || t.SmallIntValue == 8)
					select t);
		}

		[Test]
		public void GroupBySubQquery1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var p1    = Child;
				var qry1  = p1.GroupBy(x => x.ParentID).Select(x => x.Max(y => y.ChildID));
				var qry12 = p1.Where(x => qry1.Any(y => y == x.ChildID));

				var p2    = db.Child;
				var qry2  = p2.GroupBy(x => x.ParentID).Select(x => x.Max(y => y.ChildID));
				var qry22 = p2.Where(x => qry2.Any(y => y == x.ChildID));

				AreEqual(qry12, qry22);
			}
		}

		[Test]
		public void GroupBySubQquery2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var p1    = Child;
				var qry1  = p1.GroupBy(x => x.ParentID).Select(x => x.Max(y => y.ChildID));
				var qry12 = p1.Where(x => qry1.Contains(x.ChildID));

				var p2    = db.Child;
				var qry2  = p2.GroupBy(x => x.ParentID).Select(x => x.Max(y => y.ChildID));
				var qry22 = p2.Where(x => qry2.Contains(x.ChildID));

				AreEqual(qry12, qry22);
			}
		}

		[Test]
		public void GroupBySubQquery2In([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var p1    = Child;
				var qry1  = p1.GroupBy(x => x.ParentID).Select(x => x.Max(y => y.ChildID));
				var qry12 = p1.Where(x => x.ChildID.In(qry1));

				var p2    = db.Child;
				var qry2  = p2.GroupBy(x => x.ParentID).Select(x => x.Max(y => y.ChildID));
				var qry22 = p2.Where(x => x.ChildID.In(qry2));

				AreEqual(qry12, qry22);
			}
		}

		[Test]
		public void HavingTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					Child
						.GroupBy(c => c.ParentID)
						.Where(c => c.Count() > 1)
						.Select(g => new { count = g.Count() }),
					db.Child
						.GroupBy(c => c.ParentID)
						.Where  (c => c.Count() > 1)
						.Select (g => new { count = g.Count() }));
			}
		}

		[Test]
		public void HavingTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					Child
						.GroupBy(c => c.ParentID)
						.Select (g => new { count = g.Count() })
						.Where  (c => c.count > 1),
					db.Child
						.GroupBy(c => c.ParentID)
						.Select (g => new { count = g.Count() })
						.Having (c => c.count > 1)
						.Where  (c => c.count > 1));
			}
		}

		[Test]
		public void HavingTest3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					Child
						.GroupBy(c => c.ParentID)
						.Where  (c => c.Key > 1 && c.Count() > 1)
						.Select (g => g.Count()),
					db.Child
						.GroupBy(c => c.ParentID)
						.Where  (c => c.Key > 1 && c.Count() > 1)
						.Having (c => c.Key > 1)
						.Select (g => g.Count()));
			}
		}


		[Test]
		public void WhereDateTimeTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Types
						.Where(_ => _.DateTimeValue > new DateTime(2009, 1, 1))
						.Select(_ => _),
					db.Types
						.Where(_ => _.DateTimeValue > new DateTime(2009, 1, 1))
						.Select(_ => _));
			}
		}


		[Test]
		public void WhereDateTimeTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Types
						.Where(_ => _.DateTimeValue > new DateTime(2009, 1, 1))
						.Select(_ => _),
					db.Types
						.Where(_ => _.DateTimeValue > new DateTime(2009, 1, 1))
						.Select(_ => _));
			}
		}

		[Test]
		public void WhereDateTimeTest3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					GetTypes(context)
						.Where(_ => _.DateTimeValue == new DateTime(2009, 9, 27))
						.Select(_ => _),
					db.Types
						.Where(_ => _.DateTimeValue == new DateTime(2009, 9, 27))
						.Select(_ => _));
			}
		}

		[Test]
		public void WhereDateTimeTest4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Types2
						.Where(_ => _.DateTimeValue == new DateTime(2009, 9, 27))
						.Select(_ => _),
					db.Types2
						.Where(_ => _.DateTimeValue == new DateTime(2009, 9, 27))
						.Select(_ => _));
			}
		}

		[Test]
		public void WhereDateTimeTest5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					GetTypes(context)
						.Where(_ => _.DateTimeValue.Date == new DateTime(2009, 9, 20).Date)
						.Select(_ => _),
					db.Types
						.Where(_ => _.DateTimeValue.Date == new DateTime(2009, 9, 20).Date)
						.Select(_ => _));
			}
		}

		[Test]
		public void WhereDateTimeTest6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   AdjustExpectedData(db, Types2
						.Where(_ => _.DateTimeValue.Value.Date == new DateTime(2009, 9, 20).Date)
						.Select(_ => _)),
					db.Types2
						.Where(_ => _.DateTimeValue.Value.Date == new DateTime(2009, 9, 20).Date)
						.Select(_ => _));
			}
		}

		class WhereCases
		{
			[PrimaryKey]
			public int Id                  { get; set; }
			[Column]
			[Column(Configuration = ProviderName.DB2, DbType = "smallint")]
			public bool BoolValue          { get; set; }
			[Column]
			[Column(Configuration = ProviderName.DB2, DbType = "smallint")]
			public bool? NullableBoolValue { get; set; }

			public static readonly IEqualityComparer<WhereCases> Comparer = ComparerBuilder.GetEqualityComparer<WhereCases>();
		}

		[Test]
		public void WhereBooleanTest2([DataSources(TestProvName.AllSybase, TestProvName.AllFirebird)] string context)
		{
			void AreEqualLocal(IEnumerable<WhereCases> expected, IQueryable<WhereCases> actual, Expression<Func<WhereCases,bool>> predicate)
			{
				var exp = expected.Where(predicate.Compile());
				var act = actual.  Where(predicate);
				AreEqual(exp, act, WhereCases.Comparer);

				var notPredicate = Expression.Lambda<Func<WhereCases, bool>>(
					Expression.Not(predicate.Body), predicate.Parameters);

				var expNot = expected.Where(notPredicate.Compile()).ToArray();
				var actNot = actual.  Where(notPredicate).          ToArray();
				AreEqual(expNot, actNot, WhereCases.Comparer);
			}

			void AreEqualLocalPredicate(IEnumerable<WhereCases> expected, IQueryable<WhereCases> actual, Expression<Func<WhereCases,bool>> predicate, Expression<Func<WhereCases,bool>> localPredicate)
			{
				AreEqual(expected.Where(localPredicate.Compile()), actual.Where(predicate), WhereCases.Comparer);

				var notLocalPredicate = Expression.Lambda<Func<WhereCases, bool>>(
					Expression.Not(localPredicate.Body), localPredicate.Parameters);

				var notPredicate = Expression.Lambda<Func<WhereCases, bool>>(
					Expression.Not(predicate.Body), predicate.Parameters);

				AreEqual(expected.Where(notLocalPredicate.Compile()), actual.Where(notPredicate), WhereCases.Comparer);
			}

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(new[]
			{
				new WhereCases { Id = 1,  BoolValue = true,  NullableBoolValue = null  },
				new WhereCases { Id = 2,  BoolValue = true,  NullableBoolValue = true  },
				new WhereCases { Id = 3,  BoolValue = true,  NullableBoolValue = null  },
				new WhereCases { Id = 4,  BoolValue = true,  NullableBoolValue = true  },
				new WhereCases { Id = 5,  BoolValue = true,  NullableBoolValue = true  },

				new WhereCases { Id = 11, BoolValue = false, NullableBoolValue = null  },
				new WhereCases { Id = 12, BoolValue = false, NullableBoolValue = false },
				new WhereCases { Id = 13, BoolValue = false, NullableBoolValue = null  },
				new WhereCases { Id = 14, BoolValue = false, NullableBoolValue = false },
				new WhereCases { Id = 15, BoolValue = false, NullableBoolValue = false },
			}))
			{
				var local = table.ToArray();

				AreEqualLocal(local, table, t => !t.BoolValue && t.Id > 0);
				AreEqualLocal(local, table, t => !(t.BoolValue != true) && t.Id > 0);
				AreEqualLocal(local, table, t => t.BoolValue == true && t.Id > 0);
				AreEqualLocal(local, table, t => t.BoolValue != true && t.Id > 0);
				AreEqualLocal(local, table, t => t.BoolValue == false && t.Id > 0);

				AreEqualLocalPredicate(local, table,
					t => !t.NullableBoolValue.Value && t.Id > 0,
					t => (!t.NullableBoolValue.HasValue || !t.NullableBoolValue.Value) && t.Id > 0);

				AreEqualLocal(local, table, t => !(t.NullableBoolValue != true) && t.Id > 0);
				AreEqualLocal(local, table, t => t.NullableBoolValue == true && t.Id > 0);

				if (!context.StartsWith(ProviderName.Access))
				{
					AreEqualLocal(local, table, t => t.NullableBoolValue == null && t.Id > 0);
					AreEqualLocal(local, table, t => t.NullableBoolValue != null && t.Id > 0);

					AreEqualLocal(local, table, t => !(t.NullableBoolValue == null) && t.Id > 0);
					AreEqualLocal(local, table, t => !(t.NullableBoolValue != null) && t.Id > 0);
				}

				AreEqualLocal(local, table, t => (!t.BoolValue && t.NullableBoolValue != true) && t.Id > 0);
				AreEqualLocal(local, table, t => !(!t.BoolValue && t.NullableBoolValue != true) && t.Id > 0);

				AreEqualLocal(local, table, t => (!t.BoolValue && t.NullableBoolValue == false) && t.Id > 0);

				AreEqualLocal(local, table, t => !(!t.BoolValue && t.NullableBoolValue == false) && t.Id > 0);
			}
		}

		[Test]
		public void IsNullTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in db.Person.AsEnumerable()
					select p.MiddleName into nm
					where !(nm == null)
					select new { nm }
					,
					from p in db.Person
					select p.MiddleName into nm
					where !(nm == null)
					select new { nm });
			}
		}

		[Test]
		public void IsNullOrEmptyTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in db.Person.AsEnumerable()
					select p.MiddleName into nm
					where !(string.IsNullOrEmpty(nm))
					select new { nm }
					,
					from p in db.Person
					select p.MiddleName into nm
					where !(string.IsNullOrEmpty(nm))
					select new { nm });
			}
		}

		[Test]
		public void IsNullOrEmptyTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in db.Person.AsEnumerable()
					select p.FirstName into nm
					where !(string.IsNullOrEmpty(nm))
					select new { nm }
					,
					from p in db.Person
					select p.FirstName into nm
					where !(string.IsNullOrEmpty(nm))
					select new { nm });
			}
		}

		[Test]
		public void LengthTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in db.Person.AsEnumerable()
					select p.MiddleName into nm
					where !(nm?.Length == 0)
					select new { nm }
					,
					from p in db.Person
					select p.MiddleName into nm
					where !(nm.Length == 0)
					select new { nm });
			}
		}

		[Test]
		public void LengthTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in db.Person.AsEnumerable()
					select p.FirstName into nm
					where !(nm.Length == 0)
					select new { nm }
					,
					from p in db.Person
					select p.FirstName into nm
					where !(nm.Length == 0)
					select new { nm });
			}
		}

		[Test]
		public void Issue1755Test1([DataSources] string context, [Values(1, 2)] int id, [Values(null, true, false)] bool? flag)
		{
			using (var db = GetDataContext(context))
			{
				var results = (from c in db.Parent
							   where c.ParentID == id
								   && (!flag.HasValue || flag.Value && c.Value1 == null || !flag.Value && c.Value1 != null)
							   select c);

				var sql = results.ToString();

				AreEqual(
					from c in db.Parent.AsEnumerable()
					where c.ParentID == id
						&& (!flag.HasValue || flag.Value && c.Value1 == null || !flag.Value && c.Value1 != null)
					select c,
					results,
					true);

				// remote context doesn't have access to final SQL
				if (!context.EndsWith(".LinqService"))
					Assert.AreEqual(flag == null ? 0 : 1, Regex.Matches(sql, " AND ").Count);
			}
		}

		[Test]
		public void Issue1755Test2([DataSources] string context, [Values(1, 2)] int id, [Values(null, true, false)] bool? flag)
		{
			using (var db = GetDataContext(context))
			{
				var results = (from c in db.Parent
							   where c.ParentID == id
								   && (flag == null || flag.Value && c.Value1 == null || !flag.Value && c.Value1 != null)
							   select c);

				var sql = results.ToString();

				AreEqual(
					from c in db.Parent.AsEnumerable()
					where c.ParentID == id
						&& (flag == null || flag.Value && c.Value1 == null || !flag.Value && c.Value1 != null)
					select c,
					results,
					true);

				// remote context doesn't have access to final SQL
				if (!context.EndsWith(".LinqService"))
					Assert.AreEqual(flag == null ? 0 : 1, Regex.Matches(sql, " AND ").Count);
			}
		}
	}
}

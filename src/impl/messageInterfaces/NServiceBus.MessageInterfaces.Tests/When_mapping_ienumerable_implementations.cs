namespace NServiceBus.MessageInterfaces.Tests
{
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
	public class When_mapping_ienumerable_implementations
	{
		IMessageMapper mapper;

		[SetUp]
		public void SetUp()
		{
			mapper = new MessageMapper.Reflection.MessageMapper();
		}

		[Test]
		public void Class_implementing_ienumerable_string_should_be_mapped()
		{
			mapper.Initialize(new[] { typeof(ClassImplementingIEnumerable<string>) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(ClassImplementingIEnumerable<string>)));
		}

		[Test]
		public void Class_implementing_ienumerable_string_and_ireturnmyself_should_be_mapped()
		{
			mapper.Initialize(new[] { typeof(ClassImplementingIEnumerableAndIReturnMyself<string>) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(ClassImplementingIEnumerableAndIReturnMyself<string>)));
		}

		[Test]
		public void Class_implementing_ienumerable_returnmyself_should_be_mapped()
		{
			mapper.Initialize(new[] { typeof(ClassImplementingIEnumerable<ReturnMyself>) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(ClassImplementingIEnumerable<ReturnMyself>)));
		}

		[Test]
		public void Class_inheriting_from_ienumerable_returnmyself_implementation_should_be_mapped()
		{
			mapper.Initialize(new[] { typeof(DerivedReturnMyselfCollectionObject) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(DerivedReturnMyselfCollectionObject)));
		}

		[Test]
		public void Class_implementing_returnmyself_inheriting_from_ienumerable_returnmyself_implementation_should_be_mapped()
		{
			mapper.Initialize(new[] { typeof(DerivedReturnMyselfCollectionImplementingIReturnMyself) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(DerivedReturnMyselfCollectionImplementingIReturnMyself)));
		}

		[Test]
		public void Class_implementing_base_returnmyself_inheriting_from_ienumerable_returnmyself_implementation_should_be_mapped()
		{
			mapper.Initialize(new[] { typeof(DerivedReturnMyselfCollectionImplementingBaseIReturnMyself) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(DerivedReturnMyselfCollectionImplementingBaseIReturnMyself)));
		}

		internal class ClassImplementingIEnumerable<TItem> : IEnumerable<TItem>
		{
			public IEnumerator<TItem> GetEnumerator()
			{
				return new List<TItem>.Enumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		internal class ClassImplementingIEnumerableAndIReturnMyself<TItem> : IEnumerable<TItem>, IReturnMyself<ClassImplementingIEnumerableAndIReturnMyself<TItem>>
		{
			public IEnumerator<TItem> GetEnumerator()
			{
				return new List<TItem>.Enumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}


			public ClassImplementingIEnumerableAndIReturnMyself<TItem> ReturnMe()
			{
				return this;
			}
		}

		internal interface IReturnMyself<T>
		{
			T ReturnMe();
		}

		internal class ReturnMyself : IReturnMyself<ReturnMyself>
		{
			public ReturnMyself ReturnMe()
			{
				return this;
			}
		}

		internal class DerivedReturnMyselfCollectionImplementingIReturnMyself : ClassImplementingIEnumerable<ReturnMyself>, IReturnMyself<DerivedReturnMyselfCollectionImplementingIReturnMyself>
		{
			public DerivedReturnMyselfCollectionImplementingIReturnMyself ReturnMe()
			{
				return this;
			}
		}

		internal class DerivedReturnMyselfCollectionObject : ClassImplementingIEnumerable<ReturnMyself>
		{
		}

		internal class DerivedReturnMyselfCollectionImplementingBaseIReturnMyself : ClassImplementingIEnumerable<ReturnMyself>, IReturnMyself<ClassImplementingIEnumerable<ReturnMyself>>
		{
			public ClassImplementingIEnumerable<ReturnMyself> ReturnMe()
			{
				return this;
			}
		}
	}
}

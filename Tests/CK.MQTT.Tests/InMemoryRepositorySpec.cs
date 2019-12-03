using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CK.MQTT.Sdk.Storage;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Tests
{
	public class InMemoryRepositorySpec
    {
		[Test]
		public void when_creating_item_then_succeeds()
		{
			var repository = new InMemoryRepository<FooStorageObject>();

			repository.Create(new FooStorageObject { Id = "Foo1", Value = 1 });
			repository.Create(new FooStorageObject { Id = "Foo2", Value = 2 });
			repository.Create(new FooStorageObject { Id = "Foo3", Value = 3 });
			repository.Create(new FooStorageObject { Id = "Foo4", Value = 4 });

			4.Should().Be(repository.ReadAll().Count());
		}

		[Test]
		public void when_updating_item_then_succeeds()
		{
			var repository = new InMemoryRepository<FooStorageObject>();
			var item = new FooStorageObject { Id = "Foo1", Value = 1 };

			repository.Create(item);

			item.Value = 2;

			repository.Update(item);
			
			2.Should().Be(repository.ReadAll().First().Value);
		}

		[Test]
		public void when_deleting_item_then_succeeds()
		{
			var repository = new InMemoryRepository<FooStorageObject>();
			var item = new FooStorageObject { Id = "Foo1", Value = 1 };

			repository.Create(item);
			repository.Delete("Foo1");
            repository.ReadAll().Should().BeEmpty();
		}

		[Test]
		public void when_deleting_item_with_invalid_id_then_does_not_delete()
		{
			var repository = new InMemoryRepository<FooStorageObject>();
			var item = new FooStorageObject { Id = "Foo1", Value = 1 };

			repository.Create(item);
			repository.Delete("Foo2");
            repository.ReadAll().Should().NotBeEmpty();
		}

		[Test]
		public async Task when_getting_element_by_id_in_multiple_threads_then_succeeds()
		{
			var count = 100;
			var repository = new InMemoryRepository<FooStorageObject>();
			var bag = new ConcurrentBag<FooStorageObject>();
			var createTasks = new List<Task>();

			for (var i = 1; i < count; i++)
			{
				createTasks.Add(Task.Run(() =>
				{
					var item = new FooStorageObject { Id = $"Foo{i}", Value = i };

					repository.Create(item);
				}));
			}

			await Task.WhenAll(createTasks);

			var random = new Random();

			Parallel.For(fromInclusive: 1, toExclusive: count + 1, body: i => {
				var value = random.Next(minValue: 1, maxValue: count);
				var element = repository.Read($"Foo{value}");

				bag.Add(element);
			});

			count.Should().Be(bag.Count);
		}
	}

	class FooStorageObject : IStorageObject
	{
		public string Id { get; set; }

		public int Value { get; set; }
	}
}

#nullable enable

namespace NServiceBus.Core.Tests.Host;

using System;
using System.Collections;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;

[TestFixture]
public class KeyedServiceCollectionAdapterTests
{
    static KeyedServiceCollectionAdapter CreateAdapter(string serviceKey = "endpoint") =>
        new(new ServiceCollection(), serviceKey);

    [Test]
    public void Constructor_should_throw_on_null_inner()
    {
        Assert.Throws<ArgumentNullException>(() => new KeyedServiceCollectionAdapter(null!, "key"));
    }

    [Test]
    public void Constructor_should_throw_on_null_service_key()
    {
        Assert.Throws<ArgumentNullException>(() => new KeyedServiceCollectionAdapter(new ServiceCollection(), null!));
    }

    [Test]
    public void ServiceKey_should_return_keyed_service_key()
    {
        var adapter = CreateAdapter("my-endpoint");
        Assert.That(adapter.ServiceKey.BaseKey, Is.EqualTo("my-endpoint"));
    }

    [Test]
    public void Inner_should_return_wrapped_collection()
    {
        var inner = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(inner, "key");
        Assert.That(adapter.Inner, Is.SameAs(inner));
    }

    [Test]
    public void Count_should_return_zero_initially()
    {
        var adapter = CreateAdapter();
        Assert.That(adapter, Is.Empty);
    }

    [Test]
    public void IsReadOnly_should_return_false()
    {
        var adapter = CreateAdapter();
        Assert.That(adapter.IsReadOnly, Is.False);
    }

    [Test]
    public void Add_non_keyed_type_descriptor_should_register_in_inner()
    {
        var inner = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(inner, "ep");

        var descriptor = new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient);
        adapter.Add(descriptor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter, Has.Count.EqualTo(1));
            Assert.That(inner, Has.Count.EqualTo(1));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(inner[0].ServiceType, Is.EqualTo(typeof(IFoo)));
            Assert.That(inner[0].ServiceKey, Is.InstanceOf<KeyedServiceKey>());
        }
    }

    [Test]
    public void Add_non_keyed_instance_descriptor_should_register_in_inner()
    {
        var inner = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(inner, "ep");

        var instance = new Foo();
        var descriptor = new ServiceDescriptor(typeof(IFoo), instance);
        adapter.Add(descriptor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter, Has.Count.EqualTo(1));
            Assert.That(inner, Has.Count.EqualTo(1));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(inner[0].IsKeyedService, Is.True);
            Assert.That(inner[0].KeyedImplementationInstance, Is.SameAs(instance));
        }
    }

    [Test]
    public void Add_non_keyed_factory_descriptor_should_register_in_inner()
    {
        var inner = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(inner, "ep");

        var descriptor = ServiceDescriptor.Scoped<IFoo, Foo>();
        adapter.Add(descriptor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter, Has.Count.EqualTo(1));
            Assert.That(inner, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void Add_keyed_type_descriptor_should_register_in_inner_with_composite_key()
    {
        var inner = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(inner, "ep");

        var descriptor = new ServiceDescriptor(typeof(IFoo), "sub-key", typeof(Foo), ServiceLifetime.Transient);
        adapter.Add(descriptor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter, Has.Count.EqualTo(1));
            Assert.That(inner, Has.Count.EqualTo(1));
        }
        var innerKey = (KeyedServiceKey)inner[0].ServiceKey!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(innerKey.BaseKey, Is.EqualTo("ep"));
            Assert.That(innerKey.ServiceKey, Is.EqualTo("sub-key"));
        }
    }

    [Test]
    public void Add_keyed_instance_descriptor_should_register_in_inner_with_composite_key()
    {
        var inner = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(inner, "ep");

        var instance = new Foo();
        var descriptor = new ServiceDescriptor(typeof(IFoo), "sub-key", instance);
        adapter.Add(descriptor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter, Has.Count.EqualTo(1));
            Assert.That(inner[0].KeyedImplementationInstance, Is.SameAs(instance));
        }
    }

    [Test]
    public void Add_keyed_factory_descriptor_should_register_in_inner_with_composite_key()
    {
        var inner = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(inner, "ep");

        var descriptor = new ServiceDescriptor(typeof(IFoo), "sub-key", (sp, key) => new Foo(), ServiceLifetime.Scoped);
        adapter.Add(descriptor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter, Has.Count.EqualTo(1));
            Assert.That(inner[0].KeyedImplementationFactory, Is.Not.Null);
        }
    }

    [Test]
    public void Add_should_throw_on_null_descriptor()
    {
        var adapter = CreateAdapter();
        Assert.Throws<ArgumentNullException>(() => adapter.Add(null!));
    }

    [Test]
    public void Add_multiple_descriptors_should_maintain_order()
    {
        var inner = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(inner, "ep");

        var d1 = new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient);
        var d2 = new ServiceDescriptor(typeof(IBar), typeof(Bar), ServiceLifetime.Scoped);
        var d3 = new ServiceDescriptor(typeof(IBaz), typeof(Baz), ServiceLifetime.Singleton);

        adapter.Add(d1);
        adapter.Add(d2);
        adapter.Add(d3);

        Assert.That(adapter, Has.Count.EqualTo(3));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter[0].ServiceType, Is.EqualTo(typeof(IFoo)));
            Assert.That(adapter[1].ServiceType, Is.EqualTo(typeof(IBar)));
            Assert.That(adapter[2].ServiceType, Is.EqualTo(typeof(IBaz)));
        }
    }

    [Test]
    public void GetEnumerator_should_return_original_descriptors_for_tryadd_compatibility()
    {
        var adapter = CreateAdapter("ep");

        var original = new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient);
        adapter.Add(original);

        using var enumerator = adapter.GetEnumerator();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current.ServiceType, Is.EqualTo(typeof(IFoo)));
            Assert.That(enumerator.Current.ServiceKey, Is.Null);
        }
    }

    [Test]
    public void TryAdd_should_not_add_duplicate_service_type_for_non_keyed_descriptor()
    {
        var adapter = CreateAdapter("ep");

        adapter.TryAddSingleton<IFoo, Foo>();
        adapter.TryAddSingleton<IFoo, Foo>();

        Assert.That(adapter, Has.Count.EqualTo(1));
    }

    [Test]
    public void TryAdd_should_not_add_duplicate_service_type_for_keyed_descriptor()
    {
        var adapter = CreateAdapter("ep");

        adapter.TryAddKeyedSingleton<IFoo, Foo>("sub");
        adapter.TryAddKeyedSingleton<IFoo, Foo>("sub");

        Assert.That(adapter, Has.Count.EqualTo(1));
    }

    [Test]
    public void TryAdd_should_allow_different_service_types()
    {
        var adapter = CreateAdapter("ep");

        adapter.TryAddSingleton<IFoo, Foo>();
        adapter.TryAddSingleton<IBar, Bar>();

        Assert.That(adapter, Has.Count.EqualTo(2));
    }

    [Test]
    public void TryAdd_should_allow_same_service_type_with_different_keyed_service_key()
    {
        var adapter = CreateAdapter("ep");

        adapter.TryAddKeyedSingleton<IFoo, Foo>("sub1");
        adapter.TryAddKeyedSingleton<IFoo, Foo>("sub2");

        Assert.That(adapter, Has.Count.EqualTo(2));
    }

    [Test]
    public void TryAdd_enumeration_should_see_original_key_so_dedup_works()
    {
        var adapter = CreateAdapter("ep");

        adapter.Add(new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Singleton));
        adapter.Add(new ServiceDescriptor(typeof(IFoo), typeof(Foo2), ServiceLifetime.Singleton));

        Assert.That(adapter, Has.Count.EqualTo(2));

        adapter.TryAddSingleton<IFoo, Foo>();

        Assert.That(adapter, Has.Count.EqualTo(2), "TryAdd should not add a third IFoo because one already exists with ServiceKey=null.");
    }

    [Test]
    public void TryAdd_should_not_add_when_same_keyed_key_exists()
    {
        var adapter = CreateAdapter("ep");

        adapter.Add(new ServiceDescriptor(typeof(IFoo), "sub-key", typeof(Foo), ServiceLifetime.Singleton));
        adapter.TryAddKeyedSingleton<IFoo, Foo>("sub-key");

        Assert.That(adapter, Has.Count.EqualTo(1));
    }

    [Test]
    public void Contains_should_find_original_descriptor()
    {
        var adapter = CreateAdapter();
        var descriptor = new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient);
        adapter.Add(descriptor);

        Assert.That(adapter, Does.Contain(descriptor));
    }

    [Test]
    public void Contains_should_return_false_for_descriptor_not_in_collection()
    {
        var adapter = CreateAdapter();
        adapter.Add(new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient));

        var other = new ServiceDescriptor(typeof(IBar), typeof(Bar), ServiceLifetime.Scoped);
        Assert.That(adapter, Does.Not.Contain(other));
    }

    [Test]
    public void Contains_should_throw_on_null()
    {
        var adapter = CreateAdapter();
        Assert.Throws<ArgumentNullException>(() => adapter.Contains(null!));
    }

    [Test]
    public void IndexOf_should_return_index_of_original_descriptor()
    {
        var adapter = CreateAdapter();
        var d1 = new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient);
        var d2 = new ServiceDescriptor(typeof(IBar), typeof(Bar), ServiceLifetime.Scoped);
        adapter.Add(d1);
        adapter.Add(d2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter.IndexOf(d1), Is.Zero);
            Assert.That(adapter.IndexOf(d2), Is.EqualTo(1));
        }
    }

    [Test]
    public void IndexOf_should_return_negative_one_for_missing_descriptor()
    {
        var adapter = CreateAdapter();
        adapter.Add(new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient));

        var missing = new ServiceDescriptor(typeof(IBar), typeof(Bar), ServiceLifetime.Scoped);
        Assert.That(adapter.IndexOf(missing), Is.EqualTo(-1));
    }

    [Test]
    public void IndexOf_should_throw_on_null()
    {
        var adapter = CreateAdapter();
        Assert.Throws<ArgumentNullException>(() => adapter.IndexOf(null!));
    }

    [Test]
    public void Indexer_get_should_return_original_descriptor()
    {
        var adapter = CreateAdapter();
        var descriptor = new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient);
        adapter.Add(descriptor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter[0].ServiceType, Is.EqualTo(typeof(IFoo)));
            Assert.That(adapter[0].ServiceKey, Is.Null);
        }
    }

    [Test]
    public void Indexer_set_should_throw_not_supported()
    {
        var adapter = CreateAdapter();
        Assert.Throws<NotSupportedException>(() => adapter[0] = new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient));
    }

    [Test]
    public void Insert_should_throw_not_supported()
    {
        var adapter = CreateAdapter();
        Assert.Throws<NotSupportedException>(() => adapter.Insert(0, new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient)));
    }

    [Test]
    public void Remove_should_remove_descriptor_by_original()
    {
        var inner = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(inner, "ep");

        var descriptor = new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Singleton);
        adapter.Add(descriptor);

        var result = adapter.Remove(descriptor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(adapter, Is.Empty);
            Assert.That(inner, Is.Empty);
        }
    }

    [Test]
    public void Remove_should_return_false_for_missing_descriptor()
    {
        var adapter = CreateAdapter();

        var result = adapter.Remove(new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Singleton));

        Assert.That(result, Is.False);
    }

    [Test]
    public void Remove_should_throw_on_null()
    {
        var adapter = CreateAdapter();
        Assert.Throws<ArgumentNullException>(() => adapter.Remove(null!));
    }

    [Test]
    public void RemoveAt_should_remove_descriptor_at_index()
    {
        var inner = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(inner, "ep");

        var d1 = new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient);
        var d2 = new ServiceDescriptor(typeof(IBar), typeof(Bar), ServiceLifetime.Scoped);
        adapter.Add(d1);
        adapter.Add(d2);

        adapter.RemoveAt(0);

        Assert.That(adapter, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter[0].ServiceType, Is.EqualTo(typeof(IBar)));
            Assert.That(inner, Has.Count.EqualTo(1));
        }
        Assert.That(inner[0].ServiceType, Is.EqualTo(typeof(IBar)));
    }

    [Test]
    public void Clear_should_remove_all_from_adapter_and_inner()
    {
        var inner = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(inner, "ep")
        {
            new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient),
            new ServiceDescriptor(typeof(IBar), typeof(Bar), ServiceLifetime.Scoped)
        };

        adapter.Clear();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter, Is.Empty);
            Assert.That(inner, Is.Empty);
        }
    }

    [Test]
    public void CopyTo_should_copy_original_descriptors()
    {
        var adapter = CreateAdapter();
        var d1 = new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient);
        var d2 = new ServiceDescriptor(typeof(IBar), typeof(Bar), ServiceLifetime.Scoped);
        adapter.Add(d1);
        adapter.Add(d2);

        var array = new ServiceDescriptor[2];
        adapter.CopyTo(array, 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(array[0].ServiceType, Is.EqualTo(typeof(IFoo)));
            Assert.That(array[0].ServiceKey, Is.Null);
            Assert.That(array[1].ServiceType, Is.EqualTo(typeof(IBar)));
            Assert.That(array[1].ServiceKey, Is.Null);
        }
    }

    [Test]
    public void ContainsLocalService_should_distinguish_keyed_and_non_keyed_registrations()
    {
        var adapter = CreateAdapter();
        adapter.Add(new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient));
        adapter.Add(new ServiceDescriptor(typeof(IFoo), "sub", typeof(Foo2), ServiceLifetime.Transient));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter.ContainsLocalService(typeof(IFoo), null), Is.True);
            Assert.That(adapter.ContainsLocalService(typeof(IFoo), "sub"), Is.True);
            Assert.That(adapter.ContainsLocalService(typeof(IFoo), "other"), Is.False);
        }
    }

    [Test]
    public void ContainsLocalService_should_match_closed_generic_request_for_open_generic_registration()
    {
        var adapter = CreateAdapter();
        adapter.Add(new ServiceDescriptor(typeof(IGeneric<>), "sub", typeof(GenericFoo<>), ServiceLifetime.Transient));

        Assert.That(adapter.ContainsLocalService(typeof(IGeneric<Foo>), "sub"), Is.True);
    }

    [Test]
    public void ContainsLocalService_should_return_false_after_remove()
    {
        var adapter = CreateAdapter();
        var descriptor = new ServiceDescriptor(typeof(IFoo), "sub", typeof(Foo), ServiceLifetime.Transient);
        adapter.Add(descriptor);

        adapter.Remove(descriptor);

        Assert.That(adapter.ContainsLocalService(typeof(IFoo), "sub"), Is.False);
    }

    [Test]
    public void ContainsLocalService_should_return_false_after_remove_at()
    {
        var adapter = CreateAdapter();
        adapter.Add(new ServiceDescriptor(typeof(IFoo), "sub", typeof(Foo), ServiceLifetime.Transient));

        adapter.RemoveAt(0);

        Assert.That(adapter.ContainsLocalService(typeof(IFoo), "sub"), Is.False);
    }

    [Test]
    public void ContainsLocalService_should_return_false_after_clear()
    {
        var adapter = CreateAdapter();
        adapter.Add(new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient));
        adapter.Add(new ServiceDescriptor(typeof(IFoo), "sub", typeof(Foo2), ServiceLifetime.Transient));

        adapter.Clear();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter.ContainsLocalService(typeof(IFoo), null), Is.False);
            Assert.That(adapter.ContainsLocalService(typeof(IFoo), "sub"), Is.False);
        }
    }

    [Test]
    public void GetLocalServiceKey_should_project_to_endpoint_composite_key()
    {
        var adapter = CreateAdapter("ep");

        var key = adapter.GetLocalServiceKey("sub");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(key.BaseKey, Is.EqualTo("ep"));
            Assert.That(key.ServiceKey, Is.EqualTo("sub"));
        }
    }

    [Test]
    public void Add_should_augment_non_keyed_descriptor_to_keyed_in_inner()
    {
        var inner = new ServiceCollection();

        _ = new KeyedServiceCollectionAdapter(inner, "ep")
        {
            new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Singleton)
        };

        Assert.That(inner[0].ServiceKey, Is.InstanceOf<KeyedServiceKey>());
        var key = (KeyedServiceKey)inner[0].ServiceKey!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(key.BaseKey, Is.EqualTo("ep"));
            Assert.That(key.ServiceKey, Is.Null);
        }
    }

    [Test]
    public void Add_should_augment_keyed_descriptor_with_composite_key_in_inner()
    {
        var inner = new ServiceCollection();

        _ = new KeyedServiceCollectionAdapter(inner, "ep")
        {
            new ServiceDescriptor(typeof(IFoo), "sub", typeof(Foo), ServiceLifetime.Singleton)
        };

        Assert.That(inner[0].ServiceKey, Is.InstanceOf<KeyedServiceKey>());
        var key = (KeyedServiceKey)inner[0].ServiceKey!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(key.BaseKey, Is.EqualTo("ep"));
            Assert.That(key.ServiceKey, Is.EqualTo("sub"));
        }
    }

    [Test]
    public void Enumeration_should_return_local_slice_with_original_keys()
    {
        var adapter = CreateAdapter("ep");

        adapter.Add(new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Singleton));
        adapter.Add(new ServiceDescriptor(typeof(IFoo), "sub", typeof(Foo2), ServiceLifetime.Singleton));

        var list = adapter.ToList();
        Assert.That(list, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(list[0].ServiceKey, Is.Null, "Non-keyed descriptor should have null ServiceKey in local slice.");
            Assert.That(list[1].ServiceKey, Is.EqualTo("sub"), "Keyed descriptor should keep original ServiceKey in local slice.");
        }
    }

    [Test]
    public void Remove_should_sync_with_inner_collection()
    {
        var inner = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(inner, "ep");

        var d1 = new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Singleton);
        var d2 = new ServiceDescriptor(typeof(IBar), typeof(Bar), ServiceLifetime.Scoped);
        adapter.Add(d1);
        adapter.Add(d2);

        Assert.That(inner, Has.Count.EqualTo(2));

        adapter.Remove(d1);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter, Has.Count.EqualTo(1));
            Assert.That(inner, Has.Count.EqualTo(1));
        }
        Assert.That(inner[0].ServiceType, Is.EqualTo(typeof(IBar)));
    }

    [Test]
    public void Clear_should_remove_all_from_inner()
    {
        var inner = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(inner, "ep")
        {
            new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Singleton),
            new ServiceDescriptor(typeof(IBar), typeof(Bar), ServiceLifetime.Scoped)
        };

        adapter.Clear();

        Assert.That(inner, Is.Empty);
    }

    [Test]
    public void IEnumerable_non_generic_enumerator_should_return_original_descriptors()
    {
        var adapter = CreateAdapter();
        var descriptor = new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient);
        adapter.Add(descriptor);

        var enumerable = (IEnumerable)adapter;
        var count = 0;
        foreach (ServiceDescriptor item in enumerable)
        {
            count++;
            Assert.That(item.ServiceKey, Is.Null);
        }

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void TryAddSingleton_should_work_with_adapter()
    {
        var inner = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(inner, "ep");

        adapter.TryAddSingleton<IFoo, Foo>();

        Assert.That(adapter, Has.Count.EqualTo(1));
    }

    [Test]
    public void TryAddSingleton_should_not_add_duplicate()
    {
        var inner = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(inner, "ep");

        adapter.TryAddSingleton<IFoo, Foo>();
        adapter.TryAddSingleton<IFoo, Foo2>();

        Assert.That(adapter, Has.Count.EqualTo(1), "TryAdd should not add when service type already registered.");
        Assert.That(adapter[0].ImplementationType, Is.EqualTo(typeof(Foo)));
    }

    [Test]
    public void TryAddScoped_should_not_add_duplicate()
    {
        var adapter = CreateAdapter();
        adapter.TryAddScoped<IFoo, Foo>();
        adapter.TryAddScoped<IFoo, Foo2>();

        Assert.That(adapter, Has.Count.EqualTo(1));
    }

    [Test]
    public void TryAddTransient_should_not_add_duplicate()
    {
        var adapter = CreateAdapter();
        adapter.TryAddTransient<IFoo, Foo>();
        adapter.TryAddTransient<IFoo, Foo2>();

        Assert.That(adapter, Has.Count.EqualTo(1));
    }

    [Test]
    public void TryAddKeyedSingleton_should_not_add_duplicate_same_key()
    {
        var adapter = CreateAdapter("ep");
        adapter.TryAddKeyedSingleton<IFoo, Foo>("sub");
        adapter.TryAddKeyedSingleton<IFoo, Foo2>("sub");

        Assert.That(adapter, Has.Count.EqualTo(1));
    }

    [Test]
    public void TryAddKeyedSingleton_should_allow_different_key()
    {
        var adapter = CreateAdapter("ep");
        adapter.TryAddKeyedSingleton<IFoo, Foo>("sub1");
        adapter.TryAddKeyedSingleton<IFoo, Foo2>("sub2");

        Assert.That(adapter, Has.Count.EqualTo(2));
    }

    [Test]
    public void Add_then_TryAdd_should_recognize_existing_registration()
    {
        var adapter = CreateAdapter("ep");
        adapter.Add(new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Singleton));
        adapter.TryAddSingleton<IFoo, Foo2>();

        Assert.That(adapter, Has.Count.EqualTo(1));
    }

    [Test]
    public void Add_keyed_then_TryAdd_keyed_should_recognize_existing_registration()
    {
        var adapter = CreateAdapter("ep");
        adapter.Add(new ServiceDescriptor(typeof(IFoo), "sub", typeof(Foo), ServiceLifetime.Singleton));
        adapter.TryAddKeyedSingleton<IFoo, Foo2>("sub");

        Assert.That(adapter, Has.Count.EqualTo(1));
    }

    [Test]
    public void Add_keyed_then_TryAdd_non_keyed_should_allow_addition()
    {
        var adapter = CreateAdapter("ep");
        adapter.Add(new ServiceDescriptor(typeof(IFoo), "sub", typeof(Foo), ServiceLifetime.Singleton));
        adapter.TryAddSingleton<IFoo, Foo2>();

        Assert.That(adapter, Has.Count.EqualTo(2), "Keyed and non-keyed are different registrations.");
    }

    [Test]
    public void RemoveAt_with_multiple_descriptors_should_adjust_indices()
    {
        var inner = new ServiceCollection();
        var adapter = new KeyedServiceCollectionAdapter(inner, "ep");

        var d1 = new ServiceDescriptor(typeof(IFoo), typeof(Foo), ServiceLifetime.Transient);
        var d2 = new ServiceDescriptor(typeof(IBar), typeof(Bar), ServiceLifetime.Scoped);
        var d3 = new ServiceDescriptor(typeof(IBaz), typeof(Baz), ServiceLifetime.Singleton);
        adapter.Add(d1);
        adapter.Add(d2);
        adapter.Add(d3);

        adapter.RemoveAt(1);

        Assert.That(adapter, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter[0].ServiceType, Is.EqualTo(typeof(IFoo)));
            Assert.That(adapter[1].ServiceType, Is.EqualTo(typeof(IBaz)));
            Assert.That(inner, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public void Add_instance_descriptor_should_keep_instance_in_original()
    {
        var adapter = CreateAdapter();
        var instance = new Foo();
        adapter.Add(new ServiceDescriptor(typeof(IFoo), instance));

        Assert.That(adapter[0].ImplementationInstance, Is.SameAs(instance));
    }

    interface IFoo;

    interface IBar;

    interface IBaz;

    interface IGeneric<T>;

    class Foo : IFoo;

    class Foo2 : IFoo;

    class Bar : IBar;

    class Baz : IBaz;

    class GenericFoo<T> : IGeneric<T>;
}
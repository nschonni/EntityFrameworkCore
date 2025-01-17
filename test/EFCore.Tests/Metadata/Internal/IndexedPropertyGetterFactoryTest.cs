// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.InMemory.Metadata.Conventions;
using Xunit;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.Metadata.Internal
{
    public class IndexedPropertyGetterFactoryTest
    {
        [ConditionalFact]
        public void Delegate_getter_is_returned_for_indexed_property()
        {
            var modelBuilder = new ModelBuilder(InMemoryConventionSetBuilder.Build());
            modelBuilder.Entity<IndexedClass>().Property(e => e.Id);
            var entityType = modelBuilder.Model.FindEntityType(typeof(IndexedClass));
            var propertyA = entityType.AddIndexedProperty("PropertyA", typeof(string));
            var propertyB = entityType.AddIndexedProperty("PropertyB", typeof(int));
            modelBuilder.FinalizeModel();

            var indexedClass = new IndexedClass();
            Assert.Equal(
                "ValueA", new IndexedPropertyGetterFactory().Create(propertyA).GetClrValue(indexedClass));
            Assert.Equal(
                123, new IndexedPropertyGetterFactory().Create(propertyB).GetClrValue(indexedClass));
        }

        [ConditionalFact]
        public void Exception_is_returned_for_indexed_property_without_indexer()
        {
            var entityType = ((IMutableModel)new Model()).AddEntityType(typeof(NonIndexedClass));
            var idProperty = entityType.AddProperty("Id", typeof(int));
            Assert.Throws<InvalidOperationException>(
                () => entityType.AddIndexedProperty("PropA", typeof(string)));
            Assert.Throws<InvalidOperationException>(
                () => entityType.AddIndexedProperty("PropB", typeof(int)));
        }

        private class IndexedClass
        {
            private readonly Dictionary<string, object> _internalValues = new Dictionary<string, object>
            {
                { "PropertyA", "ValueA" },
                { "PropertyB", 123 }
            };

            internal int Id { get; set; }

            public object this[string name]
            {
                get => _internalValues[name];
                set => _internalValues[name] = value;
            }
        }

        private class NonIndexedClass
        {
            internal int Id { get; set; }
            public string PropA { get; set; }
            public int PropB { get; set; }
        }
    }
}

﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.Query
{
    public abstract class AsyncFromSqlSprocQueryTestBase<TFixture> : IClassFixture<TFixture>
        where TFixture : NorthwindQueryRelationalFixture<NoopModelCustomizer>, new()
    {
        protected AsyncFromSqlSprocQueryTestBase(TFixture fixture) => Fixture = fixture;

        protected TFixture Fixture { get; }

        [ConditionalFact]
        public virtual async Task From_sql_queryable_stored_procedure()
        {
            using (var context = CreateContext())
            {
                var actual = await context
                    .Set<MostExpensiveProduct>()
                    .FromSqlRaw(TenMostExpensiveProductsSproc, GetTenMostExpensiveProductsParameters())
                    .ToArrayAsync();

                Assert.Equal(10, actual.Length);

                Assert.True(
                    actual.Any(
                        mep =>
                            mep.TenMostExpensiveProducts == "Côte de Blaye"
                            && mep.UnitPrice == 263.50m));
            }
        }

        [ConditionalFact]
        public virtual async Task From_sql_queryable_stored_procedure_with_parameter()
        {
            using (var context = CreateContext())
            {
                var actual = await context
                    .Set<CustomerOrderHistory>()
                    .FromSqlRaw(CustomerOrderHistorySproc, GetCustomerOrderHistorySprocParameters())
                    .ToArrayAsync();

                Assert.Equal(11, actual.Length);

                Assert.True(
                    actual.Any(
                        coh =>
                            coh.ProductName == "Aniseed Syrup"
                            && coh.Total == 6));
            }
        }

        [ConditionalFact(Skip = "Issue #14935. Cannot eval 'where [mep].TenMostExpensiveProducts.Contains(\"C\")'")]
        public virtual async Task From_sql_queryable_stored_procedure_composed()
        {
            using (var context = CreateContext())
            {
                var actual = await context
                    .Set<MostExpensiveProduct>()
                    .FromSqlRaw(TenMostExpensiveProductsSproc, GetTenMostExpensiveProductsParameters())
                    .Where(mep => mep.TenMostExpensiveProducts.Contains("C"))
                    .OrderBy(mep => mep.UnitPrice)
                    .ToArrayAsync();

                Assert.Equal(4, actual.Length);
                Assert.Equal(46.00m, actual.First().UnitPrice);
                Assert.Equal(263.50m, actual.Last().UnitPrice);
            }
        }

        [ConditionalFact(Skip = "Issue #14935. Cannot eval 'where [coh].ProductName.Contains(\"C\")'")]
        public virtual async Task From_sql_queryable_stored_procedure_with_parameter_composed()
        {
            using (var context = CreateContext())
            {
                var actual = await context
                    .Set<CustomerOrderHistory>()
                    .FromSqlRaw(CustomerOrderHistorySproc, GetCustomerOrderHistorySprocParameters())
                    .Where(coh => coh.ProductName.Contains("C"))
                    .OrderBy(coh => coh.Total)
                    .ToArrayAsync();

                Assert.Equal(2, actual.Length);
                Assert.Equal(15, actual.First().Total);
                Assert.Equal(21, actual.Last().Total);
            }
        }

        [ConditionalFact(Skip = "Issue #14935. Cannot eval 'orderby [mep].UnitPrice desc'")]
        public virtual async Task From_sql_queryable_stored_procedure_take()
        {
            using (var context = CreateContext())
            {
                var actual = await context
                    .Set<MostExpensiveProduct>()
                    .FromSqlRaw(TenMostExpensiveProductsSproc, GetTenMostExpensiveProductsParameters())
                    .OrderByDescending(mep => mep.UnitPrice)
                    .Take(2)
                    .ToArrayAsync();

                Assert.Equal(2, actual.Length);
                Assert.Equal(263.50m, actual.First().UnitPrice);
                Assert.Equal(123.79m, actual.Last().UnitPrice);
            }
        }

        [ConditionalFact(Skip = "Issue #14935. Cannot eval 'Min()'")]
        public virtual async Task From_sql_queryable_stored_procedure_min()
        {
            using (var context = CreateContext())
            {
                Assert.Equal(
                    45.60m,
                    await context.Set<MostExpensiveProduct>()
                        .FromSqlRaw(TenMostExpensiveProductsSproc, GetTenMostExpensiveProductsParameters())
                        .MinAsync(mep => mep.UnitPrice));
            }
        }

        [ConditionalFact(Skip = "issue #15312")]
        public virtual async Task From_sql_queryable_stored_procedure_with_include_throws()
        {
            using (var context = CreateContext())
            {
                Assert.Equal(
                    RelationalStrings.StoredProcedureIncludeNotSupported,
                    (await Assert.ThrowsAsync<InvalidOperationException>(
                        async () =>
                            await context.Set<Product>()
                                .FromSqlRaw("SelectStoredProcedure", GetTenMostExpensiveProductsParameters())
                                .Include(p => p.OrderDetails)
                                .ToArrayAsync()
                    )).Message);
            }
        }

        protected virtual object[] GetTenMostExpensiveProductsParameters()
            => Array.Empty<object>();

        protected virtual object[] GetCustomerOrderHistorySprocParameters()
            => new[] { "ALFKI" };

        protected NorthwindContext CreateContext() => Fixture.CreateContext();

        protected abstract string TenMostExpensiveProductsSproc { get; }

        protected abstract string CustomerOrderHistorySproc { get; }
    }
}

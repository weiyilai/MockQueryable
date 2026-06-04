using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using MockQueryable.Core;
using MockQueryable.EntityFrameworkCore;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MockQueryable.Sample;

[TestFixture]
public class TestQueryProviderEfCoreTests
{
    [Test]
    public void CreateQuery_WithProjectionExpression_ReturnsProjectedQueryable()
    {
        var users = TestDataHelper.CreateUserList();
        var provider = CreateProvider(users);
        var expression = users.AsQueryable().Select(x => x.FirstName).Expression;

        var query = provider.CreateQuery(expression);
        var result = query.Cast<string>().ToList();

        Assert.That(query, Is.AssignableTo<IQueryable<string>>());
        Assert.That(result, Is.EqualTo(users.Select(x => x.FirstName).ToList()));
    }

    [Test]
    public void CreateQuery_WithConstantExpression_ReturnsQueryableForOriginalType()
    {
        var users = TestDataHelper.CreateUserList();
        var provider = CreateProvider(users);
        var expression = users.AsQueryable().Expression;

        var query = provider.CreateQuery(expression);
        var result = query.Cast<UserEntity>().ToList();

        Assert.That(query, Is.AssignableTo<IQueryable<UserEntity>>());
        Assert.That(result.Select(x => x.Id), Is.EqualTo(users.Select(x => x.Id)));
    }

    [Test]
    public void Execute_WithNonGenericOverload_ReturnsFirstEntity()
    {
        var users = TestDataHelper.CreateUserList();
        var provider = CreateProvider(users);
        var expression = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.FirstOrDefault),
            [typeof(UserEntity)],
            users.AsQueryable().Expression);

        var result = provider.Execute(expression);

        Assert.That(result, Is.TypeOf<UserEntity>());
        Assert.That(((UserEntity)result).Id, Is.EqualTo(users[0].Id));
    }

    [Test]
    public async Task ExecuteAsync_ReturnsTaskWithExecutedResult()
    {
        var users = TestDataHelper.CreateUserList();
        var provider = CreateProvider(users);
        var expression = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.FirstOrDefault),
            [typeof(UserEntity)],
            users.AsQueryable().Expression);

        var result = await ((IAsyncQueryProvider)provider).ExecuteAsync<Task<UserEntity>>(expression);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(users[0].Id));
    }

    [Test]
    public void NonGenericEnumerator_EnumeratesSourceItems()
    {
        var users = TestDataHelper.CreateUserList();
        var provider = CreateProvider(users);

        var result = ((IEnumerable)provider).Cast<UserEntity>().ToList();

        Assert.That(result.Select(x => x.Id), Is.EqualTo(users.Select(x => x.Id)));
    }

    [Test]
    public async Task BuildMock_ReturnsAsyncQueryableThatEnumeratesCollection()
    {
        var users = TestDataHelper.CreateUserList();

        var query = users.BuildMock();
        var result = await query.ToListAsync();

        Assert.That(result.Select(x => x.Id), Is.EqualTo(users.Select(x => x.Id)));
    }

    [Test]
    public async Task BuildMock_WithCustomExpressionVisitor_SupportsLikeQueries()
    {
        var users = TestDataHelper.CreateUserList();

        var query = users.BuildMock<UserEntity, SampleLikeExpressionVisitor>();
        var result = await query
            .Where(x => EF.Functions.Like(x.FirstName, "%naME3%"))
            .ToListAsync();

        Assert.That(result.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task BuildMock_ExecuteDeleteAsync_RemovesAffectedItemsFromSourceCollection()
    {
        var userId = Guid.NewGuid();
        var users = TestDataHelper.CreateUserList(userId);

        var query = users.BuildMock();
        var affected = await query.Where(x => x.Id == userId).ExecuteDeleteAsync();

        Assert.That(affected, Is.EqualTo(1));
        Assert.That(users.Any(x => x.Id == userId), Is.False);
    }

    private static TestAsyncEnumerableEfCore<UserEntity, TestExpressionVisitor> CreateProvider(List<UserEntity> users)
    {
        return new TestAsyncEnumerableEfCore<UserEntity, TestExpressionVisitor>(users, _ => { });
    }
}

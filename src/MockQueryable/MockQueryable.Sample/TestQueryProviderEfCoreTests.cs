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
using System.Reflection;
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

    [Test]
    public async Task BuildMock_ExecuteUpdateAsync_UpdatesMatchingEntitiesInSourceCollection()
    {
        var userId = Guid.NewGuid();
        var users = TestDataHelper.CreateUserList(userId);
        const string expectedName = "Updated Name";

        var query = users.BuildMock();
        var affected = await query
            .Where(x => x.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.FirstName, expectedName)
                .SetProperty(x => x.LastName, expectedName));

        var updatedUser = users.Single(x => x.Id == userId);
        Assert.That(affected, Is.EqualTo(1));
        Assert.That(updatedUser.FirstName, Is.EqualTo(expectedName));
        Assert.That(updatedUser.LastName, Is.EqualTo(expectedName));
    }

    [Test]
    public void ApplyUpdateChangesToDbSet_WhenSetterArgumentIsNotNewArray_DoesNothing()
    {
        var users = TestDataHelper.CreateUserList();
        var originalUsers = users
            .Select(x => new { x.Id, x.FirstName, x.LastName, x.DateOfBirth })
            .ToList();
        var methodCall = Expression.Call(
            typeof(TestQueryProviderEfCoreTests),
            nameof(FakeExecuteUpdate),
            [typeof(UserEntity)],
            users.AsQueryable().Expression,
            Expression.Constant("not-an-update-array"));

        InvokeApplyUpdateChangesToDbSet(users, methodCall);

        Assert.That(
            users.Select(x => new { x.Id, x.FirstName, x.LastName, x.DateOfBirth }),
            Is.EqualTo(originalUsers));
    }

    [Test]
    public void ExtractValue_WhenExpressionIsQuotedLambdaWithNoParameters_ReturnsComputedValue()
    {
        var expected = new DateTime(2042, 10, 14);
        Expression expression = Expression.Quote(Expression.Lambda(Expression.Constant(expected)));

        var result = InvokeExtractValue(expression, TestDataHelper.CreateUserList()[0]);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ExtractValue_WhenExpressionIsQuotedLambdaWithOneParameter_UsesItemValue()
    {
        var user = TestDataHelper.CreateUserList()[0];
        var parameter = Expression.Parameter(typeof(UserEntity), "x");
        Expression expression = Expression.Quote(
            Expression.Lambda(
                Expression.Property(parameter, nameof(UserEntity.FirstName)),
                parameter));

        var result = InvokeExtractValue(expression, user);

        Assert.That(result, Is.EqualTo(user.FirstName));
    }

    [Test]
    public void ExtractValue_WhenLambdaHasMoreThanOneParameter_ThrowsInvalidOperationException()
    {
        var first = Expression.Parameter(typeof(UserEntity), "first");
        var second = Expression.Parameter(typeof(UserEntity), "second");
        Expression expression = Expression.Lambda(
            Expression.Constant("invalid"),
            first,
            second);

        var exception = Assert.Throws<TargetInvocationException>(() =>
            InvokeExtractValue(expression, TestDataHelper.CreateUserList()[0]));

        Assert.That(exception?.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(
            exception?.InnerException?.Message,
            Is.EqualTo("Supported only lambdas with 0 or 1 params."));
    }

    [Test]
    public void ExtractValue_WhenExpressionIsNotConstantOrLambda_CompilesParameterlessExpression()
    {
        Expression expression = Expression.Property(
            Expression.Constant(new DateTime(2030, 5, 7)),
            nameof(DateTime.Year));

        var result = InvokeExtractValue(expression, TestDataHelper.CreateUserList()[0]);

        Assert.That(result, Is.EqualTo(2030));
    }

    private static TestAsyncEnumerableEfCore<UserEntity, TestExpressionVisitor> CreateProvider(List<UserEntity> users)
    {
        return new TestAsyncEnumerableEfCore<UserEntity, TestExpressionVisitor>(users, _ => { });
    }

    private static void InvokeApplyUpdateChangesToDbSet(
        IEnumerable<UserEntity> users,
        MethodCallExpression methodCallExpression)
    {
        var method = typeof(TestAsyncEnumerableEfCore<UserEntity, TestExpressionVisitor>)
            .GetMethod("ApplyUpdateChangesToDbSet", BindingFlags.NonPublic | BindingFlags.Static);

        method!.Invoke(null, [users, methodCallExpression]);
    }

    private static object InvokeExtractValue(Expression expression, UserEntity item)
    {
        var method = typeof(TestAsyncEnumerableEfCore<UserEntity, TestExpressionVisitor>)
            .GetMethod("ExtractValue", BindingFlags.NonPublic | BindingFlags.Static);

        return method!.Invoke(null, [expression, item]);
    }

    private static int FakeExecuteUpdate<TEntity>(IQueryable<TEntity> source, object setters)
    {
        return 0;
    }
}

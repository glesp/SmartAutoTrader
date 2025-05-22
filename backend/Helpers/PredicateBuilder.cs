/* <copyright file="PredicateBuilder.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the PredicateBuilder class, which provides utility methods for dynamically composing LINQ query predicates in the Smart Auto Trader application.
</summary>
<remarks>
The PredicateBuilder class is a static helper class designed to simplify the creation and combination of LINQ expressions. It includes methods for generating predicates that always evaluate to true or false, as well as methods for combining predicates using logical "and" and "or" operations. This class is typically used in scenarios where dynamic query construction is required, such as filtering data based on user input.
</remarks>
<dependencies>
- System
- System.Linq
- System.Linq.Expressions
</dependencies>
 */

namespace SmartAutoTrader.API.Helpers
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// Enables the efficient, dynamic composition of query predicates.
    /// </summary>
    /// <remarks>
    /// This static class provides methods for creating and combining LINQ expressions, allowing for flexible and reusable query logic.
    /// </remarks>
    public static class PredicateBuilder
    {
        /// <summary>
        /// Creates a predicate that always evaluates to true.
        /// </summary>
        /// <typeparam name="T">The type of the object being evaluated by the predicate.</typeparam>
        /// <returns>An expression that always evaluates to true.</returns>
        /// <example>
        /// <code>
        /// var predicate = PredicateBuilder.True<MyEntity>();
        /// var results = myEntities.Where(predicate);
        /// </code>
        /// </example>
        public static Expression<Func<T, bool>> True<T>()
        {
            return f => true;
        }

        /// <summary>
        /// Creates a predicate that always evaluates to false.
        /// </summary>
        /// <typeparam name="T">The type of the object being evaluated by the predicate.</typeparam>
        /// <returns>An expression that always evaluates to false.</returns>
        /// <example>
        /// <code>
        /// var predicate = PredicateBuilder.False<MyEntity>();
        /// var results = myEntities.Where(predicate);
        /// </code>
        /// </example>
        public static Expression<Func<T, bool>> False<T>()
        {
            return f => false;
        }

        /// <summary>
        /// Combines the first predicate with the second using the logical "or" operation.
        /// </summary>
        /// <typeparam name="T">The type of the object being evaluated by the predicates.</typeparam>
        /// <param name="expr1">The first predicate.</param>
        /// <param name="expr2">The second predicate.</param>
        /// <returns>A new predicate that evaluates to true if either of the input predicates evaluates to true.</returns>
        /// <remarks>
        /// This method uses the <see cref="Expression.OrElse"/> method to combine the predicates.
        /// </remarks>
        /// <example>
        /// <code>
        /// var predicate1 = PredicateBuilder.True<MyEntity>();
        /// var predicate2 = PredicateBuilder.False<MyEntity>();
        /// var combinedPredicate = predicate1.Or(predicate2);
        /// var results = myEntities.Where(combinedPredicate);
        /// </code>
        /// </example>
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
        }

        /// <summary>
        /// Combines the first predicate with the second using the logical "and" operation.
        /// </summary>
        /// <typeparam name="T">The type of the object being evaluated by the predicates.</typeparam>
        /// <param name="expr1">The first predicate.</param>
        /// <param name="expr2">The second predicate.</param>
        /// <returns>A new predicate that evaluates to true only if both of the input predicates evaluate to true.</returns>
        /// <remarks>
        /// This method uses the <see cref="Expression.AndAlso"/> method to combine the predicates.
        /// </remarks>
        /// <example>
        /// <code>
        /// var predicate1 = PredicateBuilder.True<MyEntity>();
        /// var predicate2 = PredicateBuilder.False<MyEntity>();
        /// var combinedPredicate = predicate1.And(predicate2);
        /// var results = myEntities.Where(combinedPredicate);
        /// </code>
        /// </example>
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
        }
    }
}
using System.Linq.Expressions;

namespace MovieAnalyticsWeb.Data
{
    public class OrderBy<TSource, TKey> : IOrderBy
    {
        private readonly Expression<Func<TSource, TKey>> expression;

        public OrderBy(Expression<Func<TSource, TKey>> expression)
        {
            this.expression = expression;
        }

        public dynamic Expression => this.expression;
    }
}

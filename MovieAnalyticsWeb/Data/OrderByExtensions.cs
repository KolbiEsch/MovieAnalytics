namespace MovieAnalyticsWeb.Data
{
    public static class OrderByExtensions
    {
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, IOrderBy orderBy, bool desc)
        {
            var ordered = desc ? Queryable.OrderByDescending(source, orderBy.Expression) :
                Queryable.OrderBy(source, orderBy.Expression);
            return ordered;
        }
    }
}

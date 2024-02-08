using System.Linq.Expressions;

namespace MovieAnalyticsWeb.Data
{
    public interface IOrderBy
    {
        dynamic Expression { get; }
    }
}
